# Báo cáo Kết quả Thực thi Bộ Nghiệm thu Toàn Vòng Đời Có thể Chạy lại (E2E Lifecycle Acceptance Result)

Ngày thực hiện: 05/11/2026  
Dự án: **Procurement & Contract Tracking System**  
Tài liệu tham chiếu: `HANDOFF_AGENT_REPRODUCIBLE_LIFECYCLE_E2E_2026-11-05.md`  
Hướng dẫn vận hành: `docs/E2E_LIFECYCLE_RUNBOOK.md`

---

## 1. Thông tin Phiên chạy & Môi trường Thực thi

Bộ kiểm thử nghiệm thu toàn vòng đời (`scripts/testing/lifecycle-e2e/`) đã được thực thi thành công **2 lần liên tiếp độc lập** thông qua điểm vào tự động duy nhất (`scripts/testing/run-lifecycle-e2e.ps1`) mà không cần bất kỳ thao tác sửa dữ liệu thủ công nào giữa hai lần chạy.

| Thông số môi trường | Lần chạy 1 (`run1_20261105`) | Lần chạy 2 (`run2_20261105`) |
|---|---|---|
| **RunId** | `run1_20261105` | `run2_20261105` |
| **Thời gian bắt đầu (UTC)** | `2026-11-05T11:17:37Z` | `2026-11-05T11:20:18Z` |
| **Thời gian hoàn tất (UTC)** | `2026-11-05T11:19:59Z` | `2026-11-05T11:23:05Z` |
| **Nhánh Git & Commit** | `dev` (`eb9d90a6380392e6d01f7a9a8a7a3c196b7e497f`) | `dev` (`eb9d90a6380392e6d01f7a9a8a7a3c196b7e497f`) |
| **Trạng thái Working Tree** | Có tệp thay đổi cục bộ (`workingTreeDirty: true`) | Có tệp thay đổi cục bộ (`workingTreeDirty: true`) |
| **Frontend Base URL** | `http://localhost:5173` | `http://localhost:5173` |
| **Backend API Base URL** | `http://localhost:5225` | `http://localhost:5225` |
| **Database cô lập (SQL Server)** | `procurement_db_e2e_run1_20261105` | `procurement_db_e2e_run2_20261105` |
| **Thư mục lưu trữ cô lập** | `scripts/testing/lifecycle-e2e/runs/run1_20261105/uploads` | `scripts/testing/lifecycle-e2e/runs/run2_20261105/uploads` |
| **Phiên bản Công cụ** | `.NET 8.0`, `Python 3.14.5`, `Playwright 1.60.0` (Chromium) | `.NET 8.0`, `Python 3.14.5`, `Playwright 1.60.0` (Chromium) |
| **Trạng thái Tổng thể** | **PASS (Exit Code 0)** | **PASS (Exit Code 0)** |

---

## 2. Danh sách Actor & Tập Dữ liệu Khởi tạo theo Từng Lần Chạy

Mỗi lần chạy tự động tạo mới cơ sở dữ liệu SQL Server riêng biệt, chạy EF Core Migration và khởi tạo đầy đủ 9 Actor nghiệp vụ độc lập theo hậu tố `<runId>`:

