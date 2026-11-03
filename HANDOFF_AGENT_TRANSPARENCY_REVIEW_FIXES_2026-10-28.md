# Handoff Agent: Sửa 7 phát hiện review Transparency Controls

Ngày bàn giao: **28/10/2026**, Asia/Saigon.

## 1. Nhiệm vụ

Sửa các phát hiện review của bản triển khai theo `thiet_ke_chi_tiet_transparency_controls.md`, rồi kiểm thử lại bằng bằng chứng thực thi. Bản triển khai hiện chưa đủ điều kiện nghiệm thu ba yêu cầu: thẩm quyền ký được xác minh, không có quyền ngoại lệ và phê duyệt đúng phiên bản hồ sơ.

Tài liệu đọc cùng:

- `HANDOFF_AGENT_TRANSPARENCY_CONTROLS_2026-10-28.md`: yêu cầu nghiệp vụ, ma trận quyền, chuyển dữ liệu cũ và tiêu chí nghiệm thu đầy đủ.
- `HANDOFF_AGENT_MEMBERSHIP_INVITATIONS_2026-10-26.md`: tài khoản cá nhân và lời mời gia nhập.
- Đặc tả Agent: `C:\Users\HP\.gemini\antigravity\brain\46b1c7d8-1233-4c14-a1dc-d59e92e8bec9\thiet_ke_chi_tiet_transparency_controls.md`.

Handoff này bổ sung yêu cầu sửa sau review; không thay các yêu cầu còn lại của handoff trước.

## 2. Snapshot và ranh giới bằng chứng

- Repo: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.
- Nhánh: `dev`; HEAD lúc kiểm tra: `0446e500f1188b8fc27311450672fde6a3be68a1`.
- Có nhiều thay đổi modified/untracked từ trước; HEAD không đại diện toàn bộ bản triển khai. Kiểm tra lại source/diff và AGENTS.md nếu có trước khi sửa. Bảo toàn dirty tree, không reset/clean/ghi đè công việc khác.
- Review vừa thực hiện bằng đọc đặc tả, source, EF model/migrations và test code. **Chưa chạy build, test suite, SQL Server hoặc E2E cho bản sửa Transparency Controls mới.**
- Cách tái hiện dưới đây là kịch bản cần thực thi; không coi là log test đã chạy.
- Lượt bàn giao này chỉ tạo file tài liệu; không sửa code, schema, database hoặc restart.
- Giữ phần đã làm đúng: entity/service/API/UI authority, chặn cấp Signer trực tiếp và kiểm tra snapshot scalar. Không quay lại tài khoản Procurement dùng chung.

## 3. Tổng hợp phát hiện

| ID | Mức | Phát hiện |
|---|---|---|
| R1 | P1 | Thiếu migration cho SigningAuthority, snapshot và milestone version. |
| R2 | P1 | Kiểm tra version trong bộ nhớ chưa ngăn duyệt đồng thời hoặc race với sửa/thu hồi. |
| R3 | P1 | Checker không kiểm membership hiện hành; thu hồi thành viên chưa đủ chặn duyệt. |
| R4 | P1 | Các quyền ngoại lệ Admin/creator/owner/Manager vẫn còn ở bước Maker; Checker chưa cấm Admin có authority. |
| R5 | P1 | ExpectedVersion tùy chọn; review có thể bị ghi đè mà không tạo revision mới. |
| R6 | P1 | Snapshot không giữ phiên bản tài liệu; upload scan thay và xóa file cũ. |
| R7 | P2 | UpdateMilestone không nạp Acceptances nên nhánh invalidation không đáng tin giữa các HTTP request. |

Mốc dòng dưới thuộc source ngày 28/10, có thể thay đổi; tìm theo symbol trước khi sửa. Service/entity thuộc `ProcurementSystem.Infrastructure`, DTO thuộc `ProcurementSystem.Core`, controller thuộc `ProcurementSystem.API`.

