# Handoff cho hai Agent sửa cảnh báo theo đơn vị và cấu hình bảo mật

Ngày lập: 05/11/2026.

Repo: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.

Baseline đã kiểm tra: nhánh `dev`, HEAD `eb9d90a`. Working tree sạch khi lập handoff. Agent phải xác nhận lại HEAD và trạng thái file trước khi sửa; không coi baseline này là trạng thái hiện hành vĩnh viễn.

## 1. Phạm vi được giao

Chỉ xử lý hai mục đã thống nhất:

1. Cảnh báo tự động gửi đúng đơn vị và đúng người nhận còn quyền.
2. Kết thúc hai cơ chế bảo mật tạm thời: đăng ký bỏ qua OTP và mã hóa dùng khóa mặc định/fallback.

Không mở rộng sang hoàn thiện bộ E2E toàn vòng đời, sửa các báo cáo cũ, ERD, thay framework UI, hoặc thêm phân hệ nghiệp vụ. Kiểm thử tập trung cần thiết cho hai mục trên vẫn thuộc phạm vi.

Hai Agent làm song song với quyền sở hữu file riêng. Đây là giao việc triển khai, không chỉ review. Không tự commit/push/merge/deploy, áp migration lên CSDL gốc, restart API đang phục vụ hay thay cấu hình/khóa thật. Không gửi email thật trong test. Giữ nguyên thay đổi của người khác.

## 2. Phân công và ranh giới file

| Agent | Công việc | File được sửa chính |
|---|---|---|
| Agent A | Người nhận cảnh báo theo entity và trạng thái truy cập | `ProcurementSystem.API/BackgroundServices/ContractExpiryNotificationWorker.cs`; helper/service mới riêng cho chọn người nhận nếu cần; test mới `tests/ProcurementSystem.Tests/NotificationRecipientScopeTests.cs` |
| Agent B | OTP bắt buộc và khóa mã hóa hợp lệ | Hai auth services đăng ký; DTO đăng ký; `OtpService.cs` nếu cần chặn bypass; `RegisterPage.jsx` và component OTP liên quan; `FileCryptoCompressionService.cs`; `StorageSettings.cs`; cấu hình mẫu, `Program.cs` nếu cần validation; các test OTP/crypto/storage liên quan |

Agent A không sửa `Program.cs`, cấu hình chung, auth services hay storage. Nếu cần seam cho kiểm thử worker, ưu tiên helper tĩnh/internal hoặc cấu trúc có thể kiểm thử mà không cần đăng ký DI mới. Nếu thật sự cần DI, gửi đề xuất cụ thể cho Agent B, để B thực hiện phần sửa `Program.cs`.

Agent B không sửa worker hay test của Agent A. Test đăng ký mới dùng file `RegistrationOtpSecurityTests.cs`; crypto/storage được sửa các test hiện có và bổ sung file riêng nếu cần. Không cùng sửa `TestFakes.cs`: mỗi Agent tạo fake trong file test riêng.

Tài liệu kết quả: A tạo `docs/RESULT_AGENT_A_NOTIFICATION_SCOPE_2026-11-05.md`; B tạo `docs/RESULT_AGENT_B_REGISTRATION_STORAGE_SECURITY_2026-11-05.md`. B có thể cập nhật `HUONG_DAN_CAU_HINH_SUPABASE_STORAGE.md` và cấu hình mẫu; không sửa các báo cáo nghiệm thu cũ.

Nếu dùng cùng working tree: không switch branch/reset/stash/clean hoặc chạy restore file hàng loạt. Thông báo trước khi cần chạm file ngoài phạm vi. Hai Agent không chạy đồng thời lệnh build/test .NET trên cùng thư mục `bin/obj`; luân phiên hoặc dùng worktree/output cô lập.

## 3. Agent A sửa phạm vi người nhận cảnh báo

### 3.1. Vấn đề đã xác nhận từ nguồn

Trong `ContractExpiryNotificationWorker.cs`, khoảng dòng 73, `internalUserIds` lấy mọi user có role `Procurement` hoặc `Admin`. Danh sách đó được dùng lại khi cảnh báo đóng thầu, hết hạn hợp đồng, milestone và bảo lãnh (các vòng lặp khoảng dòng 148, 199, 244, 275, 289).

Thông báo có tên/mã gói, hợp đồng hoặc mốc của từng đơn vị. Procurement của đơn vị khác, hoặc người đã bị thu hồi membership, vẫn có thể nằm trong danh sách nhận vì role hệ thống chưa đổi. Phạm vi email cũng bị ảnh hưởng vì dùng cùng người nhận.

### 3.2. Hành vi cần đạt