| Vai trò Nghiệp vụ | Lần chạy 1 (`run1_20261105`) | Lần chạy 2 (`run2_20261105`) | Quyền hạn / Phạm vi |
|---|---|---|---|
| **Admin Hệ thống** | `admin@procurement.com` (`UserId: 1`) | `admin@procurement.com` (`UserId: 1`) | Duyệt hồ sơ pháp nhân, duyệt đóng thầu sớm, duyệt ủy quyền ký; bị chặn duyệt chi |
| **Procuring Entity A** | `Ban Quản lý Dự án E2E run1_20261105` (`EntityId: 2`, MST: `0100277457`) | `Ban Quản lý Dự án E2E run2_20261105` (`EntityId: 2`, MST: `0100277618`) | Đơn vị mời thầu sở hữu gói thầu và hợp đồng chính |
| **Entity Manager A** | `manager_run1_20261105@procurement.com` (`UserId: 10`) | `manager_run2_20261105@procurement.com` (`UserId: 10`) | Quản trị viên đơn vị A, quản lý nhân sự, chốt kết quả trao thầu |
| **Procurement Officer A (Maker)** | `officer_run1_20261105@procurement.com` (`UserId: 11`, `MemberId: 3`) | `officer_run2_20261105@procurement.com` (`UserId: 11`, `MemberId: 3`) | Chuyên viên lập biên bản nghiệm thu kỹ thuật (Maker), không có quyền tự phê duyệt giải ngân |
| **Procurement Checker A (Signer)** | `checker_run1_20261105@procurement.com` (`UserId: 12`, `MemberId: 4`, `SigningAuthorityId: 2`) | `checker_run2_20261105@procurement.com` (`UserId: 12`, `MemberId: 4`, `SigningAuthorityId: 2`) | Lãnh đạo phê duyệt nghiệm thu và giải ngân (Checker) có `SigningAuthority` loại `DisbursementApproval` còn hiệu lực |
| **Procuring Entity B & User B** | `procurement_b_run1_20261105@procurement.com` (`UserId: 13`, `EntityId: 3`) | `procurement_b_run2_20261105@procurement.com` (`UserId: 13`, `EntityId: 3`) | Đơn vị mời thầu độc lập dùng để kiểm chứng chặn truy cập chéo đơn vị (IDOR) |
| **Contractor 1 (Trúng thầu)** | `contractor1_run1_20261105@test.com` (`ContractorId: 1`, `UserId: 6`, MST: `0301277457`) | `contractor1_run2_20261105@test.com` (`ContractorId: 1`, `UserId: 6`, MST: `0301277618`) | Nhà thầu nộp giá `1,500,000,000 VND`, trúng thầu và thực hiện hợp đồng |
| **Contractor 2 (Đối thủ)** | `contractor2_run1_20261105@test.com` (`ContractorId: 2`, `UserId: 7`, MST: `0302277457`) | `contractor2_run2_20261105@test.com` (`ContractorId: 2`, `UserId: 7`, MST: `0302277618`) | Nhà thầu cạnh tranh nộp giá `1,600,000,000 VND`, bị loại (`Rejected`) sau khi trao thầu |
| **Tổ chuyên gia chấm thầu (3 người)** | `evaluator@procurement.com`, `evaluator2@procurement.com`, `evaluator3@procurement.com` | `evaluator@procurement.com`, `evaluator2@procurement.com`, `evaluator3@procurement.com` | 3 giám khảo chấm điểm độc lập từng hồ sơ dự thầu |

---

## 3. Kết quả 11 Bước Kiểm thử Vòng Đời trên Trình duyệt (Browser E2E Flow)

Toàn bộ 11 bước nghiệp vụ được thực thi tuần tự trên trình duyệt Chromium thông qua giao diện người dùng thực tế (`http://localhost:5173`) trên một mạch dữ liệu duy nhất:

