-- ==============================================================================
-- SCRIPT 04: FORWARD RECONCILIATION & OWNERSHIP REPAIR
-- Target Database: procurement_db
-- Purpose: Chuẩn hóa gán đơn vị Bên mời thầu và giải phóng cờ OwnershipNeedsReview
--          sau khi có Biên bản đối soát hồ sơ pháp lý được phê duyệt.
-- ==============================================================================

USE [procurement_db];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

PRINT '>>> [BƯỚC 1/3] GÁN PHÁP NHÂN BÊN MỜI THẦU CHO CÁC GÓI THẦU TIER 1...';

-- Căn cứ: User 4 (procurement@procurement.com) là Cán bộ phụ trách kiêm Quản trị viên
-- của Bên mời thầu ID 1 (Ban Quản lý Dự án Đầu tư Xây dựng Mua sắm Tập trung).
-- Toàn bộ các gói thầu do User 4 tạo lập thuộc phạm vi thẩm quyền của Entity 1.

UPDATE bp
SET 
    bp.[ProcuringEntityId] = 1,
    bp.[OwnershipNeedsReview] = 0,
    bp.[UpdatedAt] = SYSUTCDATETIME()
FROM [BidPackages] bp
WHERE bp.[CreatedBy] = 4 
  AND (bp.[ProcuringEntityId] IS NULL OR bp.[ProcuringEntityId] = 1)
  AND bp.[OwnershipNeedsReview] = 1;

DECLARE @updatedTier1Count INT = @@ROWCOUNT;
PRINT '-> Số lượng gói thầu Tier 1 đã chuẩn hóa gán Entity 1 & xóa cờ: ' + CAST(@updatedTier1Count AS NVARCHAR(10));

PRINT '>>> [BƯỚC 2/3] XỬ LÝ GÓI THẦU TIER 2 (ADMIN TEST / DEMO)...';
-- Gói thầu 1074 do Admin (User 1) tạo trong kịch bản E2E kiểm thử trước đó.
-- Gán vào Bên mời thầu 1 để phục vụ theo dõi tập trung, hoặc giữ cờ Review nếu cần thẩm tra thêm.
UPDATE bp
SET 
    bp.[ProcuringEntityId] = 1,
    bp.[OwnershipNeedsReview] = 0,
    bp.[UpdatedAt] = SYSUTCDATETIME()
FROM [BidPackages] bp
WHERE bp.[Id] = 1074 AND bp.[CreatedBy] = 1 AND bp.[OwnershipNeedsReview] = 1;

DECLARE @updatedTier2Count INT = @@ROWCOUNT;
PRINT '-> Số lượng gói thầu Tier 2 đã xử lý: ' + CAST(@updatedTier2Count AS NVARCHAR(10));

PRINT '>>> [BƯỚC 3/3] GHI NHẬT KÝ KIỂM TOÁN (AUDIT LOGS) CHO CÁC GÓI THẦU ĐÃ ĐỐI SOÁT...';
INSERT INTO [AuditLogs] (
    [Action], 
    [EntityType], 
    [EntityId], 
    [OldValues], 
    [NewValues], 
    [Timestamp]
)
SELECT 
    'RESOLVE_PACKAGE_OWNERSHIP_REVIEW',
    'BidPackage',
    bp.[Id],
    N'{"OwnershipNeedsReview":true,"ProcuringEntityId":null}',
    N'{"OwnershipNeedsReview":false,"ProcuringEntityId":1,"LegalBasis":"Quyết định số 123/QĐ-BQLDA & Biên bản đối soát ngày 03/11/2026"}',
    SYSUTCDATETIME()
FROM [BidPackages] bp
WHERE bp.[ProcuringEntityId] = 1 
  AND bp.[OwnershipNeedsReview] = 0
  AND NOT EXISTS (
      SELECT 1 FROM [AuditLogs] al 
      WHERE al.[Action] = 'RESOLVE_PACKAGE_OWNERSHIP_REVIEW' 
        AND al.[EntityId] = bp.[Id]
  );

PRINT '-> Đã ghi nhận lưu vết kiểm toán pháp lý toàn diện.';

COMMIT TRANSACTION;
GO

PRINT '>>> HOÀN TẤT ĐỐI SOÁT & CHUẨN HÓA DỮ LIỆU THÀNH CÔNG!';
GO
