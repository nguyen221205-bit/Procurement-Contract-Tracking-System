

---

# PHẦN BỔ SUNG – VÒNG REVIEW 3 (sau khi sửa R2‑1 → R2‑12)

> Cập nhật 15/10/2026. Phần này **thay thế** mục C và D ở trên cho các mục R2‑x đã xử lý. `dotnet build` sạch (0 lỗi, 0 cảnh báo). Chưa chạy E2E, chưa kiểm tra nội dung migration mới (backfill `IsPriceCriteria`, bỏ unique `BidPackageId`), chưa review frontend mới.

## G. Trạng thái R2‑1 → R2‑12

| Mục | Kết quả | Ghi chú |
|---|---|---|
| R2‑1 gia hạn thầu | ✅ | `NewDeadline` trong `ChangeBidPackageStatusRequest`; `Closed→Open` cần hạn mới ở tương lai; audit `EXTEND_BID_PACKAGE_DEADLINE` |
| R2‑2 chấm đủ | ✅ | Mỗi giám khảo trong tổ chấm đủ tiêu chí chấm tay; điểm người ngoài tổ bị bỏ qua |
| R2‑3 tiêu chí giá | ✅ | Cột `IsPriceCriteria` + migration; chặn chấm tay tiêu chí giá |
| R2‑4 thu hồi trao thầu | ✅ | `revoke-award`, `disqualify`; có lý do, thông báo, audit; cho tạo HĐ mới khi HĐ cũ `Terminated` |
| R2‑5 Withdrawn/Disqualified | ✅ | Finalize bảo toàn hai trạng thái; hằng trong `BidSubmissionStatus` |
| R2‑6 quyền sở hữu HĐ | ✅ | Mọi thao tác HĐ/mốc/scan/nghiệm thu kiểm `BidPackage.CreatedBy` |
| R2‑7 audit | ✅ | `AuditSaveChangesInterceptor` (Scoped) cho 7 entity, loại trường nhạy cảm |
| R2‑8 email + worker | ✅ | Worker tự đóng gói quá hạn, gửi email qua `EmailService` |
| R2‑9 secrets/CORS | ✅/⚠️ | `appsettings.json` đã bỏ khỏi git; CORS cấu hình `Cors:AllowedOrigins`; secret cũ còn trong lịch sử git |
| R2‑10 file mồ côi | ✅ | Dọn file cũ sau commit, dọn file mới khi lỗi |
| R2‑11 Excel/rating | ⚠️ | Có xuất/nhập Excel, chống CSV injection; Rating tính tay qua endpoint; thiếu báo cáo năm/loại/tỷ lệ |
| R2‑12 độc lập Tổ CG | ⚠️ | Còn lộ điểm qua rankings (R3‑2) và không thay được giám khảo (R3‑3) |

## H. Backlog vòng 3

### 🔴 R3‑1 (P0) – Nhập Excel tạo tài khoản đã duyệt, mật khẩu cố định
- **Vị trí:** `ContractorService.ImportContractorsFromExcelAsync` (~L473‑504).
- **Vấn đề:** hash cố định `"Contractor@123"`, `IsActive=true`, `VerificationStatus="Approved"`, `Rating=5.0` → bỏ qua N10 và thẩm định GPKD; ai biết email là đăng nhập và nộp thầu.
- **Hướng sửa:** mật khẩu ngẫu nhiên (`RandomNumberGenerator`) gửi qua `IEmailService` hoặc link đặt mật khẩu; thêm cột `User.MustChangePassword` (+ migration, chặn gọi API khác đến khi đổi; cập nhật `AuthService`/frontend); `VerificationStatus="Pending"` (hoặc yêu cầu đính kèm GPKD rồi Admin duyệt); `Rating` về mặc định của luồng đăng ký (không 5.0); ghi AuditLog `IMPORT_CONTRACTORS` (số dòng, UserId) không chứa mật khẩu.
- **Test:** tài khoản import không nộp thầu được khi `Pending`; không đăng nhập được bằng `Contractor@123`; yêu cầu đổi mật khẩu lần đầu.

