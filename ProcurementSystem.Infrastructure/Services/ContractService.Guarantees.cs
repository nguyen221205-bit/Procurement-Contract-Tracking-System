using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.DTOs.Contract;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Services.StateMachines;
using ProcurementSystem.Infrastructure.Utilities;

namespace ProcurementSystem.Infrastructure.Services
{
    public partial class ContractService : IContractGuaranteeService
    {
        #region Contract Guarantees (Bảo lãnh hợp đồng)

        public async Task<ApiResponse<List<ContractGuaranteeDto>>> GetContractGuaranteesAsync(
            int contractId, int userId, bool isInternalStaff, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Guarantees)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId);
            if (contract == null)
            {
                return ApiResponse<List<ContractGuaranteeDto>>.Fail("Không tìm thấy hợp đồng.");
            }

            if (isInternalStaff && !isAdmin)
            {
                var canAccess = contract.BidPackage?.CreatedBy == userId;
                if (!canAccess && contract.BidPackage?.ProcuringEntityId != null)
                {
                    var entityId = contract.BidPackage.ProcuringEntityId.Value;
                    canAccess = await _unitOfWork.Repository<ProcuringEntityMember>().Query().AsNoTracking()
                        .AnyAsync(m => m.ProcuringEntityId == entityId && m.UserId == userId && m.Status == MemberStatus.Active);
                }
                if (!canAccess)
                {
                    return ApiResponse<List<ContractGuaranteeDto>>.Forbidden("Bạn chỉ có quyền xem bảo lãnh của hợp đồng do chính mình phụ trách.");
                }
            }