- Resolve đơn vị từ `BidPackage.ProcuringEntityId` đã lưu. Hợp đồng, milestone và bảo lãnh đi ngược FK tới gói để lấy entity. Không suy từ membership hiện tại của `CreatedBy`, `ProcuringEntity.UserId` hoặc email.
- Người nhận nội bộ thông thường: user còn hoạt động, có role Procurement và membership Active của chính entity đó. Những role trong đơn vị được nhận cảnh báo vận hành phải được liệt kê rõ trong helper/policy; mặc định cho các vai trò Manager, Officer, Finance, Signer đang Active, không biến quyền nhận cảnh báo thành quyền phê duyệt.
- Admin nền tảng còn hoạt động được nhận cảnh báo vận hành theo policy riêng, tách khỏi nhánh membership. Đây không phải quyền duyệt chi và không được thêm bypass cho nghiệp vụ nghiệm thu.
- Không gửi tới Procurement của entity khác; không gửi tới membership Revoked/Suspended, user bị khóa hoặc tài khoản không còn role thích hợp. Membership ở B không khôi phục quyền nhận của A.
- Gói thiếu `ProcuringEntityId` hoặc `OwnershipNeedsReview = true`: không đoán người nhận theo creator. Với thông báo nội bộ, chỉ gửi nhóm Admin vận hành còn hoạt động, đồng thời ghi log ngắn gọn về bản ghi cần đối soát. Giữ nguyên cơ chế chặn nghiệp vụ nhạy cảm hiện có.
- Giữ người nhận nhà thầu đúng hợp đồng/gói theo nghiệp vụ đang có; không mở rộng sang nhà thầu khác. Kiểm tra tài khoản người nhận còn hoạt động. Rà cả nhánh giải trình để bảo đảm không vô tình đổi scope.
- Deduplicate theo UserId khi một người có nhiều role/membership hoặc đồng thời là Admin. Notification trong DB, SignalR và email phải dùng cùng kết quả chọn người nhận.
- Giữ lịch chạy, quy tắc tự đóng thầu, chống trùng 24 giờ và nội dung nghiệp vụ. Tránh query theo từng user; ưu tiên truy vấn/batch theo entity và tập người nhận.
- Lựa chọn dựa trên trạng thái DB hiện tại mỗi lượt; không giữ cache người nhận xuyên lượt khiến revoke chưa có hiệu lực. Ghi rõ giới hạn nếu revoke xảy ra sau lúc đã chốt một mẻ gửi; không tuyên bố bảo vệ race khi chưa kiểm chứng.

### 3.3. Kiểm thử bắt buộc

Tạo hai entity A/B với hợp đồng và milestone riêng, user Admin, Procurement A, Procurement B, một user thuộc cả A/B, user bị khóa, membership Revoked/Suspended và một user có role chồng lặp.

1. Cảnh báo của A chỉ tới nhóm A hợp lệ và Admin vận hành; B không nhận cả notification lẫn email.
2. Thu hồi membership A rồi chạy lượt sau: user không nhận của A; vẫn có thể nhận của B nếu membership B hợp lệ.
3. User bị khóa hoặc thiếu role Procurement không nhận qua nhánh thành viên.
4. Không duplicate khi nhiều role/membership; lượt lặp trong cửa sổ chống trùng không phát thêm ngoài quy tắc.
5. Thiếu owner hoặc ownership đang chờ đối soát: không fallback creator; chỉ Admin nội bộ nhận.
6. Nhà thầu đúng hợp đồng vẫn nhận; nhà thầu khác không nhận.
7. Bao phủ các loại sự kiện đóng thầu/hợp đồng/milestone/bảo lãnh; chứng minh các nhánh thực sự dùng policy chung, không chỉ test helper trong khi worker còn dùng danh sách global.

Dùng email/SignalR fake để kiểm tra recipient, không kết nối SMTP thật. Nếu tách helper, cần ít nhất một test tích hợp đường worker → lưu notification/chọn email; không dùng test chỉ lặp lại điều kiện trong implementation.

## 4. Agent B kết thúc cơ chế bảo mật tạm thời

### 4.1. OTP khi đăng ký

Điểm nguồn:

- `ProcurementSystem.Infrastructure/Services/ContractorAuthService.cs`, khoảng dòng 39–48.
- `ProcurementSystem.Infrastructure/Services/ProcuringEntityAuthService.cs`, khoảng dòng 37–46.
- `ProcurementSystem.Core/DTOs/Auth/RegisterContractorRequest.cs` và `RegisterProcuringEntityRequest.cs`: OtpCode đang nullable/không bắt buộc.
- `frontend/src/pages/RegisterPage.jsx`: khoảng dòng 262/313 chỉ append OTP khi có giá trị; kiểm tra cả giao diện gửi mã/nhập mã hiện đang ẩn.
- `OtpService.cs` và DTO/controller OTP: rà khả năng trả `DevOtpCode` để bảo đảm mã không bị lộ ở môi trường triển khai thật.