### 🟠 R3‑2 (P1) – Rankings làm lộ điểm giám khảo khác
- **Vị trí:** `EvaluationController` GET `packages/{id}/rankings` (`Admin,Procurement,Evaluator`) và `EvaluationService.GetPackageRankingsAsync` (trả `Scores` mọi giám khảo).
- **Hướng sửa:** truyền `userId` + `isInternalStaff`; khi gói `Evaluating` và người gọi là `Evaluator` (không phải Admin/Procurement): chỉ trả điểm của chính họ, ẩn `TotalScore`/`Rank` của hồ sơ chưa chấm đủ; kiểm người gọi thuộc `BidPackageEvaluator` của gói. Sau `Evaluating` mới công khai đầy đủ. Áp dụng tương tự cho `GetEvaluationSummary`.
- **Test:** Evaluator A không thấy điểm của B qua `/rankings` khi gói `Evaluating`; thấy đầy đủ khi `Awarded`.

### 🟠 R3‑3 (P1) – Không thể thay giám khảo khi `Evaluating`
- **Vị trí:** `BidPackageService.AssignEvaluatorAsync` / `RemoveEvaluatorAsync` (kiểm số lẻ ≥ 3 sau mỗi thao tác đơn lẻ → luôn chặn).
- **Hướng sửa:** thêm `POST /api/bid-packages/{id}/evaluators/replace` (`oldEvaluatorId`, `newEvaluatorId`, `Reason`) thực hiện trong 1 transaction, giữ nguyên số lượng; áp dụng đủ kiểm tra xung đột lợi ích của Assign; điểm đã chấm của người cũ **giữ nguyên để audit nhưng không tính** vào tổng (đã lọc theo `assignedEvaluatorIds`); ghi AuditLog `REPLACE_EVALUATOR`; gọi lại `CalculateRankingsForPackageAsync`. Không nới lỏng quy tắc N4.
- **Test:** thay 1/3 giám khảo → vẫn 3, hồ sơ quay về chưa `Evaluated` cho đến khi người mới chấm đủ.

### 🟠 R3‑4 (P1) – Chọn lại nhà thầu đã bị chấm dứt hợp đồng
- **Vị trí:** `RevokeAwardAsync` khôi phục `Selected/Rejected → Evaluated`; `FinalizeEvaluationAsync` cho chọn lại.
- **Hướng sửa:** khi thu hồi vì HĐ `Terminated`, đặt hồ sơ của nhà thầu đó thành `Disqualified` (lý do "Chấm dứt hợp đồng #…") hoặc chặn Finalize chọn hồ sơ có HĐ `Terminated` của cùng gói trừ khi có `DecisionReason`; tính lại Rank.

### 🟡 R3‑5 → R3‑10 (P2)
| Mã | Vấn đề | Hướng sửa |
|---|---|---|
| R3‑5 | `RevokeAward` xóa cứng HĐ Draft, file scan thành mồ côi | Xóa file vật lý sau commit hoặc chuyển HĐ sang trạng thái hủy (không xóa cứng) |
| R3‑6 | Audit ghi trùng (service ghi tay + interceptor) | Thống nhất: interceptor cho dữ liệu, service chỉ ghi hành động nghiệp vụ; đặt `Action` rõ để lọc |
| R3‑7 | IP audit lấy từ `X-Forwarded-For` (giả mạo được) | Cấu hình `UseForwardedHeaders` với `KnownProxies`; dùng `RemoteIpAddress` |
| R3‑8 | Lý do loại hồ sơ lưu vào `SelectionReason` | Thêm `DisqualificationReason` riêng (+ migration, DTO) |
| R3‑9 | `Rating` chỉ tính khi gọi tay | Gọi `RecalculateContractorRatingAsync` khi HĐ `Terminated`/`Completed` hoặc nghiệm thu bị từ chối |
| R3‑10 | Thiếu báo cáo năm/loại gói, tỷ lệ trúng thầu, tỷ lệ đúng tiến độ, HĐ sắp hết hạn | Thêm endpoint trong `ReportsController` + `ReportService` (chỉ `Admin,Procurement`, có lọc `year`, `type`), đọc `AsNoTracking` |

### 🟢 R3‑11 (P3)
- Thay literal `"Rejected"` còn sót bằng `BidSubmissionStatus.Rejected` (vd. `BidPackageService.ChangeStatusAsync`).
- Đổi `sa`/JWT secret đã lộ trong lịch sử git, hoặc dọn lịch sử.
- Cập nhật README: `revoke-award`, `disqualify`, `import-excel`, `export-excel`, `NewDeadline`, luồng `Awarded`.

