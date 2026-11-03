# Handoff Agent: Hoàn thiện thẩm quyền ký và toàn vẹn hồ sơ nghiệm thu

Ngày bàn giao: **28/10/2026**, múi giờ Asia/Saigon.

## 1. Mục tiêu và phạm vi

Tiếp tục phần tài khoản cá nhân, membership, invitation và Maker–Checker đã triển khai để xử lý ba nút thắt còn lại:

1. Manager đang có thể cấp Signer mà chưa xác minh người nhận thực sự có thẩm quyền.
2. Admin, người tạo gói, chủ hồ sơ pháp nhân và Manager còn có đường được suy ra quyền duyệt.
3. Hồ sơ được duyệt chưa gắn với một phiên bản nội dung cố định; sửa milestone có thể giữ lại xác nhận kỹ thuật cũ.

Kết quả cần đạt: xác định được **ai làm, dựa trên thẩm quyền nào, đã kiểm tra/duyệt nội dung phiên bản nào**, và giữ lịch sử để đối chiếu sau khi thay quyền hoặc sửa hồ sơ.

Đọc cùng `HANDOFF_AGENT_MEMBERSHIP_INVITATIONS_2026-10-26.md`. Tài liệu này cụ thể hóa phần còn thiếu, giữ hướng tài khoản cá nhân độc lập tham gia doanh nghiệp qua lời mời. Không quay lại tài khoản Procurement chung hoặc tài khoản cha sở hữu mật khẩu tài khoản con.

Lượt bàn giao này chỉ tạo tài liệu. Chưa sửa code, tạo migration, thay database, restart hay chạy lại test.

## 2. Checkout và nguyên tắc tiếp nhận

- Repo: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.
- Nhánh kiểm tra ngày bàn giao: `dev`.
- HEAD: `0446e500f1188b8fc27311450672fde6a3be68a1`.
- Working tree có nhiều file modified/untracked từ các phần triển khai trước; HEAD không đại diện toàn bộ code đang có.
- Đã rà source ngày 28/10 và xác nhận các đường quyền/cập nhật nêu tại mục 4 vẫn hiện diện. Agent phải kiểm tra lại checkout/diff trước khi sửa.
- Bảo toàn thay đổi hiện có, gồm membership/invitation, FE/BE enum F01–F08, hợp đồng, đánh giá và các test. Không reset/clean hoặc ghi đè công việc khác.
- Giao tiếp và báo cáo bằng tiếng Việt. Không tự commit/push/merge/deploy.

## 3. Bằng chứng đã có và giới hạn

### 3.1. Test thực thi ngày 27/10/2026

Đây là kết quả của lượt trước, không phải kết quả kiểm thử bản sửa theo handoff này:

| Hạng mục | Bằng chứng đã thực thi |
|---|---|
| Build API | Thành công, 0 warning và 0 error trên working tree lúc chạy. |
| Gia nhập | Manager mời Officer và Signer; nhận lời tạo hai tài khoản cá nhân riêng, UserId 10 và 11; đăng nhập riêng; dùng lại lời mời đã nhận bị từ chối. |
| Đấu thầu | Gói `PKG-2026-0006`, submission 9, 2 PDF fixture, giá 1.150.000.000; 3 evaluator, tiêu chí 60/40, 6 điểm chấm; tổng điểm 93, hạng 1; trao thầu có audit `FINALIZE_AWARD`. |
| Hợp đồng | Contract 2 đi qua Draft → Active → Completed; gói chuyển Awarded → Contracted; 3 milestones 4/5/6 có tổng 1.150.000.000. |
| Hai người | SQL xác nhận cả 3 acceptance có TechnicalReviewedBy = 10, ApprovedBy = 11 và milestone Completed. |
| Tình huống bị chặn | Đóng thầu sớm thiếu lý do; kích hoạt thiếu scan; Signer làm bước Maker (403); Officer duyệt (403); duyệt lặp; Manager dùng cùng UserId vừa kiểm tra vừa duyệt (400, lỗi Maker–Checker). |

