using System.Security.Cryptography;

using System.Text;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.Logging;

using ProcurementSystem.Core.DTOs;

using ProcurementSystem.Core.DTOs.ProcuringEntity;

using ProcurementSystem.Core.Enums;

using ProcurementSystem.Core.Interfaces;

using ProcurementSystem.Infrastructure.Data;

using ProcurementSystem.Infrastructure.Entities;



namespace ProcurementSystem.Infrastructure.Services

{

    public class ProcuringEntityMemberService : IProcuringEntityMemberService

    {

        private readonly AppDbContext _context;

        private readonly IEmailService _emailService;

        private readonly IConfiguration _configuration;

        private readonly ILogger<ProcuringEntityMemberService> _logger;



        public ProcuringEntityMemberService(

            AppDbContext context,

            IEmailService emailService,

            IConfiguration configuration,

            ILogger<ProcuringEntityMemberService> logger)

        {

            _context = context;

            _emailService = emailService;

            _configuration = configuration;

            _logger = logger;

        }



        private async Task<bool> HasAdminOrManagerPermissionAsync(int procuringEntityId, int currentUserId, bool isAdmin)

        {

            if (isAdmin) return true;



            var pe = await _context.ProcuringEntities.FirstOrDefaultAsync(p => p.Id == procuringEntityId);

            if (pe == null) return false;

            // Hoặc thành viên có quyền Manager

            var isManager = await _context.ProcuringEntityMembers.AnyAsync(m =>

                m.ProcuringEntityId == procuringEntityId &&

                m.UserId == currentUserId &&

                m.RoleInEntity == ProcuringEntityMemberRole.Manager &&

                m.Status == MemberStatus.Active);



            return isManager;

        }



        public async Task<ApiResponse<List<ProcuringEntityMemberDto>>> GetMembersAsync(int procuringEntityId, int currentUserId, bool isAdmin)

        {

            var pe = await _context.ProcuringEntities.FirstOrDefaultAsync(p => p.Id == procuringEntityId);

            if (pe == null)

            {

                return ApiResponse<List<ProcuringEntityMemberDto>>.Fail("Không tìm thấy thông tin Bên mời thầu.");

            }



            // Kiểm tra quyền xem: Admin hoặc thành viên của đơn vị

            if (!isAdmin && pe.UserId != currentUserId)

            {

                var isMember = await _context.ProcuringEntityMembers.AnyAsync(m =>

                    m.ProcuringEntityId == procuringEntityId &&

                    m.UserId == currentUserId &&

                    m.Status == MemberStatus.Active);



                if (!isMember)

                {

                    return ApiResponse<List<ProcuringEntityMemberDto>>.Forbidden("Bạn không có quyền xem danh sách thành viên của đơn vị này.");

                }

            }



            var members = await _context.ProcuringEntityMembers

                .Include(m => m.User)

                .Include(m => m.InvitedByUser)

                .Include(m => m.RevokedByUser)

                .Where(m => m.ProcuringEntityId == procuringEntityId)

                .OrderByDescending(m => m.JoinedAt)

                .Select(m => new ProcuringEntityMemberDto

                {

                    Id = m.Id,

                    ProcuringEntityId = m.ProcuringEntityId,

                    UserId = m.UserId,

                    FullName = m.User.FullName,

                    Email = m.User.Email,

                    Phone = m.User.Phone,

                    RoleInEntity = m.RoleInEntity,

                    RoleInEntityName = m.RoleInEntity.ToString(),

                    Status = m.Status,

                    StatusName = m.Status.ToString(),

                    JoinedAt = m.JoinedAt,

                    InvitedByUserId = m.InvitedByUserId,

                    InvitedByName = m.InvitedByUser != null ? m.InvitedByUser.FullName : null,

                    RevokedAt = m.RevokedAt,

                    RevokedByUserId = m.RevokedByUserId,

                    RevokedByName = m.RevokedByUser != null ? m.RevokedByUser.FullName : null

                })

                .ToListAsync();



            return ApiResponse<List<ProcuringEntityMemberDto>>.Ok(members, "Lấy danh sách thành viên thành công.");

        }



