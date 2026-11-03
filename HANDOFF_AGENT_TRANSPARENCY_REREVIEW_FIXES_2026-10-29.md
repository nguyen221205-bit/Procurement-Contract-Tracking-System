# Handoff sửa các vấn đề còn lại sau re-review Transparency Controls

Ngày bàn giao: 29/10/2026. Ngôn ngữ làm việc và báo cáo: tiếng Việt.

## 1. Mục tiêu và phạm vi

Sửa dứt điểm 7 vấn đề bên dưới trước khi tuyên bố hoàn thành R1–R7 hoặc nghiệm thu tính minh bạch. Giữ phương án đã chốt: mỗi cá nhân dùng tài khoản riêng, tham gia đơn vị qua membership/invitation; quyền ký là thẩm quyền được xác minh riêng; Maker và Checker là hai người khác nhau. Không quay lại dùng tài khoản Procurement chung hoặc suy ra quyền ký từ danh xưng Manager/owner/Admin.

Repository: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.

Branch lúc bàn giao: `dev`; HEAD: `0446e500f1188b8fc27311450672fde6a3be68a1`. Working tree đang có nhiều thay đổi tracked/untracked; HEAD không đại diện toàn bộ mã được review. Trước khi sửa, ghi lại trạng thái thực tế và đọc hướng dẫn repository nếu có. Không reset/clean hoặc ghi đè thay đổi của người khác.

Tài liệu đối chiếu:

- `HANDOFF_AGENT_TRANSPARENCY_CONTROLS_2026-10-28.md`: thiết kế gốc.
- `HANDOFF_AGENT_TRANSPARENCY_REVIEW_FIXES_2026-10-28.md`: 7 mục R1–R7 ở vòng review đầu.
- `C:\Users\HP\.gemini\antigravity\brain\46b1c7d8-1233-4c14-a1dc-d59e92e8bec9\walkthrough_transparency_review_fixes_2026-10-28.md`: walkthrough được re-review.

Các mã F01–F07 trong file này là phát hiện còn lại, không thay thế mã R1–R7 cũ.

## 2. Bằng chứng đã có và giới hạn

Kết quả thực thi ở lần re-review trước khi lập handoff này:

| Kiểm tra | Kết quả | Phạm vi chứng minh |
| --- | --- | --- |
| Build API | Exit 0; 0 warning, 0 error | Biên dịch |
| Backend test suite | 39 passed, 0 failed, 0 skipped | Unit/service tests, SQLite in-memory, EnsureCreated |
| Frontend `npm test` | 6 passed | Enum contract và score preview; không phải test UI nghiệm thu |
| Frontend production build | Exit 0; cảnh báo chunk lớn | Bundle build |
| 3 probe độc lập | Tái hiện 3 lỗi F01–F03 | Service + SQLite; không phải HTTP/browser E2E |

Ngày 29/10 chỉ đối chiếu lại vị trí mã nguồn và trạng thái repo để viết handoff; không chạy lại các suite ở bảng trên. Không ghi bảng này thành kết quả mới sau khi Agent sửa.

Probe được giữ tại:
`C:\Users\HP\AppData\Local\Temp\codex-transparency-rereview-50f5145216b94ddb8b83d5e89792ef75\Program.cs`.

Chạy từ thư mục chứa project probe:

```powershell
dotnet run --project ReviewProbe.csproj --verbosity quiet
```

Probe dùng reflection tái sử dụng fixture riêng của test project, vì vậy có thể cần cập nhật nếu cấu trúc fixture thay đổi. Chuyển các kịch bản sang regression test chính thức, không phụ thuộc lâu dài vào thư mục Temp.

Output đã quan sát ở lần re-review:

```json
{"probe":"stale_acceptance_revision","firstRevision":1,"secondRevision":2,"expectedMilestoneVersion":1,"approved":true,"actuallyApprovedRevision":2}
{"probe":"document_hash","reviewSucceeded":true,"hashMatchesFileBytes":false,"hashMatchesPathText":true}
{"probe":"revoked_entity_owner","isEntityOwner":true,"membershipStatus":"Revoked","approvalSucceeded":true,"status":200}
```

