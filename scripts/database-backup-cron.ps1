<#
.SYNOPSIS
    Kịch bản tự động sao lưu cơ sở dữ liệu và tệp đính kèm theo lịch định kỳ (Scheduled Backup Job)
    Áp dụng cho môi trường phát triển & sản xuất Hệ thống Quản lý Đấu thầu & Theo dõi Hợp đồng.
.DESCRIPTION
    1. Tạo bản sao lưu Full Backup cho cơ sở dữ liệu SQL Server với nén (COMPRESSION) và kiểm định mã băm (CHECKSUM).
    2. Xác thực tính toàn vẹn ngay sau khi sao lưu (RESTORE VERIFYONLY).
    3. Tự động nén thư mục uploads thành tệp ZIP lưu trữ.
    4. Tự động dọn dẹp các bản sao lưu cũ quá RetentionDays ngày để tối ưu dung lượng ổ đĩa.
    5. Ghi nhật ký tiến trình vào tệp backups/backup_history.log.
.PARAMETER RetentionDays
    Số ngày lưu trữ bản sao lưu trước khi tự động dọn dẹp (Mặc định: 14 ngày).
.PARAMETER ServerInstance
    Địa chỉ máy chủ SQL Server (Mặc định: localhost).
.PARAMETER DatabaseName
    Tên cơ sở dữ liệu (Mặc định: procurement_db).
#>