Yêu cầu:

- Cả hai loại đăng ký phải có OTP email hợp lệ, không cho null/rỗng/whitespace đi qua. Kiểm tra ở DTO/API và service để gọi service trực tiếp cũng không bypass.
- Giữ normalize email và purpose riêng `RegisterContractor`/`RegisterProcuringEntity`. Mã cho email/purpose khác, hết hạn, dùng lại hoặc quá số lần thử đều không tạo tài khoản/đơn vị hay membership.
- Mặc định xác minh bắt buộc ở mọi môi trường. Không thêm cờ bypass public, mã cố định hoặc fallback thành công. Test dùng fake OTP service có chủ đích.
- Khôi phục UI gửi mã/nhập mã cho cả hai form, hiển thị lỗi từ API và cooldown. Thay email sau khi nhận mã phải hủy trạng thái xác minh cũ trên UI; server vẫn là nơi quyết định.
- Nếu có dev OTP trong response, chỉ cho phép khi Development được kiểm soát rõ; Production không trả mã hoặc ghi OTP vào log.
- Kiểm tra thứ tự validate/tiêu thụ mã và transaction. Báo rõ nếu đăng ký lỗi sau bước OTP khiến người dùng phải xin mã mới; không làm OTP hợp lệ dùng lại vô hạn để tránh lỗi trải nghiệm.
- Giữ signup Manager membership, trạng thái thẩm định đơn vị/nhà thầu và quy trình invitation hiện có; không tự cấp signing authority.

Kiểm thử: đủ hai loại đăng ký với missing/blank, sai mã, sai email/purpose, expired/reused, và đúng mã. Với ca bị chặn, kiểm tra không phát sinh User/entity/member hoặc file thừa. Kiểm tra Production không lộ mã; test đường API với multipart cùng test service. Kiểm tra UI gửi mã → nhập mã → đăng ký thành công và server từ chối khi gọi trực tiếp thiếu OTP; dùng môi trường test, không gửi thư thật.

### 4.2. Khóa mã hóa storage

Điểm nguồn:

- `ProcurementSystem.Infrastructure/Services/FileCryptoCompressionService.cs`: constructor đang dẫn xuất khóa từ chuỗi sai định dạng hoặc dùng chuỗi mặc định khi không có khóa.
- `ProcurementSystem.Core/DTOs/Common/StorageSettings.cs`.
- `ProcurementSystem.API/Program.cs`: đăng ký crypto service và options.
- `ProcurementSystem.API/appsettings.Example.json`.
- Các đường gọi trong `HybridFileStorageService.cs`, `SupabaseStorageService.cs`, `StorageMigrationService.cs` cần được rà tác động; chỉ sửa khi cần cho validate/compatibility.
- Tests: `FileCryptoCompressionTests.cs`, `HybridStorageAndAdminTests.cs`.

Yêu cầu:

- Khi dùng chức năng mã hóa, MasterKey bắt buộc là Base64 giải mã đúng 32 bytes. Thiếu, sai Base64, sai độ dài phải báo lỗi cấu hình rõ ràng; loại bỏ cả deterministic default lẫn DeriveKeyFromSeed fallback.
- Validate trước khi bắt đầu thao tác ghi mã hóa; nếu cấu hình bật mã hóa cho provider đang dùng, ưu tiên phát hiện ngay khi khởi động. Log chỉ nêu tên setting/lỗi, không chứa khóa hoặc credential.
- Phân biệt Local không sử dụng mã hóa và luồng đọc file đã mã hóa: không bắt mọi chế độ Local thuần phải có khóa vô cớ; nhưng không được trả plaintext hoặc giả vờ thành công khi cần giải mã mà thiếu/sai khóa.
- Không tự sinh khóa mới mỗi lần khởi động. Không tự đổi khóa thật hoặc ghi lại toàn bộ file cloud trong tác vụ này.
- Giữ khả năng đọc file đã mã hóa bằng khóa hợp lệ đang dùng và đọc tài liệu legacy plaintext theo chính sách hiện có. Không đổi format mã hóa/cipher hay triển khai key rotation đại trà trong handoff này.
- Đặc biệt: file cũ có thể từng mã hóa bằng fallback mặc định. Lập hướng dẫn kiểm kê và chuyển đổi có backup trên bản sao trước khi loại bỏ hỗ trợ vận hành; tuyệt đối không nhúng lại khóa mặc định vào code như một fallback đọc tự động. Nếu cần hỗ trợ chuyển đổi, phải là công cụ/quy trình riêng với khóa cũ được cung cấp tường minh qua secret và có scope đã duyệt. Chưa được tự chạy chuyển đổi dữ liệu thật.
- Cấu hình mẫu dùng placeholder, hướng dẫn cấp khóa ngẫu nhiên 32 bytes và lưu bằng environment/user-secrets. Nêu rõ thay khóa khi chưa chuyển đổi dữ liệu có thể làm mất khả năng đọc file cũ. Không commit khóa thật hay dùng khóa fixture test làm ví dụ triển khai.

