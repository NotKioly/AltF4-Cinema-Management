using AltF4.Cinema.Core;
using Microsoft.EntityFrameworkCore;

namespace AltF4.Cinema.WinForms;

public class BaseForm : Form
{
    public BaseForm(string title,int width=1120,int height=760)
    {
        Text=$"{title} • AltF4 Cinema";ClientSize=new(width,height);StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(245,247,251);MinimumSize=new(800,580);
    }
    protected void Run(Action action){try{action();}catch(Exception ex){if(App.Verification){App.UiErrors.Add(Text+": "+ex);return;}MessageBox.Show(this,ex is DbUpdateException?"Dữ liệu đang được sử dụng hoặc bị trùng. Hãy kiểm tra lại.":ex.Message,"Thông báo",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    public static Button Button(string text,Action action,int width=150){var b=new Button{Text=text,Width=width,Height=40,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(33,76,156),ForeColor=Color.White,Margin=new(5)};b.FlatAppearance.BorderSize=0;b.Click+=(_,_)=>action();return b;}
    public static Label Label(string text)=>new(){Text=text,AutoSize=true,Margin=new(5,10,5,4)};
    public static DataGridView Grid()=>new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,RowHeadersVisible=false,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells,ColumnHeadersHeight=38};
    protected static FlowLayoutPanel Bar()=>new(){Dock=DockStyle.Top,Height=58,Padding=new(8),WrapContents=false,AutoScroll=true};
    protected static void ShowDialog(Form form,IWin32Window owner){using(form)form.ShowDialog(owner);}
}
public class LoginForm : BaseForm
{
    readonly TextBox user=new(){Width=280,Text="admin"};readonly TextBox pass=new(){Width=280,UseSystemPasswordChar=true};
    public LoginForm():base("Đăng nhập",900,600){
        var card=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,Size=new(440,410),Location=new(230,80),BackColor=Color.White,Padding=new(45,25,30,20)};
        card.Controls.Add(new Label{Text="ALTF4 CINEMA",Font=new("Segoe UI",25,FontStyle.Bold),AutoSize=true,ForeColor=Color.FromArgb(33,76,156)});
        card.Controls.Add(Label(App.Demo?"DỮ LIỆU DEMO • Không dùng vận hành thật":"Hệ thống quản lý rạp chiếu phim"));
        card.Controls.Add(Label("Tên đăng nhập"));card.Controls.Add(user);card.Controls.Add(Label("Mật khẩu"));card.Controls.Add(pass);
        var login=Button("Đăng nhập",()=>Run(()=>{App.Actor=App.Security.Login(user.Text,pass.Text);if(App.Security.MustChange(App.Actor)){using var change=new ChangePasswordForm(true);if(change.ShowDialog(this)!=DialogResult.OK)return;}Hide();using(var main=new MainForm())main.ShowDialog();pass.Clear();Show();}),280);
        card.Controls.Add(login);AcceptButton=login;if(App.Demo)card.Controls.Add(Label("Demo: admin / manager / cashier\nMật khẩu: AltF4@Demo2026"));Controls.Add(card);
    }
}
public class ChangePasswordForm : BaseForm
{
    public ChangePasswordForm(bool required=false):base("Đổi mật khẩu",800,580){
        var p=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,Padding=new(70),WrapContents=false};
        var old=new TextBox{Width=400,UseSystemPasswordChar=true};var next=new TextBox{Width=400,UseSystemPasswordChar=true};var confirm=new TextBox{Width=400,UseSystemPasswordChar=true};
        p.Controls.Add(Label(required?"Đổi mật khẩu ban đầu để tiếp tục":"Mật khẩu có ít nhất 10 ký tự, gồm chữ và số"));
        foreach(var pair in new[]{("Mật khẩu hiện tại",old),("Mật khẩu mới",next),("Nhập lại mật khẩu",confirm)}){p.Controls.Add(Label(pair.Item1));p.Controls.Add(pair.Item2);}
        p.Controls.Add(Button("Lưu mật khẩu",()=>Run(()=>{if(next.Text!=confirm.Text)throw new InvalidOperationException("Mật khẩu nhập lại không khớp.");App.Security.ChangePassword(App.Actor,old.Text,next.Text);DialogResult=DialogResult.OK;Close();}),200));Controls.Add(p);
    }
}
public record Choice(int Id,string Name){public override string ToString()=>Name;}
public class EntityEditor<T> : BaseForm where T:Entity,new()
{
    readonly Dictionary<string,Control> fields=[];readonly T item;readonly TextBox password=new(){UseSystemPasswordChar=true,Width=410};
    public EntityEditor(T entity,string title):base(title,850,720)
    {
        item=entity;var outer=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new(25)};var layout=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2};layout.ColumnStyles.Add(new(SizeType.Absolute,235));layout.ColumnStyles.Add(new(SizeType.Absolute,460));
        foreach(var prop in typeof(T).GetProperties()){
            if(!Meta.Editable(prop.Name,typeof(T))||!new[]{typeof(string),typeof(int),typeof(long),typeof(bool),typeof(DateTime),typeof(Access)}.Contains(prop.PropertyType))continue;
            Control control;
            if(prop.PropertyType==typeof(bool))control=new CheckBox{Checked=(bool)prop.GetValue(item)!,Width=410};
            else if(prop.PropertyType==typeof(DateTime))control=new DateTimePicker{Value=(DateTime)prop.GetValue(item)!<DateTimePicker.MinimumDateTime?DateTime.Now:(DateTime)prop.GetValue(item)!,Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy HH:mm",Width=410};
            else if(prop.PropertyType==typeof(Access)){var list=new CheckedListBox{Width=410,Height=220,CheckOnClick=true};foreach(var access in Enum.GetValues<Access>().Where(x=>x!=Access.None&&x!=Access.All))list.Items.Add(access,(((Access)prop.GetValue(item)!)&access)==access);control=list;}
            else if(prop.Name.EndsWith("Id")){var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=410,DisplayMember="Name",ValueMember="Id"};combo.DataSource=Meta.Choices(prop.Name);combo.SelectedValue=prop.GetValue(item)!;control=combo;}
            else if(prop.PropertyType==typeof(int)||prop.PropertyType==typeof(long))control=new NumericUpDown{Maximum=100000000,Minimum=0,Value=Convert.ToDecimal(prop.GetValue(item)),Width=410,ThousandsSeparator=true};
            else control=new TextBox{Text=(string?)prop.GetValue(item),Width=410,MaxLength=prop.Name=="Code"?30:150};
            control.Margin=new(5,8,5,8);fields.Add(prop.Name,control);layout.Controls.Add(Label(Meta.Title(prop.Name)),0,layout.RowCount);layout.Controls.Add(control,1,layout.RowCount++);
        }
        if(typeof(T)==typeof(User)){layout.Controls.Add(Label("Mật khẩu mới (trống = giữ nguyên)"),0,layout.RowCount);layout.Controls.Add(password,1,layout.RowCount++);}
        layout.Controls.Add(Button("Lưu dữ liệu",()=>Run(()=>{
            foreach(var (name,c) in fields){var p=typeof(T).GetProperty(name)!;object? value=c switch{CheckBox b=>b.Checked,DateTimePicker d=>d.Value,ComboBox cb=>cb.SelectedValue??0,NumericUpDown n=>p.PropertyType==typeof(int)?(object)(int)n.Value:(long)n.Value,CheckedListBox cl=>cl.CheckedItems.Cast<Access>().Aggregate(Access.None,(s,v)=>s|v),_=>c.Text.Trim()};p.SetValue(item,value);}
            App.Catalog.Save(App.Actor,item,password.Text);DialogResult=DialogResult.OK;Close();})),1,layout.RowCount++);outer.Controls.Add(layout);Controls.Add(outer);
    }
}
public static class Meta
{
    public static bool Editable(string n,Type t)=>!new[]{"Id","PasswordHash","MustChangePassword","FailedAttempts","LockedUntil","Stock","Points","EndsAt"}.Contains(n);
    static readonly Dictionary<string,string> titles=new(){["Id"]="Mã",["Name"]="Tên",["Title"]="Tên phim",["GenreId"]="Thể loại",["RoomId"]="Phòng chiếu",["MovieId"]="Phim",["DurationMinutes"]="Thời lượng (phút)",["AgeRating"]="Phân loại độ tuổi",["Active"]="Đang hoạt động",["Code"]="Mã",["Surcharge"]="Phụ thu (đ)",["StartsAt"]="Bắt đầu",["EndsAt"]="Kết thúc",["BasePrice"]="Giá vé (đ)",["Phone"]="Điện thoại",["Vip"]="Khách VIP",["Points"]="Điểm tích lũy",["Price"]="Giá bán (đ)",["Stock"]="Tồn kho",["Percent"]="Giảm giá (%)",["From"]="Hiệu lực từ",["To"]="Đến trước thời điểm",["Username"]="Tên đăng nhập",["FullName"]="Họ tên",["RoleId"]="Nhóm quyền",["Permissions"]="Các quyền được cấp"};
    public static string Title(string n)=>titles.GetValueOrDefault(n,n);
    public static List<Choice> Choices(string name){using var db=App.Db();return name switch{"GenreId"=>db.Genres.Select(x=>new Choice(x.Id,x.Name)).ToList(),"RoomId"=>db.Rooms.Select(x=>new Choice(x.Id,x.Name)).ToList(),"MovieId"=>db.Movies.Select(x=>new Choice(x.Id,x.Title)).ToList(),"RoleId"=>db.Roles.Select(x=>new Choice(x.Id,x.Name)).ToList(),_=>[]};}
}
public class CatalogForm<T> : BaseForm where T:Entity,new()
{
    readonly DataGridView grid=Grid();readonly TextBox search=new(){Width=240,PlaceholderText="Tìm trong danh sách…"};List<T> data=[];
    public CatalogForm(string title):base(title){var bar=Bar();bar.Controls.Add(search);bar.Controls.Add(Button("Thêm",()=>Run(()=>{ShowDialog(new EntityEditor<T>(new(),$"Thêm {title}"),this);Reload();})));bar.Controls.Add(Button("Sửa",()=>Run(()=>{var selected=Selected();var copy=System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(selected))!;ShowDialog(new EntityEditor<T>(copy,$"Sửa {title}"),this);Reload();})));bar.Controls.Add(Button("Xóa",()=>Run(()=>{var selected=Selected();if(MessageBox.Show(this,"Xóa bản ghi đang chọn?","Xác nhận",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes){App.Catalog.Delete<T>(App.Actor,selected.Id);Reload();}})));bar.Controls.Add(Button("Tải lại",()=>Run(Reload)));Controls.Add(grid);Controls.Add(bar);search.TextChanged+=(_,_)=>Bind();Shown+=(_,_)=>Run(Reload);}
    T Selected()=>grid.CurrentRow?.DataBoundItem as T??throw new InvalidOperationException("Chọn một dòng dữ liệu.");
    void Reload(){data=App.Catalog.List<T>(App.Actor);Bind();}
    void Bind(){var scalar=typeof(T).GetProperties().Where(p=>p.PropertyType.IsValueType||p.PropertyType==typeof(string)).Where(p=>!new[]{"PasswordHash","FailedAttempts","LockedUntil","MustChangePassword"}.Contains(p.Name)).ToList();grid.DataSource=data.Where(x=>scalar.Any(p=>p.GetValue(x)?.ToString()?.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase)==true)).ToList();foreach(DataGridViewColumn col in grid.Columns){col.Visible=scalar.Any(p=>p.Name==col.Name);col.HeaderText=Meta.Title(col.Name);}}
}
public sealed class UsersForm():CatalogForm<User>("Quản lý người dùng");
public sealed class RolesForm():CatalogForm<Role>("Quản lý nhóm quyền");
public sealed class GenresForm():CatalogForm<Genre>("Thể loại phim");
public sealed class MoviesForm():CatalogForm<Movie>("Danh mục phim");
public sealed class RoomsForm():CatalogForm<Room>("Phòng chiếu");
public sealed class SeatsForm():CatalogForm<Seat>("Ghế ngồi");
public sealed class ShowtimesForm():CatalogForm<Showtime>("Lịch chiếu");
public sealed class CustomersForm():CatalogForm<Customer>("Khách hàng");
public sealed class ProductsForm():CatalogForm<Product>("Bắp nước và tồn kho");
public sealed class PromotionsForm():CatalogForm<Promotion>("Khuyến mãi");
public sealed class SuppliersForm():CatalogForm<Supplier>("Nhà cung cấp");
