using Microsoft.EntityFrameworkCore;

namespace AltF4.Cinema.Core;

public sealed class BusinessService(Func<CinemaDb> factory)
{
    public Sale Checkout(Actor actor,SaleRequest request,Action? afterHeaderForTest=null)
    {
        using var db=factory();SecurityService.Require(db,actor,Access.Sell);using var tx=db.Database.BeginTransaction();
        if(request.SeatIds.Count==0&&request.Products.Count==0)throw new InvalidOperationException("Chưa chọn vé hoặc sản phẩm.");
        if(request.SeatIds.Count>20||request.SeatIds.Distinct().Count()!=request.SeatIds.Count)throw new InvalidOperationException("Tối đa 20 ghế, không được trùng ghế.");
        var sale=new Sale{Code=$"AF4-{DateTime.Now:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",CreatedAt=DateTime.Now,UserId=actor.UserId,CustomerId=request.CustomerId,CashReceived=request.CashReceived};
        Customer? customer=null;if(request.CustomerId.HasValue)customer=db.Customers.Find(request.CustomerId.Value)??throw new InvalidOperationException("Khách hàng không tồn tại.");
        if(request.SeatIds.Count>0) {
            var show=db.Showtimes.Include(x=>x.Movie).Include(x=>x.Room).SingleOrDefault(x=>x.Id==request.ShowtimeId)??throw new InvalidOperationException("Chọn suất chiếu.");
            if(!show.Active||!show.Movie.Active||!show.Room.Active||show.StartsAt<=DateTime.Now)throw new InvalidOperationException("Suất chiếu không còn mở bán.");
            foreach(var seatId in request.SeatIds) {
                var seat=db.Seats.SingleOrDefault(s=>s.Id==seatId&&s.RoomId==show.RoomId&&s.Active)??throw new InvalidOperationException("Ghế không thuộc phòng hoặc đã ngừng sử dụng.");
                if(db.Reservations.Any(r=>r.ShowtimeId==show.Id&&r.SeatId==seatId))throw new InvalidOperationException($"Ghế {seat.Code} đã được bán. Hãy tải lại sơ đồ.");
                sale.Lines.Add(new(){Description=$"{show.Movie.Title} | {show.StartsAt:dd/MM HH:mm} | {show.Room.Name} | Ghế {seat.Code}",Quantity=1,UnitPrice=show.BasePrice+seat.Surcharge,ShowtimeId=show.Id,SeatId=seatId});
            }
        }
        foreach(var group in request.Products.GroupBy(x=>x.ProductId)) {
            if(group.Any(x=>x.Quantity<=0||x.Quantity>100))throw new InvalidOperationException("Số lượng mỗi sản phẩm từ 1 đến 100.");
            var count=group.Sum(x=>x.Quantity);if(count>100)throw new InvalidOperationException("Tối đa 100 đơn vị mỗi sản phẩm.");
            var product=db.Products.SingleOrDefault(p=>p.Id==group.Key&&p.Active)??throw new InvalidOperationException("Sản phẩm không còn bán.");
            if(product.Stock<count)throw new InvalidOperationException($"{product.Name}: chỉ còn {product.Stock}.");
            product.Stock-=count;sale.Lines.Add(new(){Description=product.Name,ProductId=product.Id,Quantity=count,UnitPrice=product.Price});
        }
        sale.Subtotal=sale.Lines.Sum(l=>l.Quantity*l.UnitPrice);sale.DiscountPercent=customer?.Vip==true?10:0;
        if(!string.IsNullOrWhiteSpace(request.PromotionCode)) {
            var code=request.PromotionCode.Trim().ToUpperInvariant();var now=DateTime.Now;
            var promo=db.Promotions.SingleOrDefault(p=>p.Code==code&&p.Active&&p.From<=now&&p.To>now)??throw new InvalidOperationException("Mã khuyến mãi không hợp lệ hoặc đã hết hạn.");
            sale.DiscountPercent=Math.Max(sale.DiscountPercent,promo.Percent);
        }
        sale.Discount=sale.Subtotal*sale.DiscountPercent/100;sale.Total=sale.Subtotal-sale.Discount;
        if(request.CashReceived<sale.Total||request.CashReceived>1000000000)throw new InvalidOperationException("Tiền nhận chưa đủ hoặc vượt giới hạn 1 tỷ đồng.");
        if(customer!=null){sale.PointsEarned=sale.Total/10000;customer.Points+=sale.PointsEarned;}
        db.Sales.Add(sale);db.SaveChanges();
        afterHeaderForTest?.Invoke(); // Điểm chèn lỗi tích hợp để chứng minh rollback sau khi đã ghi hóa đơn.
        foreach(var line in sale.Lines.Where(l=>l.SeatId.HasValue))db.Reservations.Add(new(){ShowtimeId=line.ShowtimeId!.Value,SeatId=line.SeatId!.Value,SaleId=sale.Id});
        db.AuditEntries.Add(new(){At=DateTime.Now,UserId=actor.UserId,Action="CHECKOUT",Detail=$"{sale.Code}: {sale.Total:N0} VND"});
        try{db.SaveChanges();tx.Commit();return sale;}catch(DbUpdateException ex){throw new InvalidOperationException("Ghế vừa được bán bởi phiên khác hoặc dữ liệu thay đổi. Hãy tải lại.",ex);}
    }
    public void Refund(Actor actor,int saleId,string reason)
    {
        using var db=factory();SecurityService.Require(db,actor,Access.Refund);using var tx=db.Database.BeginTransaction();
        if(reason.Trim().Length<5||reason.Length>300)throw new InvalidOperationException("Lý do hoàn cần 5–300 ký tự.");
        var sale=db.Sales.Include(s=>s.Lines).ThenInclude(l=>l.Showtime).Include(s=>s.Customer).SingleOrDefault(s=>s.Id==saleId)??throw new InvalidOperationException("Không tìm thấy hóa đơn.");
        if(sale.Status!="Paid")throw new InvalidOperationException("Hóa đơn đã hoàn trước đó.");
        if(sale.Lines.Any(l=>l.Showtime!=null&&l.Showtime.StartsAt<=DateTime.Now))throw new InvalidOperationException("Không hoàn hóa đơn có suất chiếu đã bắt đầu.");
        if(!sale.Lines.Any(l=>l.ShowtimeId.HasValue)&&DateTime.Now-sale.CreatedAt>TimeSpan.FromMinutes(30))throw new InvalidOperationException("Hóa đơn chỉ có bắp/nước chỉ hoàn trong 30 phút.");
        foreach(var l in sale.Lines.Where(l=>l.ProductId.HasValue)) {var p=db.Products.Find(l.ProductId)!;p.Stock+=l.Quantity;}
        if(sale.Customer!=null)sale.Customer.Points=Math.Max(0,sale.Customer.Points-sale.PointsEarned);
        db.Reservations.RemoveRange(db.Reservations.Where(r=>r.SaleId==sale.Id));
        sale.Status="Refunded";sale.RefundedAt=DateTime.Now;sale.RefundReason=reason.Trim();
        db.AuditEntries.Add(new(){At=DateTime.Now,UserId=actor.UserId,Action="REFUND",Detail=$"{sale.Code}: {reason.Trim()}"});db.SaveChanges();tx.Commit();
    }
    public void Receive(Actor a,int productId,int supplierId,int quantity,long unitCost)
    {
        using var db=factory();SecurityService.Require(db,a,Access.Inventory);using var tx=db.Database.BeginTransaction();
        if(quantity<1||quantity>100000||unitCost<0||unitCost>100000000)throw new InvalidOperationException("Số lượng 1–100.000; đơn giá 0–100 triệu.");
        var p=db.Products.Find(productId)??throw new InvalidOperationException("Chọn sản phẩm.");if(!db.Suppliers.Any(x=>x.Id==supplierId))throw new InvalidOperationException("Chọn nhà cung cấp.");
        if(p.Stock>1000000-quantity)throw new InvalidOperationException("Tồn kho vượt giới hạn.");
        p.Stock+=quantity;db.InventoryReceipts.Add(new(){ProductId=productId,SupplierId=supplierId,Quantity=quantity,UnitCost=unitCost,CreatedAt=DateTime.Now,UserId=a.UserId});
        db.AuditEntries.Add(new(){At=DateTime.Now,UserId=a.UserId,Action="RECEIVE",Detail=$"Sản phẩm #{productId}, +{quantity}"});db.SaveChanges();tx.Commit();
    }
    public List<ReportRow> Revenue(Actor a,DateTime startDate,DateTime toExclusive)
    {
        using var db=factory();SecurityService.Require(db,a,Access.Reports);if(startDate>=toExclusive)throw new InvalidOperationException("Khoảng ngày không hợp lệ.");
        // Doanh thu thuần theo dòng tiền: thu trong kỳ trừ tiền hoàn trong kỳ.
        var sales=db.Sales.Where(s=>s.CreatedAt>=startDate&&s.CreatedAt<toExclusive).Select(s=>new{s.CreatedAt,s.Total}).ToList();
        var refunds=db.Sales.Where(s=>s.RefundedAt>=startDate&&s.RefundedAt<toExclusive).Select(s=>new{s.RefundedAt,s.Total}).ToList();
        var rows=sales.Select(s=>new{Day=s.CreatedAt.Date,Amount=s.Total,Count=1}).Concat(refunds.Select(s=>new{Day=s.RefundedAt!.Value.Date,Amount=-s.Total,Count=-1}));
        return rows.GroupBy(x=>x.Day).OrderBy(g=>g.Key).Select(g=>new ReportRow(g.Key.ToString("dd/MM/yyyy"),g.Sum(x=>x.Amount),g.Sum(x=>x.Count))).ToList();
    }
    public List<ReportRow> MovieRevenue(Actor a,DateTime startDate,DateTime toExclusive)
    {
        using var db=factory();SecurityService.Require(db,a,Access.Reports);
        var lines=(from l in db.SaleLines join s in db.Sales on l.SaleId equals s.Id join sh in db.Showtimes on l.ShowtimeId equals sh.Id join m in db.Movies on sh.MovieId equals m.Id where s.CreatedAt>=startDate&&s.CreatedAt<toExclusive&&s.Status=="Paid" select new{m.Title,l.Quantity,l.UnitPrice,s.DiscountPercent}).ToList();
        return lines.GroupBy(l=>l.Title).Select(g=>new ReportRow(g.Key,g.Sum(l=>l.Quantity*l.UnitPrice*(100-l.DiscountPercent)/100),g.Sum(l=>l.Quantity))).ToList();
    }
}

