using Microsoft.EntityFrameworkCore;

namespace AltF4.Cinema.Core;

public sealed class CinemaDb(string path) : DbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryReceipt> InventoryReceipts => Set<InventoryReceipt>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource=path, ForeignKeys=true, DefaultTimeout=15 }.ToString());
    protected override void OnModelCreating(ModelBuilder b)
    {
        foreach (var e in b.Model.GetEntityTypes())
            foreach (var fk in e.GetForeignKeys()) fk.DeleteBehavior=DeleteBehavior.Restrict;
        b.Entity<User>().HasIndex(x=>x.Username).IsUnique();
        b.Entity<Role>().HasIndex(x=>x.Name).IsUnique();
        b.Entity<Genre>().HasIndex(x=>x.Name).IsUnique();
        b.Entity<Room>().HasIndex(x=>x.Name).IsUnique();
        b.Entity<Seat>().HasIndex(x=>new{x.RoomId,x.Code}).IsUnique();
        b.Entity<Reservation>().HasIndex(x=>new{x.ShowtimeId,x.SeatId}).IsUnique();
        b.Entity<Sale>().HasIndex(x=>x.Code).IsUnique();
        b.Entity<Promotion>().HasIndex(x=>x.Code).IsUnique();
        b.Entity<Customer>().HasIndex(x=>x.Phone).IsUnique();
        b.Entity<Product>().ToTable(t=>t.HasCheckConstraint("CK_Product_Stock", "Stock >= 0"));
        b.Entity<Showtime>().ToTable(t=>t.HasCheckConstraint("CK_Showtime_Price", "BasePrice > 0"));
        b.Entity<SaleLine>().ToTable(t=>t.HasCheckConstraint("CK_Line_Quantity", "Quantity > 0"));
    }
}
