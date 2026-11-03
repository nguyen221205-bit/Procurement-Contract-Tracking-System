# Handoff sửa Transparency Controls — vòng 4

Ngày bàn giao: 29/10/2026. Viết implementation notes và báo cáo kết quả bằng tiếng Việt.

## 1. Mục tiêu, baseline và tài liệu

Sửa 5 phát hiện còn lại sau review `walkthrough_transparency_review_round3_fixes_2026-10-29.md`. Ưu tiên hai lỗi P1 ở R4-01/R4-02, sau đó hoàn thiện bằng chứng kiểm thử R4-03/R4-04/R4-05. Không quay lại tài khoản Procurement dùng chung; giữ tài khoản cá nhân, membership Active, quyền ký được xác minh độc lập và Maker khác Checker.

Repository root: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.
Baseline đã review: branch `dev`, HEAD `0446e500f1188b8fc27311450672fde6a3be68a1`, có nhiều thay đổi chưa commit. HEAD không đại diện đầy đủ source được review. Trước khi sửa, đọc hướng dẫn repository hiện hành và ghi nhận root/branch/HEAD/dirty tree. Bảo toàn thay đổi tracked/untracked của người khác.

Đối chiếu:

- `HANDOFF_AGENT_TRANSPARENCY_REVIEW_ROUND3_2026-10-29.md`: yêu cầu T01–T06.
- `C:\Users\HP\.gemini\antigravity\brain\46b1c7d8-1233-4c14-a1dc-d59e92e8bec9\walkthrough_transparency_review_round3_fixes_2026-10-29.md`: walkthrough vừa review.
- Các mã R4-01 đến R4-05 trong file này là phát hiện vòng 4; không đổi tên các mã T/F cũ trong lịch sử tài liệu.

## 2. Bản đồ phạm vi để bắt đầu sửa

Đường dẫn dưới đây tính từ repository root; số dòng chỉ là mốc snapshot review, ưu tiên tìm theo hàm vì source có thể đã dịch chuyển.

| Mục | Mức | Phạm vi sửa chính | Phạm vi phụ thuộc cần rà | Kết quả cần đạt |
| --- | --- | --- | --- | --- |
| R4-01 | P1 | `ProcurementSystem.Infrastructure/Entities/BidPackage.cs`, `Entities/Contract.cs`; `Services/ContractService.cs` → TechnicalReviewMilestoneAsync, ApproveMilestoneAcceptanceAsync, GetAcceptanceSnapshotDocumentDownloadAsync | BidPackageService.CreateBidPackageAsync/UpdateBidPackageAsync; DTO create/update/read package; entity configuration, migration mới; FE tạo gói nếu cần chọn đơn vị | Đơn vị quản lý được gắn cố định vào dữ liệu nghiệp vụ, không đổi theo membership hiện tại của creator |
| R4-02 | P1 | `ProcurementSystem.Infrastructure/Services/ContractService.cs` → fallback entity lookup khoảng 1278–1283 và 1474–1479 | Dùng chung quan hệ/entity resolver đã sửa tại R4-01, regression tests với Officer tạo gói | Không còn exception Include vòng trong no-tracking; luồng nhân viên tạo gói chạy được |
| R4-03 | P2 | `tests/ProcurementSystem.Tests/SqlServerConcurrencyAndIntegrityIntegrationTests.cs` → các T06 tests | Test hooks/interceptors/fakes riêng cho integration; các đường approve/revoke/edit/upload nếu test phát hiện lỗi thực | Chứng minh interleaving thực, không gọi tuần tự rồi đặt tên race |
| R4-04 | P2 | Cùng file SQL Server test → T03_UpgradeForwardMigration_RepairsLegacyAcceptancesAndVersions | Migration 20261029160000, fixtures schema/history cũ, EF IMigrator | Chạy migration thật trên DB ở trạng thái trước forward migration |
| R4-05 | P2 | Cùng file SQL Server test → CheckLocalDbAvailableAsync và đầu mỗi Fact; test T01 download matrix | Test runner/CI config, HTTP test host nếu thêm; báo cáo walkthrough | Thiếu SQL Server phải Skip/Fail đúng; controller probe không bị gọi là HTTP integration |

Chỉ mở rộng file ngoài bảng khi có dependency trực tiếp và ghi rõ lý do. Không sửa evaluation/scoring/enum/dashboard hoặc format toàn repository chỉ để làm đẹp diff. Không thay đổi quyền Admin duyệt chi; Admin vẫn chỉ có quyền đọc kiểm toán theo policy hiện có.

## 3. R4-01 — Đơn vị quản lý thay đổi theo membership của creator

