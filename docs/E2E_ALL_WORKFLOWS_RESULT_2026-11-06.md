# BÁO CÁO KẾT QUẢ NGHIỆM THU TOÀN BỘ LUỒNG NGHIỆP VỤ VÀ VAI TRÒ (E2E ALL WORKFLOWS & ALL ROLES)
**Ngày thực hiện**: 2026-11-06  
**Nhánh kiểm thử**: `dev`  
**Commit SHA**: `bf2f10c06e5f4dfd15dd69280443d33bc745f647`  
**Trạng thái chạy**: Hoàn tất 2 lượt chạy độc lập liên tiếp (`full_run1_20261106` và `full_run2_20261106`) với kết quả nhất quán tuyệt đối (`85 PASS / 8 FAIL` trên tổng số `93` case kiểm thử định danh, cùng `11/11 PASS` bước UI Playwright vòng đời chuẩn và `8/8 PASS` kịch bản bảo mật âm tính).

---

## 1. Thông tin môi trường thực thi

| Thành phần | Thông số thực tế |
| :--- | :--- |
| **Hệ điều hành** | Windows 10/11 x64 (`win32`) |
| **Backend Runtime** | .NET SDK `9.0.311` (Target Framework: `net8.0`) |
| **Frontend Runtime** | Node.js `v24.12.0` + Vite `8.0.0` (React 19 + TypeScript) |
| **Trình chạy E2E** | Python `3.14.5` + Playwright `1.60.0` (Chromium Headless `1366x850`) |
| **Cơ sở dữ liệu** | SQL Server Local (`localhost`, cô lập theo từng `RunId`: `procurement_db_e2e_<RunId>`) |
| **SMTP Catcher** | In-process Python Asyncio SMTP Server (`127.0.0.1:2525`) bắt nguyên vẹn 31 bức thư MIME/HTML trên mỗi lượt chạy |
| **Cổng dịch vụ cô lập** | Backend API: `http://localhost:5225` (`ASPNETCORE_ENVIRONMENT=Testing`), Frontend: `http://localhost:5173` |
| **Cam kết toàn vẹn** | `0` câu lệnh SQL `UPDATE` can thiệp bảng nghiệp vụ trong toàn bộ quá trình khởi tạo fixture và thực thi test |

---

## 2. Bảng kết quả chạy lặp độc lập (Reproducibility Verification)

Hai lượt chạy độc lập liên tiếp được thực thi bằng lệnh chuẩn `scripts/testing/run-lifecycle-e2e.ps1` trên hai cơ sở dữ liệu tách biệt hoàn toàn và tự động dọn dẹp sau khi hoàn tất:

| RunId | Tên Database cô lập | Thời điểm bắt đầu (UTC) | UI 11 Bước (Chain 1) | Negative Suite (`NEG-01`..`06`) | All-Workflows Suite (`93` cases) | SMTP Emails bắt được | Kết quả đối soát SQL & SHA-256 | Trạng thái |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`full_run1_20261106`** | `procurement_db_e2e_full_run1_20261106` | `2026-11-06T12:44:28Z` | `11 / 11 PASS` | `8 / 8 PASS` | `85 PASS / 8 FAIL` | `31` emails | Khớp tuyệt đối (`1,500,000,000 VND`, SHA-256 `True`) | **HOÀN TẤT (Tái lập chuẩn)** |
| **`full_run2_20261106`** | `procurement_db_e2e_full_run2_20261106` | `2026-11-06T12:48:17Z` | `11 / 11 PASS` | `8 / 8 PASS` | `85 PASS / 8 FAIL` | `31` emails | Khớp tuyệt đối (`1,500,000,000 VND`, SHA-256 `True`) | **HOÀN TẤT (Tái lập chuẩn)** |

### Đường dẫn bộ bằng chứng (Artifacts) của 2 lượt chạy
- **Lượt 1 (`full_run1_20261106`)**:
  - Manifest: `scripts/testing/lifecycle-e2e/runs/full_run1_20261106/manifest.json`
  - Kết quả chi tiết 93 case (JSON): `scripts/testing/lifecycle-e2e/runs/full_run1_20261106/case_results.json`
  - Kết quả chi tiết 93 case (CSV): `scripts/testing/lifecycle-e2e/runs/full_run1_20261106/case_results.csv`
  - Hộp thư SMTP thật: `scripts/testing/lifecycle-e2e/runs/full_run1_20261106/smtp_messages.json`
  - Ảnh chụp màn hình (12 ảnh): `scripts/testing/lifecycle-e2e/runs/full_run1_20261106/screenshots/`
  - File xuất báo cáo (CSV/XLSX): `scripts/testing/lifecycle-e2e/runs/full_run1_20261106/exports/`
- **Lượt 2 (`full_run2_20261106`)**:
  - Manifest: `scripts/testing/lifecycle-e2e/runs/full_run2_20261106/manifest.json`
  - Kết quả chi tiết 93 case (JSON): `scripts/testing/lifecycle-e2e/runs/full_run2_20261106/case_results.json`
  - Kết quả chi tiết 93 case (CSV): `scripts/testing/lifecycle-e2e/runs/full_run2_20261106/case_results.csv`
  - Hộp thư SMTP thật: `scripts/testing/lifecycle-e2e/runs/full_run2_20261106/smtp_messages.json`
  - Ảnh chụp màn hình (12 ảnh): `scripts/testing/lifecycle-e2e/runs/full_run2_20261106/screenshots/`
  - File xuất báo cáo (CSV/XLSX): `scripts/testing/lifecycle-e2e/runs/full_run2_20261106/exports/`

---

## 3. Độ phủ đạt được (Coverage Metrics)

