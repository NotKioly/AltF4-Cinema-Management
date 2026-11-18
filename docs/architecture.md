# Kiến trúc và mô hình dữ liệu

```mermaid
flowchart LR
 UI[Windows Forms] --> SEC[SecurityService]
 UI --> CAT[CatalogService / Repository]
 UI --> BIZ[BusinessService]
 SEC --> DB[CinemaDb / EF Core]
 CAT --> DB
 BIZ --> DB
 DB --> SQLITE[(SQLite)]
 UI --> RDLC[ReportViewer / Cinema.rdlc]
```

```mermaid
erDiagram
 Role ||--o{ User : grants
 Genre ||--o{ Movie : classifies
 Movie ||--o{ Showtime : schedules
 Room ||--o{ Showtime : hosts
 Room ||--o{ Seat : contains
 User ||--o{ Sale : creates
 Customer o|--o{ Sale : buys
 Sale ||--|{ SaleLine : includes
 Product o|--o{ SaleLine : sells
 Showtime o|--o{ SaleLine : tickets
 Seat o|--o{ SaleLine : tickets
 Showtime ||--o{ Reservation : locks
 Seat ||--o{ Reservation : locks
 Sale ||--o{ Reservation : owns
 Supplier ||--o{ InventoryReceipt : supplies
 Product ||--o{ InventoryReceipt : receives
 User ||--o{ InventoryReceipt : creates
```

Reservation có unique index (ShowtimeId, SeatId). Khi hoàn, chỉ xóa Reservation; Sale và SaleLine được giữ để đối soát. Promotion được áp dụng thành DiscountPercent/Discount snapshot trên Sale; thay đổi khuyến mãi không làm đổi hóa đơn cũ. AuditEntry lưu UserId dạng giá trị để giữ nhật ký khi xóa một tài khoản chưa có giao dịch.
