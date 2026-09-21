using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Contract;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public ContractService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<PaginatedList<ContractSummaryDto>>> GetContractsAsync(
            ContractFilterParams filter, int userId, bool isInternalStaff)
        {
            var query = _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Include(c => c.Milestones)
                .AsNoTracking();

            // Contractor chỉ xem hợp đồng của mình
            if (!isInternalStaff)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (contractor == null)
                {
                    return ApiResponse<PaginatedList<ContractSummaryDto>>.Fail(
                        "Không tìm thấy hồ sơ nhà thầu liên kết với tài khoản này.");
                }

                query = query.Where(c => c.ContractorId == contractor.Id);
            }

            // Lọc theo số hợp đồng
            if (!string.IsNullOrWhiteSpace(filter.ContractNumber))
            {
                query = query.Where(c => c.ContractNumber.Contains(filter.ContractNumber));
            }

            // Lọc theo trạng thái
            if (filter.Status.HasValue)
            {
                query = query.Where(c => c.Status == filter.Status.Value);
            }

            // Lọc theo thời gian
            if (filter.StartDateFrom.HasValue)
            {
                query = query.Where(c => c.StartDate >= filter.StartDateFrom.Value);
            }

            if (filter.EndDateTo.HasValue)
            {
                query = query.Where(c => c.EndDate <= filter.EndDateTo.Value);
            }

            var projectedQuery = query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ContractSummaryDto
                {
                    Id = c.Id,
                    BidPackageId = c.BidPackageId,
                    BidPackageCode = c.BidPackage.Code,
                    ContractorId = c.ContractorId,
                    CompanyName = c.Contractor.CompanyName,
                    ContractNumber = c.ContractNumber,
                    Value = c.Value,
                    Status = c.Status,
                    StatusName = c.Status.ToString(),
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    MilestoneCount = c.Milestones.Count
                });

            var paginatedResult = await PaginatedList<ContractSummaryDto>.CreateAsync(
                projectedQuery, filter.PageNumber, filter.PageSize);

            return ApiResponse<PaginatedList<ContractSummaryDto>>.Ok(paginatedResult);
        }

        public async Task<ApiResponse<ContractDto>> GetContractByIdAsync(
            int id, int userId, bool isInternalStaff)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Include(c => c.Milestones)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return ApiResponse<ContractDto>.Fail("Không tìm thấy hợp đồng.");
            }

            // Contractor chỉ xem hợp đồng của mình
            if (!isInternalStaff)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (contractor == null || contractor.Id != contract.ContractorId)
                {
                    return ApiResponse<ContractDto>.Fail("Bạn không có quyền xem hợp đồng này.");
                }
            }

            return ApiResponse<ContractDto>.Ok(MapToDto(contract));
        }

        public async Task<ApiResponse<AwardedBidInfoDto>> GetAwardedBidForContractAsync(int packageId)
        {
            // Tìm gói thầu
            var bidPackage = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == packageId);

            if (bidPackage == null)
            {
                return ApiResponse<AwardedBidInfoDto>.Fail("Không tìm thấy gói thầu.");
            }

            // Tìm hồ sơ trúng thầu (Selected)
            var awardedSubmission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.Contractor)
                    .ThenInclude(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.BidPackageId == packageId && s.Status == "Selected");

            if (awardedSubmission == null)
            {
                return ApiResponse<AwardedBidInfoDto>.Fail(
                    "Gói thầu chưa hoàn tất phê duyệt kết quả trúng thầu. " +
                    "Vui lòng thực hiện 'Phê duyệt trúng thầu' (Finalize Evaluation) trước khi tạo hợp đồng.");
            }

            // Kiểm tra đã có hợp đồng chưa
            var existingContract = await _unitOfWork.Repository<Contract>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.BidPackageId == packageId);

            // Đếm tổng số hợp đồng để sinh số thứ tự tự động
            var contractCount = await _unitOfWork.Repository<Contract>()
                .Query()
                .CountAsync();
            var suggestedNumber = $"HD-{DateTime.UtcNow.Year}-{bidPackage.Code}-{contractCount + 1:D3}";

            var contractor = awardedSubmission.Contractor;

            var dto = new AwardedBidInfoDto
            {
                BidPackageId    = bidPackage.Id,
                PackageCode     = bidPackage.Code,
                PackageName     = bidPackage.Name,
                EstimatedBudget = bidPackage.Budget,

                ContractorId    = contractor.Id,
                CompanyName     = contractor.CompanyName,
                TaxCode         = contractor.TaxCode,
                Email           = contractor.User?.Email,
                Phone           = contractor.User?.Phone,
                Address         = contractor.Address,

                SubmissionId            = awardedSubmission.Id,
                TotalScore              = awardedSubmission.TotalScore,
                Rank                    = awardedSubmission.Rank,
                SuggestedContractValue  = bidPackage.Budget,

                IsAwarded              = true,
                HasContract            = existingContract != null,
                ExistingContractId     = existingContract?.Id,
                SuggestedContractNumber = suggestedNumber
            };

            return ApiResponse<AwardedBidInfoDto>.Ok(dto,
                existingContract != null
                    ? $"Cảnh báo: Gói thầu này đã có hợp đồng #{existingContract.Id}. Xem chi tiết trước khi tạo mới."
                    : "Thông tin trúng thầu sẵn sàng để lập hợp đồng.");
        }

        public async Task<ApiResponse<ContractDto>> CreateContractFromAwardedBidAsync(
            CreateContractRequest request, int userId)
        {
            // 1. Kiểm tra gói thầu tồn tại
            var bidPackage = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == request.BidPackageId);

            if (bidPackage == null)
            {
                return ApiResponse<ContractDto>.Fail("Gói thầu không tồn tại.");
            }

            // 2. Kiểm tra nhà thầu được duyệt trúng thầu (Selected)
            var awardedSubmission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(bs =>
                    bs.BidPackageId == request.BidPackageId &&
                    bs.ContractorId == request.ContractorId &&
                    bs.Status == "Selected");

            if (awardedSubmission == null)
            {
                return ApiResponse<ContractDto>.Fail(
                    "Nhà thầu này chưa được phê duyệt trúng thầu (Selected) cho gói thầu tương ứng. " +
                    "Chỉ tạo được hợp đồng cho nhà thầu đã được duyệt.");
            }

            // 3. Kiểm tra gói thầu chưa có hợp đồng (quan hệ 1-1)
            var existingContract = await _unitOfWork.Repository<Contract>()
                .ExistsAsync(c => c.BidPackageId == request.BidPackageId);

            if (existingContract)
            {
                return ApiResponse<ContractDto>.Fail(
                    "Gói thầu này đã có hợp đồng. Mỗi gói thầu chỉ được phép ký tối đa 1 hợp đồng chính thức.");
            }

            // 4. Kiểm tra thời hạn hợp lý
            if (request.EndDate <= request.StartDate)
            {
                return ApiResponse<ContractDto>.Fail("Ngày kết thúc phải sau ngày bắt đầu.");
            }

            // 5. Kiểm soát dự toán ngân sách
            if (request.Value > bidPackage.Budget)
            {
                return ApiResponse<ContractDto>.Fail(
                    $"Giá trị hợp đồng ({request.Value:N0} VNĐ) không được vượt quá ngân sách dự toán của gói thầu ({bidPackage.Budget:N0} VNĐ).");
            }

            // 6. Tự động sinh số hợp đồng hoặc kiểm tra trùng lặp
            if (string.IsNullOrWhiteSpace(request.ContractNumber))
            {
                var seq = await _unitOfWork.Repository<Contract>().Query().CountAsync() + 1;
                string candidateNumber;
                do
                {
                    candidateNumber = $"HD-{DateTime.UtcNow.Year}-{bidPackage.Code}-{seq:D3}";
                    seq++;
                } while (await _unitOfWork.Repository<Contract>().ExistsAsync(c => c.ContractNumber == candidateNumber));
                
                request.ContractNumber = candidateNumber;
            }
            else
            {
                request.ContractNumber = request.ContractNumber.Trim();
                var isDuplicateNumber = await _unitOfWork.Repository<Contract>()
                    .ExistsAsync(c => c.ContractNumber == request.ContractNumber);

                if (isDuplicateNumber)
                {
                    return ApiResponse<ContractDto>.Fail(
                        $"Số hợp đồng '{request.ContractNumber}' đã tồn tại trong hệ thống. Vui lòng chọn số khác.");
                }
            }

            // 7. Tạo hợp đồng
            var contract = new Contract
            {
                BidPackageId   = request.BidPackageId,
                ContractorId   = request.ContractorId,
                ContractNumber = request.ContractNumber,
                Value          = request.Value,
                Terms          = request.Terms,
                StartDate      = request.StartDate,
                EndDate        = request.EndDate,
                Status         = ContractStatus.Draft,
                CreatedAt      = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Contract>().AddAsync(contract);
            await _unitOfWork.SaveChangesAsync();

            // Reload với navigation properties để trả về
            var created = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Include(c => c.Milestones)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contract.Id);

            return ApiResponse<ContractDto>.Ok(MapToDto(created!), $"Tạo hợp đồng #{request.ContractNumber} thành công.");
        }

        public async Task<ApiResponse<ContractDto>> UpdateContractAsync(
            int id, UpdateContractRequest request, int userId)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Include(c => c.Milestones)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return ApiResponse<ContractDto>.Fail("Không tìm thấy hợp đồng.");
            }

            // Chỉ cho phép sửa khi hợp đồng đang ở trạng thái Draft
            if (contract.Status != ContractStatus.Draft)
            {
                return ApiResponse<ContractDto>.Fail(
                    "Chỉ được phép chỉnh sửa hợp đồng khi đang ở trạng thái Nháp (Draft).");
            }

            // Kiểm tra logic thời gian
            var newStart = request.StartDate ?? contract.StartDate;
            var newEnd = request.EndDate ?? contract.EndDate;
            if (newEnd <= newStart)
            {
                return ApiResponse<ContractDto>.Fail("Ngày kết thúc phải sau ngày bắt đầu.");
            }

            // Nếu giảm giá trị hợp đồng, kiểm tra tổng mốc thanh toán không vượt quá
            if (request.Value.HasValue)
            {
                var totalMilestones = contract.Milestones.Sum(m => m.Amount);
                if (totalMilestones > request.Value.Value)
                {
                    return ApiResponse<ContractDto>.Fail(
                        $"Không thể giảm giá trị hợp đồng xuống {request.Value.Value:N0} VNĐ vì " +
                        $"tổng các mốc thanh toán hiện tại là {totalMilestones:N0} VNĐ.");
                }
                contract.Value = request.Value.Value;
            }

            if (request.Terms != null) contract.Terms = request.Terms;
            if (request.StartDate.HasValue) contract.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue) contract.EndDate = request.EndDate.Value;
            contract.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Contract>().Update(contract);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ContractDto>.Ok(MapToDto(contract), "Cập nhật hợp đồng thành công.");
        }

        public async Task<ApiResponse<ContractDto>> ChangeStatusAsync(
            int id, ChangeContractStatusRequest request, int userId)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Include(c => c.Milestones)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return ApiResponse<ContractDto>.Fail("Không tìm thấy hợp đồng.");
            }

            // Kiểm tra luồng trạng thái hợp lệ
            var validTransitions = new Dictionary<ContractStatus, List<ContractStatus>>
            {
                { ContractStatus.Draft, new List<ContractStatus> { ContractStatus.Active } },
                { ContractStatus.Active, new List<ContractStatus> { ContractStatus.Completed, ContractStatus.Terminated } },
                { ContractStatus.Completed, new List<ContractStatus>() },
                { ContractStatus.Terminated, new List<ContractStatus>() }
            };

            if (!validTransitions[contract.Status].Contains(request.NewStatus))
            {
                return ApiResponse<ContractDto>.Fail(
                    $"Không thể chuyển hợp đồng từ trạng thái '{contract.Status}' sang '{request.NewStatus}'.");
            }

            var oldStatus = contract.Status;
            contract.Status = request.NewStatus;
            contract.UpdatedAt = DateTime.UtcNow;

            // Nếu kích hoạt hợp đồng (Draft -> Active), tự động cập nhật gói thầu sang Contracted
            if (request.NewStatus == ContractStatus.Active)
            {
                var bidPackage = await _unitOfWork.Repository<BidPackage>()
                    .Query()
                    .FirstOrDefaultAsync(bp => bp.Id == contract.BidPackageId);

                if (bidPackage != null && bidPackage.Status != BidPackageStatus.Contracted)
                {
                    bidPackage.Status = BidPackageStatus.Contracted;
                    _unitOfWork.Repository<BidPackage>().Update(bidPackage);
                }
            }

            _unitOfWork.Repository<Contract>().Update(contract);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ContractDto>.Ok(
                MapToDto(contract),
                $"Chuyển trạng thái hợp đồng từ '{oldStatus}' sang '{request.NewStatus}' thành công.");
        }

        public async Task<ApiResponse<string>> UploadScannedContractAsync(
            int contractId, IFormFile file, int userId)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<string>.Fail("Không tìm thấy hợp đồng.");
            }

            // Xóa file cũ nếu đã tồn tại
            if (!string.IsNullOrWhiteSpace(contract.ScannedFilePath))
            {
                _fileStorageService.DeleteFile(contract.ScannedFilePath);
            }

            // Lưu file mới
            var filePath = await _fileStorageService.SaveFileAsync(
                file, $"contracts/{contractId}");

            contract.ScannedFilePath = filePath;
            contract.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Contract>().Update(contract);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<string>.Ok(filePath, "Upload file scan hợp đồng thành công.");
        }

        public async Task<ApiResponse<ContractMilestoneDto>> AddMilestoneAsync(
            int contractId, CreateMilestoneRequest request, int userId)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.Milestones)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<ContractMilestoneDto>.Fail("Không tìm thấy hợp đồng.");
            }

            // Chỉ thêm mốc khi hợp đồng còn Draft hoặc Active
            if (contract.Status == ContractStatus.Completed || contract.Status == ContractStatus.Terminated)
            {
                return ApiResponse<ContractMilestoneDto>.Fail(
                    "Không thể thêm mốc thanh toán vào hợp đồng đã hoàn thành hoặc chấm dứt.");
            }

            // Kiểm tra tổng các mốc không vượt quá giá trị hợp đồng
            var currentTotal = contract.Milestones.Sum(m => m.Amount);
            if (currentTotal + request.Amount > contract.Value)
            {
                return ApiResponse<ContractMilestoneDto>.Fail(
                    $"Không thể thêm mốc {request.Amount:N0} VNĐ. " +
                    $"Tổng hiện tại: {currentTotal:N0} VNĐ. " +
                    $"Giá trị hợp đồng: {contract.Value:N0} VNĐ. " +
                    $"Còn lại có thể phân bổ: {(contract.Value - currentTotal):N0} VNĐ.");
            }

            var milestone = new ContractMilestone
            {
                ContractId = contractId,
                Title = request.Title,
                DueDate = request.DueDate,
                Amount = request.Amount,
                Status = MilestoneStatus.Pending
            };

            await _unitOfWork.Repository<ContractMilestone>().AddAsync(milestone);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ContractMilestoneDto>.Ok(
                MapMilestoneToDto(milestone),
                "Thêm mốc thanh toán thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteMilestoneAsync(
            int contractId, int milestoneId, int userId)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy hợp đồng.");
            }

            var milestone = await _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .FirstOrDefaultAsync(m => m.Id == milestoneId && m.ContractId == contractId);

            if (milestone == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy mốc thanh toán trong hợp đồng này.");
            }

            // Chỉ xóa được mốc khi hợp đồng còn Draft hoặc Active, và mốc còn Pending
            if (milestone.Status != MilestoneStatus.Pending)
            {
                return ApiResponse<bool>.Fail(
                    "Chỉ có thể xóa các mốc thanh toán đang ở trạng thái Chờ xử lý (Pending).");
            }

            _unitOfWork.Repository<ContractMilestone>().Delete(milestone);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Xóa mốc thanh toán thành công.");
        }

        private static ContractDto MapToDto(Contract contract)
        {
            return new ContractDto
            {
                Id = contract.Id,
                BidPackageId = contract.BidPackageId,
                BidPackageCode = contract.BidPackage?.Code ?? string.Empty,
                BidPackageName = contract.BidPackage?.Name ?? string.Empty,
                ContractorId = contract.ContractorId,
                CompanyName = contract.Contractor?.CompanyName ?? string.Empty,
                TaxCode = contract.Contractor?.TaxCode,
                ContractNumber = contract.ContractNumber,
                Value = contract.Value,
                Terms = contract.Terms,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Status = contract.Status,
                StatusName = contract.Status.ToString(),
                ScannedFilePath = contract.ScannedFilePath,
                CreatedAt = contract.CreatedAt,
                UpdatedAt = contract.UpdatedAt,
                Milestones = contract.Milestones?.Select(MapMilestoneToDto).ToList() ?? new()
            };
        }

        private static ContractMilestoneDto MapMilestoneToDto(ContractMilestone milestone)
        {
            return new ContractMilestoneDto
            {
                Id = milestone.Id,
                ContractId = milestone.ContractId,
                Title = milestone.Title,
                DueDate = milestone.DueDate,
                Amount = milestone.Amount,
                Status = milestone.Status,
                StatusName = milestone.Status.ToString()
            };
        }

        public async Task<ApiResponse<List<ContractSummaryDto>>> GetContractsByContractorIdAsync(
            int contractorId, int userId, bool isInternalStaff)
        {
            // Kiểm tra phân quyền chống IDOR: Contractor chỉ xem được của chính mình
            if (!isInternalStaff)
            {
                var currentContractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (currentContractor == null || currentContractor.Id != contractorId)
                {
                    return ApiResponse<List<ContractSummaryDto>>.Fail(
                        "Bạn không có quyền xem lịch sử hợp đồng của nhà thầu khác.");
                }
            }

            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractorId);

            if (contractor == null)
            {
                return ApiResponse<List<ContractSummaryDto>>.Fail(
                    $"Không tìm thấy thông tin nhà thầu có mã #{contractorId}.");
            }

            var contracts = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Include(c => c.Milestones)
                .AsNoTracking()
                .Where(c => c.ContractorId == contractorId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ContractSummaryDto
                {
                    Id = c.Id,
                    BidPackageId = c.BidPackageId,
                    BidPackageCode = c.BidPackage.Code,
                    ContractorId = c.ContractorId,
                    CompanyName = c.Contractor.CompanyName,
                    ContractNumber = c.ContractNumber,
                    Value = c.Value,
                    Status = c.Status,
                    StatusName = c.Status.ToString(),
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    MilestoneCount = c.Milestones.Count
                })
                .ToListAsync();

            return ApiResponse<List<ContractSummaryDto>>.Ok(contracts);
        }

        public async Task<ApiResponse<ProgressUpdateDto>> AddProgressUpdateAsync(
            int contractId, CreateProgressUpdateRequest request, int userId)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.Contractor)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<ProgressUpdateDto>.Fail("Không tìm thấy hợp đồng.");
            }

            // Kiểm tra quyền sở hữu IDOR: chỉ nhà thầu sở hữu hợp đồng mới được gửi báo cáo
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (contractor == null || contractor.Id != contract.ContractorId)
            {
                return ApiResponse<ProgressUpdateDto>.Fail(
                    "Bạn không có quyền cập nhật tiến độ cho hợp đồng này.");
            }

            // Chỉ cập nhật khi hợp đồng đang ở trạng thái Active
            if (contract.Status != ContractStatus.Active)
            {
                return ApiResponse<ProgressUpdateDto>.Fail(
                    "Chỉ có thể cập nhật tiến độ cho hợp đồng đang ở trạng thái Hoạt động (Active).");
            }

            var progressUpdate = new ProgressUpdate
            {
                ContractId = contractId,
                WeekNumber = request.WeekNumber,
                CompletionPercent = request.CompletionPercent,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<ProgressUpdate>().AddAsync(progressUpdate);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = new ProgressUpdateDto
            {
                Id = progressUpdate.Id,
                ContractId = progressUpdate.ContractId,
                WeekNumber = progressUpdate.WeekNumber,
                CompletionPercent = progressUpdate.CompletionPercent,
                Note = progressUpdate.Note,
                CreatedAt = progressUpdate.CreatedAt
            };

            return ApiResponse<ProgressUpdateDto>.Ok(
                resultDto,
                $"Cập nhật tiến độ tuần {request.WeekNumber} ({request.CompletionPercent}%) thành công.");
        }

        public async Task<ApiResponse<MilestoneAcceptanceDto>> ApproveMilestoneAcceptanceAsync(
            int milestoneId, ApproveMilestoneAcceptanceRequest request, int userId)
        {
            var milestone = await _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .Include(m => m.Contract)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Fail("Không tìm thấy mốc thanh toán nghiệm thu.");
            }

            if (milestone.Contract.Status != ContractStatus.Active)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Fail(
                    "Chỉ có thể nghiệm thu mốc thanh toán khi hợp đồng đang ở trạng thái Hoạt động (Active).");
            }

            var approver = await _unitOfWork.Repository<User>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var acceptance = new Acceptance
            {
                ContractId = milestone.ContractId,
                MilestoneId = milestone.Id,
                ApprovedBy = userId,
                ApprovedAt = DateTime.UtcNow,
                Status = request.IsApproved ? AcceptanceStatus.Approved : AcceptanceStatus.Rejected,
                Note = request.Note
            };

            await _unitOfWork.Repository<Acceptance>().AddAsync(acceptance);

            if (request.IsApproved)
            {
                milestone.Status = MilestoneStatus.Completed;
                _unitOfWork.Repository<ContractMilestone>().Update(milestone);
            }

            await _unitOfWork.SaveChangesAsync();

            var resultDto = new MilestoneAcceptanceDto
            {
                Id = acceptance.Id,
                ContractId = acceptance.ContractId,
                MilestoneId = acceptance.MilestoneId,
                ApprovedBy = acceptance.ApprovedBy,
                ApproverName = approver?.FullName ?? string.Empty,
                ApprovedAt = acceptance.ApprovedAt,
                Status = acceptance.Status,
                StatusName = acceptance.Status.ToString(),
                Note = acceptance.Note,
                MilestoneStatus = milestone.Status,
                MilestoneStatusName = milestone.Status.ToString()
            };

            var actionMsg = request.IsApproved ? "Phê duyệt" : "Từ chối";
            return ApiResponse<MilestoneAcceptanceDto>.Ok(
                resultDto,
                $"{actionMsg} biên bản nghiệm thu mốc '{milestone.Title}' thành công.");
        }
    }
}