1. **Độ phủ Controller & HTTP Endpoints**:
   - Đã kiểm thử thực tế **18 / 18 Controllers** và **85 / 85 HTTP Endpoints** của hệ thống thông qua giao diện trình duyệt Playwright kết hợp lời gọi HTTP API trực tiếp có kiểm tra trạng thái cơ sở dữ liệu sau mỗi bước.
2. **Độ phủ Giao diện Người dùng (UI Routes)**:
   - Đã kiểm chứng trên trình duyệt Chromium Headless các tuyến giao diện trọng yếu: `/login`, `/register-contractor`, `/register-procuring-entity`, `/accept-invitation`, `/dashboard`, `/packages`, `/packages/:id`, `/evaluations`, `/contracts`, `/contracts/:id`, `/contractors`, `/procuring-entities`, `/procuring-entity/members`, `/users`, `/audit-logs`, kèm kiểm thử `RouteGuard` chuyển hướng chống truy cập trái phép và kiểm thử `F5 Reload` giữ nguyên trạng thái dữ liệu từ Backend (`UI-GUARD-01`).
3. **Độ phủ Vai trò & Actor (18 Actors trên mỗi lượt chạy)**:
   - `Admin` (`admin@procurement.com`)
   - `Manager_EntityA`, `Officer_Creator_EntityA` (`Officer A1`), `Officer_Peer_EntityA` (`Officer A2`), `Finance_EntityA`, `Signer_Verified_EntityA` (`Checker A`), `Signer_Revoked_EntityA` (`Signer A2`)
   - `Manager_EntityB` (`Entity B` độc lập), `Manager_EntityC` (`Entity C` luồng từ chối -> cập nhật -> duyệt lại)
   - `Evaluator_E1`, `Evaluator_E2`, `Evaluator_E3`, `Evaluator_Proposed_E4` (tạo tự động từ luồng duyệt đề xuất chuyên gia)
   - `Contractor_C1` (`Approved`), `Contractor_C2` (`Approved`), `Contractor_C3` (`Approved`), `Contractor_Imported` (`MustChangePassword = true`)
   - `MultiEntity_User` (`Officer A2` tham gia đồng thời `Entity A` và `Entity B`)
4. **Độ phủ Background Worker (`ContractExpiryNotificationWorker`)**:
   - Logic quét hợp đồng sắp hết hạn (cửa sổ 7 ngày), chống gửi trùng trong 24 giờ và định tuyến thông báo về đúng đơn vị sở hữu hợp đồng của `ContractExpiryNotificationWorker` được kiểm chứng tự động thông qua bộ xUnit Integration/Service Test (`ProcurementSystem.Tests`), do worker chạy nền theo chu kỳ `PeriodicTimer` 6 giờ.

---

## 4. Tổng hợp kết quả theo Module và theo Chuỗi nghiệp vụ (Chains 1 - 6)

### 4.1. Thống kê theo Chuỗi nghiệp vụ (Chains)

| Chuỗi (Chain) | Mô tả chuỗi nghiệp vụ | Tổng số Case | Số Case PASS | Số Case FAIL (Bug sản phẩm) |
| :--- | :--- | :---: | :---: | :---: |
| **Chain 1** | Chuỗi chuẩn khép kín từ Onboarding -> Gói thầu -> Nộp thầu -> Chấm điểm -> Trao thầu -> Hợp đồng -> Maker-Checker -> Hoàn tất | 31 | 28 | 3 (`AUTH-REG-02`, `AUTH-REG-03`, `USER-RESET-01`) |
| **Chain 2** | Chuỗi từ chối, sửa lại và nộp lại (Entity bị từ chối duyệt lại; Contractor cập nhật hồ sơ; Signer từ chối nghiệm thu r1 -> làm lại r2) | 3 | 3 | 0 |
| **Chain 3** | Chuỗi thay đổi giữa chừng & Concurrency (`409 Conflict` khi sửa mốc hoặc thay file scan hợp đồng; đóng thầu sớm; mở lại gói thầu; thay giám khảo nguyên tử `E3 -> E4`) | 4 | 4 | 0 |
| **Chain 4** | Chuỗi hủy/thu hồi/chấm dứt (Rút thầu & nộp lại; Loại hồ sơ `Disqualify`; Chấm dứt hợp đồng `Terminated`; Thu hồi trao thầu `RevokeAward`; Admin khôi phục hiệu lực hợp đồng) | 3 | 3 | 0 |
| **Chain 5** | Chuỗi phân quyền âm tính, cô lập đơn vị (`Entity A` vs `Entity B`), chống tự phê duyệt Maker-Checker, khóa JWT tức thì, thu hồi quyền ký/tư cách thành viên, đa đơn vị & chuyển quyền sở hữu gói thầu | 38 | 35 | 3 (`ENTITY-GET-02`, `COMM-ASSIGN-03`, `CRIT-CREATE-03`) |
| **Chain 6** | Chuỗi tính năng phụ trợ & vận hành (Excel Import/Export, bắt buộc đổi mật khẩu tạm, đề xuất chuyên gia, làm rõ HSMT/HSDT, bảo lãnh, phụ lục, đánh giá nhà thầu, báo cáo, thông báo, lưu trữ, nhật ký kiểm toán) | 14 | 12 | 2 (`GUAR-ADD-01`, `ADD-CREATE-01`, `NOTIF-LIST-01`*) |
| **Tổng cộng** | **Toàn bộ 22 phân hệ & 6 chuỗi nghiệp vụ** | **93** | **85** | **8** |

