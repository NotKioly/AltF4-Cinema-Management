using System.Text.Json;
using AltF4.Cinema.Core;

namespace AltF4.Cinema.WinForms;

internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture=new("vi-VN");
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture=new("vi-VN");
        ApplicationConfiguration.Initialize();
        try {
            App.Demo=args.Contains("--demo");App.Verification=args.Contains("--verify-ui");App.Load();
            var initial=Bootstrap.Initialize(App.Factory,App.Demo);
            if(initial!=null&&!App.Demo) MessageBox.Show($"Tài khoản quản trị: admin\nMật khẩu ban đầu: {initial}\n\nHãy ghi lại mật khẩu này. Bạn phải đổi mật khẩu sau khi đăng nhập.","Khởi tạo lần đầu",MessageBoxButtons.OK,MessageBoxIcon.Information);
            if(args.Contains("--smoke-report")){ReportTools.Smoke();return;}
            if(App.Verification){Application.Run(new UiVerification());return;}
            Application.Run(new LoginForm());
        }catch(Exception ex){MessageBox.Show(ex.ToString(),"Không thể khởi động",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
}
public sealed class Settings { public string DatabasePath { get; set; }=""; }
public static class App
{
    public static bool Demo;public static bool Verification;public static List<string> UiErrors=[];public static Actor Actor=null!;public static Settings Settings=new();
    public static string Home=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AltF4Cinema",Demo?"Demo":"Data");
    public static string Config=>Path.Combine(Home,"appsettings.local.json");
    public static CinemaDb Db()=>new(Settings.DatabasePath);
    public static Func<CinemaDb> Factory=>Db;
    public static SecurityService Security=>new(Factory);
    public static CatalogService Catalog=>new(Factory);
    public static BusinessService Business=>new(Factory);
    public static void Load(){Directory.CreateDirectory(Home);if(File.Exists(Config))Settings=JsonSerializer.Deserialize<Settings>(File.ReadAllText(Config))??new();if(string.IsNullOrWhiteSpace(Settings.DatabasePath))Settings.DatabasePath=Path.Combine(Home,"cinema.db");Save();}
    public static void Save()=>File.WriteAllText(Config,JsonSerializer.Serialize(Settings,new JsonSerializerOptions{WriteIndented=true}));
}