## I. Quy tắc bổ sung cho Agent (thêm vào mục A3/A4)

| # | Quy tắc |
|---|---|
| N13 | Không tạo tài khoản với mật khẩu cố định/đoán được; tài khoản hàng loạt phải ở `Pending` và buộc đổi mật khẩu. |
| N14 | Trong `Evaluating`, giám khảo chỉ thấy điểm của chính mình ở **mọi** endpoint (scores, rankings, summary); không phụ thuộc endpoint để né quy tắc. |
| N15 | Số thành viên Tổ CG luôn lẻ ≥ 3 sau **mỗi** thay đổi; thay người dùng API `replace` nguyên tử, không nới lỏng kiểm tra. |
| N16 | Nhà thầu có HĐ `Terminated` của gói không tự động đủ điều kiện được chọn lại. |
| N17 | Không xóa cứng dữ liệu có giá trị pháp lý (HĐ, hồ sơ); nếu bắt buộc xóa thì dọn file vật lý và ghi audit. |
| N18 | Không tin `X-Forwarded-For` trừ khi có proxy tin cậy cấu hình. |
| N19 | Mọi endpoint **trả điểm/xếp hạng** phải được kiểm bằng test "người dùng A không đọc được dữ liệu của B". |

## J. Checklist kiểm thử bổ sung

- [ ] Import Excel: tài khoản mới `Pending`, không đăng nhập bằng mật khẩu cũ, buộc đổi mật khẩu, không nộp thầu được.
- [ ] `/rankings` & `/summary`: Evaluator A không thấy điểm B khi `Evaluating`.
- [ ] Thay giám khảo bằng `replace` giữ tổng lẻ ≥ 3; Assign/Remove đơn lẻ vẫn bị chặn đúng.
- [ ] Revoke sau `Terminated`: nhà thầu cũ không tự chọn lại được.
- [ ] Revoke xóa HĐ Draft không để lại file scan.
- [ ] Audit: một hành động nghiệp vụ không sinh bản ghi trùng gây nhiễu; IP không bị giả mạo qua header.
- [ ] Báo cáo năm/loại/tỷ lệ trả số đúng với dữ liệu seed.

## K. Thứ tự thực hiện đề xuất
1. R3‑1 (P0) → 2. R3‑2, R3‑3, R3‑4 (P1) → 3. R3‑5…R3‑9 → 4. R3‑10 (báo cáo) → 5. R3‑11 và cập nhật README. Mỗi mục: build, migration nếu đổi schema, test kịch bản âm, báo cáo theo mẫu mục F.

## L. Trạng thái Hoàn Thành Backlog Vòng 3 (Cập nhật 15/10/2026)

