# AltF4 Cinema — Ứng dụng quản lý rạp chiếu phim

AltF4 Cinema là đồ án môn **Lập trình trên Windows**, thực hiện bởi **nhóm AltF4**. Ứng dụng Desktop hỗ trợ công việc tại quầy: quản lý phim và lịch chiếu, chọn ghế, bán vé cùng bắp nước, nhập kho, ưu đãi khách VIP, hoàn hóa đơn và báo cáo doanh thu.

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Giao diện | C# Windows Forms, .NET 8, Windows x64 |
| Truy cập dữ liệu | Entity Framework Core SQLite 8.0.22 và LINQ |
| Dữ liệu | SQLite cục bộ; tự khởi tạo lần đầu |
| In và xuất báo cáo | RDLC, ReportViewerCore.WinForms 15.1.33 |
| Bảo vệ mật khẩu | PBKDF2-HMAC-SHA256, salt ngẫu nhiên, 300.000 vòng |
| Kiểm thử | xUnit; kiểm thử tích hợp bằng SQLite thật |
| Đóng gói | Publish tự chứa runtime; công thức Inno Setup |

## Các chức năng

Ứng dụng có **24 Form chức năng**, không tính các lớp giao diện cơ sở hay Form kiểm tra nội bộ:

1. Đăng nhập.
2. Màn hình chính và biểu đồ doanh thu.
3. Đổi mật khẩu.
4. Quản lý người dùng.
5. Quản lý nhóm quyền.
6. Thể loại phim.
7. Danh mục phim.
8. Phòng chiếu.
9. Ghế ngồi và phụ thu.
10. Lịch chiếu.
11. Khách hàng và VIP.
12. Bắp nước, giá bán và tồn kho.
13. Khuyến mãi.
14. Nhà cung cấp.
15. Nhập kho và lịch sử phiếu nhập.
16. Bán vé và bắp nước.
17. Tra cứu hóa đơn.
18. Hoàn toàn bộ hóa đơn.
19. Xem/in hóa đơn và vé bằng RDLC.
20. Báo cáo doanh thu theo kỳ.
21. Thống kê vé theo phim.
22. Nhật ký hệ thống.
23. Cấu hình đường dẫn cơ sở dữ liệu.
24. Hướng dẫn sử dụng.

## Chạy nhanh từ bản bàn giao

