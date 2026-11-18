namespace AltF4.Cinema.Core;

public static class Bootstrap
{
    // Chỉ chế độ demo dùng mật khẩu công khai; dữ liệu thật được cấp mật khẩu ngẫu nhiên.
    public const string DemoPassword="AltF4@Demo2026";
    public static string? Initialize(Func<CinemaDb> factory,bool demo=false)
    {
        using var db=factory();db.Database.EnsureCreated();if(db.Users.Any()) return null;
        using var tx=db.Database.BeginTransaction();
        var admin=new Role{Name="Quản trị viên",Permissions=Access.All};
        var manager=new Role{Name="Quản lý",Permissions=Access.Catalog|Access.Schedule|Access.Sell|Access.Refund|Access.Reports|Access.Inventory|Access.Audit};
        var cashier=new Role{Name="Thu ngân",Permissions=Access.Sell};
        db.Roles.AddRange(admin,manager,cashier);db.SaveChanges();
        var password=demo?DemoPassword:Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(10))+"a1!";
        db.Users.Add(new(){Username="admin",FullName="Quản trị AltF4",Role=admin,PasswordHash=Passwords.Hash(password),MustChangePassword=!demo});
        if(demo) {
            db.Users.AddRange(new User{Username="manager",FullName="Quản lý Demo",Role=manager,PasswordHash=Passwords.Hash(password),MustChangePassword=false},new User{Username="cashier",FullName="Thu ngân Demo",Role=cashier,PasswordHash=Passwords.Hash(password),MustChangePassword=false});
        }
        var genres=new[]{new Genre{Name="Hành động"},new Genre{Name="Hoạt hình"},new Genre{Name="Tâm lý"}};db.Genres.AddRange(genres);
        var rooms=new[]{new Room{Name="Phòng 01"},new Room{Name="Phòng 02"}};db.Rooms.AddRange(rooms);
        var movies=new[]{new Movie{Title="Hành trình ánh sáng",Genre=genres[0],DurationMinutes=110,AgeRating="T13"},new Movie{Title="Khu vườn kỳ diệu",Genre=genres[1],DurationMinutes=95,AgeRating="P"},new Movie{Title="Ngày mai bình yên",Genre=genres[2],DurationMinutes=105,AgeRating="T16"}};db.Movies.AddRange(movies);
        foreach(var room in rooms) for(int r=0;r<5;r++) for(int c=1;c<=8;c++) db.Seats.Add(new(){Room=room,Code=$"{(char)('A'+r)}{c:00}",Surcharge=r>=2&&r<=3?15000:0});
        db.Products.AddRange(new Product{Name="Bắp rang bơ",Price=45000,Stock=80},new Product{Name="Nước ngọt",Price=30000,Stock=120},new Product{Name="Combo bắp và nước",Price=65000,Stock=50});
        db.Customers.AddRange(new Customer{Name="Khách VIP mẫu",Phone="0900000001",Vip=true},new Customer{Name="Khách thường mẫu",Phone="0900000002"});
        db.Promotions.Add(new(){Code="ALTF4",Percent=15,From=DateTime.Today,To=DateTime.Today.AddMonths(1).AddDays(1)});
        db.Suppliers.Add(new(){Name="Nhà cung cấp mẫu",Phone="0280000000"});db.SaveChanges();
        for(int day=1;day<=7;day++) foreach(var room in rooms) for(int slot=0;slot<3;slot++) {
            var movie=movies[slot]; var start=DateTime.Today.AddDays(day).AddHours(10+slot*4);
            db.Showtimes.Add(new(){Room=room,Movie=movie,StartsAt=start,EndsAt=start.AddMinutes(movie.DurationMinutes),BasePrice=75000});
        }
        db.SaveChanges();tx.Commit();return password;
    }
}
