using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidPackage;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class BidPackageService : IBidPackageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public BidPackageService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<PaginatedList<BidPackageSummaryDto>>> GetBidPackagesAsync(BidPackageFilterParams filter)
        {
            var query = _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .AsNoTracking();

            // 1. Tìm kiếm theo mã hoặc tên gói thầu
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var keyword = filter.Search.Trim().ToLower();
                query = query.Where(bp => bp.Code.ToLower().Contains(keyword)
                                       || bp.Name.ToLower().Contains(keyword));
            }

            // 2. Lọc theo loại gói thầu (Goods, Construction, Service)
            if (filter.Type.HasValue)
            {
                query = query.Where(bp => bp.Type == filter.Type.Value);
            }

            // 3. Lọc theo trạng thái
            if (filter.Status.HasValue)
            {
                query = query.Where(bp => bp.Status == filter.Status.Value);
            }

            // 4. Lọc theo khoảng ngân sách
            if (filter.MinBudget.HasValue)
            {
                query = query.Where(bp => bp.Budget >= filter.MinBudget.Value);
            }
            if (filter.MaxBudget.HasValue)
            {
                query = query.Where(bp => bp.Budget <= filter.MaxBudget.Value);
            }

            // 5. Lọc theo thời hạn nộp thầu
            if (filter.FromDeadline.HasValue)
            {
                query = query.Where(bp => bp.Deadline >= filter.FromDeadline.Value);
            }
            if (filter.ToDeadline.HasValue)
            {
                query = query.Where(bp => bp.Deadline <= filter.ToDeadline.Value);
            }

            // Mặc định sắp xếp gói thầu mới tạo lên đầu
            query = query.OrderByDescending(bp => bp.CreatedAt);

            var totalCount = await query.CountAsync();
            var pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var items = await query.Skip((pageIndex - 1) * pageSize)
                                   .Take(pageSize)
                                   .Select(bp => new BidPackageSummaryDto
                                   {
                                       Id = bp.Id,
                                       Code = bp.Code,
                                       Name = bp.Name,
                                       Type = bp.Type,
                                       Budget = bp.Budget,
                                       Deadline = bp.Deadline,
                                       Status = bp.Status,
                                       CreatedByName = bp.Creator != null ? bp.Creator.FullName : null,
                                       CreatedAt = bp.CreatedAt,
                                       DocumentsCount = bp.BidDocuments.Count,
                                       SubmissionsCount = bp.BidSubmissions.Count
                                   })
                                   .ToListAsync();

            var paginatedList = new PaginatedList<BidPackageSummaryDto>(items, totalCount, pageIndex, pageSize);
            return ApiResponse<PaginatedList<BidPackageSummaryDto>>.Ok(paginatedList);
        }

        public async Task<ApiResponse<BidPackageDto>> GetBidPackageByIdAsync(int id)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (package == null)
            {
                return ApiResponse<BidPackageDto>.Fail("Không tìm thấy gói thầu.");
            }

            var dto = MapToBidPackageDto(package);
            return ApiResponse<BidPackageDto>.Ok(dto);
        }

        public async Task<ApiResponse<BidPackageDto>> CreateBidPackageAsync(CreateBidPackageRequest request, int userId)
        {
            // 1. Kiểm tra hạn nộp thầu phải trong tương lai
            if (request.Deadline <= DateTime.UtcNow)
            {
                return ApiResponse<BidPackageDto>.Fail("Thời hạn nộp thầu phải lớn hơn thời điểm hiện tại.");
            }

            // 2. Xác định mã gói thầu (tự sinh nếu để trống)
            string code;
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                code = $"PKG-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            }
            else
            {
                code = request.Code.Trim().ToUpper();
                var isExist = await _unitOfWork.Repository<BidPackage>().ExistsAsync(bp => bp.Code == code);
                if (isExist)
                {
                    return ApiResponse<BidPackageDto>.Fail($"Mã gói thầu '{code}' đã tồn tại trong hệ thống.");
                }
            }

            var entity = new BidPackage
            {
                Code = code,
                Name = request.Name.Trim(),
                Type = request.Type,
                Budget = request.Budget,
                Deadline = request.Deadline,
                Status = BidPackageStatus.Open,
                Description = request.Description?.Trim(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<BidPackage>().AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            // Load lại creator để hiển thị DTO
            var createdPackage = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .AsNoTracking()
                .FirstAsync(bp => bp.Id == entity.Id);

            return ApiResponse<BidPackageDto>.Ok(MapToBidPackageDto(createdPackage), "Tạo gói thầu thành công.");
        }

        public async Task<ApiResponse<BidPackageDto>> UpdateBidPackageAsync(int id, UpdateBidPackageRequest request, int userId, bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (package == null)
            {
                return ApiResponse<BidPackageDto>.Fail("Không tìm thấy gói thầu.");
            }

            // Phân quyền: Chỉ người tạo hoặc Admin mới được sửa
            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<BidPackageDto>.Fail("Bạn không có quyền chỉnh sửa gói thầu này.");
            }

            // Không cho phép sửa nếu gói thầu đang chấm điểm hoặc đã ký hợp đồng
            if (package.Status == BidPackageStatus.Evaluating || package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<BidPackageDto>.Fail("Không thể chỉnh sửa gói thầu đang trong giai đoạn chấm điểm hoặc đã ký hợp đồng.");
            }

            if (request.Deadline <= DateTime.UtcNow && package.Status == BidPackageStatus.Open)
            {
                return ApiResponse<BidPackageDto>.Fail("Thời hạn nộp thầu phải lớn hơn thời điểm hiện tại.");
            }

            package.Name = request.Name.Trim();
            package.Type = request.Type;
            package.Budget = request.Budget;
            package.Deadline = request.Deadline;
            package.Description = request.Description?.Trim();
            package.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<BidPackage>().Update(package);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<BidPackageDto>.Ok(MapToBidPackageDto(package), "Cập nhật thông tin gói thầu thành công.");
        }

        public async Task<ApiResponse<BidPackageDto>> ChangeStatusAsync(int id, ChangeBidPackageStatusRequest request, int userId, bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (package == null)
            {
                return ApiResponse<BidPackageDto>.Fail("Không tìm thấy gói thầu.");
            }

            // Phân quyền: Chỉ người tạo hoặc Admin mới được đổi trạng thái
            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<BidPackageDto>.Fail("Bạn không có quyền thay đổi trạng thái gói thầu này.");
            }

            if (package.Status == request.NewStatus)
            {
                return ApiResponse<BidPackageDto>.Ok(MapToBidPackageDto(package), "Trạng thái không thay đổi.");
            }

            // State Machine Validation
            if (package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<BidPackageDto>.Fail("Gói thầu đã ký hợp đồng, không thể thay đổi trạng thái.");
            }

            // Kiểm tra quy tắc chuyển trạng thái hợp lệ
            var isValidTransition = (package.Status, request.NewStatus) switch
            {
                // Từ Open có thể chuyển sang Closed (Đóng thầu)
                (BidPackageStatus.Open, BidPackageStatus.Closed) => true,

                // Từ Closed có thể chuyển sang Evaluating (Chấm điểm) hoặc mở lại Open (Gia hạn thầu)
                (BidPackageStatus.Closed, BidPackageStatus.Evaluating) => true,
                (BidPackageStatus.Closed, BidPackageStatus.Open) => true,

                // Từ Evaluating có thể sang Contracted (Hoàn tất đấu thầu & ký HĐ) hoặc quay lại Closed
                (BidPackageStatus.Evaluating, BidPackageStatus.Contracted) => true,
                (BidPackageStatus.Evaluating, BidPackageStatus.Closed) => true,

                _ => false
            };

            if (!isValidTransition)
            {
                return ApiResponse<BidPackageDto>.Fail($"Không thể chuyển trạng thái từ '{package.Status}' sang '{request.NewStatus}'. Quy trình hợp lệ: Mở thầu -> Đóng thầu -> Chấm điểm -> Ký hợp đồng.");
            }

            package.Status = request.NewStatus;
            package.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<BidPackage>().Update(package);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<BidPackageDto>.Ok(MapToBidPackageDto(package), $"Chuyển trạng thái gói thầu sang '{request.NewStatus}' thành công.");
        }

        public async Task<ApiResponse<List<BidDocumentDto>>> UploadDocumentsAsync(int bidPackageId, List<IFormFile> files, int userId, bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(bidPackageId);
            if (package == null)
            {
                return ApiResponse<List<BidDocumentDto>>.Fail("Không tìm thấy gói thầu.");
            }

            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<List<BidDocumentDto>>.Fail("Bạn không có quyền upload tài liệu cho gói thầu này.");
            }

            if (files == null || files.Count == 0)
            {
                return ApiResponse<List<BidDocumentDto>>.Fail("Vui lòng chọn ít nhất một file để upload.");
            }

            var uploadedDtos = new List<BidDocumentDto>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Lưu file vào thư mục uploads/bid-packages/{bidPackageId}/
                var relativePath = await _fileStorageService.SaveFileAsync(file, $"bid-packages/{bidPackageId}");

                var doc = new BidDocument
                {
                    BidPackageId = bidPackageId,
                    FileName = file.FileName,
                    FilePath = relativePath,
                    UploadedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<BidDocument>().AddAsync(doc);
                await _unitOfWork.SaveChangesAsync();

                uploadedDtos.Add(new BidDocumentDto
                {
                    Id = doc.Id,
                    BidPackageId = doc.BidPackageId,
                    FileName = doc.FileName,
                    FilePath = doc.FilePath,
                    UploadedAt = doc.UploadedAt
                });
            }

            return ApiResponse<List<BidDocumentDto>>.Ok(uploadedDtos, $"Đã upload thành công {uploadedDtos.Count} tài liệu mời thầu.");
        }

        public async Task<ApiResponse<bool>> DeleteDocumentAsync(int bidPackageId, int documentId, int userId, bool isAdmin)
        {
            var doc = await _unitOfWork.Repository<BidDocument>().GetByIdAsync(documentId);
            if (doc == null || doc.BidPackageId != bidPackageId)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy tài liệu cần xóa.");
            }

            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(bidPackageId);
            if (package == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy gói thầu liên quan.");
            }

            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<bool>.Fail("Bạn không có quyền xóa tài liệu của gói thầu này.");
            }

            if (package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<bool>.Fail("Gói thầu đã ký hợp đồng, không thể xóa tài liệu mời thầu.");
            }

            // Xóa file vật lý
            _fileStorageService.DeleteFile(doc.FilePath);

            // Xóa record trong DB
            _unitOfWork.Repository<BidDocument>().Delete(doc);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Xóa tài liệu mời thầu thành công.");
        }

        private static BidPackageDto MapToBidPackageDto(BidPackage package)
        {
            return new BidPackageDto
            {
                Id = package.Id,
                Code = package.Code,
                Name = package.Name,
                Type = package.Type,
                Budget = package.Budget,
                Deadline = package.Deadline,
                Status = package.Status,
                Description = package.Description,
                CreatedBy = package.CreatedBy,
                CreatedByName = package.Creator?.FullName,
                CreatedAt = package.CreatedAt,
                UpdatedAt = package.UpdatedAt,
                BidDocuments = package.BidDocuments?.Select(d => new BidDocumentDto
                {
                    Id = d.Id,
                    BidPackageId = d.BidPackageId,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    UploadedAt = d.UploadedAt
                }).ToList() ?? new List<BidDocumentDto>(),
                SubmissionsCount = package.BidSubmissions?.Count ?? 0
            };
        }
    }
}