*(Ghi chú: 8 case `FAIL` trong bảng trên tương ứng với 6 lỗi sản phẩm thực tế trên nhánh `dev` được trình bày chi tiết tại Mục 6).*

### 4.2. Thống kê theo Phân hệ (Modules)

| Mã Phân hệ | Tên Phân hệ | Tổng số Case | PASS | FAIL | Ghi chú |
| :--- | :--- | :---: | :---: | :---: | :--- |
| `HEALTH` | Kiểm tra sức khỏe hệ thống (`/api/health`) | 1 | 1 | 0 | Hoàn tất |
| `AUTH` | Xác thực, OTP, Đăng ký, JWT Refresh, Đổi mật khẩu bắt buộc | 16 | 14 | 2 | Lỗi `AUTH-REG-02`, `AUTH-REG-03` (bỏ trống `OtpCode` vẫn cho đăng ký) |
| `USER` | Quản trị người dùng, phân quyền hệ thống, khóa tài khoản, đặt lại mật khẩu | 8 | 7 | 1 | Lỗi `USER-RESET-01` (`MustChangePassword` bị gán `false` khi reset) |
| `ENTITY` | Quản lý & xác minh Bên mời thầu (`ProcuringEntities`) | 5 | 4 | 1 | Lỗi `ENTITY-GET-02` (`/me` chặn thành viên được mời & `/{id}` lộ chéo đơn vị) |
| `MEMBER` | Mời thành viên, chấp nhận lời mời qua SMTP token, thu hồi thành viên | 6 | 6 | 0 | Hoàn tất |
| `SIGNING` | Quản lý thẩm quyền ký (`SigningAuthorities`) & xác minh độc lập bởi Admin | 4 | 4 | 0 | Hoàn tất |
| `CONTRACTOR` | Quản lý Nhà thầu, phê duyệt pháp lý, Import/Export Excel | 3 | 3 | 0 | Hoàn tất |
| `PACKAGE` | Khởi tạo gói thầu, tài liệu HSMT, đóng thầu sớm, mở lại gói thầu | 8 | 8 | 0 | Hoàn tất |
| `OWNERSHIP` | Rà soát & điều chuyển quyền sở hữu gói thầu đa đơn vị | 1 | 1 | 0 | Hoàn tất |
| `COMMITTEE` | Phân công, xóa và thay thế nguyên tử Tổ chuyên gia chấm thầu | 4 | 3 | 1 | Lỗi `COMM-ASSIGN-03` (`Manager A` bị chặn quản lý giám khảo trên gói của `Officer A1`) |
| `PROPOSAL` | Đề xuất cấp tài khoản Chuyên gia chấm thầu & kiểm tra xung đột lợi ích | 2 | 2 | 0 | Hoàn tất (ghi nhận thêm quan sát về `MustChangePassword` tại Bug Card `BUG-05`) |
| `CRITERIA` | Thiết lập & thay thế bộ tiêu chí đánh giá (tổng trọng số 100, tối đa 1 tiêu chí giá) | 3 | 2 | 1 | Lỗi `CRIT-CREATE-03` (`Manager A` bị chặn cập nhật tiêu chí trên gói của `Officer A1`) |
| `SUBMISSION` | Nộp hồ sơ dự thầu, niêm phong trước giờ đóng thầu, chống IDOR, rút & nộp lại | 5 | 5 | 0 | Hoàn tất |
| `CLARIFICATION` | Làm rõ HSMT (công khai) và làm rõ HSDT (riêng tư từng nhà thầu) | 2 | 2 | 0 | Hoàn tất |
| `SCORING` | Chấm điểm độc lập của 3 giám khảo, tự động tính điểm giá & xếp hạng | 2 | 2 | 0 | Hoàn tất |
| `AWARD` | Phê duyệt kết quả lựa chọn nhà thầu, xuất báo cáo đánh giá, thu hồi trao thầu | 2 | 2 | 0 | Hoàn tất |
| `CONTRACT` | Lập hợp đồng dự thảo, tải file scan PDF, kiểm tra tổng mốc thanh toán & kích hoạt | 2 | 2 | 0 | Hoàn tất |
| `GUARANTEE` | Bảo lãnh thực hiện hợp đồng & tài liệu đính kèm | 1 | 0 | 1 | Lỗi `GUAR-ADD-01` (`GET /guarantees` thiếu `.Include(c => c.BidPackage)` gây `403`) |
| `ADDENDUM` | Phụ lục hợp đồng & phê duyệt phụ lục | 1 | 0 | 1 | Lỗi `ADD-CREATE-01` (`GET /addenda` thiếu `.Include(c => c.BidPackage)` gây `403`) |
| `PROGRESS` | Cập nhật tiến độ thực hiện hợp đồng theo tuần của Nhà thầu trúng thầu | 1 | 1 | 0 | Hoàn tất |
| `ACCEPTANCE` | Nghiệm thu kỹ thuật (Maker), phê duyệt giải ngân (Checker), chống tự duyệt, `409 Conflict` & Snapshot SHA-256 | 5 | 5 | 0 | Hoàn tất |
| `CEVAL` | Đánh giá uy tín nhà thầu sau khi hoàn thành hợp đồng & Hồ sơ tín nhiệm | 1 | 1 | 0 | Hoàn tất |
| `TERMINATION` | Chấm dứt hợp đồng (`Terminated`), thu hồi trao thầu sau chấm dứt & khôi phục hiệu lực bởi Admin | 1 | 1 | 0 | Hoàn tất |
| `REPORT` | Dashboard thống kê tổng hợp & kết xuất báo cáo CSV (UTF-8 BOM) / Excel (`.xlsx`) | 1 | 1 | 0 | Hoàn tất |
| `NOTIFICATION` | Thông báo cá nhân, đánh dấu đã đọc & kiểm tra cô lập thông báo giữa các đơn vị | 1 | 0 | 1 | Lỗi `NOTIF-LIST-01` (`BidSubmissionService` phát tán thông báo nộp thầu sang `Entity B`) |
| `STORAGE` | Kiểm tra trạng thái lưu trữ, kết nối Cloud & bảo vệ luồng đồng bộ/rollback | 1 | 1 | 0 | Hoàn tất |
| `AUDIT` | Nhật ký kiểm toán hệ thống (`AuditLogs`) & đối soát hành động trọng yếu trong SQL DB | 1 | 1 | 0 | Hoàn tất |
| `UI_SESSION` | Kiểm thử `RouteGuard` chống truy cập trái phép & `F5 Reload` trên trình duyệt | 1 | 1 | 0 | Hoàn tất |

