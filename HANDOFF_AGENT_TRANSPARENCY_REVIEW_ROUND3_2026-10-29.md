# Bàn giao sửa sau review walkthrough ngày 29/10/2026 — vòng 3

## 1. Đọc trước khi sửa

Mục tiêu: xử lý 6 vấn đề còn lại trong review walkthrough `walkthrough_transparency_rereview_fixes_2026-10-29.md`. Không tuyên bố hoàn tất F01–F07 chỉ từ việc suite xanh.

Repository: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.
Branch đã review: `dev`; HEAD: `0446e500f1188b8fc27311450672fde6a3be68a1`. Mã được review gồm working tree đang dirty, không chỉ nội dung commit HEAD. Trước khi sửa, xác nhận root/branch/HEAD và đọc hướng dẫn repository hiện hành. Bảo toàn thay đổi tracked/untracked của người khác.

Tài liệu nguồn:

- `HANDOFF_AGENT_TRANSPARENCY_REREVIEW_FIXES_2026-10-29.md`: yêu cầu F01–F07 của vòng trước.
- `C:\Users\HP\.gemini\antigravity\brain\46b1c7d8-1233-4c14-a1dc-d59e92e8bec9\walkthrough_transparency_rereview_fixes_2026-10-29.md`: bản Agent báo đã sửa được review lần này.

Mã T01–T06 dưới đây là phát hiện của vòng 3; giữ nguyên mã F01–F07 khi đối chiếu tài liệu cũ. Số dòng là mốc tham khảo của snapshot review, tìm theo tên hàm nếu dòng thay đổi.

## 2. Bản đồ phạm vi sửa

Tất cả đường dẫn trong bảng tính từ repository root ở trên.

| Mã | Mức | File/hàm sửa chính | File liên quan cần kiểm tra | Kết quả cần đạt |
| --- | --- | --- | --- | --- |
| T01 | P1 | `ProcurementSystem.API/Controllers/ContractsController.cs` → `DownloadAcceptanceSnapshotDocument`, khoảng dòng 776–823 | Contract/entity authorization helper hoặc service hiện có; controller/HTTP integration tests | Procurement ngoài đơn vị hoặc membership không Active không tải được snapshot |
| T02 | P1 | `ProcurementSystem.Infrastructure/Services/ContractService.cs` → `ApproveMilestoneAcceptanceAsync`, khoảng 1425, 1491–1531, 1698–1701 | `Services/ProcuringEntityMemberService.cs` → `RevokeMemberAsync`; `Services/SigningAuthorityService.cs` → revoke/update authority; `Repositories/UnitOfWork.cs`; `Data/AppDbContext.cs`; entity concurrency config nếu cần | Membership/authority/revision hợp lệ tại thứ tự quyết định được bảo vệ nguyên tử |
| T03 | P1 | **Thêm migration mới** trong `ProcurementSystem.Infrastructure/Migrations/` | Migration `20261028094453_AddTransparencyControlsIntegrityFixes.cs`, Designer, ModelSnapshot và policy dữ liệu legacy | DB đã áp dụng migration cũ vẫn nhận được sửa chữa khi upgrade |
| T04 | P2 | `frontend/src/api/contractApi.js` → thay cách tải `downloadAcceptanceSnapshotDocumentUrl`; `frontend/src/components/contracts/ApproveMilestoneModal.jsx` → link khoảng dòng 210 | `frontend/src/api/axiosClient.js`, các download flow sẵn có, UI tests | Tải đúng API origin, gửi Bearer token, xử lý file/lỗi đúng |
| T05 | P2 | `ProcurementSystem.Infrastructure/Services/ContractService.cs` → nhánh kiểm tra chung trước `if (request.IsApproved)`, khoảng 1595–1664 | DTO approve, API validation tests; FE xử lý 409 nếu cần | Cả approve và reject đều kiểm tra đúng version/snapshot đã xem |
| T06 | Verification blocker | `tests/ProcurementSystem.Tests/ProcuringEntityMemberAndMakerCheckerTests.cs` → `R2_ConcurrentApproval_ParallelRaceCondition_GuaranteesSingleWinner`, khoảng 1337–1433 | SQL Server integration fixture/project; audit interceptor test fixture | Test race dùng connection độc lập, chặn đúng điểm cạnh tranh, kiểm tra state và audit |

Không mở rộng sang evaluation, scoring, enum hoặc dashboard khi không có dependency trực tiếp. Nếu cần sửa thêm file ngoài bảng, giải thích file đó giải quyết T nào trong walkthrough mới. Chỉ dọn XML documentation bị lỗi trong vùng controller liên quan khi cần; không format lại toàn bộ controller gây diff lớn.

## 3. Hướng dẫn chi tiết

### T01 — Quyền tải snapshot đang bỏ qua phạm vi đơn vị

