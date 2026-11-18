using System.ComponentModel.DataAnnotations;

namespace AltF4.Cinema.Core;

[Flags] public enum Access { None=0, Catalog=1, Schedule=2, Sell=4, Refund=8, Reports=16, Inventory=32, Users=64, Roles=128, Audit=256, Configure=512, All=1023 }
public abstract class Entity { public int Id { get; set; } }
public class Role : Entity { public string Name { get; set; } = ""; public Access Permissions { get; set; } }
public class User : Entity { public string Username { get; set; } = ""; public string FullName { get; set; } = ""; public string PasswordHash { get; set; } = ""; public int RoleId { get; set; } public Role Role { get; set; } = null!; public bool Active { get; set; }=true; public bool MustChangePassword { get; set; }=true; public int FailedAttempts { get; set; } public DateTime? LockedUntil { get; set; } }
public class Genre : Entity { public string Name { get; set; } = ""; }
public class Movie : Entity { public string Title { get; set; } = ""; public int GenreId { get; set; } public Genre Genre { get; set; }=null!; public int DurationMinutes { get; set; }=100; public string AgeRating { get; set; }="T13"; public bool Active { get; set; }=true; }
public class Room : Entity { public string Name { get; set; }=""; public bool Active { get; set; }=true; }
public class Seat : Entity { public int RoomId { get; set; } public Room Room { get; set; }=null!; public string Code { get; set; }=""; public long Surcharge { get; set; } public bool Active { get; set; }=true; }
public class Showtime : Entity { public int MovieId { get; set; } public Movie Movie { get; set; }=null!; public int RoomId { get; set; } public Room Room { get; set; }=null!; public DateTime StartsAt { get; set; } public DateTime EndsAt { get; set; } public long BasePrice { get; set; }=75000; public bool Active { get; set; }=true; }
public class Customer : Entity { public string Name { get; set; }=""; public string Phone { get; set; }=""; public bool Vip { get; set; } public long Points { get; set; } }
public class Product : Entity { public string Name { get; set; }=""; public long Price { get; set; } public int Stock { get; set; } public bool Active { get; set; }=true; }
public class Promotion : Entity { public string Code { get; set; }=""; public int Percent { get; set; } public DateTime From { get; set; }=DateTime.Today; public DateTime To { get; set; }=DateTime.Today.AddMonths(1); public bool Active { get; set; }=true; }
public class Supplier : Entity { public string Name { get; set; }=""; public string Phone { get; set; }=""; }
public class InventoryReceipt : Entity { public int ProductId { get; set; } public Product Product { get; set; }=null!; public int SupplierId { get; set; } public Supplier Supplier { get; set; }=null!; public int Quantity { get; set; } public long UnitCost { get; set; } public DateTime CreatedAt { get; set; } public int UserId { get; set; } public User User { get; set; }=null!; }
public class Sale : Entity { public string Code { get; set; }=""; public DateTime CreatedAt { get; set; } public int UserId { get; set; } public User User { get; set; }=null!; public int? CustomerId { get; set; } public Customer? Customer { get; set; } public long Subtotal { get; set; } public int DiscountPercent { get; set; } public long Discount { get; set; } public long Total { get; set; } public long CashReceived { get; set; } public long PointsEarned { get; set; } public string Status { get; set; }="Paid"; public DateTime? RefundedAt { get; set; } public string RefundReason { get; set; }=""; public List<SaleLine> Lines { get; set; }=[]; }
public class SaleLine : Entity { public int SaleId { get; set; } public Sale Sale { get; set; }=null!; public string Description { get; set; }=""; public int Quantity { get; set; } public long UnitPrice { get; set; } public int? ProductId { get; set; } public Product? Product { get; set; } public int? ShowtimeId { get; set; } public Showtime? Showtime { get; set; } public int? SeatId { get; set; } public Seat? Seat { get; set; } }
public class Reservation : Entity { public int ShowtimeId { get; set; } public Showtime Showtime { get; set; }=null!; public int SeatId { get; set; } public Seat Seat { get; set; }=null!; public int SaleId { get; set; } public Sale Sale { get; set; }=null!; }
public class AuditEntry : Entity { public DateTime At { get; set; } public int? UserId { get; set; } public string Action { get; set; }=""; public string Detail { get; set; }=""; }
public record Actor(int UserId, string FullName);
public record ProductOrder(int ProductId, int Quantity);
public record SaleRequest(int? ShowtimeId, IReadOnlyList<int> SeatIds, IReadOnlyList<ProductOrder> Products, int? CustomerId, string PromotionCode, long CashReceived);
public record ReportRow(string Label, long Amount, int Quantity);