| Bước | Mô tả Nghiệp vụ & Kiểm chứng | Kết quả Lần 1 (`run1_20261105`) | Kết quả Lần 2 (`run2_20261105`) | Bằng chứng Ảnh chụp Màn hình |
|---|---|---|---|---|
| **Bước 1** | Bên mời thầu A tạo gói thầu mới, áp dụng mẫu bộ tiêu chí đánh giá Hàng hóa (tổng trọng số 100 điểm), đính kèm hồ sơ mời thầu PDF và mở thầu (`Open`). | **PASS** (`PackageId: 6`, Mã: `PKG-RUN1_20261105`, Ngân sách: `1,800,000,000 VND`) | **PASS** (`PackageId: 6`, Mã: `PKG-RUN2_20261105`, Ngân sách: `1,800,000,000 VND`) | `01_package_created_open.png` |
| **Bước 2** | `Contractor 1` (`1,500,000,000 VND`) và `Contractor 2` (`1,600,000,000 VND`) đăng nhập độc lập, tải lên đủ 3 tệp PDF bắt buộc (Báo giá tài chính, Hồ sơ năng lực, Tiến độ thực hiện) và nộp hồ sơ dự thầu. | **PASS** (`SubmissionId: 9` và `SubmissionId: 10`) | **PASS** (`SubmissionId: 9` và `SubmissionId: 10`) | `02_contractors_submitted_bids.png` |
| **Bước 3** | Bên mời thầu A phân công đủ 3 thành viên Tổ chuyên gia, gửi yêu cầu đóng thầu sớm; Admin phê duyệt đóng thầu (`Closed`); Bên mời thầu chuyển gói thầu sang trạng thái Đang chấm thầu (`Evaluating`). | **PASS** (3 Giám khảo được phân công, trạng thái `Evaluating`) | **PASS** (3 Giám khảo được phân công, trạng thái `Evaluating`) | `03_package_evaluating_assigned_committee.png` |
| **Bước 4** | Cả 3 Giám khảo đăng nhập riêng biệt trên trình duyệt, chấm điểm từng tiêu chí kèm nhận xét cho cả 2 nhà thầu và chốt phiếu chấm (`Completed`). | **PASS** (3/3 Giám khảo hoàn tất chấm điểm cho cả 2 hồ sơ) | **PASS** (3/3 Giám khảo hoàn tất chấm điểm cho cả 2 hồ sơ) | `04_scorecards_submitted_by_evaluators.png` |
| **Bước 5** | `Manager A` mở Trung tâm Phê duyệt & Trao thầu, kiểm tra bảng xếp hạng tổng hợp và phê duyệt trao thầu cho `Contractor 1`. | **PASS** (`Contractor 1` chuyển sang `Selected`, `Contractor 2` chuyển sang `Rejected`, gói thầu `Awarded`) | **PASS** (`Contractor 1` chuyển sang `Selected`, `Contractor 2` chuyển sang `Rejected`, gói thầu `Awarded`) | `05_award_finalized_winner_selected.png` |
| **Bước 6** | `Manager A` khởi tạo hợp đồng kinh tế từ kết quả trúng thầu của gói thầu, hệ thống tự động điền thông tin nhà thầu và giá trị trúng thầu (`1,500,000,000 VND`). | **PASS** (`ContractId: 2`, trạng thái `Draft`, giá trị `1,500,000,000 VND`) | **PASS** (`ContractId: 2`, trạng thái `Draft`, giá trị `1,500,000,000 VND`) | `06_contract_created_draft.png` |
| **Bước 7** | Tạo 2 mốc thanh toán (`Milestone 1`: `500,000,000 VND`, `Milestone 2`: `1,000,000,000 VND`, khớp tổng `1,500,000,000 VND`), tải lên tệp PDF hợp đồng scan và kích hoạt hợp đồng sang `Active`. | **PASS** (`MilestoneId: 4` và `5`, trạng thái hợp đồng `Active`) | **PASS** (`MilestoneId: 4` và `5`, trạng thái hợp đồng `Active`) | `07_contract_activated_active.png` |
| **Bước 8** | `Contractor 1` cập nhật tiến độ Tuần 1 (mức `50`) và Tuần 2 (mức `100`); đồng thời xác minh hệ thống chặn báo cáo trùng tuần (`HTTP 400`) và chặn giảm lùi tiến độ (`HTTP 400`). | **PASS** (Lưu thành công Tuần 1 & Tuần 2, chặn đúng các báo cáo vi phạm) | **PASS** (Lưu thành công Tuần 1 & Tuần 2, chặn đúng các báo cáo vi phạm) | `08_contractor_progress_recorded.png` |
| **Bước 9** | `Officer A (Maker)` thẩm tra kỹ thuật Đạt cho `Milestone 1` (tạo snapshot `r1, v1`). Kiểm chứng giao diện khóa nút phê duyệt đối với Maker. Sau đó `Checker A (Signer)` đăng nhập phê duyệt giải ngân `Milestone 1`. | **PASS** (`Milestone 1` chuyển sang `Completed`, đã giải ngân `500,000,000 VND`, còn lại `1,000,000,000 VND`, hợp đồng giữ nguyên `Active`) | **PASS** (`Milestone 1` chuyển sang `Completed`, đã giải ngân `500,000,000 VND`, còn lại `1,000,000,000 VND`, hợp đồng giữ nguyên `Active`) | `09_milestone1_disbursed_contract_active.png` |
| **Bước 10** | `Officer A (Maker)` thẩm tra kỹ thuật Đạt cho `Milestone 2` (`AcceptanceId: 2`). Thực thi toàn bộ 6 bài kiểm thử ràng buộc âm phía Server. Sau đó `Checker A (Signer)` phê duyệt giải ngân `Milestone 2`. | **PASS** (`Milestone 2` chuyển sang `Completed`, tổng giải ngân `1,500,000,000 VND`, còn lại `0 VND`, hợp đồng tự động chuyển sang `Completed`) | **PASS** (`Milestone 2` chuyển sang `Completed`, tổng giải ngân `1,500,000,000 VND`, còn lại `0 VND`, hợp đồng tự động chuyển sang `Completed`) | `10_contract_completed_fully_disbursed.png` |
| **Bước 11** | Đăng xuất, đăng nhập lại và tải lại trang để xác minh dữ liệu bền vững từ cơ sở dữ liệu; mở Nhật ký Kiểm toán (`Audit Logs`) kiểm tra vết hành động xuyên suốt. | **PASS** (Trạng thái `Completed` bền vững, hiển thị đầy đủ lịch sử kiểm toán) | **PASS** (Trạng thái `Completed` bền vững, hiển thị đầy đủ lịch sử kiểm toán) | `11_audit_logs_verified.png` |