Môi trường: API thật chạy với SQL Server sandbox riêng, SMTP sink cục bộ và PDF tổng hợp. **Không có browser E2E, email gửi ra ngoài hoặc chuyển tiền ngân hàng.** DB gốc `procurement_db` không được dùng cho các mutation của lượt đó. Các ID trên là dữ liệu sandbox đã xóa, không dùng làm fixture cố định cho lần tiếp theo.

### 3.2. Lỗi môi trường đã thực thi

Khởi tạo SQL Server DB rỗng thất bại với lỗi 1785 tại migration `20261012104913_AddIsPriceCriteriaToEvaluationCriteria`, liên quan multiple cascade paths của `BidPackageEvaluators`. Chỉ hai migration đầu được áp dụng lúc thất bại. Test tiếp tục sau khi tạo riêng bảng sandbox với FK Evaluator `NO ACTION`; workaround này không sửa source và không chứng minh clean migration đã đạt.

DB sandbox và tiến trình test đã được dừng/xóa. Lần cuối kiểm tra sau test, API 5225 và UI 5173 vẫn healthy. Một thư mục tạm `C:\Users\HP\AppData\Local\Temp\codex-procurement-lifecycle-e2e-20261027-02` còn lại do automatic approval review chặn thao tác xóa; `appsettings.json` ở root thư mục đó đã được làm rỗng. Không coi config/binary/upload trong thư mục này là môi trường triển khai hợp lệ.

### 3.3. Chưa có bằng chứng thực thi cho ba lỗ hổng đầy đủ

Manager đã cấp Signer qua lời mời trong luồng test mà không có bước xác minh thẩm quyền riêng. Các đường Admin/creator/owner và sửa nội dung giữ review cũ được xác định bằng đọc source; chưa chạy E2E khai thác tương ứng. Chưa kiểm thử đồng thời, stale version, thẩm quyền hết hạn/thu hồi trên bản sửa mới.

## 4. Điểm nối source đã xác nhận ngày 28/10

Dòng là mốc điều hướng, có thể dịch chuyển sau khi Agent sửa.

| Điểm nối | Vấn đề cần xử lý |
|---|---|
| `ProcuringEntityMemberService.cs:142`, `SendInvitationAsync` | Quyền mời dựa trên Admin/Manager; role của lời mời có thể là Signer. |
| `ProcuringEntityMemberService.cs:550–596`, `UpdateMemberRoleAsync` | Gán trực tiếp `member.RoleInEntity = request.RoleInEntity` tại dòng 574; chưa có quy trình thẩm quyền ký riêng. |
| `ProcuringEntityMemberService.cs:34`, `HasAdminOrManagerPermissionAsync` | Có nhánh chủ pháp nhân `pe.UserId`; cần phân biệt lịch sử đăng ký với quyền đang hiệu lực. |
| `ContractService.cs:1195`, `TechnicalReviewMilestoneAsync` | Cho phép Admin/creator/owner/Manager qua các nhánh kiểm quyền; ghi review lên acceptance, milestone vẫn có thể Pending. |
| `ContractService.cs:1308`, `ApproveMilestoneAcceptanceAsync` | Dòng 1347 cho `isAdmin || packageCreatedBy == userId`; dòng 1350–1353 cho owner/Manager; dòng 1377 miễn Admin khỏi Maker != Checker. |
| `ContractService.cs:799–867`, `UpdateMilestoneAsync` | Cho sửa mốc Pending, cập nhật Title/DueDate/Amount tại 856–858, không vô hiệu review cũ trong hàm này. |
| `Entities/Acceptance.cs` | Có actor/thời gian/nhận xét kỹ thuật và phê duyệt; chưa có snapshot nội dung và version/concurrency token trong entity đã đọc. |
| `Entities/ProcuringEntityMember.cs` | Có role/trạng thái/gia nhập/thu hồi; chưa là bản ghi xác minh quyền ký có căn cứ/phạm vi/thời hạn. |
| `Controllers/ContractsController.cs` | Rà cả `/technical-review`, `/acceptance`, alias `/accept`, cập nhật milestone và đổi trạng thái hợp đồng. |
| `frontend/src/pages/ContractDetailPage.jsx` | Nhiều nút đang dựa trên global Admin/Procurement; cần chuyển sang capability backend. |
| `DataSeeder.cs:16`, `Program.cs` | Startup gọi MigrateAsync/seed; restart có thể ghi database. |