Các mục F04–F07 là kết luận từ source, trừ SQL Server 1785 từng được tái hiện ở lần test 27/10. Chưa chạy lại clean/upgrade SQL Server, cạnh tranh thực trên SQL Server hoặc browser E2E cho snapshot sửa mới. Không báo PASS cho các phần đó chỉ từ SQLite hoặc walkthrough.

## 3. Các sửa đổi bắt buộc

### F01 — P1: Owner bị thu hồi membership vẫn duyệt được (R3)

**Bằng chứng:** `ProcurementSystem.Infrastructure/Services/ContractService.cs:1514` dùng điều kiện `!isCurrentActiveMember && pe.UserId != userId`. `SigningAuthorityService.cs:376` có ngoại lệ tương tự. Probe đặt membership của owner thành Revoked, giữ authority Active, rồi duyệt thành công HTTP-style status 200 từ service.

**Sửa:** Mọi Checker, kể cả owner, phải có membership Active đúng đơn vị tại thời điểm quyết định. Loại bỏ owner bypass ở cả service duyệt và helper tìm authority; rà các đường gọi tương đương. Authority Active không thay thế membership. Nếu cần membership owner ban đầu thì khởi tạo minh bạch, không dùng ngoại lệ ngầm.

**Nghiệm thu:** owner Revoked/Inactive hoặc thiếu membership đều bị từ chối, không đổi acceptance/milestone, không có audit thành công. Owner Active + authority hợp lệ + khác Maker vẫn duyệt được. Kiểm tra lại Admin vẫn không có quyền duyệt chi doanh nghiệp mặc định.

### F02 — P1: Request xem revision cũ vẫn ký revision mới (R5)

**Bằng chứng:** `ContractService.cs:1548–1551` lấy acceptance mới nhất. `:1583` chỉ so ExpectedVersion với MilestoneVersion. Hai technical review có note khác nhau vẫn giữ milestone version 1, nên request dựa trên review 1 ký thành công review 2. DTO `ApproveMilestoneAcceptanceRequest.cs` chưa ràng buộc AcceptanceId/Revision. Frontend `contractApi.js` và `ApproveMilestoneModal.jsx` cũng chưa gửi danh tính snapshot chính xác.

**Sửa:** Request phải gắn với acceptance cụ thể và revision/concurrency token của bản Checker thực sự đã xem, đồng thời kiểm tra milestone version. Backend xác nhận acceptance thuộc đúng milestone/contract/entity, còn hiệu lực và chưa bị thay thế. Không tự chuyển request cũ sang latest acceptance. Loại bỏ giá trị version mặc định 1 che giấu dữ liệu thiếu trên FE. Áp dụng ràng buộc này cho cả chấp thuận và từ chối nếu chúng tác động vào bản nghiệm thu đã xem.

**Nghiệm thu:** tạo review 1, mở modal, tạo review 2 chỉ đổi note/Maker mà không sửa milestone, gửi request từ modal cũ => conflict 409, không tác động review 2. Refresh, xem review 2 và gửi đúng token => thành công nếu đủ quyền. Request thiếu token/ID hoặc ID thuộc milestone khác phải bị chặn; UI yêu cầu tải lại và hiển thị đúng revision được quyết định.

### F03 — P1: SHA256 đang băm đường dẫn thay vì nội dung file (R6)

**Bằng chứng:** `ContractService.cs:1344` ghép thêm `wwwroot`, trong khi `FileStorageService` lưu tại `cwd/uploads` và trả `/uploads/...`. Khi không tìm thấy file hoặc gặp exception, nhánh fallback băm chuỗi đường dẫn. Probe với file thật cho `hashMatchesFileBytes=false`, `hashMatchesPathText=true`.