        public async Task<ApiResponse<List<ProcuringEntityInvitationDto>>> GetInvitationsAsync(int procuringEntityId, int currentUserId, bool isAdmin)

        {

            if (!await HasAdminOrManagerPermissionAsync(procuringEntityId, currentUserId, isAdmin))

            {

                return ApiResponse<List<ProcuringEntityInvitationDto>>.Forbidden("Bạn không có quyền quản lý lời mời của đơn vị này.");

            }



            var invitations = await _context.ProcuringEntityInvitations

                .Include(i => i.ProcuringEntity)

                .Include(i => i.InvitedByUser)

                .Include(i => i.AcceptedByUser)

                .Where(i => i.ProcuringEntityId == procuringEntityId)

                .OrderByDescending(i => i.CreatedAt)

                .Select(i => new ProcuringEntityInvitationDto

                {

                    Id = i.Id,

                    ProcuringEntityId = i.ProcuringEntityId,

                    OrganizationName = i.ProcuringEntity.OrganizationName,

                    Email = i.Email,

                    RoleInEntity = i.RoleInEntity,

                    RoleInEntityName = i.RoleInEntity.ToString(),

                    InvitedByUserId = i.InvitedByUserId,

                    InvitedByName = i.InvitedByUser.FullName,

                    ExpiresAt = i.ExpiresAt,

                    Status = i.Status,

                    StatusName = i.Status.ToString(),

                    CreatedAt = i.CreatedAt,

                    AcceptedAt = i.AcceptedAt,

                    AcceptedByUserId = i.AcceptedByUserId,

                    AcceptedByName = i.AcceptedByUser != null ? i.AcceptedByUser.FullName : null

                })

                .ToListAsync();



            return ApiResponse<List<ProcuringEntityInvitationDto>>.Ok(invitations, "Lấy danh sách lời mời thành công.");

        }



        public async Task<ApiResponse<ProcuringEntityInvitationDto>> SendInvitationAsync(

            int procuringEntityId, SendInvitationRequest request, int currentUserId, bool isAdmin)

        {

            var pe = await _context.ProcuringEntities.FirstOrDefaultAsync(p => p.Id == procuringEntityId);

            if (pe == null)

            {

                return ApiResponse<ProcuringEntityInvitationDto>.Fail("Không tìm thấy Bên mời thầu.");

            }



            if (!await HasAdminOrManagerPermissionAsync(procuringEntityId, currentUserId, isAdmin))

            {

                return ApiResponse<ProcuringEntityInvitationDto>.Forbidden("Bạn không có quyền gửi lời mời thành viên cho đơn vị này.");

            }



            if (request.RoleInEntity == ProcuringEntityMemberRole.Signer)

            {

                return ApiResponse<ProcuringEntityInvitationDto>.Fail("Không thể gán trực tiếp vai trò Người ký duyệt (Signer) qua lời mời thành viên. Thẩm quyền ký phê duyệt giải ngân yêu cầu lập hồ sơ đề nghị và được Quản trị viên hệ thống thẩm định phê duyệt độc lập.");

            }



            var normalizedEmail = request.Email.Trim().ToLowerInvariant();



            // Kiểm tra xem đã là thành viên đang hoạt động chưa

            var isMember = await _context.ProcuringEntityMembers.AnyAsync(m =>

                m.ProcuringEntityId == procuringEntityId &&

                m.User.Email.ToLower() == normalizedEmail &&

                m.Status == MemberStatus.Active);



            if (isMember)

            {

                return ApiResponse<ProcuringEntityInvitationDto>.Fail("Nhân sự này hiện đã là thành viên đang hoạt động của Bên mời thầu.");

            }



            // Hủy lời mời cũ đang Pending của email này (nếu có)

            var oldInvitations = await _context.ProcuringEntityInvitations

                .Where(i => i.ProcuringEntityId == procuringEntityId &&

                            i.Email.ToLower() == normalizedEmail &&

                            i.Status == InvitationStatus.Pending)

                .ToListAsync();



            foreach (var old in oldInvitations)

            {

                old.Status = InvitationStatus.Revoked;

            }



            // Sinh token ngẫu nhiên bảo mật

            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();



            var inviter = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);



