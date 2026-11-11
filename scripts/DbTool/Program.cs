using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

static string? FindRepoRoot(string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "ProcurementSystem.API")) &&
            Directory.Exists(Path.Combine(dir.FullName, "scripts")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }
    return null;
}

static string? ReadDefaultConnectionFromAppSettings(string filePath)
{
    if (!File.Exists(filePath)) return null;
    try
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings) &&
            connStrings.TryGetProperty("DefaultConnection", out var defaultConn))
        {
            return defaultConn.GetString();
        }
    }
    catch
    {
        // Bỏ qua nếu tệp JSON không hợp lệ hoặc đang được chỉnh sửa
    }
    return null;
}

static string? ResolveConnectionString(string repoRoot)
{
    var fromEnv = Environment.GetEnvironmentVariable("PROCUREMENT_DB_CONN");
    if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv;

    var apiDir = Path.Combine(repoRoot, "ProcurementSystem.API");
    return ReadDefaultConnectionFromAppSettings(Path.Combine(apiDir, "appsettings.Development.json"))
        ?? ReadDefaultConnectionFromAppSettings(Path.Combine(apiDir, "appsettings.json"));
}

var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "backup";
var repoRoot = FindRepoRoot(AppContext.BaseDirectory)
    ?? FindRepoRoot(Directory.GetCurrentDirectory())
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

var backupDir = Path.Combine(repoRoot, "scripts", "backups");
if (!Directory.Exists(backupDir))
{
    Directory.CreateDirectory(backupDir);
}

var rawConnStr = ResolveConnectionString(repoRoot);
if (string.IsNullOrWhiteSpace(rawConnStr))
{
    Console.WriteLine(">>> LỖI: Không tìm thấy chuỗi kết nối SQL Server.");
    Console.WriteLine(">>> Vui lòng cấu hình ConnectionStrings:DefaultConnection trong ProcurementSystem.API/appsettings.json");
    Console.WriteLine(">>> hoặc đặt biến môi trường PROCUREMENT_DB_CONN.");
    return 1;
}

var builder = new SqlConnectionStringBuilder(rawConnStr);
var server = string.IsNullOrWhiteSpace(builder.DataSource) ? "localhost" : builder.DataSource;
var database = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "procurement_db" : builder.InitialCatalog;

if (!Regex.IsMatch(database, @"^[a-zA-Z0-9_]+$"))
{
    Console.WriteLine($">>> LỖI: Tên CSDL không hợp lệ: {database}");
    return 1;
}

builder.InitialCatalog = "master";
var masterConnStr = builder.ConnectionString;

if (mode == "backup")
{
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var backupFile = Path.Combine(backupDir, $"{database}_{timestamp}.bak");
    var safeBackupPath = backupFile.Replace("'", "''");

    Console.WriteLine("==================================================");
    Console.WriteLine(" [BACKUP] BẮT ĐẦU SAO LƯU CƠ SỞ DỮ LIỆU SQL SERVER");
    Console.WriteLine($" CSDL: {database} trên {server}");
    Console.WriteLine($" Tệp sao lưu: {backupFile}");
    Console.WriteLine("==================================================");

    using var conn = new SqlConnection(masterConnStr);
    conn.Open();

    using var cmd = conn.CreateCommand();
    cmd.CommandTimeout = 300;
    cmd.CommandText = $"BACKUP DATABASE [{database}] TO DISK = N'{safeBackupPath}' WITH FORMAT, INIT, MEDIANAME = 'ProcurementBackup', NAME = 'Full Backup of {database}', STATS = 10;";
    cmd.ExecuteNonQuery();

    if (File.Exists(backupFile))
    {
        var sizeMB = Math.Round(new FileInfo(backupFile).Length / 1024.0 / 1024.0, 2);
        Console.WriteLine($">>> SAO LƯU THÀNH CÔNG! Dung lượng: {sizeMB} MB");
        Console.WriteLine($">>> Đường dẫn: {backupFile}");
    }
    else
    {
        Console.WriteLine(">>> LỖI: Không tìm thấy tệp sau khi sao lưu.");
        return 1;
    }
}
else if (mode == "restore")
{
    var targetFile = args.Length > 1 ? args[1] : null;
    if (string.IsNullOrEmpty(targetFile))
    {
        var latest = new DirectoryInfo(backupDir)
            .GetFiles($"{database}_*.bak")
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();

        if (latest == null)
        {
            Console.WriteLine($">>> LỖI: Không tìm thấy tệp .bak nào trong {backupDir}");
            return 1;
        }
        targetFile = latest.FullName;
    }

    if (!File.Exists(targetFile))
    {
        Console.WriteLine($">>> LỖI: Tệp phục hồi không tồn tại: {targetFile}");
        return 1;
    }

    var safeTargetPath = targetFile.Replace("'", "''");

    Console.WriteLine("==================================================");
    Console.WriteLine(" [RESTORE] BẮT ĐẦU PHỤC HỒI CƠ SỞ DỮ LIỆU SQL SERVER");
    Console.WriteLine($" CSDL đích: {database} trên {server}");
    Console.WriteLine($" Nguồn phục hồi: {targetFile}");
    Console.WriteLine("==================================================");

    using var conn = new SqlConnection(masterConnStr);
    conn.Open();

    using var cmd = conn.CreateCommand();
    cmd.CommandTimeout = 300;

    Console.WriteLine("Đang chuyển CSDL về SINGLE_USER (ngắt các kết nối active)...");
    try
    {
        cmd.CommandText = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
        cmd.ExecuteNonQuery();
    }
    catch
    {
        // Có thể CSDL chưa tồn tại khi restore lần đầu
    }

    Console.WriteLine("Đang thực thi lệnh RESTORE DATABASE WITH REPLACE...");
    cmd.CommandText = $"RESTORE DATABASE [{database}] FROM DISK = N'{safeTargetPath}' WITH REPLACE, STATS = 10;";
    cmd.ExecuteNonQuery();

    Console.WriteLine("Đang chuyển CSDL về MULTI_USER...");
    cmd.CommandText = $"ALTER DATABASE [{database}] SET MULTI_USER;";
    cmd.ExecuteNonQuery();

    Console.WriteLine($">>> PHỤC HỒI DỮ LIỆU THÀNH CÔNG từ {Path.GetFileName(targetFile)}!");
    Console.WriteLine($">>> CSDL {database} đã sẵn sàng hoạt động bình thường.");
}
else
{
    Console.WriteLine("Cú pháp: DbTool [backup|restore] [tùy chọn: đường_dẫn_file_bak]");
    return 1;
}

return 0;
