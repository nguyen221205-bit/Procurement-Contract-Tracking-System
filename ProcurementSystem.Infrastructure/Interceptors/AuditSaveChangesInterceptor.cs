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
    /// Áp dụng cho các thực thể cốt lõi của hệ thống, loại trừ các thuộc tính nhạy cảm và bảo vệ chống re-entrancy.
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
            CapturePendingAuditEntries(eventData.Context);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            CapturePendingAuditEntries(eventData.Context);
            return base.SavingChanges(eventData, result);
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
                AttachPendingAuditLogs(eventData.Context);
                await eventData.Context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                ResetAuditState();
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
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
                AttachPendingAuditLogs(eventData.Context);
                eventData.Context.SaveChanges();
            }
            finally
            {
                ResetAuditState();
            }

            return base.SavedChanges(eventData, result);
        }

        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            ResetAuditState();
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            ResetAuditState();
            base.SaveChangesFailed(eventData);
        }

        private void CapturePendingAuditEntries(DbContext? context)
        {
            if (_isAuditing || context == null)
            {
                return;
            }

            _pendingAuditEntries.Clear();
            _pendingAuditEntries.AddRange(CollectAuditEntries(context));
        }

        private void AttachPendingAuditLogs(DbContext context)
        {
            var currentUserId = GetCurrentUserId();
            var clientIp = GetCurrentIpAddress();

            foreach (var auditEntry in _pendingAuditEntries)
            {
                if (auditEntry.Action == "CREATE" && !auditEntry.EntityId.HasValue &&
                    auditEntry.EntityEntry.Property("Id").CurrentValue is int generatedId && generatedId > 0)
                {
                    auditEntry.EntityId = generatedId;
                }

                context.Set<AuditLog>().Add(new AuditLog
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
                });
            }
        }

        private void ResetAuditState()
        {
            _pendingAuditEntries.Clear();
            _isAuditing = false;
        }

        private static List<AuditEntry> CollectAuditEntries(DbContext context)
        {
            var auditEntries = new List<AuditEntry>();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State is EntityState.Detached or EntityState.Unchanged)
                {
                    continue;
                }

                var entityType = entry.Metadata.ClrType;
                if (!CoreAuditableEntities.Any(t => t.IsAssignableFrom(entityType)))
                {
                    continue;
                }

                var auditEntry = entry.State switch
                {
                    EntityState.Added => BuildAddedEntry(entry, entityType.Name),
                    EntityState.Deleted => BuildDeletedEntry(entry, entityType.Name),
                    EntityState.Modified => BuildModifiedEntry(entry, entityType.Name),
                    _ => null
                };

                if (auditEntry != null)
                {
                    auditEntries.Add(auditEntry);
                }
            }

            return auditEntries;
        }

        private static AuditEntry BuildAddedEntry(EntityEntry entry, string entityTypeName)
        {
            var auditEntry = new AuditEntry(entry)
            {
                EntityType = entityTypeName,
                Action = "CREATE"
            };

            foreach (var prop in entry.Properties)
            {
                if (!prop.Metadata.IsPrimaryKey() && IsAuditableProperty(prop.Metadata.Name, prop.CurrentValue))
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            return auditEntry;
        }

        private static AuditEntry BuildDeletedEntry(EntityEntry entry, string entityTypeName)
        {
            var auditEntry = new AuditEntry(entry)
            {
                EntityType = entityTypeName,
                Action = "DELETE",
                EntityId = ResolveEntityId(entry)
            };

            foreach (var prop in entry.Properties)
            {
                if (IsAuditableProperty(prop.Metadata.Name, prop.OriginalValue))
                {
                    auditEntry.OldValues[prop.Metadata.Name] = prop.OriginalValue;
                }
            }

            return auditEntry;
        }

        private static AuditEntry? BuildModifiedEntry(EntityEntry entry, string entityTypeName)
        {
            var auditEntry = new AuditEntry(entry)
            {
                EntityType = entityTypeName,
                Action = "UPDATE",
                EntityId = ResolveEntityId(entry)
            };

            foreach (var prop in entry.Properties)
            {
                if (!prop.IsModified ||
                    !IsAuditableProperty(prop.Metadata.Name, prop.CurrentValue) ||
                    !IsAuditableProperty(prop.Metadata.Name, prop.OriginalValue) ||
                    Equals(prop.OriginalValue, prop.CurrentValue))
                {
                    continue;
                }

                auditEntry.OldValues[prop.Metadata.Name] = prop.OriginalValue;
                auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
            }

            return auditEntry.OldValues.Count > 0 || auditEntry.NewValues.Count > 0
                ? auditEntry
                : null;
        }

        private static int? ResolveEntityId(EntityEntry entry)
        {
            var idProp = entry.Property("Id");
            return idProp.CurrentValue switch
            {
                int currentId => currentId,
                _ when idProp.OriginalValue is int originalId => originalId,
                _ => null
            };
        }

        private static bool IsAuditableProperty(string propertyName, object? value)
        {
            return !SensitivePropertyNames.Contains(propertyName) && value is not byte[];
        }

        private int? GetCurrentUserId()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                       ?? user.FindFirst("id")
                       ?? user.FindFirst("sub");

            return idClaim != null && int.TryParse(idClaim.Value, out var id) ? id : null;
        }

        private string? GetCurrentIpAddress()
        {
            return _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
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