            var invitation = new ProcuringEntityInvitation

            {

                ProcuringEntityId = procuringEntityId,

                Email = normalizedEmail,

                RoleInEntity = request.RoleInEntity,

                TokenHash = tokenHash,

                InvitedByUserId = currentUserId,

                ExpiresAt = DateTime.UtcNow.AddDays(7),

                Status = InvitationStatus.Pending,

                CreatedAt = DateTime.UtcNow

            };



            await _context.ProcuringEntityInvitations.AddAsync(invitation);



            // Ghi nhận AuditLog

            var auditLog = new AuditLog

            {

                UserId = currentUserId,

                Action = "SEND_MEMBER_INVITATION",

                EntityType = "ProcuringEntityInvitation",

                NewValues = System.Text.Json.JsonSerializer.Serialize(new

                {

                    ProcuringEntityId = procuringEntityId,

                    Email = normalizedEmail,

                    RoleInEntity = request.RoleInEntity.ToString()

                }),

                Timestamp = DateTime.UtcNow

            };

            await _context.AuditLogs.AddAsync(auditLog);



            await _context.SaveChangesAsync();



            // Gửi email mời

            var clientBaseUrl = _configuration["ClientApp:BaseUrl"] ?? "http://localhost:5173";

            var acceptUrl = $"{clientBaseUrl}/invitation/accept?token={rawToken}";
            _logger.LogInformation("INVITATION_ACCEPT_URL: {AcceptUrl}", acceptUrl);

            var roleDescription = request.RoleInEntity switch

            {

                ProcuringEntityMemberRole.Manager => "Quản lý Đơn vị / Đầu mối pháp nhân",

                ProcuringEntityMemberRole.Officer => "Cán bộ phụ trách dự án / Kỹ thuật hiện trường (Maker)",

                ProcuringEntityMemberRole.Signer => "Người có thẩm quyền ký duyệt / Lãnh đạo (Checker)",

                ProcuringEntityMemberRole.Finance => "Kế toán / Kiểm soát tài chính",

                _ => request.RoleInEntity.ToString()

            };



            var emailBody = $@"

                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>

                    <h2 style='color: #0284c7;'>Lời mời tham gia Bên mời thầu</h2>

                    <p>Xin chào,</p>

                    <p>Bạn đã nhận được lời mời tham gia đơn vị <b>{pe.OrganizationName}</b> với vai trò <b>{roleDescription}</b> trên Hệ thống Đấu thầu & Theo dõi Hợp đồng.</p>

                    <p>Người gửi lời mời: <b>{inviter?.FullName ?? "Quản trị viên"}</b> ({inviter?.Email}).</p>

                    <p style='margin: 25px 0;'>

                        <a href='{acceptUrl}' style='background-color: #0284c7; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>

                            Xác thực và Tham gia Đơn vị

                        </a>

                    </p>

                    <p>Hoặc truy cập trực tiếp qua liên kết sau:<br/>

                        <a href='{acceptUrl}'>{acceptUrl}</a>

                    </p>

                    <p style='color: #64748b; font-size: 13px;'>Liên kết này có hiệu lực trong vòng 7 ngày kể từ thời điểm gửi thư.</p>

                </div>";



            try

            {

                await _emailService.SendEmailAsync(normalizedEmail, $"[Đấu thầu] Lời mời tham gia Bên mời thầu: {pe.OrganizationName}", emailBody, true);

            }

            catch (Exception ex)

            {

                _logger.LogWarning(ex, "Không thể gửi email thông báo mời thành viên tới {Email}.", normalizedEmail);

            }



            var dto = new ProcuringEntityInvitationDto

            {

                Id = invitation.Id,

                ProcuringEntityId = invitation.ProcuringEntityId,

                OrganizationName = pe.OrganizationName,

                Email = invitation.Email,

                RoleInEntity = invitation.RoleInEntity,

                RoleInEntityName = invitation.RoleInEntity.ToString(),

                InvitedByUserId = invitation.InvitedByUserId,

                InvitedByName = inviter?.FullName ?? string.Empty,

                ExpiresAt = invitation.ExpiresAt,

                Status = invitation.Status,

                StatusName = invitation.Status.ToString(),

                CreatedAt = invitation.CreatedAt

            };



