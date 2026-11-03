-- ==============================================================================
-- SCRIPT 05: DISASTER RECOVERY & ROLLBACK RUNBOOK
-- Target Database: procurement_db
-- Purpose: Kịch bản phục hồi khẩn cấp khi phát sinh sự cố trong hoặc sau Migration
-- ==============================================================================

USE master;
GO

PRINT '==============================================================================';
PRINT '   KỊCH BẢN PHỤC HỒI KHẨN CẤP (DISASTER RECOVERY / ROLLBACK)                 ';
PRINT '==============================================================================';
PRINT 'LƯU Ý: Chỉ thực thi kịch bản này khi có quyết định hủy bỏ đợt phát hành (Abort/Rollback)';
PRINT 'từ Trưởng nhóm Kỹ thuật hoặc Quản trị viên CSDL (DBA).';
PRINT '------------------------------------------------------------------------------';
GO

-- ==============================================================================
-- PHƯƠNG ÁN 1: PHỤC HỒI HOÀN TOÀN TỪ BẢN SAO LƯU TRƯỚC MIGRATION (KHUYÊN DÙNG)
-- Bảo đảm CSDL trở về trạng thái toàn vẹn 100% trước thời điểm bắt đầu bảo trì.
-- ==============================================================================

DECLARE @backupFile NVARCHAR(500);

-- Tự động lấy tệp sao lưu PRE_MIGRATION mới nhất trong thư mục C:\temp\backups
SELECT TOP 1 @backupFile = physical_device_name
FROM msdb.dbo.backupmediafamily
WHERE physical_device_name LIKE N'%procurement_db_PRE_MIGRATION_%'
ORDER BY media_set_id DESC;

PRINT '-> Tệp sao lưu sử dụng để khôi phục: ' + ISNULL(@backupFile, N'KHÔNG TÌM THẤY TỆP!');

IF @backupFile IS NOT NULL
BEGIN
    PRINT '>>> [BƯỚC 1/3] NGẮT TOÀN BỘ KẾT NỐI VÀ CHUYỂN CSDL SANG CHẾ ĐỘ SINGLE_USER...';
    ALTER DATABASE [procurement_db] 
    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    PRINT '>>> [BƯỚC 2/3] TIẾN HÀNH RESTORE DATABASE ĐÈ LÊN CSDL HIỆN TẠI (WITH REPLACE)...';
    RESTORE DATABASE [procurement_db]
    FROM DISK = @backupFile
    WITH 
        REPLACE,
        RECOVERY,
        STATS = 10;

    PRINT '>>> [BƯỚC 3/3] MỞ LẠI KẾT NỐI BÌNH THƯỜNG (MULTI_USER)...';
    ALTER DATABASE [procurement_db] 
    SET MULTI_USER;

    PRINT '>>> PHỤC HỒI THÀNH CÔNG! HỆ THỐNG ĐÃ TRỞ VỀ TRẠNG THÁI TRƯỚC MIGRATION.';
END
ELSE
BEGIN
    RAISERROR(N'LỖI: Không tìm thấy tệp sao lưu hợp lệ. Vui lòng kiểm tra lại thư mục sao lưu!', 16, 1);
END;
GO

-- ==============================================================================
-- PHƯƠNG ÁN 2: HOÀN TÁC CẤU TRÚC BẰNG DDL (NẾU KHÔNG MUỐN RESTORE TOÀN BỘ FILE)
-- ==============================================================================
/*
USE [procurement_db];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Xóa cờ kiểm tra và cột phân quyền
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BidPackages') AND name = 'OwnershipNeedsReview')
    ALTER TABLE [BidPackages] DROP COLUMN [OwnershipNeedsReview];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Acceptances_MilestoneId_Revision')
    DROP INDEX [IX_Acceptances_MilestoneId_Revision] ON [Acceptances];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SigningAuthorities')
    DROP TABLE [SigningAuthorities];

-- 2. Xóa các bản ghi migration trong lịch sử EF Core
DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] IN (
    N'20261030120000_QuarantineUnprovenPackageOwnership',
    N'20261029173000_AddBidPackageProcuringEntityOwnership',
    N'20261029160000_FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency',
    N'20261028094453_AddTransparencyControlsIntegrityFixes'
);

COMMIT TRANSACTION;
GO
*/