---

## 4. Kết quả Kiểm thử Ràng buộc Âm Phía Server (Server-Side Negative Constraints)

Ngay sau khi `Officer A (Maker)` hoàn tất thẩm tra kỹ thuật cho `Milestone 2` (lúc hợp đồng đang `Active` và `Milestone 2` đang chờ `Checker A` duyệt chi), bộ kiểm thử thực thi 6 nhóm kịch bản tấn công/vi phạm trực tiếp vào Backend API:

| Mã Ca Kiểm thử | Kịch bản Kiểm thử Ràng buộc Phía Server | Mã Phản hồi Kỳ vọng | Thực tế Lần 1 (`run1_20261105`) | Thực tế Lần 2 (`run2_20261105`) | Đánh giá & Kiểm chứng Bảo toàn Dữ liệu |
|---|---|---|---|---|---|
| **NEG-01A** | `Procurement B` (thuộc `Entity B`) gọi API đọc danh sách thành viên nội bộ nhạy cảm của `Entity A` (`GET /api/procuring-entities/2/members`) | `HTTP 403 Forbidden` | `HTTP 403` | `HTTP 403` | **PASS** — Chặn truy cập đọc dữ liệu nội bộ chéo đơn vị. |
| **NEG-01B** | `Procurement B` gọi API thay đổi trạng thái hợp đồng của `Entity A` (`PUT /api/contracts/2/status`) | `HTTP 403 Forbidden` | `HTTP 403` | `HTTP 403` | **PASS** — Chặn chỉnh sửa hợp đồng chéo đơn vị; trạng thái hợp đồng giữ nguyên `Active`. |
| **NEG-01C** | `Procurement B` gọi API tải tệp snapshot nghiệm thu của `Entity A` (`GET /api/contracts/milestones/5/acceptances/2/document/download`) | `HTTP 403 Forbidden` | `HTTP 403` | `HTTP 403` | **PASS** — Chặn tải tài liệu snapshot chéo đơn vị (IDOR). |
| **NEG-02** | `Officer A (Maker)` gửi request trực tiếp tới API phê duyệt giải ngân cho chính biên bản mình vừa thẩm tra (`PUT /api/contracts/milestones/5/acceptance`) | `HTTP 400 Bad Request` (Anti-Self-Approval) | `HTTP 400` | `HTTP 400` | **PASS** — Server từ chối hành vi tự lập tự duyệt, bảo vệ nguyên tắc Maker-Checker. |
| **NEG-03** | Cấp tài khoản thành viên phụ thuộc `Entity A`, lấy JWT hợp lệ, sau đó `Manager A` thu hồi tư cách thành viên (`DELETE /members/{id}`). Dùng JWT cũ gọi API thẩm tra và phê duyệt nghiệm thu | `HTTP 403 Forbidden` | `HTTP 403 / 403` | `HTTP 403 / 403` | **PASS** — Server kiểm tra trạng thái thành viên đang hoạt động theo thời gian thực, vô hiệu hóa quyền của JWT cũ ngay lập tức. |
| **NEG-04** | `Checker A` gửi yêu cầu phê duyệt nghiệm thu kèm `expectedRevision = 999` (sai lệch so với revision thực tế `r1`) | `HTTP 409 Conflict` | `HTTP 409` | `HTTP 409` | **PASS** — Cơ chế kiểm soát đồng thời lạc quan (Optimistic Concurrency) chặn ghi đè phiên bản cũ. |
| **NEG-05A** | Gọi API tải tệp snapshot nghiệm thu mà không đính kèm Bearer Token xác thực | `HTTP 401 Unauthorized` | `HTTP 401` | `HTTP 401` | **PASS** — Endpoint tải tài liệu bắt buộc xác thực nghiêm ngặt. |
| **NEG-05B** | `Checker A` tải tệp snapshot nghiệm thu hợp lệ và đối chiếu mã băm `SHA-256` của tệp tải về với `SnapshotDocumentHash` lưu trong DB | `HTTP 200 OK` + Khớp mã băm `SHA-256` | `HTTP 200` (`84,926 bytes`, `Match: True`) | `HTTP 200` (`84,926 bytes`, `Match: True`) | **PASS** — Tệp PDF tải về nguyên vẹn tuyệt đối, khớp mã băm `SHA-256` với bản ghi snapshot. |
| **NEG-06** | `Admin` nền tảng gọi API phê duyệt giải ngân mốc thanh toán thay cho Bên mời thầu (`PUT /api/contracts/milestones/5/acceptance`) | `HTTP 403 Forbidden` | `HTTP 403` | `HTTP 403` | **PASS** — Phân tách quyền lực rõ ràng: Admin hệ thống không được duyệt chi ngân sách của đơn vị mời thầu. |