## 4. R1 — Migration/schema chưa được bàn giao

**Bằng chứng source:** `Data/AppDbContext.cs:37` có DbSet SigningAuthorities, cấu hình tại 471–511. `Entities/Acceptance.cs` có SnapshotAmount/SnapshotTitle/SnapshotDueDate/MilestoneVersion/SigningAuthorityId; `Entities/ContractMilestone.cs` có Version. Bộ migrations và model snapshot hiện chưa có các thay đổi này; migration mới nhất trong checkout vẫn là membership ngày 27/10.

**Hậu quả:** DB được tạo/nâng cấp bằng migrations hiện có sẽ thiếu schema mà model mới truy vấn. Nếu một máy đã sửa SQL bằng tay thì điều đó không làm gói migrations trong repo trở nên đầy đủ.

**Yêu cầu sửa:**

- Hoàn thiện model cho concurrency/revision/document theo R2/R5/R6 rồi tạo migration EF và snapshot tương ứng; không chỉ sửa model snapshot.
- Bổ sung constraints/FK/index phù hợp, xử lý delete behavior cho SQL Server; chuẩn bị script nâng cấp và backfill có kiểm soát.
- Dữ liệu Signer/owner/Manager cũ không tự trở thành authority đã xác minh; acceptance cũ thiếu snapshot/phiên bản phải có chính sách legacy rõ ràng, không tạo chứng cứ giả.
- Xử lý riêng blocker clean migration SQL Server 1785 đã được tái hiện ngày 27/10 ở `20261012104913_AddIsPriceCriteriaToEvaluationCriteria.cs`, liên quan BidPackageEvaluators. Đó là bằng chứng cũ cần kiểm lại, không phải kết quả mới của bản sửa.
- Phân biệt sửa lịch sử migration cho clean install với forward migration cho DB đã apply. Không dùng thao tác tạo bảng thủ công để công bố migration PASS.

**Kiểm thử:** SQL Server DB rỗng chạy đầy đủ migrations/seed; DB ở phiên bản trước nâng cấp được và giữ dữ liệu; API contract detail/authority/review/approve không lỗi thiếu bảng/cột; báo danh sách backfill chưa xác định được thay vì gán tùy ý.

## 5. R2 — Chưa có thao tác duyệt nguyên tử

**Bằng chứng source:** `Services/ContractService.cs:1365–1538` đọc milestone/acceptance/authority rồi cập nhật và SaveChanges. Không có transaction bảo vệ toàn bộ đoạn đọc–kiểm tra–ghi hoặc conditional update theo version. Các entity Acceptance, ContractMilestone, SigningAuthority chưa có concurrency token được cấu hình trong EF. `Repositories/UnitOfWork.cs` chỉ bọc SaveChanges; transaction mặc định riêng của SaveChanges không bảo vệ các lần đọc trước đó.

**Kịch bản:** hai Checker gửi đồng thời cùng phiên bản. Cả hai đọc mốc chưa Completed và authority hợp lệ, cùng vượt qua kiểm tra rồi lần lượt ghi. Có thể cùng báo thành công, ghi đè actor/quyết định và gửi thông báo trùng. Đây chưa phải bằng chứng có hai chuyển tiền ngân hàng.

**Yêu cầu sửa:**

- Dùng cơ chế DB thực sự bảo vệ version/state và quyết định: concurrency token được EF cấu hình, conditional update hoặc transaction/locking thích hợp. Đặc tả cơ chế được chọn và cách xử lý xung đột/deadlock/retry.
- Kiểm tra đầu vào, trạng thái, revision, membership và authority hiện hành trong ranh giới thực thi bảo đảm nhất quán; xử lý race với update/reopen/revoke/expiry.
- Một revision chỉ có một quyết định hiệu lực. Thao tác thua trả HTTP conflict hoặc kết quả idempotent theo contract, không ghi đè người thắng.
- Kết quả nghiệm thu, trạng thái milestone/contract, số tiền và audit phải commit nhất quán; kiểm cả `AuditSaveChangesInterceptor` thay vì chỉ audit viết tay. Notification chạy sau commit và không gửi trùng khi retry.

