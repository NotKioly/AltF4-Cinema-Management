using AltF4.Cinema.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Reporting.WinForms;

namespace AltF4.Cinema.WinForms;

public static class ReportTools
{
    public static ReportViewer Viewer()=>new(){Dock=DockStyle.Fill,ProcessingMode=ProcessingMode.Local};
    public static void Load(ReportViewer viewer,string title,string subtitle,List<PrintRow> rows){viewer.LocalReport.ReportPath=Path.Combine(AppContext.BaseDirectory,"Reports","Cinema.rdlc");viewer.LocalReport.DataSources.Clear();viewer.LocalReport.DataSources.Add(new ReportDataSource("CinemaData",rows));viewer.LocalReport.SetParameters([new ReportParameter("ReportTitle",title),new ReportParameter("Subtitle",subtitle)]);viewer.SetDisplayMode(DisplayMode.PrintLayout);viewer.RefreshReport();}
    public static void Smoke(){using var report=new LocalReport();report.ReportPath=Path.Combine(AppContext.BaseDirectory,"Reports","Cinema.rdlc");report.DataSources.Add(new ReportDataSource("CinemaData",new[]{new PrintRow("Vé mẫu • Ghế A01",1,75000),new PrintRow("TỔNG THANH TOÁN",0,75000)}));report.SetParameters([new ReportParameter("ReportTitle","HÓA ĐƠN ALTF4 CINEMA"),new ReportParameter("Subtitle","Kiểm thử kết xuất RDLC")]);Directory.CreateDirectory(Path.Combine(App.Home,"exports"));File.WriteAllBytes(Path.Combine(App.Home,"exports","rdlc-smoke.pdf"),report.Render("PDF"));}
}
public record PrintRow(string Label,int Quantity,long Amount);
public sealed class InvoiceForm : BaseForm
{
    public InvoiceForm(int id):base("Hóa đơn và vé • RDLC",1100,800){var viewer=ReportTools.Viewer();Controls.Add(viewer);Shown+=(_,_)=>Run(()=>{using var db=App.Db();var u=SecurityService.Require(db,App.Actor,Access.Sell);var sale=db.Sales.Include(s=>s.Lines).Include(s=>s.User).Include(s=>s.Customer).Single(s=>s.Id==id);
        if(sale.UserId!=u.Id&&(u.Role.Permissions&(Access.Refund|Access.Reports))==0)throw new UnauthorizedAccessException("Bạn chỉ được xem hóa đơn do mình lập.");
        var rows=sale.Lines.Select(l=>new PrintRow(l.Description,l.Quantity,l.Quantity*l.UnitPrice)).ToList();rows.AddRange([new("Tạm tính",0,sale.Subtotal),new($"Giảm giá {sale.DiscountPercent}%",0,-sale.Discount),new("TỔNG THANH TOÁN",0,sale.Total),new("Tiền khách đưa",0,sale.CashReceived),new("Tiền thừa",0,sale.CashReceived-sale.Total)]);
        ReportTools.Load(viewer,$"HÓA ĐƠN {sale.Code}",$"{sale.CreatedAt:dd/MM/yyyy HH:mm} | Thu ngân: {sale.User.FullName} | Khách: {sale.Customer?.Name??"Vãng lai"} | {sale.Status}"+(sale.Status=="Refunded"?$" | Đã hoàn {sale.RefundedAt:dd/MM/yyyy HH:mm}: {sale.RefundReason}":""),rows);});}
}
public class RevenueForm : BaseForm
{
    protected readonly DateTimePicker From=new(){Width=150,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddDays(-6)};protected readonly DateTimePicker To=new(){Width=150,Format=DateTimePickerFormat.Short,Value=DateTime.Today};readonly ReportViewer viewer=ReportTools.Viewer();readonly bool movie;
    public RevenueForm(bool byMovie=false):base(byMovie?"Thống kê vé theo phim":"Báo cáo doanh thu thuần • RDLC",1200,800){movie=byMovie;var bar=Bar();bar.Controls.Add(Label("Từ ngày"));bar.Controls.Add(From);bar.Controls.Add(Label("Đến ngày"));bar.Controls.Add(To);bar.Controls.Add(Button("Lập báo cáo",()=>Run(Reload),180));Controls.Add(viewer);Controls.Add(bar);Shown+=(_,_)=>Run(Reload);}
    void Reload(){if(From.Value.Date>To.Value.Date)throw new InvalidOperationException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");var rows=movie?App.Business.MovieRevenue(App.Actor,From.Value.Date,To.Value.Date.AddDays(1)):App.Business.Revenue(App.Actor,From.Value.Date,To.Value.Date.AddDays(1));var data=rows.Select(r=>new PrintRow(r.Label,r.Quantity,r.Amount)).ToList();data.Add(new("TỔNG CỘNG",rows.Sum(r=>r.Quantity),rows.Sum(r=>r.Amount)));
        ReportTools.Load(viewer,movie?"THỐNG KÊ VÉ THEO PHIM":"BÁO CÁO DOANH THU THUẦN",$"Từ {From.Value:dd/MM/yyyy} đến hết {To.Value:dd/MM/yyyy}. "+(movie?"Vé còn hiệu lực, phân bổ giảm giá theo dòng; không gồm bắp/nước.":"Tiền thu trong kỳ trừ tiền hoàn trong kỳ; số lượng = số thu − số hoàn."),data);}
}
public sealed class MovieStatsForm():RevenueForm(true);