*Kiểm chứng sau khi chạy toàn bộ kịch bản âm*: Hợp đồng vẫn giữ nguyên trạng thái `Active`, `Milestone 2` vẫn giữ nguyên trạng thái chờ duyệt, số tiền đã giải ngân giữ nguyên `500,000,000 VND` và số tiền còn lại giữ nguyên `1,000,000,000 VND` trước khi `Checker A` thực hiện phê duyệt hợp lệ ở Bước 10.2.

---

## 5. Bảng Đối chiếu Tính Toàn vẹn Chuỗi Khóa ngoại & Tài chính (Reconciliation)

Sau khi hoàn tất Bước 11, kịch bản thực hiện truy vấn đối soát chỉ đọc (Read-only SQL Reconciliation) trên cơ sở dữ liệu cô lập của từng lần chạy và xác nhận khớp tuyệt đối:

| Hạng mục Đối chiếu | Lần chạy 1 (`run1_20261105`) | Lần chạy 2 (`run2_20261105`) | Kết quả Kiểm chứng |
|---|---|---|---|
| **Gói thầu (`BidPackages.Id`)** | `PackageId = 6` (`PKG-RUN1_20261105`, Trạng thái: `Awarded`) | `PackageId = 6` (`PKG-RUN2_20261105`, Trạng thái: `Awarded`) | Khớp 100 điểm tiêu chí và trạng thái trao thầu |
| **Hồ sơ trúng thầu (`BidSubmissions`)** | `SubmissionId = 9` (`ContractorId = 1`, Giá: `1,500,000,000 VND`, `Selected`) | `SubmissionId = 9` (`ContractorId = 1`, Giá: `1,500,000,000 VND`, `Selected`) | Khớp nhà thầu trúng thầu và giá trúng thầu |
| **Hồ sơ không trúng thầu (`BidSubmissions`)** | `SubmissionId = 10` (`ContractorId = 2`, Giá: `1,600,000,000 VND`, `Rejected`) | `SubmissionId = 10` (`ContractorId = 2`, Giá: `1,600,000,000 VND`, `Rejected`) | Tự động chuyển sang `Rejected` khi trao thầu |
| **Hợp đồng (`Contracts.Id`)** | `ContractId = 2` (`BidPackageId = 6`, `ContractorId = 1`, Trạng thái: `Completed`) | `ContractId = 2` (`BidPackageId = 6`, `ContractorId = 1`, Trạng thái: `Completed`) | Liên kết khóa ngoại chính xác tới gói thầu và nhà thầu trúng thầu |
| **Mốc thanh toán 1 (`ContractMilestones`)** | `MilestoneId = 4` (`500,000,000 VND`, `Version = 2`, `AcceptanceId = 1`, `Revision = 1`) | `MilestoneId = 4` (`500,000,000 VND`, `Version = 2`, `AcceptanceId = 1`, `Revision = 1`) | `TechnicalReviewedBy = 11` (`Officer A`) khác `ApprovedBy = 12` (`Checker A`) |
| **Mốc thanh toán 2 (`ContractMilestones`)** | `MilestoneId = 5` (`1,000,000,000 VND`, `Version = 2`, `AcceptanceId = 2`, `Revision = 1`) | `MilestoneId = 5` (`1,000,000,000 VND`, `Version = 2`, `AcceptanceId = 2`, `Revision = 1`) | `TechnicalReviewedBy = 11` (`Officer A`) khác `ApprovedBy = 12` (`Checker A`) |
| **Đối soát Tài chính Tổng hợp** | Giá trị HĐ: `1,500,000,000 VND` = Đã giải ngân: `1,500,000,000 VND` + Còn lại: `0 VND` | Giá trị HĐ: `1,500,000,000 VND` = Đã giải ngân: `1,500,000,000 VND` + Còn lại: `0 VND` | Cân bằng tài chính tuyệt đối |
| **Nhật ký Kiểm toán (`AuditLogs`)** | Ghi nhận đầy đủ các bản ghi `CREATE`, `UPDATE`, `REVOKE_MEMBER`, `APPROVE_MILESTONE_ACCEPTANCE` gắn đúng `UserId` và `EntityId` | Ghi nhận đầy đủ các bản ghi `CREATE`, `UPDATE`, `REVOKE_MEMBER`, `APPROVE_MILESTONE_ACCEPTANCE` gắn đúng `UserId` và `EntityId` | Đầy đủ vết kiểm toán cho cả hai mốc nghiệm thu và hợp đồng |