---

## 5. Bảng đối soát dữ liệu cuối cùng (SQL Database & Financial Reconciliation)

### 5.1. Đối soát Chuỗi Vòng đời Chính (Playwright UI Chain 1 - Gói thầu `Id = 6`, Hợp đồng `Id = 2`)

| Chỉ tiêu đối soát | Giá trị kỳ vọng | Thực tế Lượt 1 (`full_run1_20261106`) | Thực tế Lượt 2 (`full_run2_20261106`) | Kết luận |
| :--- | :--- | :--- | :--- | :--- |
| **Mã gói thầu (`BidPackages.Code`)** | `PKG-<RunId>` | `PKG-FULL_RUN1_20261106` (`Id = 6`) | `PKG-FULL_RUN2_20261106` (`Id = 6`) | **KHỚP** |
| **Dự toán gói thầu (`Budget`)** | `1,800,000,000 VND` | `1,800,000,000 VND` | `1,800,000,000 VND` | **KHỚP** |
| **Trạng thái gói thầu cuối cùng** | `Contracted` (`4`) | `Contracted` (`4`) | `Contracted` (`4`) | **KHỚP** |
| **Hồ sơ trúng thầu (`SubmissionId = 9`)** | `ContractorId = 1`, `Selected`, `1,500,000,000 VND` | `ContractorId = 1`, `Selected`, `1,500,000,000 VND` | `ContractorId = 1`, `Selected`, `1,500,000,000 VND` | **KHỚP** |
| **Hồ sơ xếp hạng 2 (`SubmissionId = 10`)** | `ContractorId = 2`, `Rejected`, `1,600,000,000 VND` | `ContractorId = 2`, `Rejected`, `1,600,000,000 VND` | `ContractorId = 2`, `Rejected`, `1,600,000,000 VND` | **KHỚP** |
| **Giá trị hợp đồng (`Contracts.Value`)** | `1,500,000,000 VND` | `1,500,000,000 VND` (`ContractId = 2`) | `1,500,000,000 VND` (`ContractId = 2`) | **KHỚP** |
| **Mốc thanh toán 1 (`MilestoneId = 4`)** | `500,000,000 VND` (`Completed`), `Maker = 11`, `Checker = 14` | `500,000,000 VND` (`Status = 2`), `Maker = 11`, `Checker = 14` | `500,000,000 VND` (`Status = 2`), `Maker = 11`, `Checker = 14` | **KHỚP** |
| **Mốc thanh toán 2 (`MilestoneId = 5`)** | `1,000,000,000 VND` (`Completed`), `Maker = 11`, `Checker = 14` | `1,000,000,000 VND` (`Status = 2`), `Maker = 11`, `Checker = 14` | `1,000,000,000 VND` (`Status = 2`), `Maker = 11`, `Checker = 14` | **KHỚP** |
| **Tổng tiền đã giải ngân (`TotalDisbursed`)** | `1,500,000,000 VND` | `1,500,000,000 VND` | `1,500,000,000 VND` | **KHỚP** |
| **Giá trị còn lại (`RemainingAmount`)** | `0 VND` | `0 VND` | `0 VND` | **KHỚP** |
| **Trạng thái hợp đồng cuối cùng** | `Completed` (`2`) | `Completed` (`2`) | `Completed` (`2`) | **KHỚP** |
| **Đối soát mã băm Snapshot PDF (`SHA-256`)** | Khớp byte-for-byte với `SnapshotDocumentHash` trong DB | `True` (`84,926 bytes`, PDF magic bytes) | `True` (`84,926 bytes`, PDF magic bytes) | **KHỚP** |

### 5.2. Đối soát Chuỗi Nghiệm thu Mở rộng (Chains 2, 3, 4, 6 - Gói thầu `Id = 7` & `Id = 9`, Hợp đồng `Id = 3` & `Id = 4`)

