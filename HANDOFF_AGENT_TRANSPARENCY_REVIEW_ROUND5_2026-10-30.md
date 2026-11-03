# Handoff Transparency Controls — vòng 5

Ngày bàn giao: 30/10/2026. Báo cáo và hướng dẫn triển khai bằng tiếng Việt.

## 1. Mục tiêu và baseline

Xử lý 4 phát hiện còn lại sau review Round 4, ưu tiên quyền ghi sau khi thu hồi membership và tính đúng đắn của backfill đơn vị sở hữu. Giữ mô hình tài khoản cá nhân, membership Active theo đơn vị, thẩm quyền ký được xác minh riêng, Maker khác Checker và snapshot bất biến.

Repository: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.
Baseline review: branch `dev`, HEAD `0446e500f1188b8fc27311450672fde6a3be68a1`, working tree có nhiều thay đổi chưa commit. Trước khi sửa, xác nhận root/branch/HEAD, đọc hướng dẫn repository hiện hành và ghi lại dirty tree. Không reset/clean hoặc ghi đè thay đổi của người khác.

Tài liệu liên quan:

- `HANDOFF_AGENT_TRANSPARENCY_REVIEW_ROUND4_2026-10-29.md`: yêu cầu vòng trước.
- `C:\Users\HP\.gemini\antigravity\brain\46b1c7d8-1233-4c14-a1dc-d59e92e8bec9\walkthrough_transparency_review_round4_fixes_2026-10-29.md`: bản implementation vừa review.

Mã R5-01–R5-04 dưới đây là phát hiện vòng 5, không thay thế mã R4/T/F trong lịch sử bàn giao. Các số dòng là mốc snapshot review; tìm theo hàm khi dòng đã dịch chuyển.

## 2. Bản đồ phạm vi sửa

Đường dẫn trong bảng tính từ repository root.

| Mã | Mức | File/hàm sửa chính | Phụ thuộc cần rà | Kết quả cần đạt |
| --- | --- | --- | --- | --- |
| R5-01 | P1 | `ProcurementSystem.Infrastructure/Services/ContractService.cs` → UpdateMilestoneAsync khoảng dòng 809/823, UploadScannedContractAsync khoảng 644/663 | Các thao tác ghi cùng contract/package còn dùng CreatedBy; BidPackageService.UpdateBidPackageAsync; membership authorization helper, transaction/concurrency và tests | Creator đã bị revoke/missing/inactive không còn sửa dữ liệu đơn vị cũ |
| R5-02 | P1 | `ProcurementSystem.Infrastructure/Migrations/20261029173000_AddBidPackageProcuringEntityOwnership.cs` → backfill, đặc biệt SingleEntityMembers khoảng 64–75 | Forward repair migration mới, provenance/đối soát dữ liệu, SQL Server migration tests | Không suy nguồn gốc hợp đồng lịch sử từ một membership xuất hiện sau thời điểm tạo gói |
| R5-03 | P2 | `ProcurementSystem.Infrastructure/Services/BidPackageService.cs` → CreateBidPackageAsync, fallback khoảng 190–205 | Khởi tạo pháp nhân/owner membership, legacy onboarding/repair và create tests | Mọi người tạo gói có membership Active đúng đơn vị, không owner bypass |
| R5-04 | P2 | `frontend/src/components/packages/CreatePackageModal.jsx` → state/form/payload khoảng dòng 107 | `frontend/src/api/procuringEntityApi.js`, `bidPackageApi.js`, API trả danh sách đơn vị người dùng có quyền tạo; FE tests/browser | Người có nhiều đơn vị chọn được đơn vị hợp lệ và gửi ProcuringEntityId |

Chỉ mở rộng file ngoài bảng khi có dependency trực tiếp; giải thích rõ trong walkthrough. Không sửa scoring/evaluation/enum/dashboard hoặc format lại cả repository nếu không phục vụ 4 mục trên.

## 3. R5-01 — Creator đã bị revoke vẫn sửa được dữ liệu

**Bằng chứng đã chạy:** UpdateMilestoneAsync chỉ chặn khi user không phải CreatedBy và không phải Admin. Với gói được gắn ProcuringEntityId cố định, creator là Officer đã bị revoke vẫn nhận kết quả:

```json
{"probe":"revoked_creator_edits_milestone","Success":true,"StatusCode":200,"membership":"Revoked","acceptanceStatus":"ReviewInvalidated","milestoneTitle":"Changed by revoked creator"}
```

Không chỉ là quyền xem: request đã sửa tiêu đề và vô hiệu hóa review. UploadScannedContractAsync còn có kiểm tra CreatedBy tương tự; phần upload được xác định từ source, chưa gọi thực trong probe trên.

**Hướng sửa:** mọi thao tác ghi nghiệp vụ của Procurement phải kiểm tra membership Active và quyền tương ứng tại ProcuringEntityId cố định của package. CreatedBy là thông tin tác giả, không phải quyền vĩnh viễn. Giữ yêu cầu phụ trách/assignment nếu nghiệp vụ cần; không mở quyền sửa cho mọi member chỉ vì cùng đơn vị. Không dùng membership của creator ở đơn vị mới để cấp quyền trên dữ liệu cũ.

Ưu tiên helper/service authorization dùng chung, trả 403 nhất quán khi thiếu quyền. Rà các đường ghi trực tiếp liên quan: thêm/sửa/xóa milestone, upload scan, sửa hợp đồng/gói và đổi trạng thái. Tách quyền quản trị nền tảng theo policy hiện có; không suy Admin thành người duyệt chi. Với thao tác có ảnh hưởng tới review/giải ngân, bảo vệ cả revoke race theo cơ chế transaction/concurrency đã có; chỉ thêm AnyAsync đầu hàm chưa đủ chứng minh an toàn khi revoke chen giữa.

**Nghiệm thu:** creator Active đúng quyền sửa được; creator Revoked/Inactive/missing membership bị chặn 403; người thuộc đơn vị khác bị chặn. Request bị chặn không đổi milestone/version/acceptance, không thay file hoặc ghi audit thành công. Member hợp lệ nhưng không có quyền sửa cũng bị chặn. Có test SQL Server revoke chen giữa lúc sửa/upload nếu thao tác cần nguyên tử; state/file/audit cuối nhất quán. Không làm mất quyền của người phụ trách hợp lệ ở đơn vị cũ khi creator chuyển nơi làm việc.

## 4. R5-02 — Backfill từ membership duy nhất chưa chứng minh nguồn gốc lịch sử

**Bằng chứng SQL Server migration thật:** DB thử nghiệm được migrate tới 20261029160000, seed gói cũ và một membership tham gia muộn, rồi chạy EF migration lên latest. Kết quả:

```json
{"probe":"legacy_backfill_from_membership_joined_after_package","packageCreatedAt":"2020-01-01T00:00:00","membershipJoinedAt":"2026-10-30T10:57:44.6556755","assignedEntityId":2,"membershipEntityId":2}
```

Migration đã gán gói năm 2020 cho đơn vị creator mới tham gia năm 2026. Bảng membership chỉ có một đơn vị không đồng nghĩa có đủ lịch sử nguồn gốc. Việc này có thể mở quyền tài liệu/hợp đồng cho sai đơn vị.

**Hướng sửa:** chốt nguồn chứng cứ đủ tin cậy để gán entity của dữ liệu legacy. Điều kiện thời gian là kiểm tra cần thiết nhưng không tự chứng minh dữ liệu lịch sử đầy đủ. Nếu thiếu provenance hoặc nguồn mâu thuẫn, giữ trạng thái chưa xác định và fail closed cho thao tác nhạy cảm, có danh sách/luồng đối soát có audit. Rà cả nguồn authority và owner, không coi một giá trị duy nhất là chắc chắn khi chứng cứ nguồn không hợp lệ hoặc mâu thuẫn với nguồn khác. Không tự bịa entity/chữ ký/hash/lịch sử membership.

**Hai đường migration bắt buộc:**

1. DB chưa áp dụng migration ownership: ngăn backfill sai trước khi quyền bị mở. Nếu điều chỉnh đường migration chưa phát hành, ghi rõ lý do và tính tương thích.
2. DB đã áp dụng ownership migration cũ: dùng forward repair migration mới và kế hoạch đối soát. Chỉ sửa file cũ không có tác dụng. Không mặc định mọi ProcuringEntityId khác NULL đều đã đúng; cũng không xóa hàng loạt giá trị hợp lệ. Phải phân biệt bản ghi được tạo sau triển khai với bản ghi backfill từ nguồn yếu; nơi không thể phân biệt tự động phải công khai giới hạn và đưa vào đối soát có kiểm soát.

