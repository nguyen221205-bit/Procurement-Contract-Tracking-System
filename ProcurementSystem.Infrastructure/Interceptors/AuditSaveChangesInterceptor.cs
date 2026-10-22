using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Interceptors
{
    /// <summary>
    /// Interceptor tự động ghi nhận nhật ký kiểm toán (Audit Log) cho các thay đổi dữ liệu (CREATE/UPDATE/DELETE).
    /// Áp dụng cho 7 thực thể cốt lõi: BidPackage, BidSubmission, EvaluationScore, Contract, ContractMilestone, Acceptance, ProgressUpdate.
    /// Loại trừ các thuộc tính nhạy cảm và bảo vệ chống re-entrancy.
    /// </summary>
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly List<AuditEntry> _pendingAuditEntries = new();
        private bool _isAuditing = false;

        private static readonly HashSet<Type> CoreAuditableEntities = new()
        {
            typeof(BidPackage),
            typeof(BidSubmission),
            typeof(EvaluationScore),
            typeof(Contract),
            typeof(ContractMilestone),
            typeof(Acceptance),
            typeof(ProgressUpdate),
            // F5-9: Đăng ký 5 thực thể pháp lý mới vào hệ thống tự động ghi nhật ký kiểm toán
            typeof(BidClarification),
            typeof(PackageStatusApprovalRequest),
            typeof(ContractGuarantee),
            typeof(ContractAddendum),
            typeof(ContractorEvaluation)
        };

        private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash",
            "RefreshToken",
            "RefreshTokenExpiry",
            "Password",
            "SecretKey",
            "Token",
            "Otp",
            "OtpCode"
        };

        public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_isAuditing || eventData.Context == null)
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            _pendingAuditEntries.Clear();
            var entries = CollectAuditEntries(eventData.Context);
            _pendingAuditEntries.AddRange(entries);

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (_isAuditing || eventData.Context == null || _pendingAuditEntries.Count == 0)
            {
                return await base.SavedChangesAsync(eventData, result, cancellationToken);
            }

            _isAuditing = true;
            try
            {
                var context = eventData.Context;
                var currentUserId = GetCurrentUserId();
                var clientIp = GetCurrentIpAddress();

                foreach (var auditEntry in _pendingAuditEntries)
                {
                    if (auditEntry.Action == "CREATE" && !auditEntry.EntityId.HasValue)
                    {
                        var idProp = auditEntry.EntityEntry.Property("Id");
                        if (idProp.CurrentValue is int generatedId && generatedId > 0)
                        {
                            auditEntry.EntityId = generatedId;
                        }
                    }

                    var auditLog = new AuditLog
                    {
                        UserId = currentUserId,
                        Action = auditEntry.Action,
                        EntityType = auditEntry.EntityType,
                        EntityId = auditEntry.EntityId,
                        OldValues = auditEntry.OldValues.Count > 0
                            ? JsonSerializer.Serialize(auditEntry.OldValues)
                            : null,
                        NewValues = auditEntry.NewValues.Count > 0
                            ? JsonSerializer.Serialize(auditEntry.NewValues)
                            : null,
                        Timestamp = DateTime.UtcNow,
                        IpAddress = clientIp
                    };

                    context.Set<AuditLog>().Add(auditLog);
                }

                await context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _pendingAuditEntries.Clear();
                _isAuditing = false;
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (_isAuditing || eventData.Context == null)
            {
                return base.SavingChanges(eventData, result);
            }

            _pendingAuditEntries.Clear();
            var entries = CollectAuditEntries(eventData.Context);
            _pendingAuditEntries.AddRange(entries);

            return base.SavingChanges(eventData, result);
        }

        public override int SavedChanges(
            SaveChangesCompletedEventData eventData,
            int result)
        {
            if (_isAuditing || eventData.Context == null || _pendingAuditEntries.Count == 0)
            {
                return base.SavedChanges(eventData, result);
            }

            _isAuditing = true;
            try
            {
                var context = eventData.Context;
                var currentUserId = GetCurrentUserId();
                var clientIp = GetCurrentIpAddress();

                foreach (var auditEntry in _pendingAuditEntries)
                {
                    if (auditEntry.Action == "CREATE" && !auditEntry.EntityId.HasValue)
                    {
                        var idProp = auditEntry.EntityEntry.Property("Id");
                        if (idProp.CurrentValue is int generatedId && generatedId > 0)
                        {
                            auditEntry.EntityId = generatedId;
                        }
                    }

                    var auditLog = new AuditLog
                    {
                        UserId = currentUserId,
                        Action = auditEntry.Action,
                        EntityType = auditEntry.EntityType,
                        EntityId = auditEntry.EntityId,
                        OldValues = auditEntry.OldValues.Count > 0
                            ? JsonSerializer.Serialize(auditEntry.OldValues)
                            : null,
                        NewValues = auditEntry.NewValues.Count > 0
                            ? JsonSerializer.Serialize(auditEntry.NewValues)
                            : null,
                        Timestamp = DateTime.UtcNow,
                        IpAddress = clientIp
                    };

                    context.Set<AuditLog>().Add(auditLog);
                }

                context.SaveChanges();
            }
            finally
            {
                _pendingAuditEntries.Clear();
                _isAuditing = false;
            }

            return base.SavedChanges(eventData, result);
        }

        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            _pendingAuditEntries.Clear();
            _isAuditing = false;
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            _pendingAuditEntries.Clear();
            _isAuditing = false;
            base.SaveChangesFailed(eventData);
        }

        private List<AuditEntry> CollectAuditEntries(DbContext context)
        {
            var auditEntries = new List<AuditEntry>();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                // Bỏ qua AuditLog và các entity không thay đổi
                if (entry.Entity is AuditLog) continue;
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged) continue;

                var entityType = entry.Metadata.ClrType;
                if (!CoreAuditableEntities.Any(t => t.IsAssignableFrom(entityType)))
                {
                    continue;
                }

                var auditEntry = new AuditEntry(entry)
                {
                    EntityType = entityType.Name
                };

                if (entry.State == EntityState.Added)
                {
                    auditEntry.Action = "CREATE";
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.Metadata.IsPrimaryKey())
                        {
                            continue;
                        }
                        if (SensitivePropertyNames.Contains(prop.Metadata.Name))
                        {
                            continue;
                        }
                        if (prop.CurrentValue is byte[])
                        {
                            continue;
                        }

                        auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    auditEntries.Add(auditEntry);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    auditEntry.Action = "DELETE";
                    var idProp = entry.Property("Id");
                    if (idProp.CurrentValue is int idVal)
                    {
                        auditEntry.EntityId = idVal;
                    }
                    else if (idProp.OriginalValue is int origIdVal)
                    {
                        auditEntry.EntityId = origIdVal;
                    }

                    foreach (var prop in entry.Properties)
                    {
                        if (SensitivePropertyNames.Contains(prop.Metadata.Name))
                        {
                            continue;
                        }
                        if (prop.OriginalValue is byte[])
                        {
                            continue;
                        }

                        auditEntry.OldValues[prop.Metadata.Name] = prop.OriginalValue;
                    }
                    auditEntries.Add(auditEntry);
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditEntry.Action = "UPDATE";
                    var idProp = entry.Property("Id");
                    if (idProp.CurrentValue is int idVal)
                    {
                        auditEntry.EntityId = idVal;
                    }

                    foreach (var prop in entry.Properties)
                    {
                        if (!prop.IsModified)
                        {
                            continue;
                        }
                        if (SensitivePropertyNames.Contains(prop.Metadata.Name))
                        {
                            continue;
                        }
                        if (prop.CurrentValue is byte[] || prop.OriginalValue is byte[])
                        {
                            continue;
                        }

                        var orig = prop.OriginalValue;
                        var curr = prop.CurrentValue;
                        if (Equals(orig, curr))
                        {
                            continue;
                        }

                        auditEntry.OldValues[prop.Metadata.Name] = orig;
                        auditEntry.NewValues[prop.Metadata.Name] = curr;
                    }

                    if (auditEntry.OldValues.Count > 0 || auditEntry.NewValues.Count > 0)
                    {
                        auditEntries.Add(auditEntry);
                    }
                }
            }

            return auditEntries;
        }

        private int? GetCurrentUserId()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                           ?? user.FindFirst("id")
                           ?? user.FindFirst("sub");
                if (idClaim != null && int.TryParse(idClaim.Value, out var id))
                {
                    return id;
                }
            }
            return null;
        }

        private string? GetCurrentIpAddress()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null) return null;

            // N18 / R3-7: Sử dụng RemoteIpAddress đã được chuẩn hóa và bảo vệ qua UseForwardedHeaders middleware.
            // Tuyệt đối không đọc trực tiếp header X-Forwarded-For chưa được kiểm chứng để chống giả mạo IP trong AuditLog.
            return httpContext.Connection?.RemoteIpAddress?.ToString();
        }

        private class AuditEntry
        {
            public EntityEntry EntityEntry { get; }
            public string Action { get; set; } = string.Empty;
            public string EntityType { get; set; } = string.Empty;
            public int? EntityId { get; set; }
            public Dictionary<string, object?> OldValues { get; } = new();
            public Dictionary<string, object?> NewValues { get; } = new();

            public AuditEntry(EntityEntry entry)
            {
                EntityEntry = entry;
            }
        }
    }
}