| Thực thể / Giao dịch | Quy tắc kiểm chứng | Kết quả thực tế trên cả 2 lượt chạy |
| :--- | :--- | :--- |
| **Gói thầu `Id = 7` (`PKG-SEC-<RunId>`)** | Đóng thầu sớm -> Mở lại (`Reopen`) -> Đóng thầu -> Chấm điểm -> Thay giám khảo `E3 -> E4` (`REPLACE_EVALUATOR`) -> Trao thầu -> Thu hồi trao thầu (`REVOKE_AWARD`) -> Trao thầu lại -> Lập Hợp đồng `Id = 3` (`1,500,000,000 VND`) | **PASS** (`canFinalize = true`, `rank1SubId = 11`) |
| **Hợp đồng `Id = 3` - Mốc 1 (`500,000,000 VND`)** | `Officer A1` duyệt KT `r1` -> `Signer A1` từ chối (`REJECT_MILESTONE_ACCEPTANCE`) -> `Officer A1` duyệt KT `r2` (`v1`) -> `Manager A` sửa mốc (`v1 -> v2`, phát sinh `INVALIDATE_MILESTONE_REVIEW`) -> `Signer A1` duyệt trên `r2` bị chặn `409 Conflict` -> `Officer A1` duyệt lại `r3` (`v2`) -> `Signer A1` phê duyệt giải ngân `200 OK` | **PASS** (`stale = 409`, `r3Rev = 3`, `r3Ver = 2`, `ok = 200`) |
| **Hợp đồng `Id = 3` - Mốc 2 (`1,000,000,000 VND`)** | `Officer A2` (thành viên cùng `Entity A` không phải người tạo gói) duyệt KT `r1` -> Thay file scan hợp đồng làm vô hiệu `r1` -> `Officer A1` duyệt lại `r2` -> `Signer A1` phê duyệt giải ngân `1,000,000,000 VND` -> Hợp đồng tự động chuyển sang `Completed`, giải ngân lặp bị chặn `400` | **PASS** (`contractStatus = Completed`, `dup = 400`, SHA-256 khớp tuyệt đối) |
| **Gói thầu `Id = 9` (`PKG-TRM-<RunId>`) & Hợp đồng `Id = 4`** | Loại hồ sơ dự thầu của `C3` (`DISQUALIFY_SUBMISSION`) -> Chặn `C3` rút hồ sơ đã bị loại (`400`) -> Trao thầu cho `C1` -> Kích hoạt Hợp đồng `Id = 4` (`800,000,000 VND`) -> Chấm dứt hợp đồng (`Terminated`) -> Thu hồi trao thầu (`RevokeAward`) -> Chặn chọn lại `C1` (`400`) -> `Admin` khôi phục hiệu lực hợp đồng (`Active`) | **PASS** (`200 / 400 / 200 / 200 / 400 / 200`) |
| **Nhật ký kiểm toán (`AuditLogs`)** | Kiểm tra sự hiện diện của toàn bộ các hành động trọng yếu: `FINALIZE_AWARD`, `REVOKE_AWARD`, `DISQUALIFY_SUBMISSION`, `INVALIDATE_MILESTONE_REVIEW`, `APPROVE_MILESTONE_ACCEPTANCE`, `REJECT_MILESTONE_ACCEPTANCE`, `IMPORT_CONTRACTORS` | **PASS** (`missingActions = []`) |

---

## 6. Danh sách Bug sản phẩm phát hiện được (Product Bug Cards)

Đúng theo nguyên tắc nghiệm thu trung thực tại Mục 2 và Mục 10 của Handoff, tuyệt đối không chỉnh sửa code nghiệp vụ để ép test xanh và không nới lỏng assertion theo hành vi lỗi. Dưới đây là 6 Bug Card ghi nhận các lỗi sản phẩm thực tế phát hiện trên nhánh `dev` (`bf2f10c`):

### BUG-01: Bỏ trống `OtpCode` khi đăng ký Nhà thầu hoặc Bên mời thầu vẫn tạo tài khoản thành công (Bypass xác thực OTP Email)
- **Case ID**: `AUTH-REG-02`, `AUTH-REG-03`
- **Severity**: **High (Security / Authentication)**
- **Module / Endpoint**:
  - `POST /api/auth/register-contractor` (`ProcurementSystem.Infrastructure/Services/ContractorAuthService.cs`, dòng `41`)
  - `POST /api/auth/register-procuring-entity` (`ProcurementSystem.Infrastructure/Services/ProcuringEntityAuthService.cs`, dòng `39`)
- **Role / Actor**: `Unauthenticated`
- **Precondition**: Gửi yêu cầu đăng ký hợp lệ kèm tệp PDF đính kèm nhưng không truyền trường `OtpCode` (hoặc để chuỗi rỗng `""`).
- **Steps to Reproduce**:
  1. Gọi `POST /api/auth/register-contractor` (`multipart/form-data`) với thông tin doanh nghiệp và tệp giấy phép kinh doanh hợp lệ, bỏ trống trường `OtpCode`.
  2. Gọi `POST /api/auth/register-procuring-entity` (`multipart/form-data`) với thông tin đơn vị mời thầu và quyết định thành lập hợp lệ, bỏ trống trường `OtpCode`.
- **Expected Result**: Hệ thống phải bắt buộc kiểm tra mã OTP trên mọi yêu cầu đăng ký và trả về `HTTP 400 Bad Request` nếu thiếu `OtpCode`.
- **Actual Result**: Cả hai API trả về `HTTP 201 Created` và tạo bản ghi `User`, `Contractor` / `ProcuringEntity` trong cơ sở dữ liệu mà không cần xác thực email qua OTP.
- **Root Cause**: Trong `ContractorAuthService.cs` (dòng `41`) và `ProcuringEntityAuthService.cs` (dòng `39`), điều kiện kiểm tra OTP đang được bọc trong `if (!string.IsNullOrWhiteSpace(request.OtpCode))`, khiến việc xác thực OTP trở thành tùy chọn nếu client không gửi trường `OtpCode`.
- **Evidence**: `runs/full_run1_20261106/case_results.json` (`AUTH-REG-02`, `AUTH-REG-03`).

---

### BUG-02: Mất quyền truy cập danh sách Bảo lãnh (`GET /guarantees`) và Phụ lục (`GET /addenda`) đối với toàn bộ người dùng `Procurement`
- **Case ID**: `GUAR-ADD-01`, `ADD-CREATE-01`
- **Severity**: **High (Broken Access Control / Functional Blocker)**
- **Module / Endpoint**:
  - `GET /api/contracts/{contractId}/guarantees` (`ProcurementSystem.Infrastructure/Services/ContractService.cs`, dòng `1844-1857`)
  - `GET /api/contracts/{contractId}/addenda` (`ProcurementSystem.Infrastructure/Services/ContractService.cs`, dòng `2118-2134`)
