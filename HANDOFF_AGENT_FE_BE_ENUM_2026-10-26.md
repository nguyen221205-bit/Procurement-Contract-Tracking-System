# Handoff cho Agent: sửa ranh giới FE/BE và enum

Ngày bàn giao: **26/10/2026**, múi giờ Asia/Saigon.

## 1. Đọc trước khi tiếp nhận

- Workspace: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.
- Nhánh đã xác minh ngày 26/10: `dev`.
- HEAD/baseline sửa: `0446e500f1188b8fc27311450672fde6a3be68a1`.
- Các thay đổi **đang nằm trong working tree**, gồm file modified và untracked; chưa có commit chứa đợt sửa này. Không checkout/reset/clean làm mất chúng.
- Phạm vi tài liệu này là các sửa FE/BE và enum thực hiện trong chat, không phải toàn bộ lịch sử dự án. Giữ nguyên `HANDOFF_2026-10-26.md` tổng dự án có sẵn.
- Đọc `.agents/rules/procurement-project.md`, `REVIEW_AND_AGENT_RULES.md`, `FE_BE_ENUM_AUDIT_2026-10-25.md` và `FE_BE_IMPLEMENTATION_2026-10-25.md`.
- Các kết quả build/test bên dưới được thực thi trong lượt triển khai **25/10/2026**. Ngày 26/10 chỉ đối chiếu Git/source, soạn handoff và kiểm tra whitespace; **không chạy lại build/test hoặc xác minh runtime live**.

**Trạng thái bàn giao:** đã sửa implementation và có focused tests; chưa nghiệm thu browser/live SQL Server E2E. Ở lần triển khai trước, BE đang chạy chưa được restart. Trạng thái tiến trình hôm nay chưa xác minh, không mặc định server đang phục vụ code mới.

## 2. Mục tiêu và nguyên tắc đã áp dụng

1. BE quyết định dữ liệu được lưu/công bố, điểm chính thức, thống kê, điều kiện chuyển trạng thái và quyền thao tác.
2. FE giữ nhập liệu, định dạng, tương tác, validation sớm và preview chưa lưu có nhãn rõ ràng.
3. Request của các domain có enum số được chuyển sang ID số tại API boundary; danh mục sinh từ enum C#.
4. Giữ quyền sở hữu dữ liệu, sealed bid, tính độc lập của giám khảo và kiểm tra quyền tại BE. Cờ hiển thị ở FE không thay thế authorization của command.

## 3. Những gì đã sửa, theo finding

### F01 — Thay bộ tiêu chí: chuyển từ nhiều request FE sang transaction BE

**Trước:** `CriteriaTemplateModal` xóa từng tiêu chí cũ, có lỗi vẫn tiếp tục, rồi tạo từng tiêu chí mới. Lỗi giữa chừng có thể để lại bộ trộn cũ/mới.

**Sau:** thêm `PUT /api/evaluations/packages/{packageId}/criteria`, dành cho Admin/Procurement. FE gửi một request. BE:

- Validate bộ 1–50 tiêu chí, điểm/trọng số tối đa 2 chữ số thập phân, tổng trọng số bằng 100, tên không trùng và tối đa một tiêu chí giá.
- Kiểm chủ sở hữu hoặc Admin; chỉ cho thay khi gói Open/Closed; chặn nếu đã có điểm.
- So snapshot `expectedCriteria` với toàn bộ tiêu chí hiện tại; lệch trả **409**, yêu cầu tải lại.
- Thực hiện xóa/thêm trong transaction **Serializable**, rollback nếu lỗi, kể cả sau khi đã xóa bộ cũ.
- Đồng thời siết sửa tiêu chí đơn lẻ: chỉ ở Open/Closed.

**File/symbol:**

- `ProcurementSystem.Core/DTOs/Evaluation/ReplaceCriteriaRequest.cs`: `ReplaceCriteriaRequest`, `ExpectedCriteriaDto`.
- `ProcurementSystem.Core/Interfaces/IEvaluationService.cs`: `ReplaceCriteriaAsync`.
- `ProcurementSystem.Infrastructure/Services/EvaluationService.cs`: `ReplaceCriteriaAsync`.
- `ProcurementSystem.API/Controllers/EvaluationController.cs`: action `ReplaceCriteria`.
- `ProcurementSystem.Core/Interfaces/IUnitOfWork.cs`, `ProcurementSystem.Infrastructure/Repositories/UnitOfWork.cs`: transaction nhận isolation level; mặc định các caller cũ vẫn ReadCommitted.
- `frontend/src/api/evaluationApi.js`, `frontend/src/components/packages/CriteriaTemplateModal.jsx`.