**Kiểm thử:** dùng SQL Server và hai DbContext/HTTP request độc lập, có điểm đồng bộ để bảo đảm cả hai cạnh tranh thật. Kiểm approve–approve, approve–reject, approve–edit/reopen và approve–revoke. Đối chiếu số quyết định hiệu lực, actor thắng, tổng tiền, trạng thái contract, audit và notification. Không gọi test gửi ExpectedVersion = 99 một lần là concurrent test.

## 6. R3 — Thu hồi membership chưa chặn Checker

**Bằng chứng source:** `ContractService.cs:1413–1428` lọc authority theo entity/user/status/time/type/scope nhưng không yêu cầu membership Active. Helper `SigningAuthorityService.GetActiveDisbursementAuthorityIdAsync` cũng chưa kiểm điều kiện này. RequestAuthority kiểm membership tại lúc đề nghị không thay thế việc kiểm ở lúc ký.

**Kịch bản:** cấp authority hợp lệ cho thành viên, sau đó thu hồi/đình chỉ membership nhưng để authority Active. Dùng token Procurement cũ gọi approve; truy vấn authority vẫn tìm thấy quyền ký.

**Yêu cầu sửa:**

- Mọi quyết định Checker yêu cầu đồng thời membership Active và authority hợp lệ thuộc đúng entity/scope. Không ngoại lệ cho pe.UserId/creator/Manager.
- Kiểm quyền hiện hành phía backend; token/cache cũ không giữ khả năng ký sau thu hồi.
- Authority có thể được thu hồi cùng quy trình quản lý nhân sự theo đặc tả, nhưng không phụ thuộc duy nhất vào cleanup phụ trợ đó để bảo vệ endpoint.
- Dùng một chính sách thống nhất cho capability/UI, service kiểm quyền và command thực thi. Kiểm User/account Active theo chính sách hiện có.

**Kiểm thử:** giữ nguyên JWT, thu hồi/đình chỉ membership, gọi approve và reject: bị chặn, không thay acceptance/milestone/audit thành quyết định hợp lệ. Chạy cùng trường hợp với owner/creator cũ, authority hết hạn/thu hồi/sai scope và race thu hồi theo R2.

## 7. R4 — Quyền ngoại lệ chưa bị loại bỏ đầy đủ

**Bằng chứng source:** `TechnicalReviewMilestoneAsync`, `ContractService.cs:1277–1283`, vẫn dùng `isAdmin || packageCreatedBy == userId`, pe.UserId và Officer hoặc Manager. Không có kiểm phân công kỹ thuật riêng cho Manager. Checker đã bỏ miễn trừ Maker = Checker khi approve, nhưng chưa từ chối platform Admin trước khi xét authority. `SigningAuthorityService.VerifyAuthorityAsync` chỉ chặn Admin tự xác minh cho mình; Admin khác vẫn có thể xác minh authority cho tài khoản đó.

**Yêu cầu sửa:**

- Áp dụng ma trận đã chốt: Admin nền tảng xác minh thẩm quyền, không thực hiện nghiệm thu/duyệt chi với tư cách Admin. Theo đặc tả hiện tại, tài khoản platform Admin phải bị chặn ở command nghiệp vụ tương ứng kể cả đã có authority do người khác cấp.
- Maker phải có membership/quyền kỹ thuật/phân công rõ ràng trong đúng scope. Manager/creator/owner đơn thuần không đủ.
- Maker và Checker luôn khác UserId ở mọi đường xử lý quyết định; rà cả approve/reject, alias `/accept`, `/acceptance` và đổi trạng thái để tránh đường vòng.
- Xác định entity chịu trách nhiệm bằng liên kết dữ liệu trực tiếp và backfill được kiểm chứng. Không tiếp tục lấy entity đầu tiên qua OR theo creator có trong bất kỳ membership nào.
- Kiểm caller thật từ auth context/policy; không nhận actor/role/cờ Admin từ request body.

