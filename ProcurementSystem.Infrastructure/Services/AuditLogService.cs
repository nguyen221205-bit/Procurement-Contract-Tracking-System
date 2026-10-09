using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Audit;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<PaginatedList<AuditLogDto>>> GetAuditLogsAsync(AuditLogFilterParams filter)
        {
            var query = _unitOfWork.Repository<AuditLog>()
                .Query()
                .Include(a => a.User)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Action))
            {
                var action = filter.Action.Trim().ToUpper();
                query = query.Where(a => a.Action.ToUpper() == action);
            }

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
            {
                var entityType = filter.EntityType.Trim().ToLower();
                query = query.Where(a => a.EntityType.ToLower() == entityType);
            }

            if (filter.EntityId.HasValue)
            {
                query = query.Where(a => a.EntityId == filter.EntityId.Value);
            }

            if (filter.UserId.HasValue)
            {
                query = query.Where(a => a.UserId == filter.UserId.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= filter.ToDate.Value);
            }

            var dtoQuery = query
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserFullName = a.User != null ? a.User.FullName : null,
                    UserEmail = a.User != null ? a.User.Email : null,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    Timestamp = a.Timestamp,
                    IpAddress = a.IpAddress
                });

            var result = await PaginatedList<AuditLogDto>.CreateAsync(dtoQuery, filter.PageIndex, filter.PageSize);
            return ApiResponse<PaginatedList<AuditLogDto>>.Ok(result, "Lấy lịch sử kiểm toán thành công.");
        }

        public async Task<ApiResponse<AuditLogDto>> GetAuditLogByIdAsync(int id)
        {
            var log = await _unitOfWork.Repository<AuditLog>()
                .Query()
                .Include(a => a.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (log == null)
            {
                return ApiResponse<AuditLogDto>.Fail("Không tìm thấy bản ghi kiểm toán.");
            }

            var dto = new AuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserFullName = log.User?.FullName,
                UserEmail = log.User?.Email,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                Timestamp = log.Timestamp,
                IpAddress = log.IpAddress
            };

            return ApiResponse<AuditLogDto>.Ok(dto);
        }
    }
}
