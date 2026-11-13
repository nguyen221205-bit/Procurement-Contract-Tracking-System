using System;
using System.Collections.Generic;
using System.IO;

namespace ProcurementSystem.API.Extensions
{
    /// <summary>
    /// Nạp tệp cấu hình môi trường (.env) vào biến môi trường tiến trình trước khi ASP.NET Core khởi tạo IConfiguration.
    /// Giúp thành viên nhóm chỉ cần tệp .env vẫn chạy đầy đủ API, OTP, Email, JWT, Storage và Backup mà không cần appsettings.json cục bộ.
    /// </summary>
    public static class DotEnvLoader
    {
        private static readonly (string FlatKey, string ConfigKey)[] AliasPairs =
        {
            ("DB_CONNECTION_STRING", "ConnectionStrings__DefaultConnection"),
            ("PROCUREMENT_DB_CONN", "ConnectionStrings__DefaultConnection"),
            ("JWT_SECRET_KEY", "JwtSettings__SecretKey"),
            ("JWT_ISSUER", "JwtSettings__Issuer"),
            ("JWT_AUDIENCE", "JwtSettings__Audience"),
            ("JWT_EXPIRY_MINUTES", "JwtSettings__ExpirationInMinutes"),
            ("JWT_REFRESH_EXPIRY_DAYS", "JwtSettings__RefreshTokenExpirationInDays"),
            ("SMTP_HOST", "Smtp__Host"),
            ("SMTP_PORT", "Smtp__Port"),
            ("SMTP_ENABLE_SSL", "Smtp__EnableSsl"),
            ("SMTP_USERNAME", "Smtp__Username"),
            ("SMTP_PASSWORD", "Smtp__Password"),
            ("SMTP_SENDER_EMAIL", "Smtp__SenderEmail"),
            ("SMTP_SENDER_NAME", "Smtp__SenderName"),
            ("FRONTEND_URL", "FrontendUrl"),
            ("CLIENT_APP_BASE_URL", "ClientApp__BaseUrl"),
            ("STORAGE_PROVIDER", "StorageSettings__Provider"),
            ("STORAGE_ENCRYPTION_ENABLED", "StorageSettings__Encryption__Enabled"),
            ("STORAGE_MASTER_KEY", "StorageSettings__Encryption__MasterKey"),
            ("SUPABASE_ENDPOINT", "StorageSettings__Supabase__Endpoint"),
            ("SUPABASE_BUCKET_NAME", "StorageSettings__Supabase__BucketName"),
            ("SUPABASE_ACCESS_KEY_ID", "StorageSettings__Supabase__AccessKeyId"),
            ("SUPABASE_SECRET_ACCESS_KEY", "StorageSettings__Supabase__SecretAccessKey"),
            ("SUPABASE_REGION", "StorageSettings__Supabase__Region"),
        };

        /// <summary>
        /// Tìm và nạp tệp .env từ thư mục gốc repository hoặc thư mục ProcurementSystem.API nếu tồn tại.
        /// Không ghi đè biến môi trường đã được truyền sẵn ở mức hệ điều hành hoặc tiến trình.
        /// </summary>
        public static void LoadIfPresent()
        {
            var candidateFiles = DiscoverEnvFiles();
            foreach (var filePath in candidateFiles)
            {
                LoadFile(filePath);
            }

            SynchronizeAliases();
        }

        /// <summary>
        /// Nạp một tệp .env cụ thể và đồng bộ các khóa bí danh sang chuẩn cấu hình ASP.NET Core.
        /// </summary>
        public static void LoadFileAndSync(string filePath)
        {
            if (File.Exists(filePath))
            {
                LoadFile(filePath);
            }

            SynchronizeAliases();
        }

        private static List<string> DiscoverEnvFiles()
        {
            var ordered = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var searchRoots = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            // Thu thập theo thứ tự từ thư mục cha (root repo) đến thư mục dự án API để API-level .env có thể bổ sung nếu cần
            var repoRootCandidates = new List<string>();
            var apiCandidates = new List<string>();

            foreach (var root in searchRoots)
            {
                var dir = new DirectoryInfo(root);
                for (var depth = 0; depth < 6 && dir != null; depth++, dir = dir.Parent)
                {
                    var envPath = Path.Combine(dir.FullName, ".env");
                    if (!File.Exists(envPath))
                    {
                        continue;
                    }

                    if (dir.Name.Equals("ProcurementSystem.API", StringComparison.OrdinalIgnoreCase))
                    {
                        apiCandidates.Add(Path.GetFullPath(envPath));
                    }
                    else if (Directory.Exists(Path.Combine(dir.FullName, "ProcurementSystem.API")))
                    {
                        repoRootCandidates.Add(Path.GetFullPath(envPath));
                    }
                }
            }

            foreach (var path in repoRootCandidates)
            {
                if (seen.Add(path)) ordered.Add(path);
            }

            foreach (var path in apiCandidates)
            {
                if (seen.Add(path)) ordered.Add(path);
            }

            return ordered;
        }

        private static void LoadFile(string filePath)
        {
            foreach (var rawLine in File.ReadAllLines(filePath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                {
                    continue;
                }

                if (line.StartsWith("export ", StringComparison.Ordinal))
                {
                    line = line.Substring("export ".Length).TrimStart();
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separatorIndex).Trim();
                var value = Unquote(line.Substring(separatorIndex + 1).Trim());

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                // Ưu tiên biến môi trường đã đặt sẵn ở tiến trình / hệ điều hành
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }

        private static void SynchronizeAliases()
        {
            foreach (var (flatKey, configKey) in AliasPairs)
            {
                var flatVal = Environment.GetEnvironmentVariable(flatKey);
                var configVal = Environment.GetEnvironmentVariable(configKey);

                if (string.IsNullOrWhiteSpace(configVal) && !string.IsNullOrWhiteSpace(flatVal))
                {
                    Environment.SetEnvironmentVariable(configKey, flatVal);
                }
                else if (string.IsNullOrWhiteSpace(flatVal) && !string.IsNullOrWhiteSpace(configVal))
                {
                    Environment.SetEnvironmentVariable(flatKey, configVal);
                }
            }
        }
    }
}
