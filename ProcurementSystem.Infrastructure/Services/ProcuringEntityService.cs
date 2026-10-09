using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.ProcuringEntity;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class ProcuringEntityService : IProcuringEntityService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;

        public ProcuringEntityService(
            AppDbContext context,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<PaginatedList<ProcuringEntityDto>>> GetProcuringEntitiesAsync(ProcuringEntityFilterParams filter)
        {
            var query = _context.ProcuringEntities
                .Include(pe => pe.User)
                .Include(pe => pe.ReviewedByUser)
                .AsNoTracking();

            // 1. Tìm kiếm theo từ khóa
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var term = filter.Keyword.Trim().ToLower();
                query = query.Where(pe =>
                    pe.OrganizationName.ToLower().Contains(term) ||
                    pe.TaxCode.ToLower().Contains(term) ||
                    pe.RepresentativeName.ToLower().Contains(term) ||
                    pe.User.Email.ToLower().Contains(term) ||
                    (pe.BudgetCode != null && pe.BudgetCode.ToLower().Contains(term)));
            }

            // 2. Lọc theo mã số thuế
            if (!string.IsNullOrWhiteSpace(filter.TaxCode))
            {
                var tax = filter.TaxCode.Trim();
                query = query.Where(pe => pe.TaxCode == tax);
            }

            // 3. Lọc theo loại hình đơn vị
            if (!string.IsNullOrWhiteSpace(filter.OrganizationType))
            {
                var type = filter.OrganizationType.Trim().ToLower();
                query = query.Where(pe => pe.OrganizationType.ToLower() == type);
            }

            // 4. Lọc theo trạng thái thẩm định
            if (!string.IsNullOrWhiteSpace(filter.VerificationStatus))
            {
                var status = filter.VerificationStatus.Trim().ToLower();
                query = query.Where(pe => pe.VerificationStatus.ToLower() == status);
            }

            // Sắp xếp: Ưu tiên Pending lên đầu, sau đó theo CreatedAt giảm dần
            query = query.OrderBy(pe => pe.VerificationStatus == "Pending" ? 0 : 1)
                         .ThenByDescending(pe => pe.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(pe => MapToDto(pe))
                .ToListAsync();

            var paginated = new PaginatedList<ProcuringEntityDto>(items, totalCount, filter.PageNumber, filter.PageSize);
            return ApiResponse<PaginatedList<ProcuringEntityDto>>.Ok(paginated);
        }

        public async Task<ApiResponse<ProcuringEntityDto>> GetProcuringEntityByIdAsync(int id)
        {
            var entity = await _context.ProcuringEntities
                .Include(pe => pe.User)
                .Include(pe => pe.ReviewedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(pe => pe.Id == id);

            if (entity == null)
            {
                return ApiResponse<ProcuringEntityDto>.Fail("Không tìm thấy thông tin Bên mời thầu.");
            }

            return ApiResponse<ProcuringEntityDto>.Ok(MapToDto(entity));
        }

        public async Task<ApiResponse<ProcuringEntityDto>> GetProcuringEntityByUserIdAsync(int userId)
        {
            var entity = await _context.ProcuringEntities
                .Include(pe => pe.User)
                .Include(pe => pe.ReviewedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(pe => pe.UserId == userId);

            if (entity == null)
            {
                return ApiResponse<ProcuringEntityDto>.Fail("Không tìm thấy hồ sơ Bên mời thầu tương ứng với tài khoản này.");
            }

            return ApiResponse<ProcuringEntityDto>.Ok(MapToDto(entity));
        }

        public async Task<ApiResponse<ProcuringEntityDto>> VerifyProcuringEntityAsync(int id, int adminUserId, VerifyProcuringEntityRequest request)
        {
            var entity = await _context.ProcuringEntities
                .Include(pe => pe.User)
                .FirstOrDefaultAsync(pe => pe.Id == id);

            if (entity == null)
            {
                return ApiResponse<ProcuringEntityDto>.Fail("Không tìm thấy thông tin Bên mời thầu.");
            }

            var oldStatus = entity.VerificationStatus;
            var newStatus = request.IsApproved ? "Approved" : "Rejected";

            entity.VerificationStatus = newStatus;
            entity.AdminNotes = request.AdminNotes?.Trim();
            entity.ReviewedByUserId = adminUserId;
            entity.ReviewedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // Ghi nhận AuditLog
            var auditLog = new AuditLog
            {
                UserId = adminUserId,
                Action = "VERIFY_PROCURING_ENTITY",
                EntityType = "ProcuringEntity",
                EntityId = entity.Id,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { VerificationStatus = oldStatus }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { VerificationStatus = newStatus, request.AdminNotes }),
                Timestamp = DateTime.UtcNow
            };
            await _context.AuditLogs.AddAsync(auditLog);

            // Gửi thông báo đến tài khoản Bên mời thầu
            var notif = new Notification
            {
                UserId = entity.UserId,
                Title = request.IsApproved ? "Hồ sơ Bên mời thầu đã được phê duyệt" : "Hồ sơ Bên mời thầu bị từ chối phê duyệt",
                Message = request.IsApproved
                    ? $"Hồ sơ pháp nhân đơn vị {entity.OrganizationName} đã được Quản trị viên thẩm định phê duyệt thành công. Bạn đã có toàn quyền tạo gói thầu và tổ chức đấu thầu."
                    : $"Hồ sơ pháp nhân đơn vị {entity.OrganizationName} đã bị từ chối phê duyệt. Lý do: {request.AdminNotes ?? "Chưa đáp ứng đủ điều kiện pháp lý theo quy định."}",
                Type = request.IsApproved ? NotificationType.Info : NotificationType.Warning,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _context.Notifications.AddAsync(notif);

            await _context.SaveChangesAsync();

            var message = request.IsApproved
                ? "Phê duyệt hồ sơ pháp nhân Bên mời thầu thành công."
                : "Đã từ chối phê duyệt hồ sơ Bên mời thầu.";

            return ApiResponse<ProcuringEntityDto>.Ok(MapToDto(entity), message);
        }

        public async Task<(byte[] FileBytes, string ContentType, string FileName)?> GetEstablishmentFileAsync(int id)
        {
            var entity = await _context.ProcuringEntities.AsNoTracking().FirstOrDefaultAsync(pe => pe.Id == id);
            if (entity == null || string.IsNullOrEmpty(entity.EstablishmentDecisionFile))
            {
                return null;
            }

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), entity.EstablishmentDecisionFile.TrimStart('/', '\\'));
            if (!System.IO.File.Exists(physicalPath))
            {
                return null;
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            var fileName = Path.GetFileName(physicalPath);
            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            return (bytes, contentType, fileName);
        }

        public async Task<(byte[] FileBytes, string ContentType, string FileName)?> GetAppointmentFileAsync(int id)
        {
            var entity = await _context.ProcuringEntities.AsNoTracking().FirstOrDefaultAsync(pe => pe.Id == id);
            if (entity == null || string.IsNullOrEmpty(entity.AppointmentDecisionFile))
            {
                return null;
            }

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), entity.AppointmentDecisionFile.TrimStart('/', '\\'));
            if (!System.IO.File.Exists(physicalPath))
            {
                return null;
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            var fileName = Path.GetFileName(physicalPath);
            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            return (bytes, contentType, fileName);
        }

        private static ProcuringEntityDto MapToDto(ProcuringEntity entity)
        {
            return new ProcuringEntityDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Email = entity.User?.Email ?? string.Empty,
                FullName = entity.User?.FullName ?? string.Empty,
                Phone = entity.User?.Phone,
                OrganizationName = entity.OrganizationName,
                OrganizationType = entity.OrganizationType,
                TaxCode = entity.TaxCode,
                BudgetCode = entity.BudgetCode,
                Address = entity.Address,
                RepresentativeName = entity.RepresentativeName,
                RepresentativeTitle = entity.RepresentativeTitle,
                RepresentativePhone = entity.RepresentativePhone,
                EstablishmentDecisionFile = entity.EstablishmentDecisionFile,
                AppointmentDecisionFile = entity.AppointmentDecisionFile,
                VerificationStatus = entity.VerificationStatus,
                AdminNotes = entity.AdminNotes,
                ReviewedByUserId = entity.ReviewedByUserId,
                ReviewedByUserName = entity.ReviewedByUser?.FullName,
                ReviewedAt = entity.ReviewedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