### F02 — Bỏ báo cáo tự dựng khi API lỗi

**Trước:** FE fallback sang dữ liệu cục bộ, tự điền MST mẫu, vị trí, vai trò tổ trưởng theo index và kết luận báo cáo.

**Sau:** xóa `buildFallbackReport` và prop `fallbackData`; chỉ dùng report từ BE. API lỗi thì hiển thị lỗi/thử lại. Dùng request identity để response cũ không ghi đè khi đổi gói/đóng modal. Không khôi phục fallback báo cáo chính thức khi xử lý lỗi kết nối.

**File:** `frontend/src/components/evaluation/BidEvaluationReportModal.jsx`, `frontend/src/pages/EvaluationPage.jsx`.

### F03 — Sửa điểm sai thang MaxScore và chuyển breakdown về BE

**Trước:** phiếu chấm/leaderboard tính `score * weight / 100`; màn so sánh lại chuẩn hóa MaxScore. Ví dụ 40/50, trọng số 100% cho 40 ở phiếu chấm nhưng 80 ở BE.

**Sau:**

- Thêm `EvaluationMath`, các trường điểm chuẩn hóa/quy đổi trong `EvaluationScoreDto`, breakdown từng tiêu chí và summary từng giám khảo trên response rankings.
- Breakdown BE dùng giám khảo đang thuộc tổ, loại điểm người cũ; tính điểm giá từ giá dự thầu hợp lệ.
- Khi Evaluating, giám khảo chỉ nhận dữ liệu cá nhân; không nhận tổng điểm/hạng và điểm giá trong breakdown mới. Gói Open bị chặn trả bảng điểm.
- Leaderboard/so sánh dùng breakdown BE. Form chưa lưu dùng `manualScorePreview`, chuẩn hóa MaxScore, ghi rõ phần chấm tay dự kiến và không tự tạo điểm giá.

**File:**

- `ProcurementSystem.Core/Helpers/EvaluationMath.cs`.
- `ProcurementSystem.Core/DTOs/Evaluation/{CriteriaBreakdownDto,EvaluationScoreDto,SubmissionRankingDto}.cs`.
- `ProcurementSystem.Infrastructure/Services/EvaluationService.cs`: `GetPackageRankingsAsync`.
- `frontend/src/utils/scorePreview.js`.
- `frontend/src/components/evaluation/{ScorecardForm,LeaderboardView,BidComparisonView}.jsx`.

### F04 — KPI hợp đồng tính trên toàn query, không tính theo trang

**Sau:** `ContractService.GetContractsAsync` tính aggregate theo cùng bộ lọc và phạm vi quyền trước pagination; trả `ContractListDto` kế thừa pagination, bổ sung `summary`. FE đọc summary thay vì reduce/filter items của trang.

FE cũng tách từ khóa đang nhập khỏi bộ lọc đã áp dụng, chặn response cũ ghi đè và hiển thị dấu gạch khi chưa có summary.

**File:** `ContractListDto.cs`, `ContractListSummaryDto.cs`, `IContractService.cs`, `ContractService.cs`, `ContractsController.cs`, `frontend/src/pages/ContractsPage.jsx`.

### F05 — Đồng bộ notification enum REST/SignalR

**Sau:** helper FE normalize cả tên/số dựa trên enum sinh từ BE; bổ sung trường hợp Success. `Program.cs` cấu hình SignalR JSON converter cùng kiểu với controller REST.

**File:** `ProcurementSystem.API/Program.cs`, `frontend/src/components/common/NotificationBell.jsx`, `frontend/src/pages/NotificationsPage.jsx`, `frontend/src/api/enumContract.js`.

### F06 — Sửa quyền Admin duyệt phụ lục

**Sau:** `ContractAddendumTab` dùng `hasRole('Admin')` thay `user.role` vốn không có trong DTO user; Pending kiểm theo mã trạng thái, không theo nhãn tiếng Việt. BE vẫn kiểm quyền ở command.

**File:** `frontend/src/components/contracts/ContractAddendumTab.jsx`.

