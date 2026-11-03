-- ==============================================================================
-- SCRIPT 03: RECONCILIATION AUDIT REPORT
-- Target Database: procurement_db
-- Purpose: Báo cáo đối soát và phân tầng dữ liệu gói thầu sau Migration
-- ==============================================================================

USE [procurement_db];
GO

PRINT '==============================================================================';
PRINT '   BÁO CÁO ĐỐI SOÁT TỔNG THỂ CỜ KIỂM TRA (OWNERSHIP NEEDS REVIEW)            ';
PRINT '==============================================================================';

-- 1. Thống kê tổng hợp số lượng
SELECT 
    COUNT(*) AS [Tổng số gói thầu],
    SUM(CASE WHEN [OwnershipNeedsReview] = 1 THEN 1 ELSE 0 END) AS [Gói thầu mang cờ Cần đối soát (Review)],
    SUM(CASE WHEN [OwnershipNeedsReview] = 0 THEN 1 ELSE 0 END) AS [Gói thầu đã sạch cờ (Clean)],
    SUM(CASE WHEN [ProcuringEntityId] IS NOT NULL THEN 1 ELSE 0 END) AS [Gói thầu đã có Bên mời thầu],
    SUM(CASE WHEN [ProcuringEntityId] IS NULL THEN 1 ELSE 0 END) AS [Gói thầu chưa gán Bên mời thầu]
FROM [BidPackages];

-- 2. Chi tiết phân tầng đối soát pháp lý (Reconciliation Tiers)
SELECT 
    bp.[Id] AS [Mã Gói thầu],
    bp.[Code] AS [Mã Ký hiệu],
    bp.[Name] AS [Tên Gói thầu],
    CASE bp.[Status]
        WHEN 0 THEN N'Bản nháp (Draft)'
        WHEN 1 THEN N'Đang mở thầu (Open)'
        WHEN 2 THEN N'Đang thẩm định (Evaluating)'
        WHEN 3 THEN N'Đã trao thầu (Awarded)'
        WHEN 4 THEN N'Hoàn tất (Completed)'
        WHEN 5 THEN N'Hủy thầu (Cancelled)'
        ELSE N'Không xác định'
    END AS [Trạng thái],
    bp.[CreatedBy] AS [Người tạo],
    u.[FullName] AS [Tên người tạo],
    bp.[ProcuringEntityId] AS [ID Bên mời thầu],
    ISNULL(pe.[OrganizationName], N'— Chưa gán —') AS [Tên Bên mời thầu],
    bp.[OwnershipNeedsReview] AS [Cờ Review],
    CASE 
        WHEN bp.[OwnershipNeedsReview] = 0 AND bp.[ProcuringEntityId] IS NOT NULL 
            THEN N'HỢP LỆ: Đã đối soát & chuẩn hóa đầy đủ căn cứ'
        WHEN bp.[OwnershipNeedsReview] = 1 AND bp.[CreatedBy] = 4 
            THEN N'TIER 1 (Độ tin cậy cao): Cán bộ Entity 1 tạo lập, có QĐ thành lập số 123/QĐ'
        WHEN bp.[OwnershipNeedsReview] = 1 AND bp.[CreatedBy] = 1 
            THEN N'TIER 2 (Môi trường Test): Quản trị viên (Admin) tạo phục vụ kiểm thử'
        ELSE N'TIER 3 (Cách ly/Quarantine): Thiếu bằng chứng, cần hội đồng pháp lý xác minh'
    END AS [Phân tầng đối soát]
FROM [BidPackages] bp
LEFT JOIN [Users] u ON u.[Id] = bp.[CreatedBy]
LEFT JOIN [ProcuringEntities] pe ON pe.[Id] = bp.[ProcuringEntityId]
ORDER BY bp.[Id] ASC;

-- 3. Kiểm tra các bản ghi Nhật ký kiểm toán (Audit Log) tương ứng
SELECT 
    al.[Id] AS [LogId],
    al.[Action],
    al.[EntityType],
    al.[EntityId] AS [PackageId],
    al.[OldValues],
    al.[NewValues],
    al.[Timestamp]
FROM [AuditLogs] al
WHERE al.[Action] = 'QUARANTINE_PACKAGE_OWNERSHIP'
ORDER BY al.[Id] DESC;
GO