Kiểm thử: thiếu/blank/invalid Base64/độ dài sai bị chặn; khóa hợp lệ round-trip đúng bytes; file hợp lệ được tạo trước thay đổi vẫn đọc được; thiếu/sai khóa khi đọc file mã hóa không trả dữ liệu thành công; các đường local/cloud/fallback/migration không âm thầm bỏ mã hóa khi cấu hình sai. Phân biệt lỗi cấu hình với lỗi kết nối cloud; giữ nguyên nguyên tắc fallback storage đã được thiết kế, không cho fallback che giấu lỗi khóa.

## 5. Tiêu chí nghiệm thu chung và cách báo kết quả

Baseline kiểm thử do lượt đánh giá 05/11 đã thực thi: backend `80 passed, 0 failed, 0 skipped`; frontend `6 passed`. Đây là kết quả TRƯỚC các sửa trong handoff, không sao chép thành kết quả sau sửa. Lượt đánh giá đó chưa chạy lại browser E2E.

Mỗi Agent phải báo:

1. HEAD thực tế, file/hàm đã sửa, hành vi trước/sau.
2. Bảng ca kiểm thử gồm actor/entity, dữ liệu test, expected/actual, command và log thật. Ghi rõ service test, SQL integration, HTTP test, UI manual/browser; phần chưa chạy ghi NOT RUN/BLOCKED cùng lý do.
3. Tác động tương thích: người nhận cảnh báo thay đổi thế nào; OTP ảnh hưởng đăng ký ra sao; file mã hóa cũ cần chuẩn bị gì. Không đưa secret vào báo cáo.
4. Phạm vi chưa xử lý và vấn đề phát hiện ngoài phạm vi; không tự sửa lan sang các backlog khác.

Sau khi hai Agent hoàn thành, một người nhận trách nhiệm tích hợp chạy tuần tự trên cùng snapshot đã kết hợp:

```powershell
dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj --no-restore --verbosity minimal
dotnet build ProcurementSystem.API/ProcurementSystem.API.csproj --no-restore --verbosity minimal
npm test --prefix frontend
npm run build --prefix frontend
git diff --check
```

Nhóm SQL Server tests cần LocalDB; thiếu môi trường phải báo đúng, không bỏ qua rồi kết luận PASS toàn bộ. Khi chạy API/UI kiểm tra OTP hoặc worker, dùng database/file storage cô lập và fake SMTP; không restart API trỏ vào database gốc vì startup có thể chạy migration/worker.

Hoàn thành khi: không còn broadcast cảnh báo cho Procurement toàn hệ thống; revoke/disabled được áp dụng theo chính sách nêu trên; cả hai đăng ký không vượt qua OTP; crypto không còn khóa mặc định/dẫn xuất fallback; file cũ có hướng dẫn bảo toàn; kiểm thử mới và suite tích hợp có kết quả thật. Không gắn nhãn đã nghiệm thu toàn dự án chỉ từ việc hoàn thành hai mục này.

## 6. Prompt giao từng Agent

### Agent A

Đọc handoff này, nhận mục 3 và ranh giới file ở mục 2. Triển khai chọn người nhận cảnh báo theo entity/membership hiện hành và policy Admin riêng; bảo vệ cả notification/email/SignalR; giữ nghiệp vụ worker. Viết test đúng phạm vi, phối hợp Agent B để tránh cùng build/sửa file chung, rồi bàn giao tài liệu kết quả riêng. Không commit/push/deploy, không đổi DB gốc. Không thực hiện mục 4 hoặc các backlog ngoài phạm vi.

### Agent B

Đọc handoff này, nhận toàn bộ mục 4 và ranh giới file ở mục 2. Khôi phục OTP bắt buộc cho đăng ký BE/FE và bỏ mọi fallback khóa mã hóa; kiểm tra tương thích file cũ và viết hướng dẫn cấu hình/chuyển đổi an toàn. Phối hợp Agent A ở Program.cs/build, viết test đúng phạm vi, rồi bàn giao tài liệu kết quả riêng. Không thay khóa thật, chạy chuyển đổi cloud, commit/push/deploy hoặc đổi DB gốc. Không sửa worker và các backlog ngoài phạm vi.