**Bằng chứng đã chạy:** controller probe với user ID `999999`, claim role Procurement, không có quan hệ với hợp đồng, vẫn nhận `PhysicalFileResult`. Đây là controller/service probe, chưa phải HTTP/browser E2E. Hiện controller coi mọi Admin/Procurement là internal staff và bỏ qua kiểm tra quan hệ đối với Procurement.

**Sửa tại đâu và thế nào:** giữ kiểm tra acceptance thuộc milestone, thêm authorization theo đơn vị quản lý thực tế của hợp đồng. Procurement phải có membership Active và quyền xem tài liệu theo policy đã chốt. Không lấy role toàn hệ thống làm bằng chứng quyền xem mọi đơn vị. Contractor chỉ xem đúng hợp đồng của mình nếu policy cho phép. Quyền đọc của Admin cần được thể hiện rõ theo policy hiện có; không suy từ quyền đọc thành quyền phê duyệt chi. Ưu tiên dùng helper/service authorization thống nhất thay vì lặp logic trong controller.

Nếu dữ liệu chưa có quan hệ entity trực tiếp, phải giải thích cách xác định entity đáng tin cậy; không lấy membership đầu tiên của creator làm giải pháp mặc định cho người thuộc nhiều đơn vị.

**Test nghiệm thu:** hai đơn vị A/B có hợp đồng và snapshot riêng. Procurement A không tải B; member Revoked/Inactive/missing bị chặn; người đủ quyền đúng đơn vị tải được bytes đúng snapshot; Contractor khác bị chặn; không đăng nhập trả 401. Request bị chặn không được trả file/bytes. Kiểm tra qua HTTP có xác thực, không chỉ gọi trực tiếp controller.

### T02 — Transaction chưa bảo vệ membership khỏi revoke race

**Bằng chứng source:** approve mở ReadCommitted transaction, đọc membership bằng AsNoTracking rồi cập nhật authority/acceptance/milestone. Membership không được khóa hoặc kiểm tra phiên bản lúc ghi. RevokeMember cập nhật membership và audit riêng. Membership có thể bị revoke và commit sau lần kiểm tra, trước khi approve lưu quyết định. RowVersion của authority không bảo vệ thay đổi membership. Không kết luận transaction mới là vô ích: nó đã cải thiện tính nguyên tử cho các lệnh DB trong approve; phần còn thiếu là bảo vệ điều kiện quyền.

**Sửa tại đâu và thế nào:** chọn cơ chế transaction/locking/conditional write có thể giải thích rõ, áp dụng phối hợp trên cả approve và revoke. Không chỉ thêm lần AnyAsync thứ hai vì vẫn có khoảng trống sau kiểm tra. Quy tắc: nếu revoke được commit trước quyết định approve theo thứ tự đã bảo vệ, approve phải bị chặn; nếu approve được tuần tự hóa trước, audit phải phản ánh đúng thứ tự/quyền đã dùng. Dùng cùng quy tắc cho authority revoke/đổi scope và membership revoke/đổi trạng thái. Xem xét thời điểm hết hạn thẩm quyền tại quyết định.

Giữ audit bắt buộc cùng transaction của quyết định, kiểm tra rollback khi audit lỗi. Xác định lock order nhất quán, xử lý deadlock/conflict phù hợp và tránh giữ transaction khi gửi email/network. Không bỏ RowVersion hiện có. Không catch lỗi môi trường/provider rồi tính là đã kiểm chứng business concurrency.

**Test nghiệm thu:** SQL Server, hai connection/context độc lập; pause approve sau bước đọc quyền, cho revoke cạnh tranh theo kịch bản định trước, rồi tiếp tục. Kiểm tra membership và authority riêng. Kết quả phải tuân theo thứ tự đã thiết kế, không tồn tại quyết định thành công dùng quyền đã bị revoke trước nó. Audit lỗi phải rollback cả quyết định và trạng thái milestone/contract.

### T03 — Sửa migration cũ không cập nhật DB đã áp dụng nó

**Bằng chứng source:** default 1 và UPDATE backfill mới được viết vào `20261028094453_AddTransparencyControlsIntegrityFixes`; chưa có migration sau đó. EF không chạy lại migration đã có trong __EFMigrationsHistory. Clean DB thành công không chứng minh upgrade DB đã áp dụng phiên bản cũ.

**Sửa tại đâu và thế nào:** thêm forward repair migration mới, có thể chạy trên cả DB đi từ clean path mới và DB đã áp dụng migration cũ với Version/MilestoneVersion/Revision bằng 0. Giữ fix NO ACTION ở đường clean install. Không xóa/sửa migration history hoặc yêu cầu drop DB. Đồng bộ model snapshot nếu model thay đổi. Lập policy riêng cho acceptance legacy thiếu snapshot: đánh dấu cần review lại khi đang chờ, không bịa hash/người ký/thời điểm. Giữ lịch sử đã hoàn tất trung thực. Backfill version/revision phải nhất quán theo milestone; không gán một cách mù quáng làm mất ý nghĩa lịch sử.