Bản portable, bộ cài, báo cáo Word và video nằm trong [thư mục Google Drive bàn giao](https://drive.google.com/drive/folders/1i8e-mmW3QgEkrZVLu529mRBx8W6yr5bu). Thư mục giữ quyền chia sẻ theo tài khoản sở hữu; nếu giảng viên chưa truy cập được, chủ sở hữu cần cấp quyền xem.

Máy chạy: **Windows 10/11 x64**. Bản portable tự chứa runtime không cần Visual Studio, SDK hoặc dịch vụ SQL Server.

1. Giải nén `AltF4-Cinema-Portable-win-x64.zip`, giữ nguyên các DLL và thư mục `Reports`.
2. Chạy `AltF4.Cinema.WinForms.exe` để mở dữ liệu thật.
3. Chạy `Chay-Demo.cmd` trong bản portable hoặc shortcut **AltF4 Cinema Demo** của bộ cài để trình diễn.

Lần mở dữ liệu thật đầu tiên, ứng dụng hiển thị tài khoản **admin** và mật khẩu ngẫu nhiên một lần. Hãy ghi lại và đổi mật khẩu ngay sau đăng nhập. Mật khẩu khởi tạo/đặt lại luôn buộc đổi trước khi sử dụng nghiệp vụ.

### Tài khoản demo

Chạy với tham số `--demo` để sử dụng **cơ sở dữ liệu demo riêng**:

```powershell
.\AltF4.Cinema.WinForms.exe --demo
```

| Tài khoản | Mật khẩu chỉ dành cho demo | Quyền mặc định |
|---|---|---|
| `admin` | `AltF4@Demo2026` | Toàn bộ chức năng |
| `manager` | `AltF4@Demo2026` | Danh mục, lịch, bán, hoàn, kho, báo cáo, nhật ký |
| `cashier` | `AltF4@Demo2026` | Bán và xem/in hóa đơn do mình lập |

Dữ liệu mẫu gồm 3 phim giả định, 2 phòng × 40 ghế, lịch chiếu trong 7 ngày tiếp theo, bắp nước, khách mẫu và mã giảm giá `ALTF4`. Không sử dụng các mật khẩu demo cho dữ liệu thật. Các suất tạo từ ngày khởi tạo; nếu demo cũ hết lịch, quản lý tạo thêm suất trong tương lai.

## Build từ source

Cần **.NET SDK 8 trở lên** và Windows; Visual Studio có workload **.NET desktop development** là tùy chọn để mở solution. Lần restore đầu cần Internet tải NuGet.

```powershell
dotnet restore AltF4.Cinema.sln
dotnet build AltF4.Cinema.sln -c Release --no-restore
dotnet test tests/AltF4.Cinema.Tests -c Release
dotnet run --project src/AltF4.Cinema.WinForms -- --demo
```

Có thể dùng `scripts/build.ps1 -Publish` để build, test và tạo bản tự chứa runtime tại `deliverables/portable`:

```powershell
dotnet publish src/AltF4.Cinema.WinForms -c Release -r win-x64 --self-contained true -o deliverables/portable
```

Tạo bộ cài với Inno Setup bằng `ISCC.exe scripts/installer.iss` sau khi publish. Công thức cài theo người dùng, không yêu cầu quyền quản trị, có shortcut thật/demo và gỡ cài đặt. Dữ liệu nằm ngoài thư mục chương trình.

## Kiến trúc và cơ sở dữ liệu

```text
src/
  AltF4.Cinema.Core/       Thực thể, EF Core, bảo mật, repository và nghiệp vụ
  AltF4.Cinema.WinForms/   24 Form chức năng và Reports/Cinema.rdlc
tests/
  AltF4.Cinema.Tests/      Kiểm thử tích hợp với tệp SQLite riêng cho từng ca
scripts/
  build.ps1               Build, test, publish
  installer.iss           Công thức tạo bộ cài
docs/
  architecture.md         Mô hình quan hệ và luồng các lớp
```

Form gọi `CatalogService`, `BusinessService` và `SecurityService`. Các lớp này dùng `CinemaDb`/EF Core để đọc ghi SQLite. Truy vấn có `Where`, `Join`, `Select`, `GroupBy`, `Sum`, `Any`, `Include`; không dùng ADO.NET thuần hoặc viết câu SELECT dạng chuỗi.

16 bảng: Role, User, Genre, Movie, Room, Seat, Showtime, Customer, Product, Promotion, Supplier, InventoryReceipt, Sale, SaleLine, Reservation, AuditEntry. Chi tiết tại [tài liệu kiến trúc](docs/architecture.md).

### Vị trí dữ liệu và cấu hình

- Dữ liệu thật: `%LOCALAPPDATA%\AltF4Cinema\Data\cinema.db`.
- Demo: `%LOCALAPPDATA%\AltF4Cinema\Demo\cinema.db`.
- `appsettings.local.json` trong từng thư mục lưu `DatabasePath`.
- Màn hình Cấu hình cho phép đổi đường dẫn `.db`, lưu và thoát; mở lại để áp dụng. Không chuyển dữ liệu cũ tự động.

Đóng tất cả phiên ứng dụng trước khi sao chép thư mục dữ liệu để sao lưu. Giữ tệp dữ liệu ngoài repo. Phiên bản này khởi tạo schema bằng `EnsureCreated`, chưa có migration nâng cấp DB qua nhiều phiên bản.

## Quy tắc nghiệp vụ

### Lịch, ghế và thanh toán

- Suất chiếu mới phải trong tương lai; phim/phòng hoạt động; giờ kết thúc tính từ thời lượng phim.
- Hai suất cùng phòng không chồng nhau và chừa ít nhất **15 phút dọn phòng**.
- Vé = giá cơ bản suất + phụ thu ghế. Tối đa 20 ghế mỗi giao dịch.
- Unique index `(ShowtimeId, SeatId)` bảo vệ việc bán trùng ghế ngay tại DB.
- Bắp nước phải còn đủ tồn; tối đa 100 đơn vị mỗi sản phẩm mỗi hóa đơn.
- VIP giảm **10%**; nếu mã có tỷ lệ cao hơn thì chọn tỷ lệ cao hơn, **không cộng dồn**.
- Giảm giá làm tròn xuống đến đồng: `Subtotal * Percent / 100`. Tiền lưu bằng `long` theo VND.
- Tiền nhận phải đủ; chương trình tính tiền thừa. Mỗi 10.000đ thực thu tích 1 điểm, chưa hỗ trợ đổi điểm.
- `Checkout` dùng Transaction cho hóa đơn, chi tiết, kho, điểm, Reservation và nhật ký. Lỗi ở bất kỳ bước nào trước Commit sẽ rollback.

### Nhập kho và hoàn

- Không sửa tồn kho trực tiếp bằng CRUD; tồn tăng qua phiếu nhập, giảm qua bán và trả lại khi hoàn.
- Hoàn **toàn bộ hóa đơn** cần quyền Refund và lý do. Không hoàn khi bất kỳ suất nào đã bắt đầu.
- Hóa đơn chỉ có bắp nước chỉ hoàn trong 30 phút.
- Hoàn trả kho, mở ghế, thu hồi điểm và đánh dấu `Refunded`; không xóa lịch sử hóa đơn.
- Hoàn lần thứ hai bị từ chối. Phiếu nhập và thao tác hoàn được ghi nhật ký.

### Doanh thu

Doanh thu thuần theo ngày = **tiền thu phát sinh trong ngày − tiền hoàn phát sinh trong ngày**. Thu hôm qua, hoàn hôm nay sẽ ghi thu ở hôm qua và khoản âm ở hôm nay. Bộ lọc ngày bao gồm toàn bộ ngày cuối.

Thống kê theo phim chỉ gồm vé của hóa đơn hiện còn `Paid`, lọc ngày mua, phân bổ giảm giá theo dòng và loại bắp nước. Vì khác phạm vi, không dùng báo cáo này thay cho đối soát dòng tiền; làm tròn theo dòng có thể tạo chênh lệch nhỏ.

## Bảo mật và phân quyền

Mật khẩu lưu PBKDF2-HMAC-SHA256 với salt 16 byte, hash 32 byte, 300.000 vòng; so sánh thời gian cố định. Sau 5 lần sai, tài khoản khóa 5 phút. Mật khẩu không xuất hiện trong nhật ký hoặc lưới dữ liệu.

MainForm ẩn chức năng không có quyền. Mỗi dịch vụ kiểm tra lại quyền và trạng thái tài khoản từ DB ở từng lần gọi, kể cả khi Actor đã đăng nhập từ trước. Chương trình giữ ít nhất một quản trị viên hoạt động và chặn tài khoản tự xóa/tự khóa/tự đổi nhóm.

Đây là ứng dụng dữ liệu cục bộ: người có quyền sửa trực tiếp tệp SQLite có thể can thiệp dữ liệu. Không chia sẻ `.db` qua thư mục mạng để nhiều máy cùng ghi.

## In và báo cáo

Hóa đơn và báo cáo dùng mẫu thật `Reports/Cinema.rdlc`, hiển thị bằng ReportViewer. Dùng thanh công cụ để in hoặc xuất PDF. ReportViewer Core là bản chuyển cộng đồng cho .NET hiện đại, tham khảo [dự án gốc](https://github.com/lkosson/reportviewercore). Đã kiểm tra kết xuất PDF; chưa thử máy in vật lý.

## Kiểm thử và demo

Bộ **22 kiểm thử** bao gồm mật khẩu/khóa đăng nhập, quyền dịch vụ, VIP, mã giảm giá, kho, ghế sai phòng, lịch chồng, hoàn hai lần, báo cáo qua hai kỳ, rollback sau khi đã ghi hóa đơn và hai tác vụ cùng bán một ghế. Xem [BusinessTests.cs](tests/AltF4.Cinema.Tests/BusinessTests.cs).

Kiểm tra dựng giao diện tự động (chỉ dữ liệu demo):

```powershell
.\AltF4.Cinema.WinForms.exe --demo --verify-ui
```

Bộ kiểm tra mở 26 trạng thái giao diện, ghi PNG và `results.json` trong thư mục demo. Nó tạo một giao dịch thật trên dữ liệu demo để kiểm tra hóa đơn, ghế và doanh thu. Đây là smoke test mở/dựng hình, không thay thế toàn bộ kiểm thử tương tác. Video bàn giao dùng khung hình giao diện thật từ lần chạy tự động, có chú thích và không có giọng nói.

Kiểm tra xuất RDLC:

```powershell
.\AltF4.Cinema.WinForms.exe --demo --smoke-report
```

Tệp PDF kiểm tra được tạo ở `%LOCALAPPDATA%\AltF4Cinema\Demo\exports`.

## Giới hạn

Phiên bản phục vụ đồ án một rạp, dữ liệu cục bộ; chưa có thanh toán điện tử, đặt vé web, giữ ghế có thời hạn, QR, email lấy lại mật khẩu, đổi điểm, UI thương mại hoặc Dark Mode. Nhân viên kiểm tra độ tuổi theo nhãn phim. Cấu hình SQLite bằng JSON không phải cấu hình server SQL Server/App.config. Chưa kiểm chứng tải lớn hoặc nhiều máy; cần thử thêm trên máy Windows sạch trước triển khai.

## Tài liệu nền tảng

- [EF Core SQLite](https://learn.microsoft.com/en-us/ef/core/providers/sqlite)
- [Transaction trong EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [PBKDF2 trong .NET](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2)
- [Isolation trong SQLite](https://www.sqlite.org/isolation.html)
- [Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/)

Nhóm AltF4 chịu trách nhiệm rà soát, hiểu mã nguồn và hoàn thiện phần trình bày khi bảo vệ đồ án.