Các file service/entity ở trên thuộc `ProcurementSystem.Infrastructure/`; controllers thuộc `ProcurementSystem.API/`. Rà thêm DTO/Core, DbContext, migrations, API client và UI liên quan; không giới hạn sửa đúng các dòng này.

## 5. Yêu cầu A: Xác minh và cấp thẩm quyền ký

### 5.1. Tách ba khái niệm

- Tài khoản cá nhân xác định danh tính đăng nhập.
- Membership/quyền nghiệp vụ xác định người đó thuộc đơn vị và được làm công việc nào.
- Thẩm quyền ký xác định người đó được phê duyệt nghiệp vụ gì, cho đơn vị/hợp đồng nào và trong khoảng thời gian nào.

Global role Procurement, role Manager, tên chức danh, email và việc tạo pháp nhân không tự chứng minh quyền ký.

### 5.2. Quy trình tối thiểu cần triển khai

1. Người quản lý đang có quyền gửi đề nghị xác minh thẩm quyền cho đúng tài khoản/thành viên của đơn vị, kèm căn cứ đại diện hoặc ủy quyền.
2. Admin nền tảng xem hồ sơ, ghi nhận cách đối chiếu qua kênh độc lập và kết quả xác minh. Mô tả rõ quy trình vận hành; upload một PDF hoặc tick checkbox không tự chứng minh danh tính/chức danh.
3. Backend kiểm tra đủ thông tin bắt buộc, người xác minh khác người được cấp quyền, đơn vị/thành viên hợp lệ; tạo quyết định xác minh kèm phạm vi/thời hạn.
4. Chỉ bản ghi đã xác minh và đang có hiệu lực mới cho phép phê duyệt. Khi hết hạn, thu hồi hoặc membership không hoạt động, command mới phải bị chặn dù JWT cũ còn hạn.
5. Thay người ký, đổi phạm vi hoặc gia hạn cần hồ sơ/phiên bản quyết định mới, giữ lịch sử cũ. Manager không thể sửa record đã xác minh để mở rộng quyền.

Nếu người đại diện chưa có tài khoản, dùng lời mời để họ tự tạo/nhận tài khoản; việc nhận lời chỉ liên kết danh tính/membership. Không cấp quyền kỹ thuật hoặc quyền ký chỉ để hoàn tất onboarding. Việc xác minh thẩm quyền sau đó gắn với đúng tài khoản đã liên kết.

### 5.3. Dữ liệu gợi ý, cần đặc tả trước khi code

Một entity như `SigningAuthority` có thể lưu: entity ID, user/membership ID, loại quyền nghiệp vụ, scope, ValidFrom/ValidTo, trạng thái, căn cứ, chứng cứ/file ID, người đề nghị, người xác minh, thời điểm, lý do quyết định, version và lịch sử thu hồi/thay thế.

Agent chọn tên bảng/DTO theo convention repo. Phải định nghĩa scope kiểm tra được bằng code; không chỉ lưu chuỗi mô tả tự do rồi cho duyệt mọi hợp đồng. Bản tối thiểu có thể dùng quyền duyệt nghiệm thu trong một đơn vị, và giới hạn hợp đồng/phân công cụ thể khi nghiệp vụ yêu cầu; không suy diễn có nhiều đơn vị hoặc thêm quyền toàn hệ thống.

