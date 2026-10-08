using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.EvaluatorProposal;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class EvaluatorProposalService : IEvaluatorProposalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public EvaluatorProposalService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<PaginatedList<EvaluatorProposalDto>>> GetProposalsAsync(
            EvaluatorProposalFilterParams filter, int currentUserId, string currentUserRole)
        {
            var query = _unitOfWork.Repository<EvaluatorProposal>()
                .Query()
                .Include(ep => ep.ProposerUser)
                .Include(ep => ep.ReviewedByUser)
                .AsNoTracking();

            // Phân quyền: Procurement chỉ xem đề xuất của chính mình; Admin xem được tất cả
            if (currentUserRole != "Admin")
            {
                query = query.Where(ep => ep.ProposerUserId == currentUserId);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var s = filter.Status.Trim().ToLower();
                query = query.Where(ep => ep.Status.ToLower() == s);
            }

            // Tìm kiếm theo Họ tên, Email, Chuyên môn, Nơi công tác
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(ep =>
                    ep.FullName.ToLower().Contains(term) ||
                    ep.Email.ToLower().Contains(term) ||
                    (ep.Specialization != null && ep.Specialization.ToLower().Contains(term)) ||
                    (ep.Workplace != null && ep.Workplace.ToLower().Contains(term)));
            }

            // Sắp xếp mặc định: Pending lên đầu, sau đó theo CreatedAt mới nhất
            query = query.OrderBy(ep => ep.Status == "Pending" ? 0 : 1)
                         .ThenByDescending(ep => ep.CreatedAt);

            var dtoQuery = query.Select(ep => MapToDto(ep));
            var paged = await PaginatedList<EvaluatorProposalDto>.CreateAsync(
                dtoQuery, filter.PageIndex, filter.PageSize);

            return ApiResponse<PaginatedList<EvaluatorProposalDto>>.Ok(paged);
        }

        public async Task<ApiResponse<EvaluatorProposalDto>> GetProposalByIdAsync(
            int id, int currentUserId, string currentUserRole)
        {
            var proposal = await _unitOfWork.Repository<EvaluatorProposal>()
                .Query()
                .Include(ep => ep.ProposerUser)
                .Include(ep => ep.ReviewedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(ep => ep.Id == id);

            if (proposal == null)
            {
                return ApiResponse<EvaluatorProposalDto>.Fail("Không tìm thấy đề xuất giám khảo.");
            }

            if (currentUserRole != "Admin" && proposal.ProposerUserId != currentUserId)
            {
                return ApiResponse<EvaluatorProposalDto>.Fail("Bạn không có quyền truy cập đề xuất này.");
            }

            return ApiResponse<EvaluatorProposalDto>.Ok(MapToDto(proposal));
        }

        public async Task<ApiResponse<EvaluatorProposalDto>> CreateProposalAsync(
            CreateEvaluatorProposalRequest request, int proposerUserId)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // 1. Kiểm tra Email đã tồn tại trong bảng Users chưa
            var userExists = await _unitOfWork.Repository<User>()
                .ExistsAsync(u => u.Email.ToLower() == normalizedEmail);
            if (userExists)
            {
                return ApiResponse<EvaluatorProposalDto>.Fail(
                    $"Email '{request.Email}' đã được đăng ký tài khoản trong hệ thống.");
            }

            // 2. Kiểm tra Email đã có đề xuất Pending nào chưa duyệt không
            var pendingExists = await _unitOfWork.Repository<EvaluatorProposal>()
                .ExistsAsync(ep => ep.Email.ToLower() == normalizedEmail && ep.Status == "Pending");
            if (pendingExists)
            {
                return ApiResponse<EvaluatorProposalDto>.Fail(
                    $"Đã tồn tại một đề xuất đang chờ duyệt cho chuyên gia có email '{request.Email}'.");
            }

            // 3. Lưu file chứng chỉ (nếu có)
            string? fileUrl = null;
            if (request.CertificateFile != null && request.CertificateFile.Length > 0)
            {
                try
                {
                    fileUrl = await _fileStorageService.SaveFileAsync(request.CertificateFile, "evaluator_certificates");
                }
                catch (Exception ex)
                {
                    return ApiResponse<EvaluatorProposalDto>.Fail($"Lỗi khi lưu tệp chứng chỉ: {ex.Message}");
                }
            }

            // 4. Tạo bản ghi EvaluatorProposal
            var proposal = new EvaluatorProposal
            {
                ProposerUserId = proposerUserId,
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone?.Trim(),
                Specialization = request.Specialization?.Trim(),
                Workplace = request.Workplace?.Trim(),
                ExperienceYears = request.ExperienceYears,
                CertificateName = request.CertificateName?.Trim(),
                CertificateFile = fileUrl,
                Notes = request.Notes?.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<EvaluatorProposal>().AddAsync(proposal);
            await _unitOfWork.SaveChangesAsync();

            // 5. Gửi thông báo đến các Admin
            var adminUsers = await _unitOfWork.Repository<User>()
                .Query()
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin") && u.IsActive)
                .ToListAsync();

            foreach (var admin in adminUsers)
            {
                var notif = new Notification
                {
                    UserId = admin.Id,
                    Title = "Đề xuất chỉ định Giám khảo mới",
                    Message = $"Bên mời thầu vừa gửi đề xuất chỉ định chuyên gia {proposal.FullName} ({proposal.Specialization ?? "Chuyên môn chung"}) làm Giám khảo. Vui lòng thẩm định.",
                    Type = NotificationType.Info,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _unitOfWork.Repository<Notification>().AddAsync(notif);
            }

            // 6. Ghi AuditLog
            var auditLog = new AuditLog
            {
                UserId = proposerUserId,
                Action = "CREATE_EVALUATOR_PROPOSAL",
                EntityType = "EvaluatorProposal",
                EntityId = proposal.Id,
                NewValues = JsonSerializer.Serialize(new
                {
                    proposal.FullName,
                    proposal.Email,
                    proposal.Specialization,
                    proposal.CertificateName
                }),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            var createdProposal = await _unitOfWork.Repository<EvaluatorProposal>()
                .Query()
                .Include(ep => ep.ProposerUser)
                .FirstAsync(ep => ep.Id == proposal.Id);

            return ApiResponse<EvaluatorProposalDto>.Ok(
                MapToDto(createdProposal),
                "Gửi đề xuất chỉ định Giám khảo thành công. Yêu cầu đang chờ Quản trị viên (Admin) thẩm định phê duyệt.");
        }

        public async Task<ApiResponse<EvaluatorProposalDto>> ApproveProposalAsync(
            int id, ApproveEvaluatorProposalRequest request, int adminUserId)
        {
            var proposal = await _unitOfWork.Repository<EvaluatorProposal>()
                .Query()
                .Include(ep => ep.ProposerUser)
                .FirstOrDefaultAsync(ep => ep.Id == id);

            if (proposal == null)
            {
                return ApiResponse<EvaluatorProposalDto>.Fail("Không tìm thấy đề xuất giám khảo.");
            }

            if (proposal.Status != "Pending")
            {
                return ApiResponse<EvaluatorProposalDto>.Fail(
                    $"Đề xuất này đã được xử lý trước đó (Trạng thái hiện tại: {proposal.Status}).");
            }

            var normalizedEmail = proposal.Email.Trim().ToLowerInvariant();

            // Kiểm tra email chưa tồn tại
            var userExists = await _unitOfWork.Repository<User>()
                .ExistsAsync(u => u.Email.ToLower() == normalizedEmail);
            if (userExists)
            {
                return ApiResponse<EvaluatorProposalDto>.Fail(
                    $"Tài khoản với email '{proposal.Email}' đã tồn tại trong hệ thống. Không thể tạo trùng.");
            }

            // Bắt đầu Transaction tạo User & duyệt Proposal
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Tạo User mới
                var newUser = new User
                {
                    FullName = proposal.FullName.Trim(),
                    Email = normalizedEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Phone = proposal.Phone?.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<User>().AddAsync(newUser);
                await _unitOfWork.SaveChangesAsync(); // Lưu để sinh ID cho User

                // 2. Gán Role "Evaluator"
                var evaluatorRole = (await _unitOfWork.Repository<Role>()
                    .FindAsync(r => r.Name == "Evaluator")).FirstOrDefault();

                if (evaluatorRole == null)
                {
                    throw new Exception("Role 'Evaluator' chưa được định nghĩa trong hệ thống.");
                }

                var userRole = new UserRole
                {
                    UserId = newUser.Id,
                    RoleId = evaluatorRole.Id
                };
                await _unitOfWork.Repository<UserRole>().AddAsync(userRole);

                // 3. Cập nhật Proposal sang Approved
                proposal.Status = "Approved";
                proposal.CreatedUserId = newUser.Id;
                proposal.ReviewedByUserId = adminUserId;
                proposal.ReviewedAt = DateTime.UtcNow;
                proposal.AdminNotes = request.AdminNotes?.Trim();

                _unitOfWork.Repository<EvaluatorProposal>().Update(proposal);

                // 4. Gửi thông báo cho Bên mời thầu (Proposer)
                var notif = new Notification
                {
                    UserId = proposal.ProposerUserId,
                    Title = "Đề xuất Giám khảo đã được phê duyệt",
                    Message = $"Đề xuất chỉ định chuyên gia {proposal.FullName} làm Giám khảo đã được Quản trị viên phê duyệt thành công. Tài khoản ({proposal.Email}) đã sẵn sàng để phân công vào Tổ chuyên gia.",
                    Type = NotificationType.Info,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _unitOfWork.Repository<Notification>().AddAsync(notif);

                // 5. Ghi AuditLog
                var auditLog = new AuditLog
                {
                    UserId = adminUserId,
                    Action = "APPROVE_EVALUATOR_PROPOSAL",
                    EntityType = "EvaluatorProposal",
                    EntityId = proposal.Id,
                    OldValues = JsonSerializer.Serialize(new { Status = "Pending" }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        Status = "Approved",
                        CreatedUserId = newUser.Id,
                        UserEmail = newUser.Email,
                        AdminNotes = request.AdminNotes
                    }),
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<EvaluatorProposalDto>.Ok(
                    MapToDto(proposal),
                    $"Đã phê duyệt đề xuất và khởi tạo thành công tài khoản Giám khảo cho chuyên gia {proposal.FullName}.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<EvaluatorProposalDto>.Fail($"Lỗi khi khởi tạo tài khoản giám khảo: {ex.Message}");
            }
        }

        public async Task<ApiResponse<EvaluatorProposalDto>> RejectProposalAsync(
            int id, RejectEvaluatorProposalRequest request, int adminUserId)
        {
            var proposal = await _unitOfWork.Repository<EvaluatorProposal>()
                .Query()
                .Include(ep => ep.ProposerUser)
                .FirstOrDefaultAsync(ep => ep.Id == id);

            if (proposal == null)
            {
                return ApiResponse<EvaluatorProposalDto>.Fail("Không tìm thấy đề xuất giám khảo.");
            }

            if (proposal.Status != "Pending")
            {
                return ApiResponse<EvaluatorProposalDto>.Fail(
                    $"Đề xuất này đã được xử lý trước đó (Trạng thái hiện tại: {proposal.Status}).");
            }

            proposal.Status = "Rejected";
            proposal.AdminNotes = request.AdminNotes.Trim();
            proposal.ReviewedByUserId = adminUserId;
            proposal.ReviewedAt = DateTime.UtcNow;

            _unitOfWork.Repository<EvaluatorProposal>().Update(proposal);

            // Gửi thông báo cho Bên mời thầu
            var notif = new Notification
            {
                UserId = proposal.ProposerUserId,
                Title = "Đề xuất Giám khảo bị từ chối",
                Message = $"Đề xuất chỉ định chuyên gia {proposal.FullName} đã bị Quản trị viên từ chối. Lý do: {proposal.AdminNotes}",
                Type = NotificationType.Warning,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _unitOfWork.Repository<Notification>().AddAsync(notif);

            // Ghi AuditLog
            var auditLog = new AuditLog
            {
                UserId = adminUserId,
                Action = "REJECT_EVALUATOR_PROPOSAL",
                EntityType = "EvaluatorProposal",
                EntityId = proposal.Id,
                OldValues = JsonSerializer.Serialize(new { Status = "Pending" }),
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = "Rejected",
                    AdminNotes = request.AdminNotes
                }),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<EvaluatorProposalDto>.Ok(
                MapToDto(proposal),
                $"Đã từ chối đề xuất chỉ định Giám khảo của chuyên gia {proposal.FullName}.");
        }

        public async Task<ApiResponse<(string PhysicalPath, string ContentType, string FileName)>> GetCertificateFileForDownloadAsync(
            int id, int currentUserId, string currentUserRole)
        {
            var proposal = await _unitOfWork.Repository<EvaluatorProposal>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(ep => ep.Id == id);

            if (proposal == null)
            {
                return ApiResponse<(string, string, string)>.Fail("Không tìm thấy đề xuất giám khảo.");
            }

            if (currentUserRole != "Admin" && proposal.ProposerUserId != currentUserId)
            {
                return ApiResponse<(string, string, string)>.Fail("Bạn không có quyền xem tệp chứng chỉ của đề xuất này.");
            }

            if (string.IsNullOrWhiteSpace(proposal.CertificateFile))
            {
                return ApiResponse<(string, string, string)>.Fail("Đề xuất này không có tệp chứng chỉ đính kèm.");
            }

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), proposal.CertificateFile.TrimStart('/'));
            if (!File.Exists(physicalPath))
            {
                return ApiResponse<(string, string, string)>.Fail("Tệp chứng chỉ không tồn tại trên hệ thống lưu trữ.");
            }

            var fileName = Path.GetFileName(physicalPath);
            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            return ApiResponse<(string, string, string)>.Ok((physicalPath, contentType, fileName));
        }

        private static EvaluatorProposalDto MapToDto(EvaluatorProposal ep)
        {
            return new EvaluatorProposalDto
            {
                Id = ep.Id,
                ProposerUserId = ep.ProposerUserId,
                ProposerName = ep.ProposerUser != null ? ep.ProposerUser.FullName : string.Empty,
                ProposerEmail = ep.ProposerUser != null ? ep.ProposerUser.Email : string.Empty,
                FullName = ep.FullName,
                Email = ep.Email,
                Phone = ep.Phone,
                Specialization = ep.Specialization,
                Workplace = ep.Workplace,
                ExperienceYears = ep.ExperienceYears,
                CertificateName = ep.CertificateName,
                CertificateFile = ep.CertificateFile,
                Notes = ep.Notes,
                Status = ep.Status,
                AdminNotes = ep.AdminNotes,
                ReviewedByUserId = ep.ReviewedByUserId,
                ReviewedByName = ep.ReviewedByUser != null ? ep.ReviewedByUser.FullName : null,
                ReviewedAt = ep.ReviewedAt,
                CreatedUserId = ep.CreatedUserId,
                CreatedAt = ep.CreatedAt
            };
        }
    }
}