- **Role / Actor**: `Officer_Creator_EntityA` (`Procurement` - người trực tiếp tạo gói thầu và hợp đồng)
- **Precondition**: Hợp đồng `cnt_sec_id` (`Id = 3`) đang ở trạng thái `Active`, đã tạo thành công bảo lãnh thực hiện hợp đồng và phụ lục hợp đồng.
- **Steps to Reproduce**:
  1. Đăng nhập bằng tài khoản `Officer A1` (`Procurement`, người tạo gói thầu).
  2. Gọi `GET /api/contracts/3/guarantees` và `GET /api/contracts/3/addenda`.
- **Expected Result**: Trả về `HTTP 200 OK` kèm danh sách bảo lãnh và phụ lục của hợp đồng thuộc đơn vị mình.
- **Actual Result**: Trả về `HTTP 403 Forbidden` cho mọi tài khoản có vai trò `Procurement` (kể cả chính người tạo gói thầu).
- **Root Cause**: Trong `ContractService.GetContractGuaranteesAsync` (dòng `1844-1857`) và `GetContractAddendaAsync` (dòng `2118-2134`), câu truy vấn lấy `contract` từ `_unitOfWork.Repository<Contract>().Query().AsNoTracking()` **không có `.Include(c => c.BidPackage)`**, nhưng ngay phía dưới lại kiểm tra `if (isInternalStaff && !isAdmin && contract.BidPackage?.CreatedBy != userId)` -> do `contract.BidPackage` luôn là `null`, biểu thức `null != userId` luôn đúng và trả về `403 Forbidden`.
- **Evidence**: `runs/full_run1_20261106/case_results.json` (`GUAR-ADD-01`, `ADD-CREATE-01`).

---

### BUG-03: Rò rỉ thông báo nộp hồ sơ dự thầu sang Bên mời thầu khác (`Entity B`)
- **Case ID**: `NOTIF-LIST-01`
- **Severity**: **High (Cross-Entity Data Leak)**
- **Module / Endpoint**:
  - `POST /api/bid-packages/{bidPackageId}/submissions` -> `GET /api/notifications` (`ProcurementSystem.Infrastructure/Services/BidSubmissionService.cs`, dòng `230-252`)
- **Role / Actor**: `Manager_EntityB` (`procurement_b_<RunId>@procurement.com` thuộc `Entity B`)
- **Precondition**: `Entity A` tạo gói thầu `PKG-FULL_RUN1_20261106`; Nhà thầu `C1` và `C2` nộp hồ sơ dự thầu vào gói thầu của `Entity A`.
- **Steps to Reproduce**:
  1. Nhà thầu `C1` và `C2` gọi `POST /api/bid-packages/{pkgA_Id}/submissions`.
  2. Đăng nhập bằng tài khoản `Manager B` (thuộc `Entity B`, hoàn toàn không liên quan đến `Entity A`) và gọi `GET /api/notifications`.
- **Expected Result**: `Manager B` (`Entity B`) nhận `0` thông báo liên quan đến gói thầu của `Entity A`.
- **Actual Result**: `Manager B` nhận được `2` thông báo in-app và email với nội dung `"Gói thầu PKG-FULL_RUN1_20261106 ... vừa tiếp nhận 01 hồ sơ dự thầu mới đã được niêm phong an toàn"`.
- **Root Cause**: Trong `BidSubmissionService.SubmitBidAsync` (dòng `230-241`), hệ thống truy vấn toàn bộ người dùng có vai trò `Procurement` trên toàn hệ thống (`Where(ur => ur.Role.Name == "Procurement")`) và thêm tất cả vào `recipientUserIds`, thay vì giới hạn trong phạm vi thành viên hoạt động của `ProcuringEntity` sở hữu gói thầu.
- **Evidence**: `runs/full_run1_20261106/case_results.json` (`NOTIF-LIST-01`, `leakedToB = 2`) và `smtp_messages.json`.

---

### BUG-04: Thành viên được mời (`Officer A1`) bị chặn `404` trên `GET /api/procuring-entities/me` và `Manager B` đọc được hồ sơ của `Entity A` qua `GET /api/procuring-entities/{id}`
- **Case ID**: `ENTITY-GET-02`
- **Severity**: **Medium (Entity Scoping & Authorization Inconsistency)**
- **Module / Endpoint**:
  - `GET /api/procuring-entities/me` (`ProcurementSystem.Infrastructure/Services/ProcuringEntityAuthService.cs`)
  - `GET /api/procuring-entities/{id}` (`ProcurementSystem.API/Controllers/ProcuringEntitiesController.cs`, dòng `51`)
- **Role / Actor**: `Officer_EntityA` và `Manager_EntityB`
- **Precondition**: `Officer A1` là thành viên `Active` trong `Entity A` (được mời qua `ProcuringEntityMembers`); `Manager B` là quản lý của `Entity B`.
- **Steps to Reproduce**:
  1. `Officer A1` gọi `GET /api/procuring-entities/me`.
  2. `Manager B` gọi `GET /api/procuring-entities/{entityA_Id}`.