- Lời mời/đổi role thông thường phải từ chối yêu cầu cấp Signer hoặc payload giả trạng thái xác minh, VerifiedBy, thời hạn/phạm vi đã duyệt.
- Nếu giữ enum Signer để tương thích/hiển thị, coi nó là kết quả phản ánh thẩm quyền đã xác minh; endpoint duyệt vẫn phải kiểm record thẩm quyền hiện hành.
- Người đăng ký đầu tiên có thể có nhiệm vụ quản lý thành viên sau bootstrap hợp lệ; không tự có quyền ký. Quyền quản lý cũng cần kiểm hiện hành.
- Admin có quyền xác minh theo chức năng nền tảng; không nhận quyền duyệt chi của đơn vị từ chức năng này.

## 6. Yêu cầu B: Chính sách quyền phía backend

### 6.1. Ma trận nền tảng

| Năng lực | Điều kiện nghiệp vụ |
|---|---|
| Quản lý thành viên | Membership hoạt động và quyền quản lý được cấp hợp lệ; không kèm quyền ký mặc định. |
| Xác nhận kỹ thuật | Membership hoạt động, quyền kỹ thuật/phân công đúng đơn vị/hợp đồng, trạng thái hồ sơ hợp lệ. |
| Duyệt/từ chối nghiệm thu | Membership hoạt động, thẩm quyền ký đã xác minh còn hiệu lực và đúng scope, đúng phiên bản/trạng thái; UserId khác Maker. |
| Xác minh thẩm quyền | Quyền nền tảng chuyên biệt; không cho xác minh cho chính mình. |
| Admin duyệt nghiệm thu với tư cách Admin | Bị từ chối; không có nhánh bypass nền tảng. |
| Manager/creator/owner duyệt chỉ vì các thuộc tính đó | Bị từ chối. |

Một người có nhiều quyền cũng không được duyệt chính hồ sơ mình đã xác nhận kỹ thuật. Không miễn trừ theo role hoặc endpoint cũ. Manager muốn làm bước kỹ thuật phải có quyền/phân công kỹ thuật rõ ràng; Manager muốn duyệt phải có thẩm quyền ký riêng hợp lệ.

### 6.2. Thay cách xác định scope

- Gói thầu/hợp đồng phải liên kết trực tiếp đến pháp nhân chịu trách nhiệm, theo mô hình dữ liệu được đặc tả; CreatedBy giữ ý nghĩa audit.
- Không tìm đơn vị phụ trách bằng một phép OR theo creator thuộc bất kỳ membership nào rồi chọn FirstOrDefault.
- Với dữ liệu cũ không xác định chắc đơn vị, lập danh sách cần đối soát; không đoán hoặc gán vào đơn vị đầu tiên.
- Kiểm quyền hiện hành ở service/policy backend dùng chung; JWT chủ yếu cung cấp identity và quyền nền tảng. FE không được tự quyết định được duyệt hay không.
- Rà controller, alias `/accept`, truy cập trực tiếp API, đổi trạng thái contract/milestone, sửa/xóa mốc, attachment, report, notification và SignalR để tránh đường đi vòng.
- Endpoint đổi trạng thái hợp đồng không được làm Completed nếu chưa có đủ kết quả nghiệm thu hợp lệ theo cùng chính sách.

## 7. Yêu cầu C: Phiên bản hồ sơ và phê duyệt nguyên tử

### 7.1. Nội dung phải được ràng buộc

Khi xác nhận kỹ thuật/trình duyệt, lưu snapshot bất biến của đơn vị, hợp đồng, nhà thầu, milestone, số tiền/đơn vị tiền, nội dung, ngày hạn, chứng cứ/tài liệu và nhận xét kỹ thuật. Gắn với version của hồ sơ và actor/thời gian xác nhận.