Không tự chạy repair trên DB gốc. Cung cấp script truy vấn/report xác định bản ghi cần xử lý và migration/repair plan review được trước khi triển khai thực tế.

**Nghiệm thu SQL Server:** dùng migrator thật, không copy SQL backfill vào test. Phủ gói trước JoinedAt, lịch sử thiếu/mơ hồ, nhiều đơn vị, nguồn chứng cứ mâu thuẫn, nguồn đủ tin cậy và gói mới có entity hợp lệ. Kiểm tra clean install, upgrade từ trước ownership và repair DB đã nhận backfill cũ. Người đơn vị mới không được quyền trên gói cũ do suy đoán sai; bản ghi đủ chứng cứ không bị mất ownership. Giữ lịch sử quyết định đã hoàn tất trung thực, có audit cho đối soát.

## 5. R5-03 — Owner thiếu membership vẫn tạo được gói qua fallback legacy

**Bằng chứng đã chạy:** với pháp nhân VerificationStatus=Approved và owner không có membership, CreateBidPackageAsync tự tìm ProcuringEntity theo UserId rồi tạo thành công:

```json
{"probe":"owner_without_membership_creates_package","Success":true,"StatusCode":200,"hasMembership":false}
```

**Hướng sửa:** bỏ fallback cấp quyền ngầm tại thời điểm tạo gói. Yêu cầu membership Active với role/quyền tạo đúng đơn vị cho mọi user, kể cả owner. Tách sửa dữ liệu owner legacy thành quy trình khởi tạo/đối soát membership rõ ràng có nguồn xác nhận; không tự tái kích hoạt owner bị revoke, và không biến một nhánh tương thích thành quyền vĩnh viễn. Nếu entityId do client gửi, vẫn kiểm tra quyền trên server. Kiểm tra trường hợp tài khoản có nhiều role/membership để không chọn nhầm đơn vị.

**Nghiệm thu:** owner thiếu membership hoặc membership Revoked/Inactive bị chặn; owner Active đúng quyền tạo được. Officer/Manager Active đúng entity tạo được; role không có quyền tạo bị chặn; entity giả mạo bị chặn. Legacy repair chạy đúng phạm vi, có audit và không phục hồi người đã bị thu hồi. Người thiếu quyền không được nhận 200 chỉ vì đã có hồ sơ pháp nhân Approved.

## 6. R5-04 — UI thiếu chọn đơn vị khi có nhiều membership

**Bằng chứng source:** backend trả 400 yêu cầu ProcuringEntityId khi user có nhiều membership tạo gói. CreatePackageModal chưa có lựa chọn đơn vị trong state/form và payload không gửi trường này. Đây là kết luận source; chưa gọi browser để tái hiện.

**Hướng sửa:** cung cấp danh sách đơn vị user thực sự có quyền tạo gói qua API được scope trên server. UI hiển thị tên đơn vị; một lựa chọn có thể tự chọn rõ ràng, nhiều lựa chọn bắt buộc chọn; không có lựa chọn thì thông báo thiếu quyền và chặn submit. Gửi procuringEntityId trong payload. Không lấy danh sách pháp nhân toàn hệ thống làm danh sách quyền của user, không hardcode ID và không chọn âm thầm phần tử đầu tiên khi có nhiều đơn vị. Backend vẫn là nơi quyết định quyền cuối cùng.

Xử lý loading/error, membership bị revoke sau khi modal mở và reset lựa chọn khi phiên/user thay đổi. Thông tin kỹ thuật như tên field API không cần lộ trong thông báo sản phẩm khi có thể diễn đạt bằng tên đơn vị. Kiểm tra caller hiện có nếu API client đổi shape.

**Nghiệm thu:** user một đơn vị tạo đúng entity; user nhiều đơn vị chọn A/B và gói gắn đúng entity đã chọn; không chọn thì validation rõ; không có quyền thì không submit; membership vừa bị revoke bị server chặn. Thử payload giả mạo qua API. Có test component/API contract thích hợp và browser kiểm tra network payload + kết quả thực. Frontend enum/score tests hiện tại không chứng minh luồng này.