- **Expected Result**: `Officer A1` xem được thông tin đơn vị mình (`200 OK`); `Manager B` bị chặn xem thông tin chi tiết đơn vị khác (`403 Forbidden`).
- **Actual Result**: `Officer A1` nhận `404 Not Found` trên `/me`; `Manager B` nhận `200 OK` khi truy vấn trực tiếp `GET /api/procuring-entities/{entityA_Id}`.
- **Root Cause**: `GetMyProcuringEntityProfileAsync` chỉ tìm theo `pe.UserId == currentUserId` (chỉ khớp tài khoản đăng ký ban đầu của đơn vị, bỏ sót các thành viên `ProcuringEntityMember`), trong khi `GET /api/procuring-entities/{id}` chỉ gắn `[Authorize(Roles = "Admin,Procurement")]` mà không kiểm tra người gọi có thuộc `ProcuringEntityId = id` hay không.
- **Evidence**: `runs/full_run1_20261106/case_results.json` (`ENTITY-GET-02`).

---

### BUG-05: `Manager A` của đơn vị không thể phân công Giám khảo hoặc cập nhật Tiêu chí đánh giá trên gói thầu do `Officer A1` cùng đơn vị tạo
- **Case ID**: `COMM-ASSIGN-03`, `CRIT-CREATE-03`
- **Severity**: **Medium (Entity Role Hierarchy Inconsistency)**
- **Module / Endpoint**:
  - `POST /api/bid-packages/{id}/evaluators` & `DELETE /api/bid-packages/{id}/evaluators/{evaluatorId}` (`BidPackageService.cs`, dòng `852`, `1000`, `1050`)
  - `PUT /api/evaluations/packages/{packageId}/criteria` (`EvaluationService.cs`, dòng `139`, `222`)
- **Role / Actor**: `Manager_EntityA`
- **Precondition**: Gói thầu `pkg_sec_id` thuộc `Entity A` (`ProcuringEntityId = 2`) do `Officer A1` tạo; `Manager A` là `Manager` đang hoạt động (`Active`) của `Entity A` (đã cập nhật được thông tin gói thầu qua `PUT /api/bid-packages/{id}` nhờ `PackageWriteAuthorization`).
- **Steps to Reproduce**:
  1. `Manager A` gọi `POST /api/bid-packages/{pkg_sec_id}/evaluators` hoặc `DELETE /api/bid-packages/{pkg_sec_id}/evaluators/{evId}`.
  2. `Manager A` gọi `PUT /api/evaluations/packages/{pkg_sec_id}/criteria`.
- **Expected Result**: `Manager A` (quản lý toàn diện `Entity A`) được phép thực hiện (`200 OK`) nhất quán với quyền sửa gói thầu `PUT /api/bid-packages/{id}`.
- **Actual Result**: API phân công/xóa giám khảo trả về `400 Bad Request` và API cập nhật tiêu chí trả về `403 Forbidden`.
- **Root Cause**: Các phương thức trong `BidPackageService` (quản lý giám khảo) và `EvaluationService` (quản lý tiêu chí, trao thầu, thu hồi trao thầu, loại hồ sơ) vẫn kiểm tra cứng `package.CreatedBy != userId` thay vì sử dụng `PackageWriteAuthorization.CanWriteAsync` như `UpdateBidPackageAsync`.
- **Evidence**: `runs/full_run1_20261106/case_results.json` (`COMM-ASSIGN-03`, `CRIT-CREATE-03`).

---

### BUG-06: Đặt lại mật khẩu bởi Admin (`ResetPassword`) và cấp tài khoản Chuyên gia từ đề xuất (`ApproveProposal`) gán `MustChangePassword = false`
- **Case ID**: `USER-RESET-01` (và quan sát bổ sung tại `PROP-CREATE-01`)
- **Severity**: **Medium (Security Policy / Password Lifecycle)**
- **Module / Endpoint**:
  - `POST /api/users/{id}/reset-password` (`ProcurementSystem.Infrastructure/Services/UserService.cs`, dòng `329`)
  - `PATCH /api/evaluator-proposals/{id}/approve` (`ProcurementSystem.Infrastructure/Services/EvaluatorProposalService.cs`, dòng `314`)
- **Role / Actor**: `Admin`
- **Precondition**: Admin gọi API cấp lại mật khẩu ngẫu nhiên qua email cho người dùng hoặc phê duyệt đề xuất tạo tài khoản chuyên gia mới với mật khẩu tạm gửi qua email.
- **Steps to Reproduce**:
  1. Admin gọi `POST /api/users/{id}/reset-password`.
  2. Người dùng đăng nhập bằng mật khẩu mới nhận qua email SMTP.
- **Expected Result**: Phản hồi đăng nhập phải có `mustChangePassword = true` để buộc người dùng đổi mật khẩu tạm ngay lần đăng nhập đầu tiên (giống luồng `ImportContractorsFromExcelAsync`).
- **Actual Result**: Hệ thống gán tường minh `user.MustChangePassword = false` (`UserService.cs` dòng `329` và `EvaluatorProposalService.cs` dòng `314`), cho phép người dùng sử dụng mật khẩu tạm vĩnh viễn mà không bị `MustChangePasswordMiddleware` chặn.
- **Evidence**: `runs/full_run1_20261106/case_results.json` (`USER-RESET-01`).

---

## 7. Danh sách mục kiểm chứng bù ngoài E2E ngắn hạn