            return ApiResponse<ProcuringEntityInvitationDto>.Ok(dto, "Gửi lời mời tham gia thành công.");

        }



        public async Task<ApiResponse<InvitationDetailDto>> GetInvitationByTokenAsync(string token)

        {

            if (string.IsNullOrWhiteSpace(token))

            {

                return ApiResponse<InvitationDetailDto>.Fail("Mã xác thực lời mời không hợp lệ.");

            }



            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();



            var invitation = await _context.ProcuringEntityInvitations

                .Include(i => i.ProcuringEntity)

                .Include(i => i.InvitedByUser)

                .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);



            if (invitation == null)

            {

                return ApiResponse<InvitationDetailDto>.Fail("Lời mời không tồn tại hoặc đường dẫn không hợp lệ.");

            }



            if (invitation.Status != InvitationStatus.Pending || DateTime.UtcNow > invitation.ExpiresAt)

            {

                return ApiResponse<InvitationDetailDto>.Fail("Lời mời này đã hết hạn, đã được chấp nhận hoặc bị thu hồi.");

            }



            var hasExistingAccount = await _context.Users.AnyAsync(u => u.Email.ToLower() == invitation.Email.ToLower());



            var dto = new InvitationDetailDto

            {

                Token = token,

                ProcuringEntityId = invitation.ProcuringEntityId,

                OrganizationName = invitation.ProcuringEntity.OrganizationName,

                TaxCode = invitation.ProcuringEntity.TaxCode,

                Email = invitation.Email,

                RoleInEntity = invitation.RoleInEntity,

                RoleInEntityName = invitation.RoleInEntity.ToString(),

                InvitedByName = invitation.InvitedByUser.FullName,

                ExpiresAt = invitation.ExpiresAt,

                IsExpired = DateTime.UtcNow > invitation.ExpiresAt,

                HasExistingAccount = hasExistingAccount

            };