**Bằng chứng tái hiện:** download service tại khoảng dòng 2810–2825 xác định đơn vị bằng BidPackage.CreatedBy: tìm đơn vị owner, nếu không có thì lấy membership Active đầu tiên. BidPackage/Contract chưa có quan hệ entity ổn định dùng cho kiểm tra này. Với package do Officer tạo trong A, sau khi Officer bị revoke A và tham gia B, không thay đổi hợp đồng/gói thầu, probe ghi nhận:

```json
{"probe":"creator_moves_entity_without_contract_change","beforeStatus":403,"afterStatus":200,"entityBDownloadAllowed":true,"originalEntitySignerStatus":403}
```

Đây là lỗi authorization/data ownership: người B có thể tải tài liệu lịch sử của A, còn người A bị mất quyền. Không được sửa bằng OrderBy membership, cache lựa chọn đầu tiên hoặc giữ membership người đã nghỉ việc Active.

**Hướng sửa:** gắn ProcuringEntityId ổn định, có FK, vào gói thầu; hợp đồng có thể kế thừa qua package thay vì thêm cột trùng lặp. Nếu chọn lưu cả hai, phải bảo đảm invariant khớp nhau. CreatedBy chỉ ghi người tạo, không thay thế đơn vị sở hữu. Lúc tạo gói, xác nhận người tạo có membership Active/quyền tạo tại đơn vị được chọn, không tin entityId do FE gửi lên. Nếu có nhiều đơn vị, yêu cầu chọn rõ đơn vị hợp lệ; không âm thầm FirstOrDefault. Việc đổi đơn vị của gói đã có hồ sơ/hợp đồng không được diễn ra qua update thông thường.

**Rà nơi sử dụng:** Maker review, Checker authority/scope, snapshot download phải dùng cùng entity của gói/hợp đồng. Rà các filter/quyền đọc/ghi liên quan trực tiếp để tránh mỗi hàm tự đoán entity khác nhau. Có thể gom helper/service theo quan hệ ổn định, không chỉ thay logic download.

**Migration/backfill:** thêm forward migration, không chỉ sửa migration đã apply. Dữ liệu cũ cần provenance đáng tin để gán entity. Creator hiện thuộc một đơn vị không đủ chứng minh hợp đồng lịch sử thuộc đơn vị đó. Nếu thiếu nguồn xác định hoặc có nhiều khả năng, ghi nhận cần đối soát và fail closed cho thao tác nhạy cảm; không tự gán membership hiện tại hoặc bịa lịch sử. Ghi rõ schema/policy trước khi backfill; không xóa dữ liệu để né ambiguity.

**Nghiệm thu:** A/B độc lập, cùng creator có thể chuyển/thu hồi/tham gia nhiều đơn vị. Đổi membership hoặc revoke creator không làm đổi đơn vị của hợp đồng A; B không tải/review/duyệt hợp đồng A; thành viên A đủ quyền vẫn làm được. EntityId không hợp lệ hoặc user không thuộc entity bị chặn khi tạo gói. Owner tạo gói và Officer tạo gói đều chạy được. Dữ liệu legacy mơ hồ được đối soát minh bạch, không tự mở quyền.

## 4. R4-02 — Officer tạo gói làm truy vấn review/approve ném exception

**Bằng chứng tái hiện:** khi CreatedBy là Officer, không phải owner entity, TechnicalReviewMilestoneAsync đi vào truy vấn:
`Include(m => m.ProcuringEntity).ThenInclude(p => p.Members).AsNoTracking()`.
EF ném InvalidOperationException: `The Include path 'ProcuringEntity->Members' results in a cycle. Cycles are not allowed in no-tracking queries; either use a tracking query or remove the cycle.`

Approve có cùng cấu trúc fallback. Probe đã thực thi lỗi review; lỗi ở approve được xác định từ source, chưa coi là đã chạy qua HTTP. Fixture hiện chủ yếu owner tạo package nên các test xanh không phủ trường hợp này.

**Hướng sửa:** dùng entity FK từ R4-01, chỉ truy vấn dữ liệu/quyền cần thiết; bỏ fallback đoán đơn vị và Include vòng. Không catch exception rồi chuyển thành thành công hoặc trả Forbidden che lỗi. Không chỉ bật tracking toàn bộ graph để giữ cách xác định entity sai. Dùng query có mục đích rõ, không cần load cả Members nếu chỉ kiểm tra Any/First theo entityId/userId.

