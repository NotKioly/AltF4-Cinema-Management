using AltF4.Cinema.Core;
using Microsoft.EntityFrameworkCore;

namespace AltF4.Cinema.WinForms;

public sealed class SaleForm : BaseForm
{
    readonly ComboBox shows=new(){Width=650,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
    readonly ComboBox customers=new(){Width=280,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
    readonly ComboBox products=new(){Width=290,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
    readonly FlowLayoutPanel seats=new(){Dock=DockStyle.Fill,AutoScroll=true,Padding=new(20)};
    readonly DataGridView basket=Grid();readonly NumericUpDown quantity=new(){Minimum=1,Maximum=100,Value=1,Width=70};
    readonly NumericUpDown cash=new(){Maximum=1000000000,Increment=50000,ThousandsSeparator=true,Width=180};
    readonly TextBox promo=new(){Width=140,PlaceholderText="Mã khuyến mãi"};readonly Label total=new(){AutoSize=true,Font=new("Segoe UI",14,FontStyle.Bold)};
    readonly HashSet<int> selected=[];readonly List<ProductOrder> order=[];List<Seat> seatData=[];List<Product> productData=[];long basePrice;
    public SaleForm():base("Bán vé và bắp nước",1250,850)
    {
        var top=Bar();top.Controls.Add(Label("Suất chiếu"));top.Controls.Add(shows);top.Controls.Add(Button("Tải lại ghế",()=>Run(LoadSeats)));
        var right=new Panel{Dock=DockStyle.Right,Width=440,Padding=new(12)};
        var panel=new FlowLayoutPanel{Dock=DockStyle.Top,Height=400,FlowDirection=FlowDirection.TopDown,WrapContents=false};
        panel.Controls.Add(Label("Khách hàng • VIP giảm 10%"));panel.Controls.Add(customers);panel.Controls.Add(Label("Bắp/nước"));var productBar=new FlowLayoutPanel{Width=420,Height=42};productBar.Controls.Add(products);productBar.Controls.Add(quantity);panel.Controls.Add(productBar);
        panel.Controls.Add(Button("Thêm bắp/nước",()=>Run(()=>{var id=(int)(products.SelectedValue??0);if(id==0)throw new InvalidOperationException("Chọn sản phẩm.");order.Add(new(id,(int)quantity.Value));RefreshBasket();}),220));
        panel.Controls.Add(promo);panel.Controls.Add(Label("Tiền khách đưa (đ)"));panel.Controls.Add(cash);panel.Controls.Add(total);
        var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=100};bottom.Controls.Add(Button("Xóa bắp/nước",()=>{order.Clear();RefreshBasket();},180));bottom.Controls.Add(Button("THANH TOÁN",()=>Run(Checkout),210));bottom.Controls.Add(Label("Xanh: trống • Đỏ: đã bán • Vàng: đang chọn"));
        right.Controls.Add(basket);right.Controls.Add(panel);right.Controls.Add(bottom);
        Controls.Add(seats);Controls.Add(right);Controls.Add(top);
        shows.SelectedIndexChanged+=(_,_)=>Run(LoadSeats);customers.SelectedIndexChanged+=(_,_)=>RefreshBasket();promo.TextChanged+=(_,_)=>RefreshBasket();
        Shown+=(_,_)=>Run(LoadData);
    }
    void LoadData(){using var db=App.Db();SecurityService.Require(db,App.Actor,Access.Sell);
        var now=DateTime.Now;var choices=db.Showtimes.Include(s=>s.Movie).Include(s=>s.Room).Where(s=>s.Active&&s.StartsAt>now&&s.Movie.Active&&s.Room.Active).OrderBy(s=>s.StartsAt).ToList().Select(s=>new Choice(s.Id,$"{s.StartsAt:dd/MM HH:mm} | {s.Movie.Title} | {s.Room.Name} | {s.BasePrice:N0}đ")).ToList();choices.Insert(0,new(0,"Chỉ bán bắp/nước • không chọn suất"));shows.DataSource=choices;
        var c=db.Customers.OrderBy(c=>c.Name).Select(c=>new Choice(c.Id,c.Name+(c.Vip?" (VIP)":""))).ToList();c.Insert(0,new(0,"Khách vãng lai"));customers.DataSource=c;
        productData=db.Products.Where(p=>p.Active).ToList();products.DataSource=productData.Select(p=>new Choice(p.Id,$"{p.Name} • {p.Price:N0}đ • tồn {p.Stock}")).ToList();LoadSeats();
    }
    internal void DemoSelect(){if(shows.Items.Count>1)shows.SelectedIndex=1;if(customers.Items.Count>1)customers.SelectedIndex=1;foreach(var b in seats.Controls.OfType<Button>().Where(b=>b.Enabled).Take(2).ToArray())b.PerformClick();if(productData.Count>0)order.Add(new(productData[0].Id,1));promo.Text="ALTF4";cash.Value=500000;RefreshBasket();}
    void LoadSeats(){selected.Clear();seats.Controls.Clear();seatData=[];var id=shows.SelectedValue is int n?n:0;
        if(id==0){seats.Controls.Add(Label("Chọn suất chiếu để hiển thị sơ đồ ghế.\nBạn có thể bán riêng bắp/nước."));RefreshBasket();return;}
        using var db=App.Db();SecurityService.Require(db,App.Actor,Access.Sell);var show=db.Showtimes.Include(s=>s.Movie).Single(s=>s.Id==id);basePrice=show.BasePrice;
        var screen=new Label{Text=$"MÀN HÌNH • {show.Movie.Title} ({show.Movie.AgeRating})\nVui lòng kiểm tra độ tuổi khách trước khi bán vé.",Width=700,Height=65,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(220,228,242)};seats.Controls.Add(screen);seats.SetFlowBreak(screen,true);
        var sold=db.Reservations.Where(r=>r.ShowtimeId==id).Select(r=>r.SeatId).ToHashSet();seatData=db.Seats.Where(s=>s.RoomId==show.RoomId&&s.Active).OrderBy(s=>s.Code).ToList();
        for(int i=0;i<seatData.Count;i++){var seat=seatData[i];var b=Button($"{seat.Code}\n{basePrice+seat.Surcharge:N0}đ",()=>{},77);b.Height=60;b.Enabled=!sold.Contains(seat.Id);b.BackColor=b.Enabled?Color.SeaGreen:Color.IndianRed;
            b.Click+=(_,_)=>{if(!selected.Add(seat.Id))selected.Remove(seat.Id);b.BackColor=selected.Contains(seat.Id)?Color.DarkGoldenrod:Color.SeaGreen;RefreshBasket();};seats.Controls.Add(b);if((i+1)%8==0)seats.SetFlowBreak(b,true);}
        RefreshBasket();
    }
    void RefreshBasket(){var rows=selected.Select(id=>{var s=seatData.Single(x=>x.Id==id);return new{Mục="Ghế "+s.Code,Số_lượng=1,Thành_tiền=basePrice+s.Surcharge};}).Concat(order.GroupBy(o=>o.ProductId).Select(g=>new{Mục=productData.FirstOrDefault(p=>p.Id==g.Key)?.Name??"",Số_lượng=g.Sum(o=>o.Quantity),Thành_tiền=(productData.FirstOrDefault(p=>p.Id==g.Key)?.Price??0)*g.Sum(o=>o.Quantity)})).ToList();basket.DataSource=rows;
        using var db=App.Db();var percent=customers.SelectedValue is int id&&db.Customers.Any(c=>c.Id==id&&c.Vip)?10:0;var code=promo.Text.Trim().ToUpperInvariant();var now=DateTime.Now;var p=db.Promotions.FirstOrDefault(p=>p.Code==code&&p.Active&&p.From<=now&&p.To>now);if(p!=null)percent=Math.Max(percent,p.Percent);var subtotal=rows.Sum(x=>x.Thành_tiền);total.Text=$"Tạm tính: {subtotal:N0}đ\nGiảm {percent}% • Cần thu: {subtotal-subtotal*percent/100:N0}đ";
    }
    void Checkout(){if(MessageBox.Show(this,"Xác nhận thu tiền và phát hành vé?","Thanh toán",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        var customer=customers.SelectedValue is int c&&c>0?(int?)c:null;var show=shows.SelectedValue is int s&&s>0?(int?)s:null;
        var sale=App.Business.Checkout(App.Actor,new(show,selected.ToArray(),order.ToArray(),customer,promo.Text,(long)cash.Value));
        MessageBox.Show(this,$"Đã lưu {sale.Code}\nTổng tiền: {sale.Total:N0}đ\nTiền thừa: {sale.CashReceived-sale.Total:N0}đ","Thành công");
        order.Clear();promo.Clear();cash.Value=0;LoadData();ShowDialog(new InvoiceForm(sale.Id),this);
    }
}
