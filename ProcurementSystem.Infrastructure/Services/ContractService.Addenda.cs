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
    public partial class ContractService : IContractAddendumService
    {
        #region Contract Addenda (Phụ lục hợp đồng)

        public async Task<ApiResponse<List<ContractAddendumDto>>> GetContractAddendaAsync(
            int contractId, int userId, bool isInternalStaff, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Addenda)
                    .ThenInclude(a => a.Creator)
                .Include(c => c.Addenda)
                    .ThenInclude(a => a.Approver)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId);
            if (contract == null)
            {
                return ApiResponse<List<ContractAddendumDto>>.Fail("Không tìm thấy hợp đồng.");
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
                    return ApiResponse<List<ContractAddendumDto>>.Forbidden("Bạn chỉ có quyền xem phụ lục của hợp đồng do chính mình phụ trách.");
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
                    return ApiResponse<List<ContractAddendumDto>>.Forbidden("Bạn không có quyền xem danh sách phụ lục của hợp đồng này.");
                }

            }

            var dtos = contract.Addenda
                .OrderByDescending(a => a.SignedDate)
                .Select(a => new ContractAddendumDto
                {
                    Id = a.Id,
                    ContractId = a.ContractId,
                    AddendumNumber = a.AddendumNumber,
                    Title = a.Title,
                    Type = a.Type,
                    AdditionalValue = a.AdditionalValue,
                    AdjustedEndDate = a.AdjustedEndDate,
                    ExtensionDays = a.ExtensionDays,
                    Reason = a.Reason,
                    SignedDate = a.SignedDate,
                    ScannedDocumentPath = a.ScannedDocumentPath,
                    Status = a.Status,
                    CreatedBy = a.CreatedBy,
                    CreatedByName = a.Creator?.FullName,
                    CreatedAt = a.CreatedAt,
                    ApprovedBy = a.ApprovedBy,
                    ApprovedByName = a.Approver?.FullName,
                    ApprovedAt = a.ApprovedAt
                }).ToList();
            return ApiResponse<List<ContractAddendumDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<ContractAddendumDto>> CreateContractAddendumAsync(
            int contractId, CreateContractAddendumRequest request, int userId, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .FirstOrDefaultAsync(c => c.Id == contractId);
            if (contract == null)
            {
                return ApiResponse<ContractAddendumDto>.Fail("Không tìm thấy hợp đồng.");
            }

            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
            {
                return ApiResponse<ContractAddendumDto>.Forbidden("Bạn chỉ có quyền tạo phụ lục cho hợp đồng do chính mình phụ trách.");
            }

            if (contract.Status == ContractStatus.Draft)
            {
                return ApiResponse<ContractAddendumDto>.Fail("Hợp đồng đang ở trạng thái Nháp (Draft). Vui lòng chuyển sang có hiệu lực (Active) trước khi lập phụ lục.");
            }

            if (contract.Status == ContractStatus.Terminated)
            {
                return ApiResponse<ContractAddendumDto>.Fail("Không thể lập phụ lục cho hợp đồng đã bị chấm dứt.");
            }

            var exists = await _unitOfWork.Repository<ContractAddendum>()
                .Query()
                .AnyAsync(a => a.ContractId == contractId && a.AddendumNumber == request.AddendumNumber.Trim());
            if (exists)
            {
                return ApiResponse<ContractAddendumDto>.Fail($"Số phụ lục '{request.AddendumNumber}' đã tồn tại trong hợp đồng này.");
            }

            // F5-5: Kiểm tra tính toàn vẹn tài chính (Financial Integrity)
            var newContractValue = contract.Value + request.AdditionalValue;
            if (newContractValue <= 0)
            {
                return ApiResponse<ContractAddendumDto>.Fail("Giá trị hợp đồng sau điều chỉnh phải lớn hơn 0.");
            }

            var totalMilestonesAmount = await _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .Where(m => m.ContractId == contractId)
                .SumAsync(m => (decimal?)m.Amount) ?? 0;
            if (newContractValue < totalMilestonesAmount)
            {
                return ApiResponse<ContractAddendumDto>.Fail(
                    $"Giá trị hợp đồng sau điều chỉnh ({newContractValue:N0} VNĐ) không được nhỏ hơn tổng giá trị các mốc thanh toán hiện có ({totalMilestonesAmount:N0} VNĐ). Vui lòng điều chỉnh lại kế hoạch mốc thanh toán trước.");
            }

            // F5-5: Quy trình Maker-Checker:
            // - Nếu là Admin: Phê duyệt và cộng tiền ngay
            // - Nếu là Procurement: Tạo ở trạng thái Pending chờ Admin phê duyệt, CHƯA cộng tiền vào hợp đồng
            bool autoApprove = isAdmin;
            var addendum = new ContractAddendum
            {
                ContractId = contractId,
                AddendumNumber = request.AddendumNumber.Trim(),
                Title = request.Title.Trim(),
                Type = request.Type,
                AdditionalValue = request.AdditionalValue,
                AdjustedEndDate = request.AdjustedEndDate,
                ExtensionDays = request.ExtensionDays,
                Reason = request.Reason?.Trim(),
                SignedDate = request.SignedDate,
                Status = autoApprove ? AddendumStatus.Approved : AddendumStatus.Pending,
                CreatedBy = userId,
                ApprovedBy = autoApprove ? userId : null,
                ApprovedAt = autoApprove ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };
            if (autoApprove)
            {
                if (request.AdditionalValue != 0)
                {
                    contract.Value = newContractValue;
                }

                if (request.AdjustedEndDate.HasValue && request.AdjustedEndDate.Value > contract.EndDate)
                {
                    contract.EndDate = request.AdjustedEndDate.Value;
                }

                contract.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Contract>().Update(contract);
            }

            await _unitOfWork.Repository<ContractAddendum>().AddAsync(addendum);
            await _unitOfWork.SaveChangesAsync();
            // Nếu tạo ở trạng thái Pending, thông báo cho Admin
            if (!autoApprove)
            {
                try
                {
                    var adminUserIds = await _unitOfWork.Repository<UserRole>()
                        .Query()
                        .Include(ur => ur.Role)
                        .Where(ur => ur.Role.Name == "Admin")
                        .Select(ur => ur.UserId)
                        .ToListAsync();
                    if (adminUserIds.Any())
                    {
                        await _notificationService.CreateBulkNotificationsAsync(
                            adminUserIds,
                            "Phụ lục hợp đồng mới chờ phê duyệt",
                            $"Bên mời thầu vừa đề xuất phụ lục #{addendum.AddendumNumber} cho hợp đồng #{contract.ContractNumber} ({addendum.Title}). Vui lòng thẩm định phê duyệt.",
                            NotificationType.Info,
                            sendEmail: false
                        );
                    }

                }

                catch
                {
                }

            }

            var user = await _unitOfWork.Repository<User>().Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var dto = new ContractAddendumDto
            {
                Id = addendum.Id,
                ContractId = addendum.ContractId,
                AddendumNumber = addendum.AddendumNumber,
                Title = addendum.Title,
                Type = addendum.Type,
                AdditionalValue = addendum.AdditionalValue,
                AdjustedEndDate = addendum.AdjustedEndDate,
                ExtensionDays = addendum.ExtensionDays,
                Reason = addendum.Reason,
                SignedDate = addendum.SignedDate,
                ScannedDocumentPath = addendum.ScannedDocumentPath,
                Status = addendum.Status,
                CreatedBy = addendum.CreatedBy,
                CreatedByName = user?.FullName,
                CreatedAt = addendum.CreatedAt,
                ApprovedBy = addendum.ApprovedBy,
                ApprovedByName = user?.FullName,
                ApprovedAt = addendum.ApprovedAt
            };
            return ApiResponse<ContractAddendumDto>.Ok(dto, $"Ban hành phụ lục hợp đồng #{addendum.AddendumNumber} thành công.");
        }

        public async Task<ApiResponse<ContractAddendumDto>> ApproveContractAddendumAsync(
            int contractId, int addendumId, ApproveContractAddendumRequest request, int userId, bool isAdmin)
        {
            var addendum = await _unitOfWork.Repository<ContractAddendum>()
                .Query()
                .Include(a => a.Contract)
                    .ThenInclude(c => c.BidPackage)
                .FirstOrDefaultAsync(a => a.Id == addendumId && a.ContractId == contractId);
            if (addendum == null)
            {
                return ApiResponse<ContractAddendumDto>.Fail("Không tìm thấy phụ lục hợp đồng.");
            }

            // F5-5: Chống Double Counting - Bắt buộc phụ lục phải ở trạng thái Pending hoặc Draft
            if (addendum.Status != AddendumStatus.Pending && addendum.Status != AddendumStatus.Draft)
            {
                return ApiResponse<ContractAddendumDto>.Fail("Phụ lục hợp đồng này đã được xử lý trước đó (không ở trạng thái chờ duyệt).");
            }

            // F5-5: Maker-Checker: Phụ lục do Procurement lập phải do Admin phê duyệt
            if (!isAdmin)
            {
                return ApiResponse<ContractAddendumDto>.Forbidden("Chỉ Quản trị viên (Admin) mới có thẩm quyền phê duyệt phụ lục hợp đồng.");
            }

            if (request.IsApproved)
            {
                var contract = addendum.Contract;
                // F5-5: Kiểm tra toàn vẹn tài chính trước khi duyệt
                var newContractValue = contract.Value + addendum.AdditionalValue;
                if (newContractValue <= 0)
                {
                    return ApiResponse<ContractAddendumDto>.Fail("Giá trị hợp đồng sau khi áp dụng phụ lục phải lớn hơn 0.");
                }

                var totalMilestonesAmount = await _unitOfWork.Repository<ContractMilestone>()
                    .Query()
                    .Where(m => m.ContractId == contractId)
                    .SumAsync(m => (decimal?)m.Amount) ?? 0;
                if (newContractValue < totalMilestonesAmount)
                {
                    return ApiResponse<ContractAddendumDto>.Fail(
                        $"Giá trị hợp đồng sau điều chỉnh ({newContractValue:N0} VNĐ) không được nhỏ hơn tổng giá trị các mốc thanh toán hiện có ({totalMilestonesAmount:N0} VNĐ).");
                }

                if (addendum.AdditionalValue != 0)
                {
                    contract.Value = newContractValue;
                }

                if (addendum.AdjustedEndDate.HasValue && addendum.AdjustedEndDate.Value > contract.EndDate)
                {
                    contract.EndDate = addendum.AdjustedEndDate.Value;
                }

                contract.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Contract>().Update(contract);
            }

            addendum.Status = request.IsApproved ? AddendumStatus.Approved : AddendumStatus.Rejected;
            addendum.ApprovedBy = userId;
            addendum.ApprovedAt = DateTime.UtcNow;
            _unitOfWork.Repository<ContractAddendum>().Update(addendum);
            await _unitOfWork.SaveChangesAsync();
            var user = await _unitOfWork.Repository<User>().Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var dto = new ContractAddendumDto
            {
                Id = addendum.Id,
                ContractId = addendum.ContractId,
                AddendumNumber = addendum.AddendumNumber,
                Title = addendum.Title,
                Type = addendum.Type,
                AdditionalValue = addendum.AdditionalValue,
                AdjustedEndDate = addendum.AdjustedEndDate,
                ExtensionDays = addendum.ExtensionDays,
                Reason = addendum.Reason,
                SignedDate = addendum.SignedDate,
                ScannedDocumentPath = addendum.ScannedDocumentPath,
                Status = addendum.Status,
                CreatedBy = addendum.CreatedBy,
                CreatedByName = addendum.Creator?.FullName,
                CreatedAt = addendum.CreatedAt,
                ApprovedBy = addendum.ApprovedBy,
                ApprovedByName = user?.FullName,
                ApprovedAt = addendum.ApprovedAt
            };
            var msg = request.IsApproved ? "Phê duyệt phụ lục thành công." : "Từ chối phụ lục thành công.";
            return ApiResponse<ContractAddendumDto>.Ok(dto, msg);
        }

        public async Task<ApiResponse<string>> UploadAddendumDocumentAsync(
            int contractId, int addendumId, IFormFile file, int userId, bool isAdmin)
        {
            var addendum = await _unitOfWork.Repository<ContractAddendum>()
                .Query()
                .Include(a => a.Contract)
                    .ThenInclude(c => c.BidPackage)
                .FirstOrDefaultAsync(a => a.Id == addendumId && a.ContractId == contractId);
            if (addendum == null)
            {
                return ApiResponse<string>.Fail("Không tìm thấy phụ lục hợp đồng.");
            }

            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, addendum.Contract.BidPackage, userId, isAdmin))
            {
                return ApiResponse<string>.Forbidden("Bạn không có quyền tải lên tệp scan cho phụ lục này.");
            }

            var allowedExts = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExts.Contains(ext))
            {
                return ApiResponse<string>.Fail("Tệp đính kèm phải là định dạng PDF hoặc hình ảnh (PNG, JPG).");
            }

            if (file.Length > 20 * 1024 * 1024)
            {
                return ApiResponse<string>.Fail("Dung lượng tệp không được vượt quá 20MB.");
            }

            var oldFilePath = addendum.ScannedDocumentPath;
            var savedPath = await _fileStorageService.SaveFileAsync(file, $"contracts/{contractId}/addenda");
            try
            {
                addendum.ScannedDocumentPath = savedPath;
                _unitOfWork.Repository<ContractAddendum>().Update(addendum);
                await _unitOfWork.SaveChangesAsync();
                // F5-6 & Rule N17: Dọn dẹp tệp scan cũ trên đĩa sau khi cập nhật thành công
                if (!string.IsNullOrEmpty(oldFilePath) && !string.Equals(oldFilePath, savedPath, StringComparison.OrdinalIgnoreCase))
                {
                    _fileStorageService.DeleteFile(oldFilePath);
                }

                return ApiResponse<string>.Ok(savedPath, "Tải lên bản scan phụ lục hợp đồng thành công.");
            }

            catch
            {
                // F5-6 & Rule N17: Dọn dẹp tệp scan mới nếu lưu DB thất bại
                _fileStorageService.DeleteFile(savedPath);
                throw;
            }

        }

        public async Task<ProcurementSystem.Core.DTOs.Common.FileDownloadResult?> GetAddendumDocumentForDownloadAsync(
            int contractId, int addendumId, int userId, bool isInternalStaff, bool isAdmin)
        {
            var addendum = await _unitOfWork.Repository<ContractAddendum>()
                .Query()
                .Include(a => a.Contract)
                    .ThenInclude(c => c.BidPackage)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == addendumId && a.ContractId == contractId);
            if (addendum == null || string.IsNullOrWhiteSpace(addendum.ScannedDocumentPath))
            {
                return null;
            }

            if (isInternalStaff && !isAdmin &&
                !await PackageWriteAuthorization.CanReadEntityStaffAsync(_unitOfWork, addendum.Contract.BidPackage, userId, isAdmin))
            {
                return null;
            }

            if (!isInternalStaff)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (contractor == null || contractor.Id != addendum.Contract.ContractorId)
                {
                    return null;
                }

            }

            var originalFileName = Path.GetFileName(addendum.ScannedDocumentPath);
            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            var contentType = ContentTypeResolver.ResolveByFileName(originalFileName);
            var fileName = $"PhuLuc_{addendum.AddendumNumber}{ext}";
            try
            {
                return await _fileStorageService.ResolveDownloadAsync(addendum.ScannedDocumentPath, fileName, contentType);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