**Kiểm thử:** Admin không authority; Admin có authority do Admin thứ hai xác minh; Manager không phân công kỹ thuật; creator/owner không còn membership; member có cả hai quyền tự duyệt. Tất cả phải bị chặn ở điều kiện tương ứng. Officer được phân công và Checker khác người có quyền hợp lệ vẫn hoàn thành được luồng.

## 8. R5 — Phiên bản tùy chọn và review có thể bị ghi đè

**Bằng chứng source:** `DTOs/Contract/ApproveMilestoneAcceptanceRequest.cs` có `int? ExpectedVersion`; `ContractService.cs:1471` chỉ so sánh nếu HasValue. `TechnicalReviewMilestoneAsync:1298–1323` tái sử dụng acceptance Pending/TechnicalApproved, ghi đè Maker/time/note/snapshot và gán milestone.Version hiện hành, chưa tạo revision riêng cho mỗi nội dung review.

**Kịch bản 1:** Checker mở v1; Manager sửa và Maker xác nhận v2; gửi request approve bỏ ExpectedVersion. Backend dùng bản mới nhất và có thể duyệt v2 mà Checker chưa xem.

**Kịch bản 2:** Checker mở snapshot v1 của Maker A/nhận xét A; Maker B hoặc Maker A đổi review/nhận xét trên acceptance cũ mà milestone scalar không thay đổi. ExpectedVersion vẫn v1 nên bản cũ không được phát hiện.

**Yêu cầu sửa:**

- Bắt buộc định danh acceptance revision và expected version/token cho quyết định; thiếu dữ liệu trả validation error. Backend không tự chọn latest thay cho phiên bản người ký đã xem.
- Phân biệt milestone content version với acceptance/review revision. Thay người xác nhận, nhận xét hoặc chứng cứ trọng yếu phải tạo revision/tăng token thích hợp.
- Giữ snapshot đã trình; thay đổi dùng thao tác trả về chỉnh sửa có lý do và hồ sơ/revision mới theo state machine đã chốt, không ghi đè review trước.
- Backend từ chối stale revision cho cả quyết định approve/reject theo contract; frontend xử lý conflict bằng tải/xem bản mới, không tự ký bản khác.
- `ContractsController.cs:654–664` hiện chuyển lỗi service ngoài 403/404 thành BadRequest, kể cả StatusCode = 409. Sửa HTTP status mapping nhất quán; kiểm status HTTP thực tế chứ không chỉ trường StatusCode trong unit test.
- Không điền ApprovedBy bằng Maker để lấp trường bắt buộc khi chưa có Checker; thể hiện đúng trạng thái chưa có quyết định.

**Kiểm thử:** thiếu/null expected version; v1 request sau v2; sai acceptance ID thuộc milestone khác; thay Maker/note mà không đổi Amount; retry quyết định cũ; HTTP 409 qua controller; lịch sử revision vẫn giữ actor/nội dung cũ.

## 9. R6 — Tài liệu căn cứ chưa được giữ theo phiên bản

**Bằng chứng source:** Acceptance snapshot chỉ giữ milestone version/amount/title/due date. Trường SnapshotDocumentHash được mô tả trong đặc tả nhưng chưa có trong entity thực tế. `UploadScannedContractAsync`, `ContractService.cs:700–726`, thay ScannedFilePath rồi xóa file cũ; không kiểm quan hệ với snapshot hoặc trạng thái quyết định.