**Sửa:** Sử dụng mapping/storage abstraction thực tế, đọc stream byte vật lý và tính SHA256 trên byte. Khi có tài liệu được tham chiếu nhưng thiếu file/không đọc được, từ chối tạo bằng chứng hợp lệ, không fallback sang hash path. Nếu nghiệp vụ cho phép chưa có tài liệu, biểu diễn trạng thái đó rõ ràng và không giả lập hash. Bảo đảm bytes được lưu và hash tham chiếu cùng một phiên bản bất biến.

**Nghiệm thu:** hash khớp SHA256 của bytes đã upload; cùng bytes ở tên khác có cùng hash; khác bytes phải khác hash. File thiếu/không đọc được không tạo TechnicalApproved với hash giả. Kiểm tra qua upload thực và download bản snapshot, không chỉ fixture path.

### F04 — P1: Thay bản scan không làm mất hiệu lực review đang chờ (R6)

**Bằng chứng:** `ContractService.cs:669–742`, `UploadScannedContractAsync`, đổi ScannedFilePath và giữ file cũ nếu snapshot tham chiếu, nhưng không invalidate/version review đang chờ. Khi duyệt, `:1590–1598` chỉ kiểm tra scalar milestone, không gắn quyết định với thay đổi tài liệu. Giữ file cũ là tiến bộ nhưng chưa bảo đảm Checker biết mình đang ký bản nào.

**Sửa:** Trong cùng đơn vị thay đổi nguyên tử, khi thay tài liệu liên quan đến nghiệm thu, invalidate review đang chờ chịu ảnh hưởng và cập nhật version/token tương ứng; bắt buộc review lại trước khi duyệt. Giữ nguyên lịch sử quyết định đã hoàn tất, không sửa snapshot quá khứ. Modal hiển thị và mở được đúng file snapshot, revision và dấu nhận diện nội dung; không lấy file hiện hành của contract để giả làm file đã review. Xác định rõ policy thay scan theo trạng thái hợp đồng.

**Nghiệm thu:** upload A -> review A -> mở Checker -> upload B -> request cũ bị chặn; review B và request mới được xử lý đúng. File A vẫn tải được từ lịch sử; hash đúng bytes A. Test upload và approve cạnh tranh, không chỉ hai thao tác tuần tự.

### F05 — P1: Chưa chứng minh chống revoke race và tính nguyên tử audit (R2)

**Bằng chứng:** RowVersion đã được thêm cho acceptance/milestone/authority; đây là cải thiện thật. Tuy nhiên, approve đọc membership/authority bằng AsNoTracking rồi chỉ ghi acceptance/milestone. Revoke sau lần đọc chưa được bảo vệ bởi RowVersion của bản authority không bị update. Source ContractService không có BeginTransaction như walkthrough mô tả. `AuditSaveChangesInterceptor` ghi audit tại SavedChangesAsync bằng lần SaveChanges tiếp theo; nếu không có outer transaction phù hợp, business state có thể commit trước audit.

Test `R2_ConcurrentApproval_FirstSucceeds_SecondFailsWithCompletedMessage` tại `tests/ProcurementSystem.Tests/ProcuringEntityMemberAndMakerCheckerTests.cs:1286` await approval đầu xong mới gọi approval thứ hai. Đây là kiểm tra replay tuần tự, không phải race. SQLite randomblob mặc định cũng không mô phỏng đầy đủ SQL Server rowversion khi update.

**Sửa:** Thiết kế transaction/locking hoặc conditional write bảo vệ toàn bộ điều kiện quyết định: membership, authority/scope/hiệu lực, acceptance revision, milestone và audit. Đường revoke và approve phải phối hợp cùng cơ chế; chỉ bọc ReadCommitted transaction không tự loại bỏ read-check-write race. Chốt quy tắc thứ tự: revoke đã commit trước quyết định approve thì approve phải thất bại; nếu approve được tuần tự hóa trước revoke, phải lưu được bằng chứng quyền và thứ tự đó. Audit thất bại phải rollback quyết định nếu audit là phần bắt buộc của hành động.