## 7. Bằng chứng review, phạm vi và các phần cần giữ

Lần review Round 4 đã chạy backend 56/56, frontend 6/6 và frontend production build thành công; các probe R5-01/R5-03 là service + SQLite, R5-02 dùng SQL Server LocalDB migration thật trên DB thử nghiệm độc lập. Các số này là baseline trước sửa vòng 5, không được báo thành kết quả sau sửa.

Probe và project giữ ngoài repository:
`C:\Users\HP\AppData\Local\Temp\codex-transparency-round4-review-20261030\Program.cs` và `Probe.csproj`.
Chạy từ thư mục đó bằng `dotnet run --project Probe.csproj --verbosity quiet`. Probe dựa vào reflection fixture và SQL seed của test hiện có nên có thể cần cập nhật theo code mới. Chuyển kịch bản sang regression tests chính thức. DB có prefix `Codex_R4_MigrationProbe_` do probe tạo đã được xóa và kiểm tra không còn sau review.

Giữ các phần đã tốt: FK đơn vị cố định; không cyclic Include; membership/authority RowVersion; AcceptanceId/ExpectedRevision/ExpectedVersion; Maker–Checker cho approve/reject; hash bytes của snapshot; tải qua API client có token; business/audit cùng transaction; notification sau commit. Giữ tests điểm tranh chấp SQL Server, upgrade bằng migrator thật, HTTP/JWT pipeline và Fail rõ khi thiếu SQL Server.

Không tự commit/push/deploy/reset/clean, restart dịch vụ đang chạy, stop SQL Server instance dùng chung hoặc migrate database gốc. Chỉ dùng DB thử nghiệm định danh riêng, kiểm tra connection trước khi startup vì seeder có thể chạy MigrateAsync. Browser cần API thử nghiệm riêng hoặc phạm vi được xác nhận; không dùng dữ liệu gốc để tạo/xóa test records ngoài phạm vi đã cho phép.

## 8. Trình tự và bàn giao lại

1. Xác nhận baseline; thêm regression tests fail cho quyền creator revoked và owner missing membership.
2. Sửa authorization dùng chung cho các đường ghi liên quan, giữ assignment/role policy và bảo vệ revoke race.
3. Chốt provenance và kế hoạch ownership repair; triển khai/test cả trước và sau ownership migration cũ.
4. Hoàn thiện UI chọn entity và API danh sách quyền tương ứng; test payload/authorization.
5. Chạy backend tests/build, frontend tests/build, SQL Server clean/upgrade/repair/concurrency và browser tạo gói theo entity. Báo NOT RUN/BLOCKED đúng phần chưa thực thi.
6. Trả walkthrough theo R5-01–R5-04, ghi file/hàm thay đổi, repro trước/sau, commands/log thật, DB test đã dùng, migration mới, policy legacy và cleanup. Phân biệt source review, SQLite, SQL Server integration, HTTP và browser E2E; không gộp thành PASS chung.

Không tuyên bố hoàn tất chỉ từ suite xanh. Không fabricate logs/results. Nếu dữ liệu cũ không đủ provenance để repair tự động, nêu rõ giới hạn và bàn giao kế hoạch đối soát review được, không tự đoán để làm test qua.

## 9. Prompt giao Agent

> Đọc HANDOFF_AGENT_TRANSPARENCY_REVIEW_ROUND5_2026-10-30.md tại repository root. Sửa R5-01–R5-04 theo bản đồ phạm vi: chặn creator bị revoke sửa milestone/scan và các đường ghi liên quan; không backfill đơn vị hợp đồng lịch sử từ membership duy nhất xuất hiện sau gói; xử lý cả DB đã nhận backfill cũ bằng kế hoạch forward repair/đối soát; bỏ owner bypass tạo gói khi thiếu membership; bổ sung UI/API chọn đơn vị có quyền tạo. Giữ các invariant và kiểm thử tốt của Round 4, bảo toàn dirty tree, không tự thao tác DB gốc/restart/commit/push/deploy. Trả walkthrough với bằng chứng thực thi phân tầng và phần chưa chạy.
