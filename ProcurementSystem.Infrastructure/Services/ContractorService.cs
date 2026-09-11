using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Contractor;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class ContractorService : IContractorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public ContractorService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<PaginatedList<ContractorDto>>> GetContractorsAsync(ContractorFilterParams filter)
        {
            var query = _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .AsNoTracking();

            // 1. Tìm kiếm theo Tên công ty hoặc Mã số thuế
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(c => c.CompanyName.ToLower().Contains(term)
                    || (c.TaxCode != null && c.TaxCode.ToLower().Contains(term)));
            }

            // 2. Lọc theo điểm đánh giá Rating
            if (filter.MinRating.HasValue)
            {
                query = query.Where(c => c.Rating >= filter.MinRating.Value);
            }

            if (filter.MaxRating.HasValue)
            {
                query = query.Where(c => c.Rating <= filter.MaxRating.Value);
            }

            // 3. Sắp xếp
            query = (filter.SortBy?.ToLower()) switch
            {
                "name" or "companyname" => filter.SortDescending
                    ? query.OrderByDescending(c => c.CompanyName)
                    : query.OrderBy(c => c.CompanyName),
                "createdat" or "date" => filter.SortDescending
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt),
                _ => filter.SortDescending
                    ? query.OrderByDescending(c => c.Rating)
                    : query.OrderBy(c => c.Rating)
            };

            // 4. Ánh xạ sang DTO và phân trang
            var dtoQuery = query.Select(c => new ContractorDto
            {
                Id = c.Id,
                UserId = c.UserId,
                CompanyName = c.CompanyName,
                TaxCode = c.TaxCode,
                Address = c.Address,
                BusinessLicenseFile = c.BusinessLicenseFile,
                Rating = c.Rating,
                CreatedAt = c.CreatedAt,
                FullName = c.User.FullName,
                Email = c.User.Email,
                Phone = c.User.Phone
            });

            var paginatedResult = await PaginatedList<ContractorDto>.CreateAsync(
                dtoQuery, filter.PageIndex, filter.PageSize);

            return ApiResponse<PaginatedList<ContractorDto>>.Ok(paginatedResult);
        }

        public async Task<ApiResponse<ContractorDto>> GetContractorByIdAsync(int id)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy thông tin nhà thầu.");
            }

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor));
        }

        public async Task<ApiResponse<ContractorDto>> GetCurrentContractorProfileAsync(int userId)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy hồ sơ nhà thầu liên kết với tài khoản này.");
            }

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor));
        }

        public async Task<ApiResponse<ContractorDto>> UpdateCurrentContractorProfileAsync(int userId, UpdateContractorProfileRequest request)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy hồ sơ nhà thầu để cập nhật.");
            }

            contractor.CompanyName = request.CompanyName.Trim();
            contractor.Address = request.Address?.Trim();

            // Nếu người dùng tải lên file GPKD mới để thay thế
            if (request.BusinessLicenseFile != null && request.BusinessLicenseFile.Length > 0)
            {
                try
                {
                    var oldFilePath = contractor.BusinessLicenseFile;
                    var newFilePath = await _fileStorageService.SaveFileAsync(request.BusinessLicenseFile, "contractors");
                    contractor.BusinessLicenseFile = newFilePath;

                    // Xóa file cũ nếu lưu file mới thành công
                    if (!string.IsNullOrEmpty(oldFilePath))
                    {
                        _fileStorageService.DeleteFile(oldFilePath);
                    }
                }
                catch (Exception ex)
                {
                    return ApiResponse<ContractorDto>.Fail($"Lỗi khi lưu trữ file Giấy phép kinh doanh: {ex.Message}");
                }
            }

            _unitOfWork.Repository<Contractor>().Update(contractor);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor), "Cập nhật hồ sơ nhà thầu thành công.");
        }

        public async Task<ApiResponse<ContractorDto>> UpdateContractorRatingAsync(int contractorId, UpdateContractorRatingRequest request)
        {
            if (request.Rating < 0 || request.Rating > 5)
            {
                return ApiResponse<ContractorDto>.Fail("Điểm đánh giá phải nằm trong khoảng từ 0.0 đến 5.0 sao.");
            }

            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == contractorId);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy nhà thầu.");
            }

            contractor.Rating = Math.Round(request.Rating, 2);

            _unitOfWork.Repository<Contractor>().Update(contractor);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor), "Cập nhật điểm đánh giá uy tín nhà thầu thành công.");
        }

        private static ContractorDto MapToDto(Contractor contractor)
        {
            return new ContractorDto
            {
                Id = contractor.Id,
                UserId = contractor.UserId,
                CompanyName = contractor.CompanyName,
                TaxCode = contractor.TaxCode,
                Address = contractor.Address,
                BusinessLicenseFile = contractor.BusinessLicenseFile,
                Rating = contractor.Rating,
                CreatedAt = contractor.CreatedAt,
                FullName = contractor.User?.FullName ?? string.Empty,
                Email = contractor.User?.Email ?? string.Empty,
                Phone = contractor.User?.Phone
            };
        }
    }
}
