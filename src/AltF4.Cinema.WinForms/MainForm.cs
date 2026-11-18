using AltF4.Cinema.Core;

namespace AltF4.Cinema.WinForms;

public sealed class MainForm : BaseForm
{
    readonly FlowLayoutPanel menu=new(){Dock=DockStyle.Left,Width=235,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.FromArgb(20,35,65),Padding=new(10)};
    readonly Panel content=new(){Dock=DockStyle.Fill,Padding=new(30)};
    readonly List<(Button,Access)> buttons=[];
    public MainForm():base("Trung tâm điều hành",1280,820)
    {
        Controls.Add(content);Controls.Add(menu);
        menu.Controls.Add(new Label{Text="ALTF4 CINEMA",AutoSize=true,ForeColor=Color.White,Font=new("Segoe UI",17,FontStyle.Bold),Margin=new(5,15,5,20)});
        Add("Tổng quan",Access.None,()=>Dashboard());
        Add("Bán vé và bắp nước",Access.Sell,()=>Open(new SaleForm()));
        Add("Tra cứu hóa đơn",Access.Sell,()=>Open(new SalesForm()));
        Add("Hoàn hóa đơn",Access.Refund,()=>Open(new RefundForm()));
        Add("Phim",Access.Catalog,()=>Open(new MoviesForm()));Add("Thể loại",Access.Catalog,()=>Open(new GenresForm()));
        Add("Phòng chiếu",Access.Catalog,()=>Open(new RoomsForm()));Add("Ghế ngồi",Access.Catalog,()=>Open(new SeatsForm()));Add("Lịch chiếu",Access.Schedule,()=>Open(new ShowtimesForm()));
        Add("Khách hàng",Access.Catalog,()=>Open(new CustomersForm()));Add("Bắp nước",Access.Catalog,()=>Open(new ProductsForm()));Add("Khuyến mãi",Access.Catalog,()=>Open(new PromotionsForm()));Add("Nhà cung cấp",Access.Catalog,()=>Open(new SuppliersForm()));
        Add("Nhập kho",Access.Inventory,()=>Open(new InventoryForm()));Add("Báo cáo doanh thu",Access.Reports,()=>Open(new RevenueForm()));Add("Thống kê theo phim",Access.Reports,()=>Open(new MovieStatsForm()));
        Add("Người dùng",Access.Users,()=>Open(new UsersForm()));Add("Nhóm quyền",Access.Roles,()=>Open(new RolesForm()));Add("Nhật ký hệ thống",Access.Audit,()=>Open(new AuditForm()));Add("Cấu hình",Access.Configure,()=>Open(new SettingsForm()));
        Add("Đổi mật khẩu",Access.None,()=>Open(new ChangePasswordForm()));Add("Hướng dẫn",Access.None,()=>Open(new HelpForm()));Add("Đăng xuất",Access.None,()=>Close());
        Activated+=(_,_)=>Run(()=>{RefreshRights();});Shown+=(_,_)=>Run(Dashboard);
    }
    void Add(string text,Access permission,Action action){var b=Button(text,()=>Run(()=>{if(permission!=Access.None){using var db=App.Db();SecurityService.Require(db,App.Actor,permission);}action();}),200);b.Height=37;b.TextAlign=ContentAlignment.MiddleLeft;menu.Controls.Add(b);buttons.Add((b,permission));}
    void Open(Form f){using(f)f.ShowDialog(this);RefreshRights();Dashboard();}
    void RefreshRights(){var rights=App.Security.Permissions(App.Actor);foreach(var(b,p)in buttons)b.Visible=p==Access.None||(rights&p)==p;}
    void Dashboard()
    {
        content.Controls.Clear();var stack=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};content.Controls.Add(stack);
        stack.Controls.Add(new Label{Text=$"Xin chào, {App.Actor.FullName}",Font=new("Segoe UI",23,FontStyle.Bold),AutoSize=true,Margin=new(0,8,0,14)});
        stack.Controls.Add(Label($"{DateTime.Now:dddd, dd/MM/yyyy}  •  {(App.Demo?"Môi trường DEMO":"Dữ liệu vận hành")}"));
        stack.Controls.Add(Label("Chọn chức năng từ thanh điều hướng bên trái.\nGhế đã bán được khóa theo từng suất chiếu. Tồn kho cập nhật sau thanh toán."));
        var rights=App.Security.Permissions(App.Actor);
        if((rights&Access.Reports)==Access.Reports){var rows=App.Business.Revenue(App.Actor,DateTime.Today.AddDays(-6),DateTime.Today.AddDays(1));
            stack.Controls.Add(new Label{Text=$"DOANH THU THUẦN 7 NGÀY\n{rows.Sum(x=>x.Amount):N0} đ",AutoSize=true,Font=new("Segoe UI",20,FontStyle.Bold),ForeColor=Color.FromArgb(33,76,156),Margin=new(0,25,0,10)});
            stack.Controls.Add(new RevenueChart(rows){Width=850,Height=280});
            stack.Controls.Add(Label("Doanh thu thuần = tiền thu trong kỳ − tiền hoàn trong kỳ.\nSố hóa đơn trên báo cáo = số thu − số hoàn."));
        }
    }
}
public sealed class RevenueChart(List<ReportRow> rows) : Control
{
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.Clear(Color.White);using var f=new Font("Segoe UI",9);using var brush=new SolidBrush(Color.FromArgb(33,100,180));
        if(rows.Count==0){e.Graphics.DrawString("Chưa có giao dịch trong kỳ",f,Brushes.Gray,25,100);return;}
        var max=Math.Max(1,rows.Max(x=>Math.Abs(x.Amount)));var w=Math.Max(25,(Width-60)/rows.Count);var zero=Height/2;
        e.Graphics.DrawLine(Pens.Gray,25,zero,Width-10,zero);
        for(int i=0;i<rows.Count;i++){var r=rows[i];var h=(int)(Math.Abs(r.Amount)*1.0/max*(Height/2-50));var x=30+i*w;e.Graphics.FillRectangle(brush,x,r.Amount>=0?zero-h:zero,Math.Max(12,w-15),Math.Max(1,h));e.Graphics.DrawString(r.Amount.ToString("N0"),f,Brushes.Black,x,r.Amount>=0?zero-h-20:zero+h+2);e.Graphics.DrawString(r.Label[..5],f,Brushes.Gray,x,Height-25);}
    }
}
