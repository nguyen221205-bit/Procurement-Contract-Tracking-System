# KẾ HOẠCH TRIỂN KHAI MIGRATION & ĐỐI SOÁT CSDL CHÍNH THỨC
## (PRODUCTION MIGRATION & DATA RECONCILIATION RUNBOOK)

**Dự án**: Hệ thống Quản lý Đấu thầu và Theo dõi Hợp đồng (*Procurement & Contract Tracking System*)  
**Phân hệ**: Quản trị Dữ liệu & Kiểm soát Tính Minh Bạch (*Transparency & Ownership Controls*)  
**Ngày lập kế hoạch**: 03/11/2026  
**Người thực hiện**: Phạm Minh Tài — Thực tập sinh Kỹ thuật Backend  
**Môi trường áp dụng**: SQL Server 2022 Production (`procurement_db`)  
**Tình trạng kiểm thử tiên quyết**: Đạt kết quả toàn diện trên CSDL bản sao (`procurement_db_replica`), 63/63 bài kiểm thử Backend C# và 6/6 bài kiểm thử Frontend vượt qua (*Passed*).

---

## I. MỤC TIÊU & CƠ SỞ PHÁP LÝ

### 1. Mục tiêu kỹ thuật
1. **Thiết lập cơ chế kiểm soát chéo Maker - Checker**: Triển khai cấu trúc bảng thẩm quyền ký duyệt pháp lý (`SigningAuthorities`), kiểm soát mốc giải ngân (`ContractMilestones`) và nghiệm thu (`Acceptances`) với kiểm tra phân tách trách nhiệm tuyệt đối.
2. **Bảo toàn dữ liệu bất biến (Snapshot Immutability)**: Lưu trữ mã băm SHA-256 (`SnapshotDocumentHash`), đường dẫn tài liệu và số tiền nghiệm thu nhằm ngăn chặn mọi hành vi chỉnh sửa lén sau khi ký duyệt.
3. **Kiểm soát tranh chấp đồng thời (Optimistic Concurrency Control)**: Bổ sung cột phiên bản dữ liệu `RowVersion` và `Version` trên các bảng giao dịch tài chính trọng yếu.
4. **Giải quyết triệt để dữ liệu gói thầu lịch sử thiếu pháp nhân thụ hưởng**: Kích hoạt cờ kiểm tra `OwnershipNeedsReview`, đưa toàn bộ các gói thầu chưa có bằng chứng vào vùng thẩm định độc lập (*Quarantine*), ngăn chặn việc tự ý gán quyền sở hữu sai lệch pháp luật.

### 2. Căn cứ pháp lý
- **Luật Đấu thầu số 22/2023/QH15**: Quy định về tính minh bạch, lưu vết kiểm toán và trách nhiệm giải trình của Bên mời thầu / Chủ đầu tư.
- **Nghị định số 24/2024/NĐ-CP**: Quy định chi tiết một số điều và biện pháp thi hành Luật Đấu thầu về lựa chọn nhà thầu, nghiệm thu và thanh quyết toán hợp đồng.

---

## II. BẢN ĐỒ THAY ĐỔI CẤU TRÚC DỮ LIỆU (SCHEMA CHANGES)

Đợt nâng cấp CSDL chính thức bao gồm chuỗi 4 bản migration liên tiếp từ EF Core 8.0:

```
[20261027052112_AddProcuringEntityMembershipAndInvitations] (Trạng thái hiện tại trên Production)
       │
       ▼
[20261028094453_AddTransparencyControlsIntegrityFixes]
  • Tạo bảng [SigningAuthorities] (Thẩm quyền ký duyệt pháp lý).
  • Bổ sung [RowVersion], [Version] trên [ContractMilestones].
  • Bổ sung Snapshot fields: [SnapshotAmount], [SnapshotDocumentHash] (SHA-256), [SnapshotDocumentPath], [Revision].
       │
       ▼
[20261029160000_FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency]
  • Bổ sung [RowVersion] cho [ProcuringEntityMembers] (Chống tranh chấp khi cập nhật vai trò).
       │
       ▼
[20261029173000_AddBidPackageProcuringEntityOwnership]
  • Bổ sung cột [ProcuringEntityId] trên [BidPackages] kèm khóa ngoại liên kết tới [ProcuringEntities].
  • Tạo Unique Index [IX_Acceptances_MilestoneId_Revision] (Đảm bảo mỗi mốc chỉ có 1 bản nghiệm thu duy nhất trên mỗi revision).
       │
       ▼
[20261030120000_QuarantineUnprovenPackageOwnership]
  • Bổ sung cờ [OwnershipNeedsReview] (bit, mặc định 0).
  • Tự động kích hoạt cờ Review cho các gói thầu lịch sử thiếu bằng chứng liên kết Bên mời thầu.
  • Tự động ghi nhật ký kiểm toán [AuditLogs] với hành động 'QUARANTINE_PACKAGE_OWNERSHIP'.
```