param (
    [int]$RetentionDays = 14,
    [string]$ServerInstance = "localhost",
    [string]$DatabaseName = "procurement_db",
    [string]$SqlUser = "sa",
    [string]$SqlPassword = "123"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$backupRootDir = Join-Path $projectRoot "backups"
$dbBackupDir = Join-Path $backupRootDir "db"
$docsBackupDir = Join-Path $backupRootDir "docs"
$logFile = Join-Path $backupRootDir "backup_history.log"
$uploadsDir = Join-Path $projectRoot "uploads"

# Đảm bảo các thư mục tồn tại
if (-not (Test-Path $dbBackupDir)) { New-Item -ItemType Directory -Path $dbBackupDir -Force | Out-Null }
if (-not (Test-Path $docsBackupDir)) { New-Item -ItemType Directory -Path $docsBackupDir -Force | Out-Null }

# Tự động nạp cấu hình kết nối từ biến môi trường hoặc appsettings.json nếu dùng mật khẩu mặc định
if ($SqlPassword -eq "123") {
    $envConn = $env:PROCUREMENT_DB_CONN
    $appsettingsDev = Join-Path $projectRoot "ProcurementSystem.API\appsettings.Development.json"
    $appsettingsProd = Join-Path $projectRoot "ProcurementSystem.API\appsettings.json"
    
    $resolvedConn = $null
    if (-not [string]::IsNullOrWhiteSpace($envConn)) {
        $resolvedConn = $envConn
    } elseif (Test-Path $appsettingsDev) {
        try {
            $json = Get-Content $appsettingsDev -Raw | ConvertFrom-Json
            $resolvedConn = $json.ConnectionStrings.DefaultConnection
        } catch {
            # Bỏ qua nếu lỗi đọc tệp
        }
    } elseif (Test-Path $appsettingsProd) {
        try {
            $json = Get-Content $appsettingsProd -Raw | ConvertFrom-Json
            $resolvedConn = $json.ConnectionStrings.DefaultConnection
        } catch {
            # Bỏ qua nếu lỗi đọc tệp
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($resolvedConn)) {
        if ($resolvedConn -match "Server=([^;]+)") { $ServerInstance = $matches[1] }
        elseif ($resolvedConn -match "Data Source=([^;]+)") { $ServerInstance = $matches[1] }
        
        if ($resolvedConn -match "Database=([^;]+)") { $DatabaseName = $matches[1] }
        elseif ($resolvedConn -match "Initial Catalog=([^;]+)") { $DatabaseName = $matches[1] }
        
        if ($resolvedConn -match "User Id=([^;]+)") { $SqlUser = $matches[1] }
        elseif ($resolvedConn -match "User ID=([^;]+)") { $SqlUser = $matches[1] }
        elseif ($resolvedConn -match "Uid=([^;]+)") { $SqlUser = $matches[1] }
        
        if ($resolvedConn -match "Password=([^;]+)") { $SqlPassword = $matches[1] }
        elseif ($resolvedConn -match "Pwd=([^;]+)") { $SqlPassword = $matches[1] }
    }
}

function Write-BackupLog {
    param ([string]$Message, [string]$Level = "INFO")
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $logLine = "[$timestamp] [$Level] $Message"
    Write-Host $logLine
    Add-Content -Path $logFile -Value $logLine -Encoding utf8
}

Write-BackupLog "=========================================================="
Write-BackupLog "BẮT ĐẦU TIẾN TRÌNH SAO LƯU ĐỊNH KỲ (SCHEDULED BACKUP JOB)"
Write-BackupLog "=========================================================="

# 1. SAO LƯU CƠ SỞ DỮ LIỆU SQL SERVER
$timestamp = (Get-Date).ToString("yyyyMMdd_HHmmss")
$dbBackupFileName = "${DatabaseName}_backup_${timestamp}.bak"
$dbBackupFilePath = Join-Path $dbBackupDir $dbBackupFileName

try {
    Write-BackupLog "Đang tiến hành sao lưu cơ sở dữ liệu $DatabaseName vào $dbBackupFilePath..."
    $backupSql = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$dbBackupFilePath'
WITH FORMAT, INIT, CHECKSUM, COMPRESSION, NAME = N'$DatabaseName Automated Backup $timestamp';

RESTORE VERIFYONLY FROM DISK = N'$dbBackupFilePath' WITH CHECKSUM;
"@

    $backupOutput = sqlcmd -S $ServerInstance -U $SqlUser -P $SqlPassword -Q $backupSql 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Lệnh sqlcmd trả về mã lỗi: $LASTEXITCODE. Chi tiết: $backupOutput"
    }

    $fileInfo = Get-Item $dbBackupFilePath
    $sizeMb = [math]::Round($fileInfo.Length / 1MB, 2)
    Write-BackupLog "Sao lưu CSDL thành công! Tệp: $dbBackupFileName (Dung lượng: $sizeMb MB)"
}
catch {
    Write-BackupLog "LỖI khi sao lưu cơ sở dữ liệu: $_" "ERROR"
}

# 2. SAO LƯU THƯ MỤC TỆP ĐÍNH KÈM UPLOADS
$docsBackupFileName = "procurement_uploads_backup_${timestamp}.zip"
$docsBackupFilePath = Join-Path $docsBackupDir $docsBackupFileName

try {
    if (Test-Path $uploadsDir) {
        Write-BackupLog "Đang nén thư mục uploads thành $docsBackupFilePath..."
        Compress-Archive -Path "$uploadsDir\*" -DestinationPath $docsBackupFilePath -Force
        $docFileInfo = Get-Item $docsBackupFilePath
        $docSizeMb = [math]::Round($docFileInfo.Length / 1MB, 2)
        Write-BackupLog "Sao lưu tệp tải lên thành công! Tệp: $docsBackupFileName (Dung lượng: $docSizeMb MB)"
    } else {
        Write-BackupLog "Thư mục uploads không tồn tại, bỏ qua bước nén tệp." "WARN"
    }
}
catch {
    Write-BackupLog "LỖI khi nén thư mục uploads: $_" "ERROR"
}

# 3. DỌN DẸP BẢN SAO LƯU CŨ HƠN RetentionDays NGÀY
try {
    Write-BackupLog "Đang quét và dọn dẹp các bản sao lưu cũ hơn $RetentionDays ngày..."
    $cutoffDate = (Get-Date).AddDays(-$RetentionDays)

    Get-ChildItem -Path $dbBackupDir -Filter "*.bak" | Where-Object { $_.CreationTime -lt $cutoffDate } | ForEach-Object {
        Write-BackupLog "Xóa bản sao lưu CSDL cũ: $($_.Name) (Tạo ngày: $($_.CreationTime))"
        Remove-Item $_.FullName -Force
    }

    Get-ChildItem -Path $docsBackupDir -Filter "*.zip" | Where-Object { $_.CreationTime -lt $cutoffDate } | ForEach-Object {
        Write-BackupLog "Xóa bản sao lưu tệp cũ: $($_.Name) (Tạo ngày: $($_.CreationTime))"
        Remove-Item $_.FullName -Force
    }
}
catch {
    Write-BackupLog "LỖI khi dọn dẹp bản sao lưu cũ: $_" "ERROR"
}

Write-BackupLog "HOÀN TẤT TIẾN TRÌNH SAO LƯU ĐỊNH KỲ."