| Mục | Ưu tiên | Kết quả | Chi tiết hiện thực |
|---|---|---|---|
| **R3-1** | 🔴 P0 | ✅ Đã hoàn thành | Khởi tạo mật khẩu ngẫu nhiên qua `RandomNumberGenerator.GetInt32`, gửi mail qua `IEmailService`, tài khoản mặc định `VerificationStatus = "Pending"`, `Rating = 0`. Thêm `MustChangePassword` vào `User` (+ migration `20261015091936_AddMustChangePasswordToUser`), bổ sung `POST /api/auth/change-password` và `MustChangePasswordMiddleware` chặn toàn bộ API nghiệp vụ (403) đến khi đổi mật khẩu lần đầu. |
| **R3-2** | 🟠 P1 | ✅ Đã hoàn thành | Trong giai đoạn `Evaluating`, Evaluator chỉ xem điểm của chính mình; ẩn `TotalScore`, `Rank`, và các chỉ số tóm tắt (`HighestScore`, `LowestScore`, `AverageScore`) ở cả `GET /packages/{id}/rankings` và `GET /packages/{id}/summary` để bảo đảm tính độc lập khách quan tuyệt đối (N14, N19). |
| **R3-3** | 🟠 P1 | ✅ Đã hoàn thành | Hiện thực `POST /api/bid-packages/{id}/evaluators/replace` (`OldEvaluatorId`, `NewEvaluatorId`, `Reason`) chạy nguyên tử trong 1 transaction, giữ nguyên số thành viên Tổ CG lẻ ≥ 3, kiểm tra xung đột lợi ích, bảo lưu điểm cũ trong DB để audit nhưng loại khỏi phép tính và tự động gọi `RecalculatePackageRankingsAsync`. |
| **R3-4** | 🟠 P1 | ✅ Đã hoàn thành | Thêm cột `DisqualificationReason` vào `BidSubmission` (+ migration `20261015093345_AddDisqualificationReasonToBidSubmission`). Trong `FinalizeEvaluationAsync`: chặn chọn nhà thầu có hợp đồng `Terminated`. Trong `RevokeAwardAsync`: nếu thu hồi vì HĐ `Terminated`, đánh dấu hồ sơ trúng thầu là `Disqualified` kèm lý do chi tiết và xóa `Rank`. |
| **R3-5** | 🟡 P2 | ✅ Đã hoàn thành | Trong `RevokeAwardAsync`: thu thập `ScannedFilePath` của các hợp đồng Draft trước khi xóa bản ghi, và thực hiện xóa vật lý tệp PDF scan sau khi transaction commit thành công, triệt tiêu file scan mồ côi (N17). |
| **R3-6** | 🟡 P2 | ✅ Đã hoàn thành | Phân định ranh giới audit rõ ràng: `AuditSaveChangesInterceptor` tự động ghi nhận CUD dữ liệu cho 7 core entities; các service chỉ ghi nhận sự kiện nghiệp vụ chuyên biệt (`IMPORT_CONTRACTORS`, `REVOKE_AWARD`, `DISQUALIFY_SUBMISSION`, `REPLACE_EVALUATOR`, `EXTEND_BID_PACKAGE_DEADLINE`), loại bỏ bản ghi trùng generic. |
| **R3-7** | 🟡 P2 | ✅ Đã hoàn thành | Cấu hình `ForwardedHeadersOptions` với `KnownNetworks` tin cậy (loopback/proxy nội bộ) và đăng ký `app.UseForwardedHeaders()`. Cập nhật `AuditSaveChangesInterceptor.GetCurrentIpAddress()` sử dụng `RemoteIpAddress` đã chuẩn hóa, loại bỏ đọc trực tiếp header `X-Forwarded-For` chưa kiểm chứng để chống giả mạo IP (N18). |
| **R3-8** | 🟡 P2 | ✅ Đã hoàn thành | Tách biệt lý do truất quyền vào trường `DisqualificationReason` riêng biệt trên `BidSubmission`, cập nhật DTOs (`BidSubmissionDto`, `BidSubmissionSummaryDto`, `SubmissionRankingDto`), bảo đảm `SelectionReason` chỉ dùng cho lý do trao thầu. |
| **R3-9** | 🟡 P2 | ✅ Đã hoàn thành | Inject `IContractorService` vào `ContractService`, tự động gọi `RecalculateContractorRatingAsync(contractorId)` khi: hợp đồng chuyển sang `Terminated` hoặc `Completed` (trong `ChangeStatusAsync`), và khi mốc nghiệm thu bị từ chối hoặc hợp đồng hoàn thành tự động (trong `ApproveMilestoneAcceptanceAsync`). |
| **R3-10** | 🟡 P2 | ✅ Đã hoàn thành | Bổ sung 3 endpoint báo cáo thống kê chuyên sâu trong `ReportsController` + `ReportService` (yêu cầu vai trò `Admin,Procurement`, truy vấn `AsNoTracking`): `GET /api/reports/procurement-summary` (theo năm, loại gói), `GET /api/reports/contractor-performance` (tỷ lệ trúng/trượt thầu, giá trị trúng thầu), và `GET /api/reports/contracts-progress` (tỷ lệ mốc đúng hạn, danh sách HĐ sắp hết hạn/quá hạn). |
| **R3-11** | 🟢 P3 | ✅ Đã hoàn thành | Thay thế triệt để literal `"Rejected"` bằng `BidSubmissionStatus.Rejected` trong `BidPackageService.cs`. Cập nhật tài liệu hướng dẫn kỹ thuật `README.md` với đầy đủ Step 10 mô tả chi tiết các endpoint và cơ chế bảo mật mới. |

---

# PHẦN BỔ SUNG – VÒNG REVIEW 4 (Deep Audit sau Round 3)

