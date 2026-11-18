using Microsoft.EntityFrameworkCore;

namespace AltF4.Cinema.Core;

/// <summary>Repository danh mục: EF Core, kiểm tra quyền và ràng buộc trước khi ghi.</summary>
public sealed class CatalogService(Func<CinemaDb> factory)
{
    public static Access Permission(Type t) => t==typeof(User)?Access.Users:t==typeof(Role)?Access.Roles:t==typeof(Showtime)?Access.Schedule:Access.Catalog;
    public List<T> List<T>(Actor a) where T:Entity
    {
        using var db=factory();SecurityService.Require(db,a,Permission(typeof(T)));return db.Set<T>().AsNoTracking().ToList();
    }
    public void Save<T>(Actor a,T entity,string? newPassword=null) where T:Entity
    {
        using var db=factory();SecurityService.Require(db,a,Permission(typeof(T)));using var tx=db.Database.BeginTransaction();
        var old=entity.Id==0?null:db.Set<T>().Find(entity.Id);
        if(entity.Id!=0&&old==null) throw new InvalidOperationException("Dữ liệu đã bị xóa. Hãy tải lại.");
        Validate(db,entity,old,a,newPassword);
        if(old==null) db.Add(entity);else db.Entry(old).CurrentValues.SetValues(entity);
        db.AuditEntries.Add(new(){At=DateTime.Now,UserId=a.UserId,Action=old==null?"CREATE":"UPDATE",Detail=$"{typeof(T).Name} #{entity.Id}"});
        try {db.SaveChanges();tx.Commit();}catch(DbUpdateException ex){throw new InvalidOperationException("Không lưu được: dữ liệu trùng hoặc liên kết không hợp lệ.",ex);}
    }
    public void Delete<T>(Actor a,int id) where T:Entity
    {
        using var db=factory();SecurityService.Require(db,a,Permission(typeof(T)));using var tx=db.Database.BeginTransaction();
        var entity=db.Set<T>().Find(id)??throw new InvalidOperationException("Không tìm thấy dữ liệu.");
        if(entity is User u) { if(u.Id==a.UserId) throw new InvalidOperationException("Không thể xóa tài khoản đang sử dụng.");ProtectAdmin(db,u,null); }
        if(entity is Role r && db.Users.Any(x=>x.RoleId==r.Id)) throw new InvalidOperationException("Nhóm quyền đang có người dùng.");
        if(entity is Product p && p.Stock!=0) throw new InvalidOperationException("Chỉ xóa sản phẩm khi tồn kho bằng 0; có thể ngừng bán thay thế.");
        if(entity is Customer c && c.Points!=0) throw new InvalidOperationException("Khách còn điểm tích lũy, không thể xóa.");
        db.Remove(entity);db.AuditEntries.Add(new(){At=DateTime.Now,UserId=a.UserId,Action="DELETE",Detail=$"{typeof(T).Name} #{id}"});
        try{db.SaveChanges();tx.Commit();}catch(DbUpdateException ex){throw new InvalidOperationException("Dữ liệu đang được sử dụng; hãy ngừng hoạt động thay cho xóa.",ex);}
    }
    private static void Text(string s,string label,int max=150) {if(string.IsNullOrWhiteSpace(s)||s.Length>max)throw new InvalidOperationException($"{label} không được trống và tối đa {max} ký tự.");}
    private static void Money(long n) {if(n<0||n>100000000)throw new InvalidOperationException("Số tiền phải từ 0 đến 100.000.000 đồng.");}
    private static void ProtectAdmin(CinemaDb db,User old,User? next)
    {
        var oldRole=db.Roles.Find(old.RoleId)!;
        if(old.Active&&oldRole.Permissions==Access.All && (next==null||!next.Active||db.Roles.Find(next.RoleId)?.Permissions!=Access.All))
            if(!db.Users.Include(x=>x.Role).Any(x=>x.Id!=old.Id&&x.Active&&x.Role.Permissions==Access.All))throw new InvalidOperationException("Phải giữ ít nhất một quản trị viên đang hoạt động.");
    }
    private static void Validate(CinemaDb db,Entity e,Entity? old,Actor a,string? password)
    {
        switch(e) {
            case Genre x: x.Name=x.Name.Trim();Text(x.Name,"Thể loại");break;
            case Room x: x.Name=x.Name.Trim();Text(x.Name,"Phòng"); if(old!=null && !x.Active && db.Showtimes.Any(s=>s.RoomId==x.Id&&s.Active&&s.EndsAt>DateTime.Now))throw new InvalidOperationException("Phòng còn lịch chiếu chưa kết thúc.");break;
            case Movie x: x.Title=x.Title.Trim();Text(x.Title,"Tên phim");if(x.DurationMinutes<10||x.DurationMinutes>360||!new[]{"P","K","T13","T16","T18"}.Contains(x.AgeRating))throw new InvalidOperationException("Thời lượng 10–360 phút; phân loại P, K, T13, T16 hoặc T18.");
                if(!db.Genres.Any(g=>g.Id==x.GenreId))throw new InvalidOperationException("Chọn thể loại hợp lệ.");
                if(old is Movie om && (om.DurationMinutes!=x.DurationMinutes||!x.Active)&&db.Showtimes.Any(s=>s.MovieId==x.Id&&s.Active&&s.EndsAt>DateTime.Now))throw new InvalidOperationException("Phim còn lịch chiếu; không đổi thời lượng/ngừng chiếu.");break;
            case Seat x: x.Code=x.Code.Trim().ToUpperInvariant();Text(x.Code,"Mã ghế",8);Money(x.Surcharge);if(!db.Rooms.Any(r=>r.Id==x.RoomId))throw new InvalidOperationException("Chọn phòng.");
                if(old is Seat os && (os.RoomId!=x.RoomId||os.Code!=x.Code||os.Active!=x.Active||os.Surcharge!=x.Surcharge)&&db.Showtimes.Any(s=>s.RoomId==os.RoomId&&s.Active&&s.EndsAt>DateTime.Now))throw new InvalidOperationException("Phòng còn lịch chiếu; không thay đổi ghế.");break;
            case Showtime x:
                var m=db.Movies.Find(x.MovieId);if(m==null||!m.Active||!db.Rooms.Any(r=>r.Id==x.RoomId&&r.Active))throw new InvalidOperationException("Phim hoặc phòng không hoạt động.");
                if(x.StartsAt<=DateTime.Now)throw new InvalidOperationException("Suất chiếu phải bắt đầu trong tương lai.");
                x.EndsAt=x.StartsAt.AddMinutes(m.DurationMinutes);Money(x.BasePrice);if(x.BasePrice==0)throw new InvalidOperationException("Giá vé phải lớn hơn 0.");
                if(db.SaleLines.Any(l=>l.ShowtimeId==x.Id))throw new InvalidOperationException("Suất đã phát sinh vé, không thể sửa.");
                var cleanupStart=x.StartsAt.AddMinutes(-15);var cleanupEnd=x.EndsAt.AddMinutes(15);
                if(x.Active&&db.Showtimes.Any(s=>s.Id!=x.Id&&s.Active&&s.RoomId==x.RoomId&&s.StartsAt<cleanupEnd&&s.EndsAt>cleanupStart))throw new InvalidOperationException("Trùng lịch phòng hoặc chưa đủ 15 phút dọn phòng.");break;
            case Customer x: x.Name=x.Name.Trim();Text(x.Name,"Tên khách");if(!System.Text.RegularExpressions.Regex.IsMatch(x.Phone,@"^\d{10,11}$"))throw new InvalidOperationException("Điện thoại cần 10–11 chữ số.");x.Points=(old as Customer)?.Points??0;break;
            case Product x: x.Name=x.Name.Trim();Text(x.Name,"Sản phẩm");Money(x.Price);if(x.Price==0)throw new InvalidOperationException("Giá bán phải lớn hơn 0.");x.Stock=(old as Product)?.Stock??0;break;
            case Promotion x: x.Code=x.Code.Trim().ToUpperInvariant();Text(x.Code,"Mã khuyến mãi",30);if(x.Percent<1||x.Percent>50||x.From>=x.To)throw new InvalidOperationException("Giảm giá 1–50%; thời điểm kết thúc phải sau bắt đầu.");break;
            case Supplier x: x.Name=x.Name.Trim();Text(x.Name,"Nhà cung cấp");Text(x.Phone,"Điện thoại",20);break;
            case Role x: x.Name=x.Name.Trim();Text(x.Name,"Tên nhóm");if((x.Permissions&~Access.All)!=0)throw new InvalidOperationException("Quyền không hợp lệ.");
                if(old is Role orole && orole.Permissions==Access.All && x.Permissions!=Access.All && db.Users.Any(u=>u.RoleId==x.Id&&u.Active) && !db.Users.Include(u=>u.Role).Any(u=>u.RoleId!=x.Id&&u.Active&&u.Role.Permissions==Access.All))throw new InvalidOperationException("Phải giữ ít nhất một nhóm quản trị đang được sử dụng.");break;
            case User x: x.Username=x.Username.Trim().ToLowerInvariant();Text(x.Username,"Tên đăng nhập",50);Text(x.FullName,"Họ tên");if(!db.Roles.Any(r=>r.Id==x.RoleId))throw new InvalidOperationException("Chọn nhóm quyền.");
                if(old is User ou) {ProtectAdmin(db,ou,x);if(x.Id==a.UserId&&(!x.Active||x.RoleId!=ou.RoleId))throw new InvalidOperationException("Không thể tự khóa hoặc đổi nhóm của chính mình.");x.PasswordHash=ou.PasswordHash;x.MustChangePassword=ou.MustChangePassword;x.FailedAttempts=ou.FailedAttempts;x.LockedUntil=ou.LockedUntil;}
                if(old==null&&string.IsNullOrEmpty(password))throw new InvalidOperationException("Nhập mật khẩu khởi tạo.");
                if(!string.IsNullOrEmpty(password)){x.PasswordHash=Passwords.Hash(password);x.MustChangePassword=true;x.FailedAttempts=0;x.LockedUntil=null;}break;
            default: throw new InvalidOperationException("Danh mục không hỗ trợ.");
        }
    }
}
