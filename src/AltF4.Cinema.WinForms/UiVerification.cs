using System.Text.Json;
using AltF4.Cinema.Core;

namespace AltF4.Cinema.WinForms;

/// <summary>Kiểm tra giao diện và ghi khung hình từ chính WinForms. Chỉ bật bằng --demo --verify-ui.</summary>
public sealed class UiVerification : Form
{
    readonly System.Windows.Forms.Timer timer=new(){Interval=200};readonly Queue<(string title,Func<Form> factory)> steps=[];
    Form? current;int tick;int frame;string title="";readonly string output=Path.Combine(App.Home,"ui-verification");
    readonly List<object> outcomes=[];
    public UiVerification()
    {
        if(!App.Demo)throw new InvalidOperationException("Kiểm tra tự động chỉ cho phép dữ liệu demo.");
        ShowInTaskbar=false;Opacity=0;Width=1;Height=1;
        Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));Directory.CreateDirectory(Path.Combine(output,"screenshots"));
        App.Actor=App.Security.Login("admin",Bootstrap.DemoPassword);
        // Tạo giao dịch thật trên CSDL demo để kiểm tra sơ đồ đã bán, hóa đơn và doanh thu.
        using var db=App.Db();var show=db.Showtimes.OrderBy(x=>x.StartsAt).First(x=>x.StartsAt>DateTime.Now);var available=db.Seats.Where(s=>s.RoomId==show.RoomId&&!db.Reservations.Any(r=>r.ShowtimeId==show.Id&&r.SeatId==s.Id)).Select(s=>s.Id).Take(2).ToArray();
        int saleId;
        if(available.Length==2)saleId=App.Business.Checkout(App.Actor,new(show.Id,available,[new(db.Products.First().Id,1)],db.Customers.First(c=>c.Vip).Id,"ALTF4",500000)).Id;
        else saleId=db.Sales.First().Id;
        steps.Enqueue(("01-dang-nhap",()=>new LoginForm()));steps.Enqueue(("02-tong-quan",()=>new MainForm()));
        steps.Enqueue(("03-ban-ve",()=>{var f=new SaleForm();f.Shown+=(_,_)=>f.DemoSelect();return f;}));steps.Enqueue(("04-hoa-don-rdlc",()=>new InvoiceForm(saleId)));
        steps.Enqueue(("05-tra-cuu-hoa-don",()=>new SalesForm()));steps.Enqueue(("06-phim",()=>new MoviesForm()));steps.Enqueue(("07-the-loai",()=>new GenresForm()));steps.Enqueue(("08-phong",()=>new RoomsForm()));steps.Enqueue(("09-ghe",()=>new SeatsForm()));steps.Enqueue(("10-lich-chieu",()=>new ShowtimesForm()));steps.Enqueue(("11-khach-hang",()=>new CustomersForm()));steps.Enqueue(("12-san-pham",()=>new ProductsForm()));steps.Enqueue(("13-khuyen-mai",()=>new PromotionsForm()));steps.Enqueue(("14-nha-cung-cap",()=>new SuppliersForm()));steps.Enqueue(("15-nhap-kho",()=>new InventoryForm()));steps.Enqueue(("16-doanh-thu",()=>new RevenueForm()));steps.Enqueue(("17-theo-phim",()=>new MovieStatsForm()));steps.Enqueue(("18-hoan-hoa-don",()=>new RefundForm()));steps.Enqueue(("19-nguoi-dung",()=>new UsersForm()));steps.Enqueue(("20-nhom-quyen",()=>new RolesForm()));steps.Enqueue(("21-sua-phim",()=>{using var d=App.Db();return new EntityEditor<Movie>(d.Movies.First(),"Chỉnh sửa phim");}));steps.Enqueue(("22-nhat-ky",()=>new AuditForm()));steps.Enqueue(("23-doi-mat-khau",()=>new ChangePasswordForm()));steps.Enqueue(("24-cau-hinh",()=>new SettingsForm()));steps.Enqueue(("25-huong-dan",()=>new HelpForm()));
        steps.Enqueue(("26-thu-ngan-rbac",()=>{App.Actor=App.Security.Login("cashier",Bootstrap.DemoPassword);return new MainForm();}));
        timer.Tick+=(_,_)=>Step();Shown+=(_,_)=>timer.Start();
    }
    void Step()
    {
        try {
            if(current==null){if(steps.Count==0){Finish();return;}var next=steps.Dequeue();title=next.title;current=next.factory();current.StartPosition=FormStartPosition.Manual;current.Location=new(20,20);current.Show();tick=0;}
            tick++;if(tick<8)return;
            if(current.WindowState!=FormWindowState.Normal)current.WindowState=FormWindowState.Normal;
            using var bitmap=new Bitmap(current.Width,current.Height);current.DrawToBitmap(bitmap,new Rectangle(0,0,current.Width,current.Height));
            // DrawToBitmap ghi lại giao diện được WinForms dựng thật, không dựng lại bằng HTML.
            bitmap.Save(Path.Combine(output,"frames",$"frame-{frame++:00000}.png"));
            if(tick==15){bitmap.Save(Path.Combine(output,"screenshots",title+".png"));outcomes.Add(new{screen=title,form=current.GetType().Name,errors=App.UiErrors.Count});}
            if(tick>=27){current.Close();current.Dispose();current=null;}
        }catch(Exception ex){App.UiErrors.Add(title+": "+ex);current?.Dispose();current=null;}
    }
    void Finish(){timer.Stop();File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(new{screens=outcomes,errors=App.UiErrors,frames=frame},new JsonSerializerOptions{WriteIndented=true}));Close();}
}