### F07 — Bổ sung Cancelled = 5 ở FE

**Sau:** thêm constant, badge tên/số và tab lọc gói đã hủy. Test kiểm mọi trạng thái gói từ enum BE đều có mapping.

**File:** `frontend/src/utils/constants.js`, `frontend/src/pages/PackagesPage.jsx` và test enum.

### F08 — Thống nhất kết quả phân bổ tiền với BE

**Sau:** `ContractDto` trả `allocatedAmount`, `unallocatedAmount`, `isFullyAllocated`, `canAddMilestone`. FE dùng kết quả đó thay ngưỡng chênh dưới 1 đồng; số dư phân bổ hiển thị được phần thập phân.

`unallocatedAmount` là tiền chưa chia mốc; **khác** `totalRemainingAmount` là tiền chưa giải ngân. Không thay thế lẫn nhau.

**File:** `ProcurementSystem.Core/DTOs/Contract/ContractDto.cs`, `frontend/src/pages/ContractDetailPage.jsx`.

## 4. Các thay đổi nghiệp vụ bổ sung

| Nội dung | Thay đổi/file chính |
|---|---|
| Điều kiện mở chấm điểm | `BidPackageDto`/`BidPackageService.GetBidPackageByIdAsync` trả `isCommitteeValid`, `canStartEvaluation`, `evaluationBlockingReasons`, `canEditCommittee`, `requiresEarlyCloseReason`; `PackageDetailPage` dùng dữ liệu BE, kết hợp quyền Admin/chủ gói. |
| Điều kiện trao thầu | `EvaluationSummaryDto`/`GetEvaluationSummaryAsync` trả `canFinalize`, `eligibleSubmissionIds`, có kiểm actor và loại ứng viên có hợp đồng Terminated theo điều kiện đang triển khai. `EvaluationPage`, leaderboard và comparison dùng eligibility BE. |
| Xếp hạng tín nhiệm | Thêm `ContractorRatingPolicy` tại Core; DTO nhà thầu/credit profile và `ContractService` dùng chung. Danh sách FE dùng grade BE; form chưa lưu chỉ thể hiện điểm dự kiến, thông báo sau lưu dùng response. |
| Tiền/tỷ lệ bảo lãnh | `AddGuaranteeModal` gửi Amount làm dữ liệu gốc; BE tính Percentage theo giá trị hợp đồng hiện tại. Client cũ gửi tỷ lệ không khớp bị từ chối. Tổng bảo lãnh hiệu lực dùng trường BE. |
| Gợi ý tiêu chí giá | `criteriaTemplates.js` và `CriteriaModal.jsx` mang flag `isPriceCriteria` rõ ràng, bỏ suy ra flag bằng substring tên tiếng Việt. |

## 5. Contract cần biết để nối FE và BE

### 5.1. Request thay bộ tiêu chí

Payload minh họa cấu trúc, không phải dữ liệu thật để chạy trên gói đang có:

```json
{
  "items": [
    { "name": "Kỹ thuật", "maxScore": 50, "weight": 70, "isPriceCriteria": false },
    { "name": "Giá", "maxScore": 100, "weight": 30, "isPriceCriteria": true }
  ],
  "expectedCriteria": [
    { "id": 123, "name": "Tiêu chí hiện tại", "maxScore": 100, "weight": 100, "isPriceCriteria": false }
  ]
}
```

`expectedCriteria` phải lấy từ dữ liệu thực tế vừa tải, không bịa ID hay gửi rỗng để ghi đè bộ đã tồn tại. Thành công trả `ApiResponse<List<EvaluationCriteriaDto>>`; sai quyền 403, snapshot lệch 409, validation nghiệp vụ thất bại theo `StatusCode` của response.

### 5.2. Trường response bổ sung

- `GET /api/contracts`: `data.summary` gồm `totalContracts`, `activeContracts`, `draftContracts`, `completedContracts`, `totalValue`, `totalDisbursedAmount`, `disbursementRate`; pagination/items vẫn còn.
- Rankings: mỗi hồ sơ thêm `criteriaBreakdown[]` gồm `criteriaId`, `isPriceCriteria`, `averageScore`, `normalizedScore`, `weightedScore`; và `evaluatorSummaries[]` gồm `evaluatorId`, `manualWeightedScore`, `manualMaxWeight`, `hasCompletedScoring`.
- Scores: thêm `normalizedScore`, `weightedScore`.
- Summary đánh giá: thêm `canFinalize`, `eligibleSubmissionIds`.
- Chi tiết hợp đồng/gói: các trường ở F08 và mục 4. Các cờ điều kiện không thay thế kiểm quyền tại BE.