---

## III. QUY TRÌNH SAO LƯU & AN TOÀN TRƯỚC MIGRATION (PRE-MIGRATION)

### 1. Thời gian bảo trì đề xuất (Maintenance Window)
- **Khung giờ thực hiện**: 23:00 – 00:30 (Khung giờ thấp điểm, lưu lượng truy cập gần bằng 0).
- **Thời lượng dự kiến**: 30 phút (bao gồm 5 phút sao lưu, 5 phút chạy migration, 10 phút đối soát dữ liệu và 10 phút kiểm tra hệ thống).
- **RTO (Recovery Time Objective)**: Tối đa 15 phút trong trường hợp kích hoạt kịch bản Rollback.
- **RPO (Recovery Point Objective)**: 0 phút (không mất mát dữ liệu nhờ sao lưu Full Backup ngay trước giờ bảo trì).

### 2. Kịch bản Sao lưu Toàn vẹn (Full Backup with Checksum & Compression)
Trước khi thực thi bất kỳ câu lệnh DDL nào, quản trị viên CSDL bắt buộc phải chạy kịch bản sao lưu:

- **Tệp kịch bản**: `docs/scripts/01_pre_migration_backup.sql`
- **Lệnh thực thi qua SQL Server Management Studio (SSMS) hoặc sqlcmd**:
  ```sql
  USE master;
  GO
  DECLARE @backupDir NVARCHAR(255) = N'C:\temp\backups';
  EXEC master.dbo.xp_create_subdir @backupDir;

  DECLARE @timestamp NVARCHAR(30) = CONVERT(NVARCHAR(8), GETDATE(), 112) + '_' + 
                                    REPLACE(CONVERT(NVARCHAR(8), GETDATE(), 108), ':', '');
  DECLARE @backupFile NVARCHAR(500) = @backupDir + N'\procurement_db_PRE_MIGRATION_' + @timestamp + N'.bak';

  BACKUP DATABASE [procurement_db]
  TO DISK = @backupFile
  WITH FORMAT, INIT, CHECKSUM, COMPRESSION, STATS = 10,
       NAME = N'procurement_db Pre-Migration Full Backup';

  -- Kiểm tra tính hợp lệ của bản sao lưu
  RESTORE VERIFYONLY FROM DISK = @backupFile WITH CHECKSUM;
  GO
  ```
- **Tiêu chuẩn chấp nhận**: Thông báo trả về `"The backup set on file 1 is valid"` và tốc độ sao lưu đạt trên 100 MB/giây.

---

## IV. QUY TRÌNH THỰC THI MIGRATION TRÊN CSDL CHÍNH THỨC

### 1. Thiết lập CSDL sang chế độ bảo trì
Nhằm ngăn chặn các giao dịch mới phát sinh trong quá trình sửa đổi schema:
```sql
USE master;
GO
ALTER DATABASE [procurement_db] SET RESTRICTED_USER WITH ROLLBACK IMMEDIATE;
GO
```

### 2. Áp dụng Kịch bản Idempotent Migration
- **Tệp kịch bản**: `docs/scripts/02_production_migration_apply.sql`
- **Đặc tính kỹ thuật**:
  - Tự động kiểm tra bảng `__EFMigrationsHistory`, chỉ chạy các khối lệnh chưa được áp dụng.
  - Sử dụng cờ `SET XACT_ABORT ON;` bảo đảm mọi lỗi phát sinh sẽ kích hoạt `ROLLBACK TRANSACTION` toàn bộ chuỗi lệnh, ngăn chặn trạng thái CSDL bị phân mảnh dở dang.
  - Tự động chuẩn hóa trùng lặp dữ liệu `Revision` trên bảng `Acceptances` trước khi tạo Unique Index, triệt tiêu nguy cơ lỗi vi phạm ràng buộc dữ liệu cũ.