**Nghiệm thu:** dùng SQL Server, hai DbContext/request độc lập và barrier để tạo interleaving thật: approve/approve, approve/revoke authority, approve/revoke membership, approve/edit milestone, approve/upload file, review/review. Kiểm tra trạng thái cuối, số quyết định/audit và rollback khi inject lỗi audit. Chứng minh request thực sự chồng thời gian; Task.WhenAll không đủ nếu không đạt điểm cạnh tranh cần test. Không báo PASS concurrency bằng test tuần tự.

### F06 — P1: Migration nâng cấp đưa Version về 0, API yêu cầu >= 1 (R1)

**Bằng chứng:** `20261028094453_AddTransparencyControlsIntegrityFixes.cs:26–31` thêm ContractMilestones.Version default 0; `:41–46` thêm Acceptance.MilestoneVersion default 0; Revision cũng default 0, chưa có backfill. Technical review sao chép version 0. Request ExpectedVersion có Range(1, int.MaxValue), nên luồng HTTP bình thường của dữ liệu nâng cấp có thể bị validation chặn.

**Sửa:** Tạo kế hoạch migration/backfill nhất quán cho dữ liệu hiện hữu; version bắt đầu hợp lệ. Phân biệt acceptance legacy với snapshot được xác thực mới. Không bịa người ký, hash, thời điểm xác minh hoặc snapshot cho dữ liệu cũ. Review đang chờ không đủ bằng chứng phải được đánh dấu cần lập lại. Không yêu cầu người dùng sửa nội dung milestone chỉ để tăng version.

**Nghiệm thu:** SQL Server database ở schema trước migration có dữ liệu hợp đồng/milestone/acceptance đại diện -> nâng cấp -> duyệt qua HTTP bằng account hợp lệ mà không sửa milestone -> thành công theo policy mới. Kiểm tra lịch sử legacy trung thực, version/revision nhất quán, migration không mất dữ liệu và không có ExpectedVersion=0 hợp lệ được UI gửi đi.

### F07 — P1: Forward migration chưa giải quyết clean install lỗi SQL Server 1785 (R1)

**Bằng chứng:** migration mới drop/re-add FK Evaluator thành Restrict, nhưng migration cũ `20261012104913_AddIsPriceCriteriaToEvaluationCriteria.cs:72` vẫn tạo cascade gây multiple cascade paths. Clean DB chạy đến migration cũ trước và không thể tới bản sửa mới. Lỗi 1785 đã tái hiện trên SQL Server ngày 27/10; chưa chạy lại trong vòng re-review này.

**Sửa:** Chọn và giải thích chiến lược migration phù hợp lịch sử triển khai: sửa đường clean-install trước điểm lỗi hoặc cung cấp baseline được quản lý chính thức, đồng thời giữ upgrade path cho DB đã tồn tại. Không chỉ thêm forward fix phía sau migration đang fail. Đồng bộ migration/model snapshot, không dùng thao tác sửa tay không được ghi nhận làm điều kiện cài mới.

**Nghiệm thu:** chạy toàn bộ migrations trên SQL Server DB trống từ đầu, không tạo trước bảng để né lỗi. Chạy upgrade trên DB ở schema cũ với dữ liệu đại diện. Kiểm tra FK/delete behavior và migration history sau cùng. Ghi rõ SQL Server version, migration đầu/cuối, command, exit code và log thực tế. Không tác động database gốc để thực hiện các kiểm tra này nếu chưa có phạm vi phê duyệt riêng.

## 4. Giữ các phần đã cải thiện

- Admin bị chặn ở technical review và approve; Maker phải là Officer Active, không suy quyền từ người tạo/Manager.
- Maker khác Checker; ApprovedBy nullable khi chưa có quyết định.
- Technical review tạo revision mới và giữ lịch sử.
- UpdateMilestone truy vấn acceptance trực tiếp để invalidate, không phụ thuộc navigation đã được load bởi cùng DbContext.
- Giữ tài liệu lịch sử được acceptance tham chiếu; hoàn thiện tính bất biến thay vì xóa cơ chế này.