### 5.3. Phạm vi enum đã chuyển và chưa chuyển

- `frontend/scripts/generate-enums.mjs` sinh `frontend/src/api/enums.generated.js` từ **enum số** trong `ProcurementSystem.Core/Enums`. Không sửa generated file bằng tay.
- `axiosClient.js` gọi `encodeRequestEnums` tại request interceptor. Hiện chuyển `BidPackageType`, `BidPackageStatus`, `ContractStatus`, `AddendumType`, `GuaranteeStatus` trên các URL body/query được khai báo trong `enumContract.js`.
- Helper bảo toàn giá trị 0 và từ chối mã không thuộc enum. Bộ lọc gói đã tránh kiểm tra truthy làm mất Open/Goods khi giá trị bằng 0.
- BE thêm `EnumDataType` trên request/filter, `JsonRequired` cho các trường JSON bắt buộc được chọn; `BidSubmissionService` kiểm `Enum.IsDefined` cho loại file multipart trước khi ghi file.
- **Response vẫn giữ tên enum theo contract hiện có** trong đợt này; REST/SignalR dùng `JsonStringEnumConverter`. Không được mô tả là toàn bộ API đã chuyển thành enum số.
- `BidSubmissionStatus` là static const string; verification status, trạng thái đề xuất giám khảo và role code cũng theo DTO/entity dạng chuỗi. Chưa đổi DB hoặc bịa ID cho các domain này. Chưa đổi command phân quyền thành `roleIds`.
- Nếu tiếp tục đổi numeric response, phải kiểm cả DTO có string tường minh, component đọc response và realtime; không xóa converter toàn cục hoặc replace chuỗi hàng loạt.

## 6. Bằng chứng kiểm thử và giới hạn

Kết quả thực thi **25/10**, được chép từ lượt triển khai và báo cáo đã có; chưa chạy lại ngày 26/10:

| Kiểm tra | Kết quả ghi nhận |
|---|---|
| Build solution vào OutDir tạm để không đè binary server đang chạy | Thành công, 0 errors, 0 warnings |
| `dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj --no-restore -v:q` | **21 passed**, 0 failed, 0 skipped; net8.0 |
| `npm test` trong frontend | **6 passed**, 0 failed |
| `npm run build` trong frontend | Thành công; có cảnh báo chunk >500 kB |
| Lint, kiểm kê lần cuối bằng JSON formatter | Exit 0, **0 errors, 248 warnings**; không tuyên bố lint sạch warning |
| `git diff --check` | Không có lỗi; kiểm tra này cũng đã chạy lại khi soạn handoff ngày 26/10 |

**File kiểm thử:**

- `tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj` — đã thêm vào `ProcurementSystem.slnx`.
- `tests/ProcurementSystem.Tests/EvaluationTests.cs`.
- `tests/ProcurementSystem.Tests/ContractAndEnumTests.cs`.
- `frontend/tests/enum-contract.test.mjs`.
- `frontend/tests/score-preview.test.mjs`.

Các test BE dùng service/repository thật và SQLite trong bộ nhớ; test rollback có chèn lỗi insert sau khi xóa bộ tiêu chí cũ. Có test ownership, snapshot, trạng thái khóa, loại file 999, chuẩn hóa điểm, điểm giá, cô lập giám khảo, KPI trước phân trang và scope nhà thầu, readiness, precision và enum bắt buộc/giá trị lạ.

SQLite context test chuyển decimal sang double để hỗ trợ order/aggregate; production SQL Server vẫn decimal. Vì vậy kết quả trên **không chứng minh** SQL Server query translation, deadlock/concurrency hay browser E2E đã qua. Chưa chạy E2E trên phiên người dùng, chưa nghiệm thu luồng live sau restart. Swagger live từng trả lỗi trong lượt audit; chưa có bằng chứng lỗi đó đã được khắc phục.

## 7. Agent tiếp nhận nên làm tiếp theo

