using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementSystem.Core.DTOs.SigningAuthority;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class SigningAuthorityService : ISigningAuthorityService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SigningAuthorityService> _logger;

        public SigningAuthorityService(AppDbContext context, ILogger<SigningAuthorityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private async Task<bool> IsPlatformAdminAsync(int userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Admin");
        }

        private async Task<bool> IsEntityManagerAsync(int procuringEntityId, int userId)
        {
            var pe = await _context.ProcuringEntities.FirstOrDefaultAsync(p => p.Id == procuringEntityId);
            if (pe == null) return false;

            return await _context.ProcuringEntityMembers.AnyAsync(m =>
                m.ProcuringEntityId == procuringEntityId &&
                m.UserId == userId &&
                m.RoleInEntity == ProcuringEntityMemberRole.Manager &&
                m.Status == MemberStatus.Active);
        }

        public async Task<SigningAuthorityDto> RequestAuthorityAsync(
            int procuringEntityId, int requestedByUserId, RequestSigningAuthorityRequest request)
        {
            var pe = await _context.ProcuringEntities.FirstOrDefaultAsync(p => p.Id == procuringEntityId);
            if (pe == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin Bên mời thầu.");
            }

            var isAdmin = await IsPlatformAdminAsync(requestedByUserId);
            var isManager = await IsEntityManagerAsync(procuringEntityId, requestedByUserId);

            if (!isAdmin && !isManager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền lập hồ sơ đề nghị cấp thẩm quyền ký cho đơn vị này.");
            }

            // Target user must exist and be an active member of this entity (or the registered entity user)
            var isMember = await _context.ProcuringEntityMembers.AnyAsync(m =>
                m.ProcuringEntityId == procuringEntityId &&
                m.UserId == request.UserId &&
                m.Status == MemberStatus.Active);

            if (!isMember)
            {
                throw new InvalidOperationException("Nhân sự được đề nghị phải là thành viên đang hoạt động của Bên mời thầu.");
            }

            if (request.ValidTo <= request.ValidFrom)
            {
                throw new ArgumentException("Thời điểm hết hiệu lực (ValidTo) phải sau thời điểm bắt đầu hiệu lực (ValidFrom).");
            }

            var authority = new SigningAuthority
            {
                ProcuringEntityId = procuringEntityId,
                UserId = request.UserId,
                AuthorityType = request.AuthorityType,
                Scope = string.IsNullOrWhiteSpace(request.Scope) ? "EntityWide" : request.Scope.Trim(),
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                Status = SigningAuthorityStatus.PendingVerification,
                DecisionNumber = request.DecisionNumber.Trim(),
                DocumentUrl = request.DocumentUrl?.Trim(),
                LegalBasis = request.LegalBasis.Trim(),
                RequestedByUserId = requestedByUserId,
                RequestedAt = DateTime.UtcNow
            };

            await _context.SigningAuthorities.AddAsync(authority);

            var auditLog = new AuditLog
            {
                UserId = requestedByUserId,
                Action = "REQUEST_SIGNING_AUTHORITY",
                EntityType = "SigningAuthority",
                NewValues = JsonSerializer.Serialize(new
                {
                    ProcuringEntityId = procuringEntityId,
                    TargetUserId = request.UserId,
                    request.AuthorityType,
                    request.Scope,
                    request.ValidFrom,
                    request.ValidTo,
                    request.DecisionNumber
                }),
                Timestamp = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(auditLog);

            await _context.SaveChangesAsync();

            return await GetAuthorityByIdAsync(authority.Id, requestedByUserId)
                ?? throw new InvalidOperationException("Không thể tải thông tin hồ sơ vừa tạo.");
        }

        public async Task<SigningAuthorityDto> VerifyAuthorityAsync(
            int authorityId, int adminUserId, VerifySigningAuthorityRequest request)
        {
            var isAdmin = await IsPlatformAdminAsync(adminUserId);
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("Chỉ Quản trị viên hệ thống (Admin) mới có quyền thẩm định hồ sơ thẩm quyền ký.");
            }

            var authority = await _context.SigningAuthorities
                .Include(a => a.User)
                .Include(a => a.ProcuringEntity)
                .FirstOrDefaultAsync(a => a.Id == authorityId);

            if (authority == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hồ sơ thẩm quyền ký.");
            }

            if (authority.Status != SigningAuthorityStatus.PendingVerification)
            {
                throw new InvalidOperationException("Chỉ có thể thẩm định hồ sơ đang ở trạng thái chờ thẩm định (PendingVerification).");
            }

            // Self-approval check: Admin cannot approve authority for their own account
            if (authority.UserId == adminUserId)
            {
                throw new InvalidOperationException("Quản trị viên không thể tự thẩm định hồ sơ thẩm quyền cho chính mình.");
            }

            // R4: Admin prohibition - Không thể cấp thẩm quyền phê duyệt giải ngân cho tài khoản Platform Admin
            var isTargetPlatformAdmin = await IsPlatformAdminAsync(authority.UserId);
            if (isTargetPlatformAdmin && (authority.AuthorityType == SigningAuthorityType.DisbursementApproval || authority.AuthorityType == SigningAuthorityType.FullLegalRepresentative))
            {
                throw new InvalidOperationException("Không thể cấp thẩm quyền phê duyệt giải ngân cho tài khoản Quản trị viên hệ thống (Admin). Admin chỉ thực hiện vai trò giám sát nền tảng và thẩm định hồ sơ.");
            }

            if (request.ValidFrom.HasValue && request.ValidTo.HasValue)
            {
                if (request.ValidTo.Value <= request.ValidFrom.Value)
                {
                    throw new ArgumentException("Thời điểm hết hiệu lực phải sau thời điểm bắt đầu hiệu lực.");
                }
                authority.ValidFrom = request.ValidFrom.Value;
                authority.ValidTo = request.ValidTo.Value;
            }

            if (request.IsApproved)
            {
                authority.Status = SigningAuthorityStatus.Active;
            }
            else
            {
                authority.Status = SigningAuthorityStatus.Rejected;
            }

            authority.VerifiedByUserId = adminUserId;
            authority.VerifiedAt = DateTime.UtcNow;
            authority.VerificationNotes = request.VerificationNotes?.Trim();

            var auditLog = new AuditLog
            {
                UserId = adminUserId,
                Action = "VERIFY_SIGNING_AUTHORITY",
                EntityType = "SigningAuthority",
                EntityId = authority.Id,
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = authority.Status.ToString(),
                    IsApproved = request.IsApproved,
                    authority.VerificationNotes,
                    authority.ValidFrom,
                    authority.ValidTo
                }),
                Timestamp = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(auditLog);

            await _context.SaveChangesAsync();

            return await GetAuthorityByIdAsync(authority.Id, adminUserId)
                ?? throw new InvalidOperationException("Không thể tải thông tin hồ sơ vừa cập nhật.");
        }

        public async Task<SigningAuthorityDto> RevokeAuthorityAsync(
            int authorityId, int currentUserId, RevokeSigningAuthorityRequest request)
        {
            var authority = await _context.SigningAuthorities
                .Include(a => a.User)
                .Include(a => a.ProcuringEntity)
                .FirstOrDefaultAsync(a => a.Id == authorityId);

            if (authority == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hồ sơ thẩm quyền ký.");
            }

            if (authority.Status != SigningAuthorityStatus.Active &&
                authority.Status != SigningAuthorityStatus.PendingVerification)
            {
                throw new InvalidOperationException("Chỉ có thể thu hồi thẩm quyền đang có hiệu lực hoặc đang chờ thẩm định.");
            }

            var isAdmin = await IsPlatformAdminAsync(currentUserId);
            var isManager = await IsEntityManagerAsync(authority.ProcuringEntityId, currentUserId);

            if (!isAdmin && !isManager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thu hồi thẩm quyền này.");
            }

            authority.Status = SigningAuthorityStatus.Revoked;
            authority.RevokedByUserId = currentUserId;
            authority.RevokedAt = DateTime.UtcNow;
            authority.RevocationReason = request.RevocationReason.Trim();

            var auditLog = new AuditLog
            {
                UserId = currentUserId,
                Action = "REVOKE_SIGNING_AUTHORITY",
                EntityType = "SigningAuthority",
                EntityId = authority.Id,
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = SigningAuthorityStatus.Revoked.ToString(),
                    authority.RevocationReason
                }),
                Timestamp = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(auditLog);

            await _context.SaveChangesAsync();

            return await GetAuthorityByIdAsync(authority.Id, currentUserId)
                ?? throw new InvalidOperationException("Không thể tải thông tin hồ sơ vừa thu hồi.");
        }

        public async Task<List<SigningAuthorityDto>> GetAuthoritiesByEntityAsync(
            int procuringEntityId, int currentUserId)
        {
            var isAdmin = await IsPlatformAdminAsync(currentUserId);
            var isManager = await IsEntityManagerAsync(procuringEntityId, currentUserId);
            var isMember = await _context.ProcuringEntityMembers.AnyAsync(m =>
                m.ProcuringEntityId == procuringEntityId &&
                m.UserId == currentUserId &&
                m.Status == MemberStatus.Active);

            if (!isAdmin && !isManager && !isMember)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách thẩm quyền của đơn vị này.");
            }

            var authorities = await _context.SigningAuthorities
                .Include(a => a.User)
                .Include(a => a.ProcuringEntity)
                .Include(a => a.RequestedByUser)
                .Include(a => a.VerifiedByUser)
                .Include(a => a.RevokedByUser)
                .Where(a => a.ProcuringEntityId == procuringEntityId)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            // Cập nhật trạng thái hiển thị nếu đã hết hạn
            var now = DateTime.UtcNow;
            return authorities.Select(a =>
            {
                var dto = MapToDto(a);
                if (dto.Status == SigningAuthorityStatus.Active && now > dto.ValidTo)
                {
                    dto.Status = SigningAuthorityStatus.Expired;
                    dto.StatusName = "Đã hết hạn";
                }
                return dto;
            }).ToList();
        }

        public async Task<List<SigningAuthorityDto>> GetPendingVerificationsAsync(int adminUserId)
        {
            var isAdmin = await IsPlatformAdminAsync(adminUserId);
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("Chỉ Quản trị viên hệ thống mới có quyền truy cập danh sách chờ thẩm định.");
            }

            var pending = await _context.SigningAuthorities
                .Include(a => a.User)
                .Include(a => a.ProcuringEntity)
                .Include(a => a.RequestedByUser)
                .Where(a => a.Status == SigningAuthorityStatus.PendingVerification)
                .OrderBy(a => a.RequestedAt)
                .ToListAsync();

            return pending.Select(MapToDto).ToList();
        }

        public async Task<SigningAuthorityDto?> GetAuthorityByIdAsync(int authorityId, int currentUserId)
        {
            var authority = await _context.SigningAuthorities
                .Include(a => a.User)
                .Include(a => a.ProcuringEntity)
                .Include(a => a.RequestedByUser)
                .Include(a => a.VerifiedByUser)
                .Include(a => a.RevokedByUser)
                .FirstOrDefaultAsync(a => a.Id == authorityId);

            if (authority == null) return null;

            var isAdmin = await IsPlatformAdminAsync(currentUserId);
            var isManager = await IsEntityManagerAsync(authority.ProcuringEntityId, currentUserId);
            var isMember = await _context.ProcuringEntityMembers.AnyAsync(m =>
                m.ProcuringEntityId == authority.ProcuringEntityId &&
                m.UserId == currentUserId &&
                m.Status == MemberStatus.Active);

            if (!isAdmin && !isManager && !isMember && authority.UserId != currentUserId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem thông tin hồ sơ thẩm quyền này.");
            }

            var dto = MapToDto(authority);
            if (dto.Status == SigningAuthorityStatus.Active && DateTime.UtcNow > dto.ValidTo)
            {
                dto.Status = SigningAuthorityStatus.Expired;
                dto.StatusName = "Đã hết hạn";
            }
            return dto;
        }

        public async Task<bool> HasActiveDisbursementAuthorityAsync(
            int procuringEntityId, int userId, int? contractId = null)
        {
            var authorityId = await GetActiveDisbursementAuthorityIdAsync(procuringEntityId, userId, contractId);
            return authorityId.HasValue;
        }

        public async Task<int?> GetActiveDisbursementAuthorityIdAsync(
            int procuringEntityId, int userId, int? contractId = null)
        {
            // R4: Platform Admin bị cấm hoàn toàn phê duyệt giải ngân
            var isPlatformAdmin = await IsPlatformAdminAsync(userId);
            if (isPlatformAdmin)
            {
                return null;
            }

            // R3: Yêu cầu đồng thời nhân sự phải là thành viên Active của Bên mời thầu
            var isMember = await _context.ProcuringEntityMembers
                .AsNoTracking()
                .AnyAsync(m =>
                    m.ProcuringEntityId == procuringEntityId &&
                    m.UserId == userId &&
                    m.Status == MemberStatus.Active);

            if (!isMember)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var query = _context.SigningAuthorities
                .AsNoTracking()
                .Where(a =>
                    a.ProcuringEntityId == procuringEntityId &&
                    a.UserId == userId &&
                    a.Status == SigningAuthorityStatus.Active &&
                    a.ValidFrom <= now &&
                    a.ValidTo >= now &&
                    (a.AuthorityType == SigningAuthorityType.DisbursementApproval ||
                     a.AuthorityType == SigningAuthorityType.FullLegalRepresentative));

            if (contractId.HasValue)
            {
                var contractScope = $"Contract:{contractId.Value}";
                // Ưu tiên hợp đồng cụ thể trước
                var specificAuthority = await query
                    .Where(a => a.Scope == contractScope)
                    .OrderByDescending(a => a.ValidTo)
                    .Select(a => (int?)a.Id)
                    .FirstOrDefaultAsync();

                if (specificAuthority.HasValue)
                {
                    return specificAuthority;
                }
            }

            // Fallback sang EntityWide
            return await query
                .Where(a => a.Scope == "EntityWide")
                .OrderByDescending(a => a.ValidTo)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync();
        }

        private static SigningAuthorityDto MapToDto(SigningAuthority a)
        {
            return new SigningAuthorityDto
            {
                Id = a.Id,
                ProcuringEntityId = a.ProcuringEntityId,
                ProcuringEntityName = a.ProcuringEntity != null ? a.ProcuringEntity.OrganizationName : string.Empty,
                UserId = a.UserId,
                FullName = a.User != null ? a.User.FullName : string.Empty,
                Email = a.User != null ? a.User.Email : string.Empty,
                AuthorityType = a.AuthorityType,
                AuthorityTypeName = a.AuthorityType switch
                {
                    SigningAuthorityType.DisbursementApproval => "Phê duyệt giải ngân",
                    SigningAuthorityType.ContractSigning => "Ký kết hợp đồng",
                    SigningAuthorityType.FullLegalRepresentative => "Đại diện toàn diện theo pháp luật",
                    _ => a.AuthorityType.ToString()
                },
                Scope = a.Scope,
                ValidFrom = a.ValidFrom,
                ValidTo = a.ValidTo,
                Status = a.Status,
                StatusName = a.Status switch
                {
                    SigningAuthorityStatus.PendingVerification => "Chờ thẩm định",
                    SigningAuthorityStatus.Active => "Đang hiệu lực",
                    SigningAuthorityStatus.Rejected => "Từ chối",
                    SigningAuthorityStatus.Revoked => "Đã thu hồi",
                    SigningAuthorityStatus.Expired => "Đã hết hạn",
                    _ => a.Status.ToString()
                },
                DecisionNumber = a.DecisionNumber,
                DocumentUrl = a.DocumentUrl,
                LegalBasis = a.LegalBasis,
                RequestedByUserId = a.RequestedByUserId,
                RequestedByName = a.RequestedByUser != null ? a.RequestedByUser.FullName : string.Empty,
                RequestedAt = a.RequestedAt,
                VerifiedByUserId = a.VerifiedByUserId,
                VerifiedByName = a.VerifiedByUser != null ? a.VerifiedByUser.FullName : null,
                VerifiedAt = a.VerifiedAt,
                VerificationNotes = a.VerificationNotes,
                RevokedByUserId = a.RevokedByUserId,
                RevokedByName = a.RevokedByUser != null ? a.RevokedByUser.FullName : null,
                RevokedAt = a.RevokedAt,
                RevocationReason = a.RevocationReason
            };
        }
    }
}