File phải có định danh phiên bản và được lưu theo cách không thể thay bytes ở đường dẫn cũ mà vẫn giữ hiệu lực duyệt. Có thể dùng nội dung/hash để đối chiếu; hash đơn lẻ không thay thế việc giữ bản chứng cứ thực tế.

Signer xem đúng snapshot/version được trình và gửi version đó khi duyệt. Backend không đọc lại milestone hiện tại rồi mặc nhiên coi đó là nội dung Maker đã xác nhận.

### 7.2. Quy tắc chỉnh sửa tối thiểu

- Trước xác nhận kỹ thuật: sửa theo quyền nghiệp vụ, tăng version khi thay đổi nội dung liên quan.
- Sau xác nhận, đang chờ Signer: khóa sửa trực tiếp nội dung trọng yếu. Muốn sửa phải có thao tác trả về chỉnh sửa có lý do, giữ snapshot cũ, đánh dấu yêu cầu cũ không còn hiệu lực và tạo revision mới cần Maker xác nhận lại.
- Sau phê duyệt: không sửa/xóa snapshot, actor, quyết định hoặc chứng cứ cũ qua CRUD thông thường. Điều chỉnh dùng hồ sơ tiếp theo có liên kết lịch sử và lý do.
- Rà các đường thay gián tiếp nội dung như cập nhật contract, thay nhà thầu, phụ lục thay giá trị, thêm/xóa milestone, thay file; mỗi đường cần quy tắc giữ/invalidate review rõ ràng.
- Không dùng việc milestone còn Pending làm điều kiện đủ để cho sửa sau technical review.

Agent đặc tả state machine của acceptance/revision trước khi triển khai; không tái sử dụng enum số theo suy đoán. Tương thích cơ chế enum contract/sinh danh mục hiện có.

### 7.3. Cạnh tranh và audit

- Dùng concurrency token/version phía DB và thao tác kiểm trạng thái/version nguyên tử; một transaction chỉ đọc rồi SaveChanges ở isolation mặc định chưa tự bảo đảm một người thắng.
- Hai yêu cầu duyệt đồng thời cùng hồ sơ/version chỉ được tạo một quyết định nghiệp vụ và một hiệu ứng hoàn tất/ghi nhận số tiền; request thua trả conflict hoặc kết quả idempotent đúng contract, không tạo bản phê duyệt thứ hai.
- Kiểm phiên bản và thẩm quyền/membership hiện hành tại điểm thực thi được bảo vệ; đặc tả cách tránh race với sửa hồ sơ hoặc thu hồi quyền.
- Ghi kết quả nghiệp vụ, chuyển trạng thái/tổng tiền và audit trong cùng transaction. Notification sau commit, có cơ chế tránh gửi trùng do retry.
- Audit tối thiểu: actor thật, entity/scope, đối tượng, revision/snapshot, decision, authority ID/version dùng để duyệt, thời điểm, lý do và thay đổi trạng thái.
- Không tạo ApprovedBy giả bằng Maker để lấp trường bắt buộc. Checker chưa quyết định thì thông tin phê duyệt phải thể hiện đúng là chưa có.
- Không log mật khẩu, OTP, raw invitation token hoặc khóa ký. Không gọi bảng log cùng DB là bằng chứng chống chối bỏ tuyệt đối; quy định quyền sửa/xóa và lưu trữ thực tế.

## 8. API, UI và dữ liệu cũ

### 8.1. UI/API