1. Xác minh root, branch, HEAD, diff và các file untracked. Đọc source thực tế; không chỉ dựa vào snapshot tài liệu.
2. Chạy lại build/tests theo lệnh dưới; xử lý lỗi mới nếu có, giữ rõ bằng chứng và môi trường thực thi.
3. Xác định tiến trình nào đang giữ cổng 5225/5173 và phiên bản binary đang phục vụ; không tự kết luận source mới đã được load.
4. Trước khi restart, kiểm DB đích và startup behavior: `Program.cs` gọi `DataSeeder.SeedAsync`, trong `DataSeeder.cs` có `Database.MigrateAsync` và seed/SQL. **Không khởi động mù rồi coi đó là thao tác chỉ đọc.** Lần bàn giao này không yêu cầu restart hoặc migration. Chỉ thực hiện thay đổi DB/deploy trong phạm vi được người dùng cho phép.
5. Khi hai phía đã chạy đồng bộ trên môi trường/dữ liệu test được xác định, kiểm live: thay template thành công/thất bại/snapshot cũ; báo cáo lỗi/thành công; điểm giữa hai giám khảo; KPI qua trang/bộ lọc; Admin duyệt phụ lục; Cancelled; enum 0/999; tiền phân bổ còn 0.50.
6. Kiểm riêng transaction/concurrency và query aggregate trên SQL Server. Không coi SQLite regression là bằng chứng tương đương.
7. Ghi PASS/FAIL/BLOCKED/NOT RUN theo từng kiểm tra. Không dùng gói/hợp đồng đã hoàn tất để lặp thao tác ghi nếu chưa có kế hoạch dữ liệu test.
8. Sau kiểm tra, cập nhật báo cáo bàn giao với lệnh/log thực tế. Không tự merge, push, deploy hoặc chạy production migration.

### Lệnh tái kiểm tra focused tests

Chạy trong PowerShell; các lệnh này chưa được thực thi lại trong lượt viết handoff:

```powershell
Set-Location -LiteralPath 'D:\Thuc_Tap_backend\Procurement & Contract Tracking System'
git status --short
git rev-parse HEAD
dotnet restore ProcurementSystem.slnx
$handoffBuildOutput = Join-Path $env:TEMP 'procurement-agent-verification-20261026'
dotnet build ProcurementSystem.slnx --no-restore "-p:OutDir=$handoffBuildOutput/" -v:q
dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj --no-restore -v:q
Set-Location -LiteralPath '.\frontend'
npm test
npm run build
npm run lint
```

Nếu đổi enum C#: chạy `npm run generate:enums` trong frontend và review generated diff trước build. `prebuild` đã kiểm danh mục sinh có khớp nguồn. Lệnh lint được đổi thành gọi Node trực tiếp để tránh shim bị lỗi do đường dẫn workspace chứa `&`.

## 8. Lưu ý về working tree và tài liệu

- Không có migration mới cho đợt sửa này; không sửa JWT/role claims; không commit/push/deploy trong lượt triển khai trước.
- Không có thay đổi DB procurement người dùng do các focused tests; test DB là SQLite trong bộ nhớ.
- `PackageDetailPage.jsx` đã normalize line ending legacy sang LF, nên diff file này lớn hơn thay đổi logic thực.
- Giữ tất cả file test/helper/generated/DTO mới khi chuyển giao; chỉ copy các file tracked modified sẽ thiếu implementation cần thiết.
- `FE_BE_ENUM_AUDIT_2026-10-25.md` là findings trước sửa. `FE_BE_IMPLEMENTATION_2026-10-25.md` mô tả triển khai. Handoff này là điểm bắt đầu cho Agent tiếp nhận, không thay bằng chứng live còn thiếu.

## 9. Prompt có thể giao cho Agent tiếp nhận

> Tiếp nhận working tree trên nhánh dev theo HANDOFF_AGENT_FE_BE_ENUM_2026-10-26.md. Đọc source và diff trước, bảo toàn các thay đổi chưa commit. Xác minh F01–F08, contract request enum số nhưng response còn tương thích, chạy lại focused tests và báo kết quả thật. Kiểm tra riêng runtime BE/FE và điều kiện DB trước khi đề xuất/chạy live test; startup có migration/seed, không coi restart là read-only. Không bịa test logs, không gọi fixture là E2E, không tự merge/push/deploy hoặc production migration. Nêu file/symbol, lỗi còn lại, điều kiện tái hiện và việc cần làm tiếp.