---

## 6. Danh mục Hiện vật & Bằng chứng Lưu trữ

Toàn bộ hiện vật kiểm thử của cả hai lần chạy được lưu trữ độc lập tại:

1. **Lần chạy 1 (`run1_20261105`)**:
   - Manifest tổng hợp: `scripts/testing/lifecycle-e2e/runs/run1_20261105/manifest.json`
   - Bộ 11 ảnh chụp màn hình: `scripts/testing/lifecycle-e2e/runs/run1_20261105/screenshots/01_package_created_open.png` đến `11_audit_logs_verified.png`
   - Nhật ký tiến trình: `scripts/testing/lifecycle-e2e/runs/run1_20261105/backend_stdout.log`, `frontend_stdout.log`

2. **Lần chạy 2 (`run2_20261105`)**:
   - Manifest tổng hợp: `scripts/testing/lifecycle-e2e/runs/run2_20261105/manifest.json`
   - Bộ 11 ảnh chụp màn hình: `scripts/testing/lifecycle-e2e/runs/run2_20261105/screenshots/01_package_created_open.png` đến `11_audit_logs_verified.png`
   - Nhật ký tiến trình: `scripts/testing/lifecycle-e2e/runs/run2_20261105/backend_stdout.log`, `frontend_stdout.log`

3. **Dọn dẹp Cơ sở dữ liệu Tạm thời**:
   - Hai cơ sở dữ liệu kiểm thử tạm thời (`procurement_db_e2e_run1_20261105` và `procurement_db_e2e_run2_20261105`) đã được thu thập đầy đủ đối soát vào `manifest.json` và xóa sạch khỏi SQL Server sau khi hoàn tất nghiệm thu.