> Cập nhật 16/10/2026. Chi tiết toàn bộ 10 phát hiện (R4-1 đến R4-10), giải pháp kỹ thuật, quy tắc Agent N20–N26 và checklist kiểm thử đã được tổng hợp tại tệp riêng: [BACKLOG_R4.md](file:///d:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/BACKLOG_R4.md).

## M. Tóm tắt Danh mục Backlog Vòng 4 (R4-1 → R4-10)

| Mã | Mức độ | Phân hệ | Vấn đề trọng tâm |
|---|:---:|---|---|
| **R4-1** | 🔴 P0 | `BidSubmission` | Lỗ hổng IDOR xem/tải HSDT trái thẩm quyền & rò rỉ điểm số/xếp hạng khi gói đang `Evaluating` (Bypass N14/N19). |
| **R4-2** | 🟠 P1 | `User` / `Role` | Xung đột lợi ích (gán chung vai trò Nhà thầu và Quản lý/Đánh giá); khóa/gỡ role Giám khảo đang chấm thầu làm treo gói thầu; thiếu AuditLog. |
| **R4-3** | 🟠 P1 | `EvaluatorProposal` | Duyệt tạo tài khoản Giám khảo không bật `MustChangePassword = true`, không gửi email; tách rời 2 lần `SaveChangesAsync`. |
| **R4-4** | 🟠 P1 | `Auth` / `Middleware` | Token JWT cũ bị kẹt claim `"MustChangePassword"` sau khi đổi mật khẩu; Middleware chặn nhầm `/api/auth/refresh-token`. |
| **R4-5** | 🟠 P1 | `Contractor` / `ProcuringEntity` | Tải GPKD mới không reset trạng thái về `"Pending"`; bế tắc không cho nộp/xét duyệt lại khi hồ sơ bị `"Rejected"`. |
| **R4-6** | 🟡 P2 | `BidSubmission` | Cho phép rút (`Withdraw`) cả HSDT đã bị loại (`Disqualified`) để xóa dấu vết nộp lại; trả sai HTTP Code 400 thay vì 403. |
| **R4-7** | 🟡 P2 | `FileStorage` / `RBAC` | 4 API tải tệp chưa áp dụng `GetPhysicalFilePath` (nguy cơ Path Traversal); chặn chủ hồ sơ tự xem file pháp lý của mình. |
| **R4-8** | 🟡 P2 | `Frontend` | Frontend chưa tích hợp màn hình đổi mật khẩu bắt buộc (`mustChangePassword`), thiếu API `replaceEvaluator` và 3 API báo cáo thống kê mới. |
| **R4-9** | 🟢 P3 | `ContractorsController` | Trả về `400 BadRequest` thay vì `404 NotFound` khi không tìm thấy nhà thầu (sai lệch OpenAPI/Swagger). |
| **R4-10**| 🟢 **P3** | `Validation` | Thiếu Regex kiểm tra chuẩn định dạng Mã số thuế (`^\d{10}(-\d{3})?$`) khi Nhà thầu / Bên mời thầu đăng ký trực tuyến. |

## N. Quy tắc bổ sung cho Agent (N20 → N26)
- **N20:** Phân quyền chi tiết HSDT (IDOR): Cán bộ chỉ xem/tải HSDT khi là Giám khảo active của gói, người tạo gói, hoặc Admin.
- **N21:** Ẩn tuyệt đối `TotalScore` và `Rank` trên toàn bộ endpoint khi gói thầu đang `Evaluating`.
- **N22:** Chặn gán chéo role Nhà thầu và Nội bộ; chặn khóa/gỡ role Giám khảo đang ngồi Hội đồng gói `Open` / `Evaluating`.
- **N23:** Tạo tài khoản Giám khảo bắt buộc `MustChangePassword = true` và gửi thông tin qua email an toàn.
- **N24:** Cập nhật giấy tờ pháp lý (GPKD, Quyết định thành lập) tự động reset `VerificationStatus` về `"Pending"`.
- **N25:** HSDT bị `Disqualified` tuyệt đối không được phép rút (`Withdraw`).
- **N26:** 100% endpoint tải file phải dùng `_fileStorageService.GetPhysicalFilePath` và trả về luồng `PhysicalFile`.