**Kịch bản:** Maker xác nhận khi hợp đồng có scan A. Chủ gói upload scan B; file A bị xóa, snapshot/version milestone không đổi. Checker vẫn có thể duyệt trên review cũ; lịch sử sau đó không truy xuất được chứng cứ A. Rà cả trường hợp đã Approved/Completed.

**Yêu cầu sửa:**

- Lưu tài liệu theo định danh/phiên bản bất biến, giữ bytes của bản được snapshot tham chiếu và metadata/hash để đối chiếu. Không chỉ thêm một hash nhưng vẫn xóa file được duyệt.
- Snapshot ràng buộc entity/contract/contractor, số tiền, tài liệu/chứng cứ và review theo phạm vi handoff gốc; đặc tả đầy đủ trường có ảnh hưởng nội dung quyết định.
- Khi thay tài liệu/nội dung trọng yếu, khóa sửa hoặc tạo revision mới và vô hiệu yêu cầu cũ qua quy trình có lý do; không âm thầm áp dụng review cũ cho bản mới.
- Rà scan contract, attachment nghiệm thu, phụ lục và các API thay/xóa file; file cũ đang có tham chiếu audit/snapshot không được cleanup như file tạm.
- Giữ quyền tải file theo entity/assignment, bảo vệ đường dẫn; không dùng URL tự nhập làm bằng chứng đã có file được xác thực.

**Kiểm thử:** review với scan A, thay bằng B, duyệt revision A; phải bị chặn hoặc vẫn buộc xử lý đúng immutable snapshot theo workflow đã đặc tả. Sau điều chỉnh và sau Completed, vẫn đọc được chứng cứ A của quyết định cũ; dữ liệu cross-entity và bytes bị thay sai version/hash không được chấp nhận.

## 10. R7 — Invalidation phụ thuộc dữ liệu đang được EF track

**Bằng chứng source:** `UpdateMilestoneAsync`, `ContractService.cs:804–808`, chỉ Include BidPackage và Milestones; đoạn 867–868 tìm review trong milestone.Acceptances. Không thấy cấu hình lazy-loading proxies. Navigation collection được khởi tạo rỗng không có nghĩa các acceptance đã được tải từ DB.

**Kịch bản:** HTTP request A thực hiện technical review rồi kết thúc DbContext. Request B mở DbContext mới và sửa milestone. Do không nạp acceptance, không tìm thấy review để đổi ReviewInvalidated/ghi audit. Test `Milestone...` hiện review và update trên cùng setup/DbContext nên relationship fix-up có thể làm test xanh.

**Yêu cầu sửa:**

- Query trực tiếp đúng acceptance/revision hoặc load navigation cần thiết một cách rõ ràng, trong cùng cơ chế bảo vệ R2/R5. Không dựa vào ChangeTracker của request trước.
- Không chỉ thêm Include rồi coi đã giải quyết concurrency; xử lý trạng thái, version, snapshot và audit phải nguyên tử.
- Kiểm chính xác revision hiện hành; không lấy FirstOrDefault tùy ý khi có nhiều acceptance.
- Nếu chuyển sang khóa sửa/trả về chỉnh sửa, điều chỉnh code và test theo workflow đó, bảo đảm request mới vẫn xác định đúng hồ sơ đang chờ duyệt.

**Kiểm thử:** review bằng context/request A, dispose; sửa bằng context/request B; đọc bằng context/request C. Xác nhận kết quả khóa sửa hoặc invalidate đúng revision, có audit và Checker không dùng bản cũ. Chạy trên SQL Server; có thể bổ sung Clear ChangeTracker để phát hiện giả định tracking trong unit test nhưng không dùng nó thay HTTP/SQL test.

## 11. Trình tự sửa và tiêu chí bàn giao