- Bổ sung UI đề nghị/xem kết quả xác minh thẩm quyền; Admin UI thẩm định; lịch sử cấp/thu hồi/thay thế.
- Bỏ chọn Signer tự do ở lời mời/đổi role thông thường; vẫn chặn cùng payload tại backend.
- Contract detail hiển thị Maker, Checker, thẩm quyền và version/nội dung được duyệt; phân biệt chưa trình, chờ duyệt, bị trả lại, đã duyệt và dữ liệu lịch sử thiếu chứng cứ.
- Backend trả capability và lý do bị chặn phù hợp; UI ẩn/khóa nút theo capability thay vì chỉ `Admin || Procurement`.
- UI phát hiện conflict/stale version, tải lại hồ sơ và yêu cầu xem bản mới; không tự retry phê duyệt bản khác hoặc mock thành công.
- Đặc tả DTO, validation, response/error code; thiếu quyền dùng 403, xung đột version/trạng thái/cạnh tranh dùng contract nhất quán (thường 409), không lộ thông tin chéo đơn vị.
- Chưa chốt PIN/OTP/passkey/chữ ký số/USB token làm cơ chế ký; không tự đưa chúng thành điều kiện phụ thuộc của bản sửa này.

### 8.2. Migration/backfill

- Giữ nguyên identity, password và lịch sử; không viết lại người duyệt cũ cho giống quy trình mới.
- Membership Signer/owner/Manager có sẵn không tự thành authority đã xác minh; có trạng thái cần đối soát/xác minh, ghi rõ tác động nghiệp vụ và danh sách cần xử lý.
- Không tạo chứng cứ, VerifiedBy hoặc ngày hiệu lực giả từ dữ liệu đăng ký cũ. Hồ sơ cũ thiếu snapshot/đủ hai người phải hiển thị đúng giới hạn, không tự nâng thành bản đạt chuẩn.
- Backfill quan hệ đơn vị cần xác định duy nhất; báo các trường hợp không xác định được thay vì chọn tùy ý.
- Chuẩn bị migration SQL Server và kế hoạch nâng cấp dữ liệu hiện có, backup/khôi phục và xử lý dữ liệu không hợp lệ. Không tự chạy trên database gốc hoặc restart để áp dụng migration.
- Sửa blocker migration 1785 trong thay đổi riêng, xác định lịch sử migration đã áp dụng trước khi chọn cách sửa; không mặc định chỉnh file migration cũ sẽ sửa được DB đã apply. Kiểm cả clean-install và upgrade.

## 9. Kiểm thử bắt buộc cho bản sửa

Mỗi test phải ghi snapshot code, môi trường/DB, actor, request, response và trạng thái/audit sau request; phân loại PASS / FAIL / BLOCKED / NOT RUN. Kết quả mục 3 không được chép thành kết quả của bản sửa mới.