Rà thêm việc xác định đơn vị thực tế của hợp đồng/gói thầu: không khẳng định đã bind entity chỉ vì tìm theo creator hoặc membership đầu tiên. Nếu sửa phần này làm mở rộng phạm vi, trình bày dữ liệu/quan hệ cần bổ sung và test cách ly giữa hai đơn vị.

## 5. Trình tự triển khai và kiểm thử

1. Ghi nhận baseline thực tế; chuyển 3 probe thành regression test fail trước sửa.
2. Sửa F01–F04 và cập nhật đồng bộ DTO/API/FE. Xây cơ chế transaction và thứ tự quyết định cho F05, tránh patch rời từng điều kiện.
3. Hoàn thiện migrations/backfill và kiểm tra cả clean/upgrade SQL Server cho F06–F07.
4. Chạy backend tests/build, frontend tests/build; bổ sung test UI modal revision/file và xử lý 409.
5. Chạy SQL Server race tests có đồng bộ điểm cạnh tranh và HTTP authorization/validation tests.
6. Browser E2E với tài khoản riêng: mời/join thành viên -> phân quyền và xác minh thẩm quyền -> mở thầu, đóng thầu, đánh giá, trao thầu -> hợp đồng -> Maker review -> Checker duyệt các mốc -> hoàn thành hợp đồng. Kèm các nhánh phủ định ở F01–F05, kiểm tra actor/audit/file ở trạng thái cuối.

Chạy các lệnh chuẩn từ đúng thư mục dự án; ví dụ:

```powershell
dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj --no-restore --verbosity minimal
dotnet build ProcurementSystem.API/ProcurementSystem.API.csproj --no-restore --verbosity minimal
```

Trong `frontend`:

```powershell
npm test
npm run build
```

Các lệnh này không thay thế kiểm thử migrations, race hoặc browser. Dùng DB thử nghiệm được định danh riêng và lưu script/fixture tái hiện; không sao chép bí mật/PII vào log. Không tự commit, push, deploy, migrate DB gốc hoặc restart dịch vụ đang chạy. Lưu ý startup có thể gọi seeder/MigrateAsync, phải kiểm tra config DB trước khi mở API thử nghiệm.

## 6. Điều kiện bàn giao lại

Agent trả walkthrough mới với bảng F01–F07: file/dòng thay đổi, cách tái hiện trước/sau, test thực thi, kết quả và phần chưa chạy. Ghi rõ source review, SQLite/service, HTTP, SQL Server integration và browser E2E; không gộp chúng thành một nhãn PASS.

Đính kèm bằng chứng clean/upgrade migration, concurrency có interleaving, rollback audit, hash bytes và request revision cũ bị từ chối. Mô tả thay đổi hợp đồng API và policy legacy. Không tiếp tục tuyên bố có transaction hoặc test đồng thời nếu source/log không chứng minh điều đó. Nếu thiếu môi trường, ghi BLOCKED/NOT RUN cho đúng hạng mục, không fabricate logs hoặc kết quả.

## 7. Prompt gửi Agent

> Hãy đọc file HANDOFF_AGENT_TRANSPARENCY_REREVIEW_FIXES_2026-10-29.md trong repository và sửa F01–F07 theo tiêu chí nghiệm thu. Giữ mô hình tài khoản cá nhân, membership Active, thẩm quyền được xác minh, Maker–Checker và snapshot bất biến. Bảo toàn dirty tree hiện có; không tự commit/push/deploy/restart hoặc migrate DB gốc. Chuyển 3 probe đã tái hiện thành regression tests, kiểm thử SQL Server clean/upgrade và race thực, cập nhật FE/API đồng bộ, rồi trả walkthrough phân biệt rõ các cấp bằng chứng và phần chưa chạy.