**Test nghiệm thu:** (1) clean install toàn bộ migrations; (2) DB ở schema trước integrity migration có dữ liệu đại diện; (3) DB đã áp dụng integrity migration bản cũ có cột version 0 và history ghi nhận migration đó. Dùng forward migration mới để nâng cấp (3), không sửa history để ép chạy lại migration cũ. Kiểm tra dữ liệu, FK, pending/legacy policy và luồng approve/reject qua HTTP. Lưu fixture/baseline tái hiện trường hợp (3), không dùng source migration mới đã sửa làm giả baseline cũ.

### T04 — Link tải snapshot không đi qua API client có xác thực

**Bằng chứng source:** modal dùng href `/api/contracts/.../document/download`; frontend hiện không có Vite proxy, axios baseURL là `http://localhost:5225/api` và gắn JWT từ sessionStorage. Anchor mở tab không qua axios interceptor, nên origin và Authorization đều không đúng với cấu hình hiện tại.

**Sửa tại đâu và thế nào:** thêm hàm download qua axiosClient với responseType blob, URL theo baseURL hiện có, dùng interceptor token. Modal gọi hàm tải bằng button/handler, tạo object URL để mở/lưu file, thu hồi URL đúng thời điểm; không đưa JWT lên URL. Xử lý JSON error trả dưới dạng blob, 401/403/404 và file thiếu. axiosClient hiện unwrap response.data và chuyển lỗi thành Error mới: nếu cần status để phân biệt lỗi, bảo toàn status có chủ đích, kiểm tra tác động tới các caller hiện có. Không thêm origin/API path rải rác trong component.

**Test nghiệm thu:** UI dùng frontend/API khác origin đúng cấu hình, xác thực thành công và tải bytes đúng snapshot. Không gửi request vào Vite origin, không rò token vào URL. Token thiếu/hết hạn hoặc scope sai hiển thị thông báo lỗi, không lưu JSON thành PDF. Có trạng thái loading/chống bấm lặp và không leak object URL.

### T05 — Reject bỏ qua ExpectedVersion

**Bằng chứng đã chạy:** request IsApproved=false với AcceptanceId và ExpectedRevision hợp lệ nhưng ExpectedVersion lệch +999 vẫn lưu acceptance thành Rejected. Fixture hiện truyền null contractor rating service nên probe gặp NullReferenceException sau commit; lỗi dependency này là giới hạn fixture, không phải bằng chứng production trả 500. Bằng chứng cần sửa là quyết định sai version đã được persist.

**Sửa tại đâu và thế nào:** chuyển kiểm tra request ExpectedVersion và các điều kiện snapshot chung ra trước nhánh approve/reject. Giữ AcceptanceId/ExpectedRevision/latest/status guards hiện có. Cả hai quyết định phải tác động đúng bản người dùng đã xem. Kiểm tra Maker–Checker cho cả reject theo nguyên tắc hai người đã chốt; không để Maker có authority tự ghi quyết định Checker bằng cách chọn reject. Điều kiện riêng của giải ngân (TechnicalApproved/IsTechnicalPassed, hoàn thành milestone, cộng số tiền) vẫn nằm ở nhánh approve.

**Test nghiệm thu:** reject sai ExpectedVersion trả 409 và không đổi dữ liệu/audit quyết định; reject đúng version bởi Checker hợp lệ hoạt động đúng; reject bản đã superseded/invalidated bị chặn; Maker không thể tự thực hiện quyết định Checker. Dùng fake contractor service hợp lệ để chạy hết happy path, tránh test kết thúc trước các bước hậu xử lý.

### T06 — Test Barrier hiện có chưa chứng minh SQL Server concurrency

**Bằng chứng source:** hai context trong test chia sẻ cùng `s.Connection` SQLite. Barrier chỉ ở đầu task; service bắt lỗi nested transactions/locked thành 409. Một task bị lỗi provider có thể tạo kết quả một thắng/một thua mà chưa từng đến điểm cùng đọc rồi tranh cập nhật dữ liệu.

**Sửa tại đâu và thế nào:** có thể giữ test SQLite như smoke test nhưng đổi mô tả cho đúng. Thêm integration test SQL Server dùng connection/context độc lập, barrier/hook đặt ở điểm sau read/validation và trước write theo kịch bản. Không chấp nhận nested transaction, lỗi dùng chung connection hay lỗi setup làm kết quả thua hợp lệ. Quan sát SQL/state đủ chứng minh hai request đã đi đến vùng tranh chấp. Kiểm tra error code/business reason cụ thể thay vì chỉ message có chữ xung đột.