### 3. Kiểm tra tính toàn vẹn sau khi chạy script
Ngay sau khi lệnh commit thành công, thực hiện truy vấn kiểm tra:
```sql
USE [procurement_db];
GO
SELECT MigrationId, ProductVersion 
FROM [__EFMigrationsHistory] 
ORDER BY MigrationId DESC;
```
**Yêu cầu nghiệm thu**: CSDL phải ghi nhận đủ 13 bản ghi migration (bao gồm 4 migration mới nhất).

---

## V. CHIẾN LƯỢC ĐỐI SOÁT DỮ LIỆU CỜ OWNERSHIPNEEDSREVIEW

Theo kiến trúc an toàn, khi bản migration thứ 4 chạy xong, toàn bộ 10 gói thầu hiện hữu trên Production sẽ được gắn cờ `OwnershipNeedsReview = 1` và ghi nhận sự kiện cách ly vào bảng `AuditLogs`.

Quá trình đối soát được phân định thành **3 tầng dữ liệu (3 Tiers)** rõ ràng:

```
┌────────────────────────────────────────────────────────────────────────┐
│             TỔNG SỐ 10 GÓI THẦU TRÊN PRODUCTION (CỜ REVIEW = 1)        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
          ┌─────────────────────────┼─────────────────────────┐
          ▼                         ▼                         ▼
┌───────────────────┐     ┌───────────────────┐     ┌───────────────────┐
│      TIER 1       │     │      TIER 2       │     │      TIER 3       │
│  (9 GÓI THẦU)     │     │   (1 GÓI THẦU)    │     │   (0 GÓI THẦU)    │
│  CreatedBy = 4    │     │   CreatedBy = 1   │     │  Vô chủ / Không   │
│  Thuộc Ban QLDA 1 │     │ Gói thầu Admin    │     │  rõ nguồn gốc     │
└─────────┬─────────┘     └─────────┬─────────┘     └─────────┬─────────┘
          │                         │                         │
          ▼                         ▼                         ▼
┌───────────────────┐     ┌───────────────────┐     ┌───────────────────┐
│ Căn cứ QĐ 123/QĐ  │     │ Căn cứ mục đích   │     │ Tiếp tục giữ cờ   │
│ Gán EntityId = 1  │     │ kiểm thử E2E      │     │ Review = 1 để     │
│ Xóa cờ Review = 0 │     │ Gán EntityId = 1  │     │ thanh tra hồ sơ   │
│ Ghi Audit Log     │     │ Xóa cờ Review = 0 │     │                   │
└───────────────────┘     └───────────────────┘     └───────────────────┘
```

### 1. Báo cáo đối soát hiện trạng
- **Tệp kịch bản**: `docs/scripts/03_reconciliation_audit_report.sql`
- Kịch bản xuất danh sách phân loại chi tiết từng gói thầu, người tạo lập, pháp nhân tương ứng và tình trạng cờ.

### 2. Kịch bản chuẩn hóa & Giải phóng cờ đối soát (Forward Repair)
Sau khi Hội đồng thẩm định độc soát ký biên bản xác nhận pháp lý:
- **Tệp kịch bản**: `docs/scripts/04_forward_reconciliation_repair.sql`
- **Nội dung thực thi**:
  1. Gán `ProcuringEntityId = 1` cho 9 gói thầu Tier 1 (ID 1069, 1070, 1071, 1072, 1073, 1075, 2069, 2070, 2071) và chuyển `OwnershipNeedsReview = 0`.
  2. Gán `ProcuringEntityId = 1` cho gói thầu Tier 2 (ID 1074) do Admin tạo lập và chuyển `OwnershipNeedsReview = 0`.
  3. Ghi nhận 10 bản ghi nhật ký kiểm toán mới với hành động `'RESOLVE_PACKAGE_OWNERSHIP_REVIEW'` kèm căn cứ pháp lý: *"Quyết định số 123/QĐ-BQLDA & Biên bản đối soát ngày 03/11/2026"*.