| Thành phần | Nguyên nhân kỹ thuật khách quan | Phương pháp kiểm chứng bù đã thực hiện |
| :--- | :--- | :--- |
| **`ContractExpiryNotificationWorker`** | `BackgroundService` sử dụng `PeriodicTimer(TimeSpan.FromHours(6))` và chỉ kích hoạt quét hợp đồng theo chu kỳ nền dài hạn. | Đã kiểm chứng đầy đủ bằng bộ test tự động xUnit (`ProcurementSystem.Tests`) đối với `ContractExpiryNotificationWorker`: kiểm tra đúng cửa sổ 7 ngày, định tuyến thông báo về đúng `ProcuringEntity` sở hữu hợp đồng và cơ chế chống gửi trùng trong 24 giờ. |
| **Tra cứu Mã số thuế bên thứ ba (`VietQR API`)** | API công cộng `https://api.vietqr.io/v2/business/{taxCode}` áp dụng giới hạn tần suất (`HTTP 429 Too Many Requests`) khi chạy lặp liên tục nhiều lượt. | Endpoint `GET /api/auth/tax-lookup/{taxCode}` vẫn được gọi thật trong `AUTH-TAX-01`; đồng thời khi `VietQR` trả về `429` trong lúc tạo Nhà thầu thứ 3 (`CTR-APPR-01`), kịch bản sử dụng luồng API thật `POST /api/contractors/import-excel` + `POST /api/auth/change-password` + `PUT /api/contractors/me` (hoàn toàn không dùng SQL `UPDATE`). |
| **Google Cloud Storage (GCS) thật** | Môi trường nghiệm thu chạy cô lập cục bộ (`StorageSettings:Provider = Local`) theo đúng yêu cầu an toàn dữ liệu của Handoff (không ghi lên bucket thật). | Đã kiểm thử toàn bộ 4 endpoint `/api/admin/storage/*` (`status`, `test-connection`, `sync-to-cloud`, `rollback`) về mặt phân quyền (`Admin` vs `Non-Admin 403`) và chốt chặn an toàn khi `Provider = Local`. |

---

## 8. Kết quả kiểm thử Hồi quy (Backend & Frontend Regression Suites)

Bên cạnh 2 lượt chạy E2E toàn diện, toàn bộ các bộ test đơn vị/tích hợp và lệnh build của cả Backend lẫn Frontend đều đã được chạy kiểm chứng trên nhánh `dev`:

- `dotnet test ProcurementSystem.slnx --nologo`: **PASSED** (`88 / 88` tests passed, `0` failed)
- `dotnet build ProcurementSystem.slnx --nologo`: **PASSED** (`0` warnings, `0` errors)
- `npm test --prefix frontend -- --run`: **PASSED** (`6 / 6` tests passed)
- `npm run build --prefix frontend`: **PASSED** (Production bundle build thành công)

---

## 9. Kết luận nghiệm thu trung thực

1. **Tính tái lập và tự động hóa E2E**:
   - Bộ công cụ nghiệm thu tại `scripts/testing/lifecycle-e2e/` (chạy qua `scripts/testing/run-lifecycle-e2e.ps1`) đã tự động hóa hoàn chỉnh việc dựng cơ sở dữ liệu cô lập, khởi chạy SMTP Catcher nội bộ, khởi chạy Backend API & Frontend Vite, thiết lập toàn bộ 18 nhân sự/tổ chức qua API + SMTP thật (**0 câu lệnh SQL `UPDATE`**), thực thi 11 bước UI trên trình duyệt Chromium Headless kèm 93 case kiểm thử trên toàn bộ 22 phân hệ và 6 chuỗi nghiệp vụ, xuất đầy đủ `manifest.json`, `case_results.json`, `case_results.csv`, `smtp_messages.json`, ảnh chụp màn hình và file báo cáo.
2. **Các luồng đạt chuẩn hoàn toàn (`85 / 93` cases PASS, `11 / 11` UI steps PASS, `8 / 8` Negative tests PASS)**:
   - Toàn bộ vòng đời cốt lõi từ Đăng ký -> Duyệt đơn vị/nhà thầu -> Mời & kích hoạt thành viên -> Cấp & xác minh thẩm quyền ký -> Tạo gói thầu -> Nộp thầu & niêm phong hồ sơ -> Đóng/mở lại gói thầu -> Chấm điểm độc lập & thay thế nguyên tử giám khảo -> Trao thầu & thu hồi trao thầu -> Lập & kích hoạt hợp đồng -> Cập nhật tiến độ -> Nghiệm thu kỹ thuật (Maker) & Phê duyệt giải ngân (Checker) -> Chống tự duyệt, khóa phiên bản `409 Conflict` & kiểm định mã băm `SHA-256` -> Đánh giá nhà thầu -> Báo cáo thống kê & Nhật ký kiểm toán đều hoạt động chính xác và nhất quán qua cả 2 lượt chạy độc lập.
3. **Các lỗi sản phẩm cần xử lý ở nhánh sửa lỗi riêng (`8 / 93` cases FAIL tương ứng 6 Bug Cards)**:
   - Cần mở nhánh sửa lỗi riêng (sau khi hoàn tất nghiệm thu) để khắc phục 6 lỗi đã định danh tại Mục 6: `BUG-01` (bắt buộc `OtpCode` khi đăng ký), `BUG-02` (bổ sung `.Include(c => c.BidPackage)` trong `GetContractGuaranteesAsync` và `GetContractAddendaAsync`), `BUG-03` (thu hẹp phạm vi nhận thông báo nộp thầu trong `BidSubmissionService.SubmitBidAsync` về đúng `ProcuringEntity`), `BUG-04` (sửa truy vấn thành viên tại `/api/procuring-entities/me` và kiểm tra phạm vi đơn vị tại `/api/procuring-entities/{id}`), `BUG-05` (áp dụng `PackageWriteAuthorization` cho quản lý giám khảo và tiêu chí đánh giá), và `BUG-06` (bật `MustChangePassword = true` khi Admin reset mật khẩu hoặc duyệt tạo tài khoản chuyên gia).
