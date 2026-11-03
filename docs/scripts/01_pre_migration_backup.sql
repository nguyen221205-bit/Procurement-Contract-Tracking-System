-- ==============================================================================
-- SCRIPT 01: PRE-MIGRATION BACKUP & INTEGRITY VERIFICATION
-- Target Database: procurement_db (Production)
-- Purpose: Tạo bản sao lưu toàn vẹn (Full Backup) trước khi áp dụng Migration
-- ==============================================================================

USE master;
GO

PRINT '>>> [BƯỚC 1/3] KIỂM TRA TRẠNG THÁI CSDL VÀ TIẾN TRÌNH KẾT NỐI...';
SELECT 
    name AS DatabaseName,
    state_desc AS CurrentState,
    recovery_model_desc AS RecoveryModel,
    compatibility_level AS CompatibilityLevel
FROM sys.databases
WHERE name = N'procurement_db';

-- Kiểm tra số lượng phiên kết nối đang mở
SELECT 
    COUNT(session_id) AS ActiveConnections
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID(N'procurement_db');
GO

PRINT '>>> [BƯỚC 2/3] TIẾN HÀNH SAO LƯU TOÀN VẸN (FULL BACKUP VỚI CHECKSUM & COMPRESSION)...';
DECLARE @backupDir NVARCHAR(255) = N'C:\temp\backups';
EXEC master.dbo.xp_create_subdir @backupDir;

DECLARE @timestamp NVARCHAR(30) = CONVERT(NVARCHAR(8), GETDATE(), 112) + '_' + 
                                  REPLACE(CONVERT(NVARCHAR(8), GETDATE(), 108), ':', '');
DECLARE @backupFile NVARCHAR(500) = @backupDir + N'\procurement_db_PRE_MIGRATION_' + @timestamp + N'.bak';
DECLARE @backupName NVARCHAR(255) = N'procurement_db Pre-Migration Full Backup ' + @timestamp;

PRINT '-> Đường dẫn tệp sao lưu: ' + @backupFile;

BACKUP DATABASE [procurement_db]
TO DISK = @backupFile
WITH 
    FORMAT,
    INIT,
    CHECKSUM,
    COMPRESSION,
    STATS = 10,
    NAME = @backupName;
GO

PRINT '>>> [BƯỚC 3/3] KIỂM TRA TOÀN VẸN BẢN SAO LƯU (RESTORE VERIFYONLY)...';
DECLARE @backupDir NVARCHAR(255) = N'C:\temp\backups';
-- Lấy tệp mới nhất
DECLARE @latestBackup NVARCHAR(500);
SELECT TOP 1 @latestBackup = physical_device_name
FROM msdb.dbo.backupmediafamily
WHERE physical_device_name LIKE N'%procurement_db_PRE_MIGRATION_%'
ORDER BY media_set_id DESC;

PRINT '-> Kiểm tra tệp: ' + @latestBackup;

RESTORE VERIFYONLY 
FROM DISK = @latestBackup 
WITH CHECKSUM;

PRINT '>>> HOÀN TẤT SAO LƯU AN TOÀN! SẴN SÀNG THỰC THI MIGRATION.';
GO