**Nghiệm thu:** tạo gói bằng Officer thông qua API có entity hợp lệ; thực hiện technical review, Checker approve/reject, download snapshot tới cuối luồng. Kiểm tra bằng context/request mới, không dựa vào navigation đã seed cùng tracker. Không có 500/exception Include vòng; owner flow vẫn hoạt động, membership ngoài entity vẫn bị chặn.

## 5. R4-03 — Chưa chứng minh concurrency ở điểm tranh chấp

**Bằng chứng source:** T06_ApproveVsRevokeMember khoảng dòng 503 await revoke hoàn tất trước rồi mới gọi approve. Đây là after-revoke regression, không phải race. Test approve/approve đặt Barrier ở trước service call (khoảng 440/454), chưa ép cả hai cùng đọc điều kiện trước lúc ghi. Walkthrough khẳng định có test revoke authority, nhưng file hiện không có test đó. Ma trận approve/edit/upload và review/review trong bàn giao trước cũng chưa được chứng minh.

**Hướng sửa:** giữ test tuần tự với tên/mô tả đúng; thêm SQL Server tests bằng connection/context độc lập và hook/interceptor kiểm thử để pause đúng điểm sau khi đọc quyền/revision, trước SaveChanges. Hook phải chỉ dùng trong test/DI phù hợp, không thêm endpoint điều khiển tranh chấp vào production. Có timeout rõ để không treo suite. Chứng minh cả request tới điểm đồng bộ, không chỉ Task.WhenAll.

**Ma trận:** approve/approve; approve/revoke membership; approve/revoke authority; approve/edit milestone; approve/upload scan; review/review. Kiểm tra thứ tự quyết định hợp lệ, không có approve sử dụng quyền đã bị thu hồi trước nó theo cơ chế tuần tự hóa đã chọn. RowVersion membership/authority hiện có là cải thiện cần giữ, nhưng không tuyên bố đủ mọi race nếu chưa test.

**Assertions:** một quyết định cho cùng acceptance, không giải ngân hai lần; trạng thái/version/revision và audit đúng trong context mới; lỗi thua là conflict/quyền theo policy, không phải lỗi setup/nested transaction/connection. Audit failure phải rollback toàn bộ. Test fault hiện throw trước base.SaveChanges chỉ chứng minh không lưu khi lỗi trước persist; nếu khẳng định rollback sau business SQL, cần fault injection ở đúng giai đoạn sau business write nhưng trước audit/commit và xác nhận không có partial commit.

## 6. R4-04 — Upgrade test đang chạy SQL sao chép, không chạy migration thật

**Bằng chứng source:** T03 test migrate tới latest khoảng dòng 578, sửa dữ liệu thành version 0, rồi ExecuteSqlRawAsync script sao chép khoảng dòng 594. Test không chứng minh migration discovery/order/history, AddColumn RowVersion hoặc migration thực có áp dụng được trên schema cũ.

**Hướng sửa:** dùng database thử nghiệm mới. Migrate tới `20261028094453_AddTransparencyControlsIntegrityFixes` (chưa chạy 20261029160000), seed dữ liệu legacy đại diện và kiểm tra history chưa có forward migration. Sau đó dùng EF IMigrator/MigrateAsync chạy migration thật tới latest. Không copy nguyên SQL repair vào test thay cho gọi migration. Trường hợp cần schema/defaults chính xác của bản cũ trước khi file bị chỉnh, dùng baseline fixture được ghi nhận rõ; đừng giả rằng file migration đã chỉnh là toàn bộ bản cũ.

**Nghiệm thu:** chạy được clean install và upgrade từ schema trước forward migration; version/revision được repair đúng; pending/legacy policy đúng; RowVersion tồn tại và hoạt động; lịch sử hoàn tất không bị bịa/chỉnh sai. Kiểm tra __EFMigrationsHistory trước/sau và giữ dữ liệu nghiệp vụ. Log command, SQL Server version, migration đầu/cuối, exit code thật. Không sửa history để ép chạy lại migration cũ.

## 7. R4-05 — Không báo PASS khi thiếu môi trường; phân biệt HTTP và controller

**Bằng chứng source:** đầu cả 6 SQL Server tests có `if (!await CheckLocalDbAvailableAsync()) return;`. Runner coi test kết thúc bình thường là passed, nên môi trường không có LocalDB vẫn có thể báo 49/49, skipped 0. Lần review vừa rồi LocalDB đã được kiểm tra kết nối độc lập thành công; phát hiện này là lỗi báo cáo/portability của tests, không khẳng định lần chạy đó đã bỏ qua toàn bộ integration.