**Ma trận tối thiểu:** approve/approve, approve/revoke authority, approve/revoke membership, approve/edit milestone, approve/upload scan; bổ sung review/review để kiểm tra revision không nhập nhằng. Xác nhận một quyết định thành công cho cùng bản, không giải ngân hai lần, audit actor/revision đúng, không có trạng thái hoàn tất một phần. Với audit failure injection, phải chứng minh rollback bằng truy vấn context mới.

## 4. Các phần đã tốt, cần giữ

- Owner Revoked bị chặn 403; bỏ owner bypass ở helper lẫn service.
- Request revision cũ có AcceptanceId/ExpectedRevision đúng vẫn bị chặn 409; không tự ký latest thay người dùng.
- Hash từ bytes thật và fail khi file tham chiếu không đọc được; không fallback hash path.
- Upload scan invalidate pending review, giữ file lịch sử được tham chiếu; tiếp tục kiểm tra cạnh tranh với approve.
- Admin không được duyệt chi thay doanh nghiệp; tài khoản riêng, authority xác minh độc lập, Maker khác Checker.
- Explicit transaction approve, RowVersion và audit history hiện có không được gỡ để làm test xanh.

## 5. Bằng chứng và môi trường

Review gần nhất đã thực thi: backend 43/43, frontend 6/6, frontend production build thành công; API build 0 lỗi, 60 cảnh báo XML documentation. Frontend tests hiện là enum/score preview, không chứng minh UI snapshot. Không coi các số này là kết quả sau khi sửa vòng 3.

Probe vòng 3: `C:\Users\HP\AppData\Local\Temp\codex-transparency-review-20261029\Program.cs`, project `Probe.csproj`. Chạy từ thư mục đó bằng `dotnet run --project Probe.csproj --verbosity quiet`. Output trước sửa:

```json
{"probe":"stale_revision_with_valid_identity","Success":false,"StatusCode":409}
{"probe":"reject_wrong_expected_version","persistedStatus":"Rejected","note":"Reject based on wrong version","fixtureLimitation":"null contractor rating service after commit"}
{"probe":"outsider_procurement_download","outsiderId":999999,"resultType":"PhysicalFileResult","downloadGranted":true}
```

Probe dùng fixture reflection và không thay thế regression tests chính thức. Chuyển kịch bản sang test trong repo có dependency hợp lệ, scope/actor thật và assertion state sau hành động. Review vừa rồi không chạy lại SQL Server migrations hoặc browser E2E, nên chưa xác nhận claim clean/upgrade trong walkthrough.

Chỉ dùng database thử nghiệm độc lập, định danh rõ. Không tự migrate DB gốc, restart dịch vụ đang chạy, stop shared SQL Server instance, commit/push/deploy hoặc reset/clean dirty tree. Startup có thể gọi seeder/MigrateAsync: kiểm tra connection trước khi chạy API. Dùng test host/API tách biệt nếu cần. Báo NOT RUN/BLOCKED đúng hạng mục khi thiếu môi trường; không fabricate log.

## 6. Thứ tự thực hiện và bàn giao lại

1. Ghi baseline; thêm regression tests fail cho T01/T05. Xác định entity scope và policy revoke/approve trước khi patch.
2. Sửa T01/T04 cho đường tải; sửa T02/T05 cho đường quyết định và revoke liên quan.
3. Thêm forward migration T03 và fixtures clean/upgrade; xây SQL Server test T06.
4. Chạy build/test backend và frontend; chạy HTTP authorization/validation, SQL Server clean/upgrade/race/audit rollback và UI download.
5. Viết walkthrough mới theo bảng T01–T06: file/hàm thay đổi, hành vi trước/sau, test/command/log, cấp bằng chứng và phần chưa chạy. Nêu migration mới áp dụng thế nào cho DB đã có migration cũ.

Chưa nghiệm thu toàn bộ cho tới khi có đủ bằng chứng các mục trên. Không gộp source review, SQLite, HTTP, SQL Server integration và browser thành một nhãn E2E PASS.

## 7. Prompt bàn giao

> Đọc HANDOFF_AGENT_TRANSPARENCY_REVIEW_ROUND3_2026-10-29.md tại repository root. Sửa T01–T06 theo bản đồ file/hàm và tiêu chí nghiệm thu trong file. Giữ các invariant và phần đã đúng của F01–F07; bảo toàn dirty tree. Đặc biệt sửa scope tải snapshot, nguyên tử membership revoke/approve, thêm forward migration cho DB đã áp dụng bản cũ, tải file qua API client có token, kiểm tra version cho reject và SQL Server race test với connection độc lập. Không tự thao tác DB gốc/restart/commit/push/deploy. Trả walkthrough có bằng chứng thực thi, phân biệt rõ cấp kiểm thử và phần chưa chạy.