            return ApiResponse<InvitationDetailDto>.Ok(dto, "Lấy thông tin lời mời thành công.");

        }



        public async Task<ApiResponse<ProcuringEntityMemberDto>> AcceptInvitationAsync(AcceptInvitationRequest request, int? currentUserId)

        {

            if (string.IsNullOrWhiteSpace(request.Token))

            {

                return ApiResponse<ProcuringEntityMemberDto>.Fail("Mã xác thực lời mời không được để trống.");

            }



            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token.Trim()))).ToLowerInvariant();



            using var transaction = await _context.Database.BeginTransactionAsync();

            try

            {

                var invitation = await _context.ProcuringEntityInvitations

                    .Include(i => i.ProcuringEntity)

                    .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);



                if (invitation == null)

                {

                    return ApiResponse<ProcuringEntityMemberDto>.Fail("Lời mời không tồn tại hoặc liên kết không đúng.");

                }



                if (invitation.Status != InvitationStatus.Pending || DateTime.UtcNow > invitation.ExpiresAt)

                {

                    return ApiResponse<ProcuringEntityMemberDto>.Fail("Lời mời này đã hết hạn, đã được chấp nhận hoặc bị thu hồi.");

                }



                User user;



                // TH1: Người dùng đã đăng nhập vào hệ thống

                if (currentUserId.HasValue && currentUserId.Value > 0)

                {

                    var loggedInUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId.Value);

                    if (loggedInUser == null)

                    {

                        return ApiResponse<ProcuringEntityMemberDto>.Fail("Tài khoản đang đăng nhập không hợp lệ.", statusCode: 401);

                    }



                    if (!string.Equals(loggedInUser.Email.Trim(), invitation.Email.Trim(), StringComparison.OrdinalIgnoreCase))

                    {

                        return ApiResponse<ProcuringEntityMemberDto>.Fail(

                            $"Bạn đang đăng nhập bằng tài khoản ({loggedInUser.Email}) khác với email nhận lời mời ({invitation.Email}). Vui lòng đăng nhập đúng tài khoản để chấp nhận lời mời.");

                    }



                    user = loggedInUser;

                }

                // TH2: Người dùng chưa đăng nhập

                else

                {

                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == invitation.Email.ToLower());

                    if (existingUser != null)

                    {

                        return ApiResponse<ProcuringEntityMemberDto>.Fail(

                            "Tài khoản với email này đã tồn tại trên hệ thống. Vui lòng đăng nhập vào tài khoản của bạn để xác nhận lời mời.");

                    }



                    // Đăng ký tài khoản cá nhân mới

                    if (string.IsNullOrWhiteSpace(request.FullName))

                    {

                        return ApiResponse<ProcuringEntityMemberDto>.Fail("Vui lòng nhập họ và tên của bạn.");

                    }



                    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)

                    {

                        return ApiResponse<ProcuringEntityMemberDto>.Fail("Mật khẩu phải có độ dài tối thiểu 6 ký tự.");

                    }



                    user = new User

                    {

                        FullName = request.FullName.Trim(),

                        Email = invitation.Email.Trim().ToLowerInvariant(),

                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),

                        IsActive = true,

                        CreatedAt = DateTime.UtcNow

                    };



                    await _context.Users.AddAsync(user);

                    await _context.SaveChangesAsync();



                    // Gán vai trò Procurement hệ thống

                    var procurementRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Procurement");

                    if (procurementRole != null)

                    {

                        await _context.UserRoles.AddAsync(new UserRole

                        {

                            UserId = user.Id,

                            RoleId = procurementRole.Id

                        });

                        await _context.SaveChangesAsync();

                    }

                }



                // Kiểm tra xem đã là thành viên của đơn vị này chưa

                var existingMember = await _context.ProcuringEntityMembers

                    .FirstOrDefaultAsync(m => m.ProcuringEntityId == invitation.ProcuringEntityId && m.UserId == user.Id);



                if (existingMember != null)

                {

                    if (existingMember.Status == MemberStatus.Active)

                    {

                        invitation.Status = InvitationStatus.Accepted;

                        invitation.AcceptedAt = DateTime.UtcNow;

                        invitation.AcceptedByUserId = user.Id;

                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();



                        return ApiResponse<ProcuringEntityMemberDto>.Ok(new ProcuringEntityMemberDto

                        {

                            Id = existingMember.Id,

                            ProcuringEntityId = existingMember.ProcuringEntityId,

                            UserId = user.Id,

                            FullName = user.FullName,

                            Email = user.Email,

                            RoleInEntity = existingMember.RoleInEntity,

                            RoleInEntityName = existingMember.RoleInEntity.ToString(),

                            Status = existingMember.Status,

                            StatusName = existingMember.Status.ToString(),

                            JoinedAt = existingMember.JoinedAt

                        }, "Bạn đã là thành viên của Bên mời thầu này.");

                    }

                    else

                    {

                        // Khôi phục trạng thái thành viên

                        existingMember.Status = MemberStatus.Active;

                        existingMember.RoleInEntity = invitation.RoleInEntity;

                        existingMember.JoinedAt = DateTime.UtcNow;

                        existingMember.RevokedAt = null;

                        existingMember.RevokedByUserId = null;

                    }

                }

                else

                {

                    existingMember = new ProcuringEntityMember

                    {

                        ProcuringEntityId = invitation.ProcuringEntityId,

                        UserId = user.Id,

                        RoleInEntity = invitation.RoleInEntity,

                        Status = MemberStatus.Active,

                        JoinedAt = DateTime.UtcNow,

                        InvitedByUserId = invitation.InvitedByUserId

                    };

                    await _context.ProcuringEntityMembers.AddAsync(existingMember);

                }



                // Đánh dấu lời mời đã chấp nhận

                invitation.Status = InvitationStatus.Accepted;

                invitation.AcceptedAt = DateTime.UtcNow;

                invitation.AcceptedByUserId = user.Id;



                // Ghi nhận AuditLog

                var auditLog = new AuditLog

                {

                    UserId = user.Id,

                    Action = "ACCEPT_MEMBER_INVITATION",

                    EntityType = "ProcuringEntityMember",

                    NewValues = System.Text.Json.JsonSerializer.Serialize(new

                    {

                        ProcuringEntityId = invitation.ProcuringEntityId,

                        UserId = user.Id,

                        RoleInEntity = invitation.RoleInEntity.ToString()

                    }),

                    Timestamp = DateTime.UtcNow

                };

                await _context.AuditLogs.AddAsync(auditLog);



                await _context.SaveChangesAsync();

                await transaction.CommitAsync();



                var dto = new ProcuringEntityMemberDto

                {

                    Id = existingMember.Id,

                    ProcuringEntityId = existingMember.ProcuringEntityId,

                    UserId = user.Id,

                    FullName = user.FullName,

                    Email = user.Email,

                    Phone = user.Phone,

                    RoleInEntity = existingMember.RoleInEntity,

                    RoleInEntityName = existingMember.RoleInEntity.ToString(),

                    Status = existingMember.Status,

                    StatusName = existingMember.Status.ToString(),

                    JoinedAt = existingMember.JoinedAt

                };



                return ApiResponse<ProcuringEntityMemberDto>.Ok(dto, "Chấp nhận lời mời tham gia Bên mời thầu thành công.");

            }

            catch (Exception ex)

            {

                await transaction.RollbackAsync();

                _logger.LogError(ex, "Lỗi xảy ra trong quá trình chấp nhận lời mời tham gia.");

                return ApiResponse<ProcuringEntityMemberDto>.Fail($"Lỗi khi xử lý chấp nhận lời mời: {ex.Message}");

            }

        }



        public async Task<ApiResponse<bool>> RevokeInvitationAsync(

            int procuringEntityId, int invitationId, int currentUserId, bool isAdmin)

        {

            if (!await HasAdminOrManagerPermissionAsync(procuringEntityId, currentUserId, isAdmin))

            {

                return ApiResponse<bool>.Forbidden("Bạn không có quyền thu hồi lời mời của đơn vị này.");

            }



            var invitation = await _context.ProcuringEntityInvitations

                .FirstOrDefaultAsync(i => i.Id == invitationId && i.ProcuringEntityId == procuringEntityId);



            if (invitation == null)

            {

                return ApiResponse<bool>.Fail("Không tìm thấy thông tin lời mời.");

            }



            if (invitation.Status != InvitationStatus.Pending)

            {

                return ApiResponse<bool>.Fail("Chỉ có thể thu hồi lời mời đang ở trạng thái chờ chấp nhận (Pending).");

            }



            invitation.Status = InvitationStatus.Revoked;



            var auditLog = new AuditLog

            {

                UserId = currentUserId,

                Action = "REVOKE_MEMBER_INVITATION",

                EntityType = "ProcuringEntityInvitation",

                EntityId = invitation.Id,

                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Status = "Revoked" }),

                Timestamp = DateTime.UtcNow

            };

            await _context.AuditLogs.AddAsync(auditLog);



            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Thu hồi lời mời thành công.");

        }



        public async Task<ApiResponse<ProcuringEntityMemberDto>> UpdateMemberRoleAsync(

            int procuringEntityId, int memberId, UpdateMemberRoleRequest request, int currentUserId, bool isAdmin)

        {

            if (!await HasAdminOrManagerPermissionAsync(procuringEntityId, currentUserId, isAdmin))

            {

                return ApiResponse<ProcuringEntityMemberDto>.Forbidden("Bạn không có quyền thay đổi vai trò thành viên của đơn vị này.");

            }



            var member = await _context.ProcuringEntityMembers

                .Include(m => m.User)

                .FirstOrDefaultAsync(m => m.Id == memberId && m.ProcuringEntityId == procuringEntityId);



            if (member == null)

            {

                return ApiResponse<ProcuringEntityMemberDto>.Fail("Không tìm thấy thông tin thành viên.");

            }



            if (request.RoleInEntity == ProcuringEntityMemberRole.Signer)

            {

                return ApiResponse<ProcuringEntityMemberDto>.Fail("Không thể gán trực tiếp vai trò Người ký duyệt (Signer). Thẩm quyền ký phê duyệt giải ngân yêu cầu lập hồ sơ đề nghị và được Quản trị viên hệ thống thẩm định phê duyệt độc lập.");

            }



            var pe = await _context.ProcuringEntities.FirstOrDefaultAsync(p => p.Id == procuringEntityId);

            if (pe != null && pe.UserId == member.UserId && request.RoleInEntity != ProcuringEntityMemberRole.Manager)

            {

                return ApiResponse<ProcuringEntityMemberDto>.Fail("Không thể hạ quyền Quản lý của Người đại diện pháp nhân ban đầu.");

            }



            var oldRole = member.RoleInEntity;

            member.RoleInEntity = request.RoleInEntity;

            if (request.Status.HasValue)

            {

                member.Status = request.Status.Value;

            }



            var auditLog = new AuditLog

            {

                UserId = currentUserId,

                Action = "UPDATE_MEMBER_ROLE",

                EntityType = "ProcuringEntityMember",

                EntityId = member.Id,

                OldValues = System.Text.Json.JsonSerializer.Serialize(new { RoleInEntity = oldRole.ToString() }),

                NewValues = System.Text.Json.JsonSerializer.Serialize(new

                {

                    RoleInEntity = member.RoleInEntity.ToString(),

                    Status = member.Status.ToString()

                }),

                Timestamp = DateTime.UtcNow

            };

            await _context.AuditLogs.AddAsync(auditLog);



            await _context.SaveChangesAsync();



            var dto = new ProcuringEntityMemberDto

            {

                Id = member.Id,

                ProcuringEntityId = member.ProcuringEntityId,

                UserId = member.UserId,

                FullName = member.User.FullName,

                Email = member.User.Email,

                Phone = member.User.Phone,

                RoleInEntity = member.RoleInEntity,

                RoleInEntityName = member.RoleInEntity.ToString(),

                Status = member.Status,

                StatusName = member.Status.ToString(),

                JoinedAt = member.JoinedAt

            };



            return ApiResponse<ProcuringEntityMemberDto>.Ok(dto, "Cập nhật vai trò thành viên thành công.");

        }



        public async Task<ApiResponse<bool>> RevokeMemberAsync(

            int procuringEntityId, int memberId, int currentUserId, bool isAdmin)

        {

            if (!await HasAdminOrManagerPermissionAsync(procuringEntityId, currentUserId, isAdmin))

            {

                return ApiResponse<bool>.Forbidden("Bạn không có quyền thu hồi tư cách thành viên của đơn vị này.");

            }



            var member = await _context.ProcuringEntityMembers

                .Include(m => m.User)

                .FirstOrDefaultAsync(m => m.Id == memberId && m.ProcuringEntityId == procuringEntityId);



            if (member == null)

            {

                return ApiResponse<bool>.Fail("Không tìm thấy thông tin thành viên.");

            }



            var pe = await _context.ProcuringEntities.FirstOrDefaultAsync(p => p.Id == procuringEntityId);

            if (pe != null && pe.UserId == member.UserId)

            {

                return ApiResponse<bool>.Fail("Không thể thu hồi tư cách thành viên của Người đại diện pháp nhân ban đầu.");

            }



            if (member.UserId == currentUserId && !isAdmin)

            {

                return ApiResponse<bool>.Fail("Bạn không thể tự thu hồi tư cách thành viên của chính mình.");

            }



            member.Status = MemberStatus.Revoked;

            member.RevokedAt = DateTime.UtcNow;

            member.RevokedByUserId = currentUserId;



            var auditLog = new AuditLog

            {

                UserId = currentUserId,

                Action = "REVOKE_MEMBER",

                EntityType = "ProcuringEntityMember",

                EntityId = member.Id,

                NewValues = System.Text.Json.JsonSerializer.Serialize(new

                {

                    Status = "Revoked",

                    RevokedByUserId = currentUserId,

                    RevokedAt = member.RevokedAt

                }),

                Timestamp = DateTime.UtcNow

            };

            await _context.AuditLogs.AddAsync(auditLog);



            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Thu hồi tư cách thành viên thành công.");

        }

    }

}