| ID | Trường hợp | Kết quả cần đạt |
|---|---|---|
| A01 | Người đăng ký pháp nhân/chủ hồ sơ ban đầu duyệt khi chưa được xác minh quyền ký | Bị chặn. |
| A02 | Manager mời Signer hoặc sửa payload role thành Signer | Không tự tạo quyền ký; không có authority hợp lệ phát sinh. |
| A03 | Manager tự nâng quyền hoặc dùng tài khoản thứ hai để được cấp Signer thông thường | Bị chặn; cần quy trình xác minh độc lập. |
| A04 | Giả VerifiedBy/status/phạm vi/thời hạn qua request thường | Backend từ chối/không nhận trường không được phép; audit đúng. |
| A05 | Xác minh thiếu căn cứ, người xác minh trùng người nhận quyền, sai đơn vị | Bị chặn. |
| A06 | Authority pending/rejected/expired/revoked hoặc sai scope | Không được duyệt, kể cả JWT còn hạn. |
| A07 | Authority hợp lệ nhưng membership bị thu hồi/đình chỉ | Command bị chặn. |
| B01 | Admin gọi trực tiếp technical-review/acceptance/accept và đường chuyển trạng thái liên quan | Không có quyền nghiệp vụ từ Admin hoặc đường bypass. |
| B02 | Creator/owner/Manager không có quyền kỹ thuật/ký riêng | Không được thực hiện bước tương ứng chỉ nhờ thuộc tính cũ. |
| B03 | Một UserId có cả quyền kỹ thuật và quyền ký, cố tự duyệt | Bị chặn ở mọi endpoint, không ngoại lệ. |
| B04 | Officer có quyền kỹ thuật; Signer được xác minh khác UserId | Đúng scope thì review/approve thành công, lưu đủ actor/authority/version. |
| B05 | Sửa entity/contract/milestone/file ID sang đơn vị khác | Bị chặn; không lộ chứng cứ hoặc thông tin hồ sơ. |
| C01 | Đổi Amount/Title/DueDate sau technical review bằng API | Khóa sửa hoặc trả về revision mới; review cũ không dùng để duyệt nội dung mới. |
| C02 | Thay bytes/file/chứng cứ, phụ lục hoặc dữ liệu contract ảnh hưởng snapshot | Yêu cầu duyệt cũ không được âm thầm áp dụng cho nội dung mới. |
| C03 | Duyệt version cũ sau khi hồ sơ đã sửa/trả về | Bị chặn; UI yêu cầu xem phiên bản mới. |
| C04 | Hai request approve đồng thời cùng version | Chỉ một kết quả nghiệp vụ hiệu lực; audit/tổng tiền/notification không trùng. |
| C05 | Approve cạnh tranh với sửa hồ sơ hoặc revoke authority | Không chấp nhận trạng thái/phiên bản/quyền đã mất hiệu lực; có bằng chứng interleaving thật. |
| C06 | Retry/duyệt lại hồ sơ đã Approved | Không tạo hiệu ứng thứ hai. |
| C07 | Sửa/xóa snapshot, actor, audit cũ qua CRUD thông thường | Bị chặn; thao tác điều chỉnh giữ liên kết lịch sử. |
| C08 | Lỗi lưu audit hoặc lỗi giữa transaction | Không tồn tại kết quả nghiệp vụ đã duyệt nhưng thiếu audit tương ứng. |
| D01 | DB cũ có Signer/owner/creator và acceptance thiếu chứng cứ | Không tự cấp authority; dữ liệu lịch sử giữ đúng sự thật và có danh sách đối soát. |
| D02 | SQL Server DB hoàn toàn mới | Migrations + seed chạy được không cần tạo bảng/FK thủ công. |
| D03 | Upgrade bản DB trước thay đổi với dữ liệu đại diện | Không mất dữ liệu, không ngầm nâng quyền, constraints/backfill đúng. |
| E01 | Browser: mời/nhận lời, đề nghị/xác minh authority, hiển thị quyền và conflict | UI thực sự đi qua API, các role nhìn/thao tác đúng; không báo browser PASS từ API test. |
| E02 | Mở thầu → nộp hồ sơ → đóng/mở đánh giá → trao thầu → hợp đồng → nghiệm thu đủ mốc → Completed | Chạy lại toàn tuyến với Signer đã xác minh, Maker khác người; SQL đối chiếu actor/version/tổng tiền và audit. |

Unit/service test phù hợp để kiểm chính sách; dùng SQL Server thật cho constraints, migration và concurrency; dùng trình duyệt cho luồng UI. Không gọi fixture/SQLite/unit là browser E2E. Kiểm thử workflow không tự chứng minh nghiệm thu hiện trường là có thật; hệ thống cần giữ chứng cứ và trách nhiệm cá nhân để đối soát.

## 10. Môi trường và ranh giới thao tác