---

## VI. KỊCH BẢN PHỤC HỒI KHẨN CẤP (DISASTER RECOVERY / ROLLBACK)

Nếu trong quá trình thực thi phát sinh sự cố nghiêm trọng (lỗi vi phạm khóa ngoại không thể khắc phục hoặc ứng dụng bị gián đoạn):

- **Tệp kịch bản**: `docs/scripts/05_disaster_recovery_rollback.sql`
- **Phương án 1 (Khuyên dùng - Phục hồi từ bản sao lưu)**:
  ```sql
  USE master;
  GO
  ALTER DATABASE [procurement_db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  GO
  RESTORE DATABASE [procurement_db]
  FROM DISK = N'C:\temp\backups\procurement_db_PRE_MIGRATION_<timestamp>.bak'
  WITH REPLACE, RECOVERY, STATS = 10;
  GO
  ALTER DATABASE [procurement_db] SET MULTI_USER;
  GO
  ```
- **Phương án 2 (Rollback Schema bằng DDL)**: Kịch bản hoàn tác từng bước bằng lệnh `DROP COLUMN`, `DROP TABLE` và xóa dòng tương ứng trong `__EFMigrationsHistory` (được cung cấp sẵn trong phần comment của file script).

---

## VII. BẢNG KIỂM TRA VẬN HÀNH (OPERATIONAL CHECKLIST)

| Giai đoạn | Hạng mục kiểm tra | Tiêu chuẩn đạt | Người phụ trách | Xác nhận |
| :--- | :--- | :--- | :--- | :---: |
| **Trước triển khai** | Dung lượng ổ đĩa lưu trữ bản sao lưu | Trống tối thiểu 10 GB tại `C:\temp\backups` | Quản trị CSDL | [x] Đạt |
| | Ngắt kết nối phiên làm việc bên ngoài | 0 kết nối người dùng thông thường | Quản trị CSDL | [x] Đạt |
| | Kiểm tra Full Backup & Verify | Trả về `The backup set on file 1 is valid` | Quản trị CSDL | [x] Đạt |
| **Trong triển khai** | Thực thi `02_production_migration_apply.sql` | Exit Code = 0, không có thông báo lỗi | Kỹ sư triển khai | [ ] Chờ |
| | Kiểm tra bảng `__EFMigrationsHistory` | Đủ 13 bản ghi migration | Kỹ sư triển khai | [ ] Chờ |
| | Chạy báo cáo `03_reconciliation_audit_report.sql` | 10 gói thầu vào diện Review an toàn | Kỹ sư triển khai | [ ] Chờ |
| | Chạy chuẩn hóa `04_forward_reconciliation_repair.sql` | 10 gói thầu gán đúng Entity 1, Review = 0 | Kỹ sư triển khai | [ ] Chờ |
| **Sau triển khai** | Mở lại kết nối `MULTI_USER` | CSDL ONLINE và tiếp nhận truy vấn | Quản trị CSDL | [ ] Chờ |
| | Khởi động lại dịch vụ Backend API | Health check trả về HTTP 200 OK | Kỹ sư vận hành | [ ] Chờ |
| | Chạy kiểm thử tự động E2E nhanh (Smoke Test) | Đăng nhập và xem danh sách gói thầu thông suốt | Kỹ sư QA/Test | [ ] Chờ |

---
**Tài liệu tham chiếu đính kèm**:
- Bộ mã lệnh SQL: [01_pre_migration_backup.sql](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/docs/scripts/01_pre_migration_backup.sql)
- Bộ mã lệnh SQL: [02_production_migration_apply.sql](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/docs/scripts/02_production_migration_apply.sql)
- Bộ mã lệnh SQL: [03_reconciliation_audit_report.sql](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/docs/scripts/03_reconciliation_audit_report.sql)
- Bộ mã lệnh SQL: [04_forward_reconciliation_repair.sql](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/docs/scripts/04_forward_reconciliation_repair.sql)
- Bộ mã lệnh SQL: [05_disaster_recovery_rollback.sql](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/docs/scripts/05_disaster_recovery_rollback.sql)