1. Đối chiếu lại 7 phát hiện trên source mới nhất. Nếu đã có thay đổi khác, ghi chính xác diff/bằng chứng khiến phát hiện hết hiệu lực; không sửa dựa vào dòng cũ một cách máy móc.
2. Chốt policy membership/authority/Admin/Maker theo R3/R4 và handoff gốc; chốt identity/revision/state machine/document theo R5/R6.
3. Triển khai backend policy, revision/snapshot, transactions/concurrency và tải dữ liệu R2/R7; cập nhật DTO/controller/UI cùng contract.
4. Hoàn thiện migration R1 sau khi schema cần thiết đã rõ; kiểm clean và upgrade/backfill.
5. Viết/chạy test tái hiện từng phát hiện. Test tương tranh dùng request/context độc lập và SQL Server; test HTTP xác minh status thật; test browser xác minh UI conflict và đúng snapshot được xem.
6. Chạy lại toàn tuyến mở thầu → nộp/chấm/trao thầu → hợp đồng → nghiệm thu đủ mốc → Completed với Maker được phân công và Checker có authority đã xác minh, khác người.
7. Báo mapping R1–R7 → file/diff → test → kết quả. Dùng PASS / FAIL / BLOCKED / NOT RUN, kèm môi trường/DB/code snapshot và hạn chế còn lại.

Không dùng kết quả API/SQL sandbox ngày 27/10 để tuyên bố bản sửa mới PASS; không gọi SQLite EnsureCreated hoặc service unit test là migration/SQL Server/browser E2E. Không coi cập nhật trạng thái/tổng nghiệm thu là chuyển tiền ngân hàng thực tế.

## 12. Ranh giới thao tác khi Agent triển khai

- Bảo toàn source/dữ liệu hiện có; không tự commit/push/merge/deploy.
- Xác định và nêu rõ DB/API/UI mục tiêu cùng dữ liệu test/cleanup trước mutation test. Người dùng đã hỏi vì sao trước đây không dùng DB gốc; không âm thầm lựa chọn môi trường khác rồi báo như test hệ thống đang dùng.
- Câu hỏi đó không tự cấp quyền migrate/restart/ghi DB gốc. Dùng phạm vi người dùng đã giao trong phiên triển khai; không xin lại khi đã có quyền rõ ràng.
- Startup hiện có migration/seed; kiểm database/process trước khi restart. Chuẩn bị script/migration và bằng chứng trong môi trường được giao trước khi đề nghị áp dụng lên DB gốc.
- Không gửi email ra ngoài nếu chưa được giao; dùng SMTP sink khi kiểm thử mô phỏng và ghi đúng giới hạn.
- Không xóa audit hoặc restore đè DB đang dùng để làm sạch dấu vết test; cleanup phải bảo toàn dữ liệu/thay đổi khác.

## 13. Prompt giao việc

> Đọc HANDOFF_AGENT_TRANSPARENCY_REVIEW_FIXES_2026-10-28.md cùng hai handoff trước. Sửa đầy đủ R1–R7 của bản Transparency Controls: migration thiếu; concurrency chưa nguyên tử; thiếu membership hiện hành; quyền ngoại lệ Admin/creator/owner/Manager; expected version tùy chọn và review bị ghi đè; chứng cứ/file không bất biến; invalidation phụ thuộc DbContext tracking. Đối chiếu source mới nhất và bảo toàn dirty tree trên dev. Chốt policy/revision/document model rồi triển khai BE, DTO/controller và FE nhất quán; migration/backfill không tự nâng quyền hoặc tạo chứng cứ lịch sử giả. Test bằng context/request độc lập, SQL Server thật cho concurrency/migration, HTTP cho status và browser cho UI. Kiểm lại cả luồng hợp lệ và các đường vượt quyền/stale/revoke/file replacement. Ghi rõ DB/process mục tiêu và phạm vi đã được giao trước mutation/migration/restart, không tự gửi email ra ngoài, commit/push/merge/deploy. Báo kết quả từng R1–R7 với bằng chứng mới, không dùng test cũ hoặc nhận xét trong code làm bằng chứng đã đạt.
