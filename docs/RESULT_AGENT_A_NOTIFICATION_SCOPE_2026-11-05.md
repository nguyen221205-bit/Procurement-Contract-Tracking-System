# Báo cáo Kết quả Triển khai Agent A: Phạm vi Người nhận Cảnh báo theo Đơn vị và Quyền Truy cập

- Ngày thực hiện: 05/11/2026
- Vai trò: Agent A (Độc lập phân hệ Cảnh báo & Thông báo)
- Repository: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`
- Nhánh: `dev` | Baseline HEAD thực tế: `eb9d90a` (Working tree sạch, không commit/push trái phép)

---

## 1. Tóm tắt Thay đổi Mã nguồn và Hành vi Hệ thống

### 1.1. Danh mục file đã thực hiện
1. **File tạo mới**:
   - [NotificationRecipientResolver.cs](file:///d:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/ProcurementSystem.API/BackgroundServices/NotificationRecipientResolver.cs): Helper nội bộ giải quyết danh sách người nhận theo snapshot từng lượt quét, gom cụm truy vấn (batching), không giữ cache xuyên lượt, hỗ trợ danh mục vai trò vận hành `OperationalAlertRoles` (Manager, Officer, Finance, Signer) và chính sách quản trị viên nền tảng (Admin).
   - [NotificationRecipientScopeTests.cs](file:///d:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/tests/ProcurementSystem.Tests/NotificationRecipientScopeTests.cs): Bộ 8 ca kiểm thử tự động chuyên sâu bao phủ đầy đủ các ràng buộc bảo mật người nhận thông báo, cô lập với Sqlite in-memory, fake SMTP riêng không chạm `TestFakes.cs`.
2. **File chỉnh sửa**:
   - [ContractExpiryNotificationWorker.cs](file:///d:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/ProcurementSystem.API/BackgroundServices/ContractExpiryNotificationWorker.cs): Chuyển đổi phương thức `RunNotificationCheckAsync` sang public để hỗ trợ test tích hợp; thay thế hoàn toàn danh sách broadcast toàn cục `internalUserIds` bằng bộ giải quyết theo entity; loại bỏ suy đoán người nhận theo creator khi thiếu đơn vị; đồng bộ hóa người nhận giữa CSDL, SignalR và Email; bổ sung kiểm tra tài khoản hoạt động (`IsActive`).

### 1.2. So sánh Hành vi Trước và Sau Thay đổi

| Hạng mục | Trước khi sửa (Baseline) | Sau khi sửa (Agent A hoàn thành) |
|---|---|---|
| **Phạm vi người nhận nội bộ** | Truy vấn toàn bộ User có role `Procurement` hoặc `Admin` trong toàn hệ thống (`internalUserIds`). Bất kỳ cảnh báo nào của đơn vị A cũng gửi tới cả cán bộ đơn vị B. | Phân giải chính xác theo `ProcuringEntityId`. Cán bộ đơn vị B không nhận bất kỳ thông báo hay email nào từ đơn vị A. |
| **Trạng thái thành viên (MemberStatus)** | Không kiểm tra `ProcuringEntityMember.Status`. Người bị thu hồi quyền (`Revoked`) hoặc tạm khóa (`Suspended`) vẫn nhận thông báo nếu role hệ thống chưa đổi. | Chỉ người có `MemberStatus.Active` mới được nhận cảnh báo. Trạng thái `Revoked` hoặc `Suspended` bị chặn ngay lập tức ở lượt quét tiếp theo. |
| **Trạng thái tài khoản người dùng (`IsActive`)** | Người dùng bị khóa (`IsActive = false`) vẫn được tạo bản ghi Notification và xếp vào danh sách gửi email. | Kiểm tra `IsActive == true` cho cả cán bộ nội bộ và nhà thầu; tài khoản khóa bị loại bỏ hoàn toàn khỏi Notification và Email. |
| **Gói thầu thiếu đơn vị hoặc cờ `OwnershipNeedsReview`** | Tự động gửi cho `CreatedBy` (người tạo cũ) và broadcast toàn bộ chuyên viên mua sắm. | Tuyệt đối không fallback theo `CreatedBy`. Chỉ gửi nhóm Admin vận hành còn hoạt động để đối soát; ghi log cảnh báo ngắn gọn. |
| **Nhà thầu nhận cảnh báo** | Lấy nhà thầu gắn với hợp đồng nhưng không kiểm tra trạng thái hoạt động tài khoản. | Kiểm tra tài khoản nhà thầu còn hoạt động (`IsActive`); tuyệt đối không phát tán sang nhà thầu của hợp đồng khác. |
| **Deduplicate người nhận** | Nếu một người có nhiều vai trò hoặc vừa là Admin vừa là thành viên đơn vị, có thể bị gửi lặp. | Khử trùng lặp (`HashSet<int>`) theo UserId. Mỗi người nhận tối đa 1 thông báo và 1 email cho cùng một sự kiện; duy trì chống trùng 24 giờ. |
| **Hiệu năng truy vấn (Batching)** | N/A (trước đó lấy 1 danh sách global). | Tải snapshot người nhận trong 3 truy vấn gom cụm tối ưu mỗi lượt quét (`BuildContextAsync`); không query từng user lẻ. |

---

## 2. Kết quả Kiểm thử Chi tiết

### 2.1. Lệnh thực thi kiểm thử
```powershell
dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj --filter "FullyQualifiedName~NotificationRecipientScopeTests" --verbosity minimal
dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj --verbosity minimal
npm test --prefix frontend
```

### 2.2. Bảng Ca kiểm thử (Test Cases Execution Matrix)

| Mã Ca kiểm thử | Đối tượng / Thực thể | Dữ liệu Đầu vào | Kết quả Mong đợi | Kết quả Thực tế | Trạng thái |
|---|---|---|---|---|:---:|
| **TC-A-01** | Entity A vs Entity B | Package A hết hạn (`PKG-A-001`, `ProcuringEntityId = 1`). User gồm: `ProcUserA`, `DualUserAB`, `MultiRoleAdminUserA`, `AdminUser`, `ProcUserB`, `ContractorUserB`. | Chỉ nhóm hợp lệ Entity A và Admin nhận cảnh báo. `ProcUserB` và `ContractorUserB` không nhận cả notification lẫn email. | Pass. Cán bộ đơn vị B hoàn toàn không xuất hiện trong danh sách thông báo và hàng đợi email. | **PASS** |
| **TC-A-02** | Thu hồi quyền tức thì | `DualUserAB` là thành viên cả A và B. Thu hồi quyền tại A (`MemberStatus.Revoked`). Chạy lượt quét tiếp theo. | `DualUserAB` không nhận cảnh báo của A; vẫn nhận được cảnh báo của B nếu membership B hợp lệ. | Pass. Thông báo của A loại trừ `DualUserAB`; thông báo của B gửi tới `DualUserAB` bình thường. | **PASS** |
| **TC-A-03** | Khóa tài khoản & Thiếu Role | `LockedUserA` (`IsActive=false`), `NoRoleUserA` (thiếu role Procurement), `SuspendedMemberA` (`Status=Suspended`), `RevokedMemberA` (`Status=Revoked`). | Toàn bộ 4 đối tượng trên không nhận cảnh báo qua nhánh thành viên đơn vị. | Pass. CSDL không sinh bản ghi Notification, không có email gửi tới các tài khoản này. | **PASS** |
| **TC-A-04** | Chống trùng lặp & Cửa sổ 24h | `MultiRoleAdminUserA` (vừa Admin, vừa Procurement, vừa thành viên đơn vị). Chạy 2 lượt liên tiếp trong 24 giờ. | Lượt 1: chỉ sinh đúng 1 notification và 1 email. Lượt 2: không sinh thêm bản ghi hoặc email nào. | Pass. Khử trùng theo UserId thành công, bộ đếm bản ghi và email không thay đổi ở lượt 2. | **PASS** |
| **TC-A-05** | Thiếu Owner / Chờ đối soát | `PackageUnassigned` (`ProcuringEntityId=null`, `CreatedBy=ProcUserA`) và `PackageNeedsReview` (`OwnershipNeedsReview=true`). | Không fallback người tạo `CreatedBy`. Chỉ nhóm Admin vận hành nhận thông báo; ghi log đối soát. | Pass. `ProcUserA` không nhận thông báo; chỉ `AdminUser` và `MultiRoleAdminUserA` nhận. | **PASS** |
| **TC-A-06** | Nhà thầu đúng hợp đồng | `ContractA` (gắn `ContractorA`) sắp hết hạn; `MilestoneA` (gắn `ContractA`) sắp đến hạn. | Chỉ `ContractorUserA` nhận thông báo; `ContractorUserB` không nhận bất kỳ cảnh báo nào. | Pass. Đúng nhà thầu nhận thông báo hợp đồng và mốc tiến độ; nhà thầu khác bị cách ly an toàn. | **PASS** |
| **TC-A-07** | Bao phủ 5 nhánh sự kiện | 5 sự kiện: Đóng thầu tự động, Hợp đồng sắp hết hạn, Mốc tiến độ sắp đến hạn, Bảo lãnh hết hạn, Giải trình HSDT. | Toàn bộ 5 nhánh đều tuân thủ chính sách scope người nhận chung; Entity B và nhà thầu B không nhận. | Pass. Không nhánh nào sử dụng danh sách broadcast cũ, chính sách phân giải áp dụng nhất quán. | **PASS** |
| **TC-A-08** | Tích hợp trọn vẹn Worker | Chạy `ContractExpiryNotificationWorker.RunNotificationCheckAsync` từ Scope Factory thật. | Gói thầu đổi sang `Closed`, bảo lãnh đổi sang `Expired`, Notification lưu vào DB, email gửi qua service. | Pass. Toàn bộ chu trình nghiệp vụ worker hoàn thành trơn tru, CSDL cập nhật và email gửi thành công. | **PASS** |

### 2.3. Nhật ký Chạy Test Toàn bộ Hệ thống (Real Test Run Output)
```text
Test run for ...\tests\ProcurementSystem.Tests\bin\Debug\net8.0\ProcurementSystem.Tests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88, Duration: 22 s - ProcurementSystem.Tests.dll (net8.0)
```
- Số lượng bài test Backend: **88/88 passed** (80 bài test baseline + 8 bài test mới cho Notification Recipient Scope).
- Số lượng bài test Frontend: **6/6 passed** (không có hồi quy).

---

## 3. Tác động Tương thích và Khuyến nghị

1. **Thay đổi về người nhận cảnh báo**:
   - Chuyên viên Mua sắm sẽ không còn bị làm phiền bởi các email/thông báo của những đơn vị khác mà họ không trực thuộc.
   - Khi một cán bộ nghỉ việc hoặc chuyển công tác và bị quản trị viên đơn vị thu hồi tư cách thành viên (`Revoked` hoặc `Suspended`), họ sẽ lập tức ngừng nhận toàn bộ thông báo nghiệp vụ ở chu kỳ quét kế tiếp, ngay cả khi tài khoản Identity của họ chưa kịp thu hồi role.
2. **Đối với các gói thầu cũ mang cờ `OwnershipNeedsReview`**:
   - Thông báo nội bộ tự động chuyển hướng về cho Quản trị viên hệ thống để đối soát và xử lý gán đơn vị chủ quản, không gửi lung tung ra bên ngoài.
3. **Tính độc lập và không phụ thuộc**:
   - Agent A không can thiệp vào `Program.cs`, không thay đổi dependency injection toàn cục, không tác động tới các phân hệ xác thực đăng ký hoặc mã hóa lưu trữ thuộc quyền của Agent B.
   - Không can thiệp vào CSDL thật, không chạy restart máy chủ, không commit/push mã nguồn.

---

## 4. Phạm vi Chưa xử lý và Lưu ý Vận hành

1. **Về tính đồng thời (Race Condition)**: Snapshot người nhận được chốt ở đầu mỗi chu kỳ quét hạn (1 giờ/lần). Nếu một thành viên bị thu hồi quyền ngay giữa thời điểm một mẻ gửi email/notification đang xử lý, mẻ đó vẫn dùng snapshot tại thời điểm bắt đầu quét. Chu kỳ quét tiếp theo sẽ phản ánh trạng thái mới nhất của CSDL.
2. **Bàn giao Agent B**: Agent A đã hoàn thành trọn vẹn phần việc được phân công, bảo đảm mã nguồn sạch, build thành công 0 cảnh báo, 88/88 bài kiểm thử đạt kết quả xuất sắc. Sẵn sàng tích hợp cùng phân hệ của Agent B.