- Người dùng đã hỏi vì sao lượt trước không dùng CSDL gốc; lần tiếp theo phải nêu rõ DB/API/UI mục tiêu và dữ liệu/cleanup trước khi mutation test, không âm thầm chọn sandbox rồi báo như test trên DB đang dùng.
- Câu hỏi trước đó không tự là lệnh ghi DB gốc. Dùng phạm vi đã được người dùng giao trong phiên triển khai; nếu đã cho phép DB gốc và các thao tác cần thiết thì tiếp tục theo phạm vi đó, không hỏi lại.
- Migration/restart có tác động riêng vì startup chạy MigrateAsync/seed. Làm rõ đúng database/process trước khi chạy; không restart tiến trình của người dùng để lấy code mới theo suy đoán.
- Không gửi email ra ngoài khi chưa được giao; SMTP sink cục bộ phải được ghi là mô phỏng nhận thư, không báo email live thành công.
- Với DB gốc được giao kiểm thử, dùng dữ liệu test nhận diện rõ và kế hoạch bảo toàn/cleanup; không xóa lịch sử audit để làm báo cáo sạch hoặc reset dữ liệu đang dùng. Không restore đè toàn DB sau test khi có thể làm mất thay đổi khác.
- Không thay schema bằng tay để vượt lỗi rồi công bố migration PASS. Nếu cần workaround để điều tra, ghi riêng và không dùng kết quả đó làm điều kiện nghiệm thu clean migration.

## 11. Trình tự triển khai và sản phẩm bàn giao

1. Xác minh root/branch/HEAD/diff, source/backend contract và migrations hiện tại; lập permission matrix, state machine, dữ liệu authority/version và chính sách chuyển dữ liệu cũ.
2. Triển khai authority bootstrap/xác minh/thu hồi và khóa các đường cấp Signer thông thường; giữ onboarding tài khoản cá nhân.
3. Chuyển scope về pháp nhân; dùng policy backend chung, bỏ các nhánh Admin/creator/owner/Manager cấp quyền duyệt và mọi ngoại lệ Maker = Checker.
4. Triển khai snapshot/revision, trả về chỉnh sửa, concurrency/transaction/audit; rà tất cả mutation có thể thay nội dung được duyệt.
5. Cập nhật DTO/enum contract/API client/UI và hiển thị legacy/conflict/capability.
6. Xử lý migration blocker, kiểm clean/upgrade SQL Server trong môi trường được xác định; chạy test âm, concurrency, browser và toàn tuyến.
7. Bàn giao danh sách file thay đổi, quyết định thiết kế, migration/backfill, test thực thi cùng bằng chứng, các mục FAIL/BLOCKED/NOT RUN và việc vận hành còn cần đối soát. Không tuyên bố đã giải quyết minh bạch chỉ vì happy path chạy hết.

## 12. Prompt chuyển giao

> Tiếp nhận HANDOFF_AGENT_TRANSPARENCY_CONTROLS_2026-10-28.md và handoff membership ngày 26/10 trong repo Procurement & Contract Tracking System. Hoàn thiện ba nút thắt: xác minh/cấp thẩm quyền ký độc lập với quản lý thành viên; bỏ quyền duyệt suy ra từ Admin, CreatedBy, owner hoặc Manager; gắn technical review/approval vào snapshot và phiên bản được kiểm tra nguyên tử. Bảo toàn dirty tree hiện có. Người đăng ký không tự thành người ký; Manager không tự phong Signer; Admin nền tảng không duyệt nghiệp vụ của doanh nghiệp; Maker và Checker luôn khác UserId. Đặc tả permission matrix/state machine/authority/revision/backfill theo source rồi triển khai BE và FE. Giữ lịch sử thật, kiểm quyền hiện hành, scope, expiry/revocation và cạnh tranh. Rà endpoint/alias/đổi trạng thái/cập nhật tài liệu để chặn đường đi vòng. Kết quả API/SQL E2E ngày 27/10 chỉ là baseline có giới hạn, không phải kết quả của bản sửa. Nêu rõ database/process mục tiêu và phạm vi đã được giao trước mutation/migration/restart; không tự ghi DB gốc, gửi email ra ngoài, commit/push/merge/deploy. Chạy kiểm thử phù hợp và báo PASS/FAIL/BLOCKED/NOT RUN bằng bằng chứng thực thi, gồm clean migration, upgrade, các test âm/concurrency và browser/full lifecycle.