            if (!isInternalStaff)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (contractor == null || contractor.Id != contract.ContractorId)
                {
                    return ApiResponse<List<ContractGuaranteeDto>>.Forbidden("Bạn không có quyền xem thông tin bảo lãnh của hợp đồng này.");
                }

            }

            var dtos = contract.Guarantees
                .OrderByDescending(g => g.CreatedAt)
                .Select(g => new ContractGuaranteeDto
                {
                    Id = g.Id,
                    ContractId = g.ContractId,
                    GuaranteeNumber = g.GuaranteeNumber,
                    BankName = g.BankName,
                    Amount = g.Amount,
                    Percentage = g.Percentage,
                    EffectiveDate = g.EffectiveDate,
                    ExpiryDate = g.ExpiryDate,
                    ScannedDocumentPath = g.ScannedDocumentPath,
                    Status = g.Status,
                    Notes = g.Notes,
                    CreatedAt = g.CreatedAt
                }).ToList();
            return ApiResponse<List<ContractGuaranteeDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<ContractGuaranteeDto>> AddContractGuaranteeAsync(
            int contractId, CreateContractGuaranteeRequest request, int userId, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .FirstOrDefaultAsync(c => c.Id == contractId);
            if (contract == null)
            {
                return ApiResponse<ContractGuaranteeDto>.Fail("Không tìm thấy hợp đồng.");
            }

            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
            {
                return ApiResponse<ContractGuaranteeDto>.Forbidden("Bạn chỉ có quyền thêm bảo lãnh cho gói thầu do chính mình phụ trách.");
            }

            if (contract.Status == ContractStatus.Terminated)
            {
                return ApiResponse<ContractGuaranteeDto>.Fail("Không thể thêm chứng thư bảo lãnh cho hợp đồng đã bị chấm dứt.");
            }

            if (request.ExpiryDate <= request.EffectiveDate)
            {
                return ApiResponse<ContractGuaranteeDto>.Fail("Ngày hết hạn bảo lãnh phải sau ngày có hiệu lực.");
            }

            var pct = contract.Value > 0 ? Math.Round(request.Amount / contract.Value * 100m, 2) : 0m;
            if (request.Amount <= 0 || decimal.Round(request.Amount, 2) != request.Amount || pct > 100m)
                return ApiResponse<ContractGuaranteeDto>.Fail("Số tiền bảo lãnh không hợp lệ hoặc vượt giá trị hợp đồng.");
            if (request.Percentage > 0 && request.Percentage != pct)
                return ApiResponse<ContractGuaranteeDto>.Fail("Tỷ lệ bảo lãnh không khớp số tiền và giá trị hợp đồng hiện tại. Vui lòng tải lại.");
            var guarantee = new ContractGuarantee
            {
                ContractId = contractId,
                GuaranteeNumber = request.GuaranteeNumber.Trim(),
                BankName = request.BankName.Trim(),
                Amount = request.Amount,
                Percentage = pct,
                EffectiveDate = request.EffectiveDate,
                ExpiryDate = request.ExpiryDate,
                Status = GuaranteeStatus.Valid,
                Notes = request.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<ContractGuarantee>().AddAsync(guarantee);
            await _unitOfWork.SaveChangesAsync();
            var dto = new ContractGuaranteeDto
            {
                Id = guarantee.Id,
                ContractId = guarantee.ContractId,
                GuaranteeNumber = guarantee.GuaranteeNumber,
                BankName = guarantee.BankName,
                Amount = guarantee.Amount,
                Percentage = guarantee.Percentage,
                EffectiveDate = guarantee.EffectiveDate,
                ExpiryDate = guarantee.ExpiryDate,
                ScannedDocumentPath = guarantee.ScannedDocumentPath,
                Status = guarantee.Status,
                Notes = guarantee.Notes,
                CreatedAt = guarantee.CreatedAt
            };
            return ApiResponse<ContractGuaranteeDto>.Ok(dto, "Lưu chứng thư bảo lãnh thực hiện hợp đồng thành công.");
        }

        public async Task<ApiResponse<ContractGuaranteeDto>> UpdateGuaranteeStatusAsync(
            int contractId, int guaranteeId, UpdateGuaranteeStatusRequest request, int userId, bool isAdmin)
        {
            var guarantee = await _unitOfWork.Repository<ContractGuarantee>()
                .Query()
                .Include(g => g.Contract)
                    .ThenInclude(c => c.BidPackage)
                .FirstOrDefaultAsync(g => g.Id == guaranteeId && g.ContractId == contractId);
            if (guarantee == null)
            {
                return ApiResponse<ContractGuaranteeDto>.Fail("Không tìm thấy chứng thư bảo lãnh.");
            }

            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, guarantee.Contract.BidPackage, userId, isAdmin))
            {
                return ApiResponse<ContractGuaranteeDto>.Forbidden("Bạn không có quyền cập nhật trạng thái bảo lãnh của hợp đồng này.");
            }

            if (!Enum.IsDefined(request.Status))
                return ApiResponse<ContractGuaranteeDto>.Fail("Trạng thái bảo lãnh không hợp lệ.");
            guarantee.Status = request.Status;
            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                guarantee.Notes = request.Notes.Trim();
            }

            guarantee.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<ContractGuarantee>().Update(guarantee);
            await _unitOfWork.SaveChangesAsync();
            var dto = new ContractGuaranteeDto
            {
                Id = guarantee.Id,
                ContractId = guarantee.ContractId,
                GuaranteeNumber = guarantee.GuaranteeNumber,
                BankName = guarantee.BankName,
                Amount = guarantee.Amount,
                Percentage = guarantee.Percentage,
                EffectiveDate = guarantee.EffectiveDate,
                ExpiryDate = guarantee.ExpiryDate,
                ScannedDocumentPath = guarantee.ScannedDocumentPath,
                Status = guarantee.Status,
                Notes = guarantee.Notes,
                CreatedAt = guarantee.CreatedAt
            };
            return ApiResponse<ContractGuaranteeDto>.Ok(dto, "Cập nhật trạng thái chứng thư bảo lãnh thành công.");
        }

        public async Task<ApiResponse<string>> UploadGuaranteeDocumentAsync(
            int contractId, int guaranteeId, IFormFile file, int userId, bool isAdmin)
        {
            var guarantee = await _unitOfWork.Repository<ContractGuarantee>()
                .Query()
                .Include(g => g.Contract)
                    .ThenInclude(c => c.BidPackage)
                .FirstOrDefaultAsync(g => g.Id == guaranteeId && g.ContractId == contractId);
            if (guarantee == null)
            {
                return ApiResponse<string>.Fail("Không tìm thấy chứng thư bảo lãnh.");
            }

            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, guarantee.Contract.BidPackage, userId, isAdmin))
            {
                return ApiResponse<string>.Forbidden("Bạn không có quyền tải lên tệp scan cho bảo lãnh này.");
            }

            var allowedExts = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExts.Contains(ext))
            {
                return ApiResponse<string>.Fail("Tệp đính kèm phải là định dạng PDF hoặc hình ảnh (PNG, JPG).");
            }

            if (file.Length > 15 * 1024 * 1024)
            {
                return ApiResponse<string>.Fail("Dung lượng tệp không được vượt quá 15MB.");
            }

            var oldFilePath = guarantee.ScannedDocumentPath;
            var savedPath = await _fileStorageService.SaveFileAsync(file, $"contracts/{contractId}/guarantees");
            try
            {
                guarantee.ScannedDocumentPath = savedPath;
                guarantee.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<ContractGuarantee>().Update(guarantee);
                await _unitOfWork.SaveChangesAsync();
                // F5-6 & Rule N17: Dọn dẹp tệp scan cũ trên đĩa sau khi cập nhật thành công
                if (!string.IsNullOrEmpty(oldFilePath) && !string.Equals(oldFilePath, savedPath, StringComparison.OrdinalIgnoreCase))
                {
                    _fileStorageService.DeleteFile(oldFilePath);
                }

                return ApiResponse<string>.Ok(savedPath, "Tải lên bản scan chứng thư bảo lãnh thành công.");
            }

            catch
            {
                // F5-6 & Rule N17: Dọn dẹp tệp scan mới tải lên nếu thao tác DB thất bại
                _fileStorageService.DeleteFile(savedPath);
                throw;
            }

        }

        public async Task<ProcurementSystem.Core.DTOs.Common.FileDownloadResult?> GetGuaranteeDocumentForDownloadAsync(
            int contractId, int guaranteeId, int userId, bool isInternalStaff, bool isAdmin)
        {
            var guarantee = await _unitOfWork.Repository<ContractGuarantee>()
                .Query()
                .Include(g => g.Contract)
                    .ThenInclude(c => c.BidPackage)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == guaranteeId && g.ContractId == contractId);
            if (guarantee == null || string.IsNullOrWhiteSpace(guarantee.ScannedDocumentPath))
            {
                return null;
            }

            if (isInternalStaff && !isAdmin &&
                !await PackageWriteAuthorization.CanReadEntityStaffAsync(_unitOfWork, guarantee.Contract.BidPackage, userId, isAdmin))
            {
                return null;
            }

            if (!isInternalStaff)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (contractor == null || contractor.Id != guarantee.Contract.ContractorId)
                {
                    return null;
                }

            }

            var originalFileName = Path.GetFileName(guarantee.ScannedDocumentPath);
            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            var contentType = ContentTypeResolver.ResolveByFileName(originalFileName);
            var fileName = $"BaoLanh_{guarantee.GuaranteeNumber}{ext}";
            try
            {
                return await _fileStorageService.ResolveDownloadAsync(guarantee.ScannedDocumentPath, fileName, contentType);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