**Hướng sửa:** dùng cơ chế Skip có lý do tương thích runner hoặc Fail rõ khi integration bắt buộc. Không catch mọi lỗi kết nối rồi pass. Tách job/command integration nếu cần; lưu số test thực chạy và số skip. Test T01 hiện gọi controller trực tiếp với DefaultHttpContext, không qua routing/JWT middleware/HTTP serialization: ghi đúng là controller integration với SQL Server. Nếu muốn khẳng định HTTP, thêm test host/WebApplicationFactory và gửi HttpClient request có authentication setup rõ.

**Nghiệm thu:** môi trường không có SQL Server => Skip hoặc Fail hiển thị rõ, không có false PASS. Môi trường có SQL Server => chứng minh tests thực thi. HTTP matrix phải phủ unauthenticated 401, sai entity/revoked 403, đúng scope tải file thành công và bytes đúng. Browser E2E chưa chạy thì ghi NOT RUN, không suy từ build hoặc controller tests.

## 8. Bằng chứng gần nhất và phần cần giữ nguyên

Review vừa rồi đã chạy backend 49/49, frontend 6/6 và frontend production build thành công. Có LocalDB 15.0.4382.1 đang kết nối được. Suite xanh chưa bao phủ lỗi R4-01/R4-02, chưa chứng minh đầy đủ các race/upgrade nêu trên. Không ghi các kết quả cũ thành log chạy sau khi sửa vòng 4.

Probe vòng 4 ở ngoài repository: `C:\Users\HP\AppData\Local\Temp\codex-transparency-round3-review-20261029\Program.cs` và `Probe.csproj`. Chạy từ thư mục chứa project với `dotnet run --project Probe.csproj --verbosity quiet`. Chuyển các kịch bản liên quan sang regression tests chính thức; fixture reflection trong Temp không phải công cụ lâu dài. File probe còn có thí nghiệm integrity phụ, không lấy kết quả đó thay thế các tiêu chí chính trong handoff này.

Giữ các phần đã sửa đúng:

- Owner Revoked bị chặn; không có owner/Admin bypass duyệt chi.
- AcceptanceId/ExpectedRevision/ExpectedVersion gắn đúng bản đã xem; stale revision trả 409.
- Reject sai version trả 409; Maker không tự quyết định Checker.
- File hash từ bytes thật; download qua API client có Bearer token, không đưa token lên URL.
- Membership và authority RowVersion; audit decision trong transaction; notification sau commit.
- Forward migration 20261029160000 và fix NO ACTION cho đường clean install; bổ sung forward migration nếu schema tiếp tục thay đổi.

## 9. Thực hiện và báo cáo lại

1. Xác nhận baseline, thêm regression fail cho R4-01/R4-02; chốt schema entity/provenance legacy.
2. Sửa quan hệ entity, create/update/authorization liên quan và migration; loại bỏ Include vòng.
3. Sửa integration harness, thêm race có interleaving, upgrade bằng migration thật và Skip/Fail đúng.
4. Chạy backend tests/build, frontend tests/build nếu sửa FE; chạy SQL Server clean/upgrade, HTTP matrix và các race trên. Có UI flow mới thì kiểm tra browser trên API thử nghiệm riêng.
5. Trả walkthrough mới theo R4-01–R4-05: file/hàm thay đổi, repro trước/sau, command/log thật, cấp bằng chứng, phần NOT RUN/BLOCKED và policy legacy. Nêu rõ migration nào mới, DB nào đã dùng và cleanup trong phạm vi test.

Không tự commit/push/deploy/reset/clean, restart dịch vụ đang chạy, migrate database gốc hoặc stop SQL Server instance dùng chung. Chỉ dùng DB thử nghiệm định danh riêng; startup có thể gọi seeder/MigrateAsync nên kiểm tra connection trước khi mở API. Không sửa/xóa DB gốc để tạo môi trường test. Khi thiếu môi trường, ghi rõ hạng mục chưa chạy; không fabricate logs/results.

## 10. Prompt giao Agent

> Đọc HANDOFF_AGENT_TRANSPARENCY_REVIEW_ROUND4_2026-10-29.md ở repository root. Sửa R4-01–R4-05 theo bản đồ phạm vi và tiêu chí nghiệm thu: cố định đơn vị quản lý gói/hợp đồng bằng quan hệ dữ liệu đáng tin, không suy theo membership hiện tại của creator; sửa Include vòng ở luồng Officer tạo gói; kiểm thử SQL Server race tại điểm tranh chấp, chạy upgrade bằng migration thật và báo Skip/Fail đúng khi thiếu DB. Giữ các kiểm soát đã đúng, bảo toàn dirty tree, không tự thao tác DB gốc/restart/commit/push/deploy. Trả walkthrough với bằng chứng thực thi phân tầng và phần chưa chạy.
