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

            // Tìm kiếm đa năng theo từ khóa (Số hợp đồng, tên nhà thầu, mã hoặc tên gói thầu)
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchLower = filter.Search.Trim().ToLower();
                query = query.Where(c =>
                    c.ContractNumber.ToLower().Contains(searchLower) ||
                    (c.Contractor != null && c.Contractor.CompanyName.ToLower().Contains(searchLower)) ||
                    (c.BidPackage != null && c.BidPackage.Code.ToLower().Contains(searchLower)) ||
                    (c.BidPackage != null && c.BidPackage.Name.ToLower().Contains(searchLower)));
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
                    MilestoneCount = c.Milestones.Count,
                    TotalDisbursedAmount = c.Milestones
                        .Where(m => m.Status == MilestoneStatus.Completed)
                        .Sum(m => m.Amount),
                    TotalRemainingAmount = c.Value - c.Milestones
                        .Where(m => m.Status == MilestoneStatus.Completed)
                        .Sum(m => m.Amount) > 0
                            ? c.Value - c.Milestones
                                .Where(m => m.Status == MilestoneStatus.Completed)
                                .Sum(m => m.Amount)
                            : 0
                });

            var paginatedResult = await PaginatedList<ContractSummaryDto>.CreateAsync(
                projectedQuery, filter.PageIndex, filter.PageSize);

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
                    return ApiResponse<ContractDto>.Forbidden("Bạn không có quyền xem hợp đồng này.");
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
                .FirstOrDefaultAsync(s => s.BidPackageId == packageId && s.Status == BidSubmissionStatus.Selected);

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
                WinningBidPrice         = awardedSubmission.BidPrice,
                TotalScore              = awardedSubmission.TotalScore,
                Rank                    = awardedSubmission.Rank,
                SuggestedContractValue  = (awardedSubmission.BidPrice.HasValue && awardedSubmission.BidPrice.Value > 0)
                                            ? awardedSubmission.BidPrice.Value
                                            : bidPackage.Budget,

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
                    bs.Status == BidSubmissionStatus.Selected);

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

            // 5. Kiểm soát dự toán ngân sách và giá trúng thầu (Điều 64 Luật Đấu thầu 2023)
            if (request.Value > bidPackage.Budget)
            {
                return ApiResponse<ContractDto>.Fail(
                    $"Giá trị hợp đồng ({request.Value:N0} VNĐ) không được vượt quá ngân sách dự toán của gói thầu ({bidPackage.Budget:N0} VNĐ).");
            }

            var winningSubmission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.BidPackageId == bidPackage.Id && s.Status == BidSubmissionStatus.Selected);

            if (winningSubmission?.BidPrice.HasValue == true && request.Value > winningSubmission.BidPrice.Value)
            {
                return ApiResponse<ContractDto>.Fail(
                    $"Căn cứ Điều 64 Luật Đấu thầu 2023: Giá trị hợp đồng ({request.Value:N0} VNĐ) không được vượt quá giá trúng thầu đã phê duyệt ({winningSubmission.BidPrice.Value:N0} VNĐ).");
            }

            // 6. Tự động sinh số hợp đồng hoặc kiểm tra trùng lặp (P2-6)
            if (string.IsNullOrWhiteSpace(request.ContractNumber))
            {
                var year = DateTime.UtcNow.Year;
                var count = await _unitOfWork.Repository<Contract>().Query().CountAsync(c => c.CreatedAt.Year == year);
                int seq = count + 1;
                string candidateNumber;
                do
                {
                    candidateNumber = $"HD-{year}-{bidPackage.Code}-{seq:D3}";
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

            // P2-6: Kiểm tra quyền quản lý hợp đồng (Procurement chỉ thao tác trên hợp đồng thuộc gói do mình phụ trách)
            var userRoles = await _unitOfWork.Repository<UserRole>()
                .Query()
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            var isAdmin = userRoles.Contains("Admin");
            if (!isAdmin && contract.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<ContractDto>.Forbidden("Bạn chỉ có quyền quản trị hợp đồng thuộc gói thầu do chính mình phụ trách.");
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
                { ContractStatus.Completed, new List<ContractStatus>() }, // Cấm Completed -> Active (P1-4)
                { ContractStatus.Terminated, new List<ContractStatus>() }
            };

            if (!validTransitions[contract.Status].Contains(request.NewStatus))
            {
                return ApiResponse<ContractDto>.Fail(
                    $"Không thể chuyển hợp đồng từ trạng thái '{contract.Status}' sang '{request.NewStatus}'.");
            }

            // Nghiệp vụ P1-4: Kích hoạt hợp đồng (Draft -> Active) bắt buộc thỏa mãn đầy đủ điều kiện pháp lý
            if (contract.Status == ContractStatus.Draft && request.NewStatus == ContractStatus.Active)
            {
                // 1. Bắt buộc có tệp scan hợp đồng đã ký
                if (string.IsNullOrWhiteSpace(contract.ScannedFilePath))
                {
                    return ApiResponse<ContractDto>.Fail(
                        "Căn cứ Điều 65 Luật Đấu thầu: Hợp đồng phải được tải lên bản scan có chữ ký, con dấu pháp lý trước khi kích hoạt có hiệu lực.");
                }

                // 2. Bắt buộc có mốc thanh toán và tổng mốc phải đúng bằng 100% giá trị hợp đồng
                var milestones = contract.Milestones?.ToList() ?? new List<ContractMilestone>();
                if (!milestones.Any())
                {
                    return ApiResponse<ContractDto>.Fail(
                        "Hợp đồng chưa thiết lập các mốc thanh toán. Vui lòng tạo kế hoạch mốc thanh toán trước khi kích hoạt.");
                }

                var totalMilestoneAmount = milestones.Sum(m => m.Amount);
                if (totalMilestoneAmount != contract.Value)
                {
                    return ApiResponse<ContractDto>.Fail(
                        $"Tổng giá trị các mốc thanh toán ({totalMilestoneAmount:N0} VNĐ) phải đúng bằng 100% giá trị hợp đồng ({contract.Value:N0} VNĐ).");
                }
            }

            // Nghiệp vụ: Chỉ cho phép chuyển sang Completed khi đã nghiệm thu và giải ngân đủ 100% giá trị hợp đồng
            if (request.NewStatus == ContractStatus.Completed)
            {
                var totalDisbursed = contract.Milestones?
                    .Where(m => m.Status == MilestoneStatus.Completed)
                    .Sum(m => m.Amount) ?? 0;

                var allMilestonesCompleted = contract.Milestones != null && contract.Milestones.Count > 0 &&
                    contract.Milestones.All(m => m.Status == MilestoneStatus.Completed);

                if (!allMilestonesCompleted || totalDisbursed < contract.Value)
                {
                    return ApiResponse<ContractDto>.Fail(
                        $"Không thể hoàn thành hợp đồng khi chưa nghiệm thu và giải ngân đủ 100% giá trị hợp đồng. " +
                        $"Hiện tại đã giải ngân: {totalDisbursed:N0} / {contract.Value:N0} VNĐ.");
                }
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
                    bidPackage.UpdatedAt = DateTime.UtcNow;
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

            // P2-8: Validate định dạng PDF và dung lượng tối đa 20MB
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
            {
                return ApiResponse<string>.Fail("Tệp tài liệu scan hợp đồng bắt buộc phải là định dạng PDF (.pdf).");
            }

            if (file.Length > 20 * 1024 * 1024)
            {
                return ApiResponse<string>.Fail("Dung lượng tệp scan không được vượt quá 20MB.");
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

            // P2-7: Ràng buộc DueDate phải nằm trong khoảng StartDate và EndDate của hợp đồng
            if (request.DueDate < contract.StartDate || request.DueDate > contract.EndDate)
            {
                return ApiResponse<ContractMilestoneDto>.Fail(
                    $"Hạn hoàn thành mốc thanh toán ({request.DueDate:dd/MM/yyyy}) phải nằm trong thời hạn hiệu lực của hợp đồng " +
                    $"({contract.StartDate:dd/MM/yyyy} - {contract.EndDate:dd/MM/yyyy}).");
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

        public async Task<ApiResponse<ContractMilestoneDto>> UpdateMilestoneAsync(
            int contractId, int milestoneId, CreateMilestoneRequest request, int userId)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.Milestones)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<ContractMilestoneDto>.Fail("Không tìm thấy hợp đồng.");
            }

            if (contract.Status == ContractStatus.Completed || contract.Status == ContractStatus.Terminated)
            {
                return ApiResponse<ContractMilestoneDto>.Fail(
                    "Không thể cập nhật mốc thanh toán của hợp đồng đã hoàn thành hoặc chấm dứt.");
            }

            var milestone = contract.Milestones.FirstOrDefault(m => m.Id == milestoneId);
            if (milestone == null)
            {
                return ApiResponse<ContractMilestoneDto>.Fail("Không tìm thấy mốc thanh toán trong hợp đồng này.");
            }

            if (milestone.Status != MilestoneStatus.Pending)
            {
                return ApiResponse<ContractMilestoneDto>.Fail(
                    "Chỉ có thể cập nhật các mốc thanh toán đang ở trạng thái Chờ xử lý (Pending).");
            }

            // P2-7: Ràng buộc DueDate phải nằm trong khoảng StartDate và EndDate của hợp đồng
            if (request.DueDate < contract.StartDate || request.DueDate > contract.EndDate)
            {
                return ApiResponse<ContractMilestoneDto>.Fail(
                    $"Hạn hoàn thành mốc thanh toán ({request.DueDate:dd/MM/yyyy}) phải nằm trong thời hạn hiệu lực của hợp đồng " +
                    $"({contract.StartDate:dd/MM/yyyy} - {contract.EndDate:dd/MM/yyyy}).");
            }

            var totalExcludingCurrent = contract.Milestones
                .Where(m => m.Id != milestoneId)
                .Sum(m => m.Amount);

            if (totalExcludingCurrent + request.Amount > contract.Value)
            {
                return ApiResponse<ContractMilestoneDto>.Fail(
                    $"Không thể cập nhật mốc thành {request.Amount:N0} VNĐ. " +
                    $"Tổng các mốc khác: {totalExcludingCurrent:N0} VNĐ. " +
                    $"Giá trị hợp đồng: {contract.Value:N0} VNĐ. " +
                    $"Còn lại có thể phân bổ: {(contract.Value - totalExcludingCurrent):N0} VNĐ.");
            }

            milestone.Title = request.Title;
            milestone.DueDate = request.DueDate;
            milestone.Amount = request.Amount;
            contract.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<ContractMilestone>().Update(milestone);
            _unitOfWork.Repository<Contract>().Update(contract);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ContractMilestoneDto>.Ok(
                MapMilestoneToDto(milestone),
                "Cập nhật mốc thanh toán thành công.");
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
            var totalDisbursedAmount = GetTotalDisbursedAmount(contract);
            var totalRemainingAmount = GetTotalRemainingAmount(contract);

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
                TotalDisbursedAmount = totalDisbursedAmount,
                TotalRemainingAmount = totalRemainingAmount,
                DisbursementRate = contract.Value > 0
                    ? Math.Round((totalDisbursedAmount / contract.Value) * 100, 2)
                    : 0,
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
                    return ApiResponse<List<ContractSummaryDto>>.Forbidden(
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
                    MilestoneCount = c.Milestones.Count,
                    TotalDisbursedAmount = c.Milestones
                        .Where(m => m.Status == MilestoneStatus.Completed)
                        .Sum(m => m.Amount),
                    TotalRemainingAmount = c.Value - c.Milestones
                        .Where(m => m.Status == MilestoneStatus.Completed)
                        .Sum(m => m.Amount) > 0
                            ? c.Value - c.Milestones
                                .Where(m => m.Status == MilestoneStatus.Completed)
                                .Sum(m => m.Amount)
                            : 0
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
                return ApiResponse<ProgressUpdateDto>.Forbidden(
                    "Bạn không có quyền cập nhật tiến độ cho hợp đồng này.");
            }

            // Chỉ cập nhật khi hợp đồng đang ở trạng thái Active
            if (contract.Status != ContractStatus.Active)
            {
                return ApiResponse<ProgressUpdateDto>.Fail(
                    "Chỉ có thể cập nhật tiến độ cho hợp đồng đang ở trạng thái Hoạt động (Active).");
            }

            // P1-5: Chống lỗi nộp trùng tuần (DbUpdateException -> HTTP 500)
            var isDuplicateWeek = await _unitOfWork.Repository<ProgressUpdate>()
                .ExistsAsync(p => p.ContractId == contractId && p.WeekNumber == request.WeekNumber);
            if (isDuplicateWeek)
            {
                return ApiResponse<ProgressUpdateDto>.Fail(
                    $"Hợp đồng đã tồn tại báo cáo tiến độ cho Tuần {request.WeekNumber}. Vui lòng không nộp trùng tuần.");
            }

            // P1-5: Kiểm tra % hoàn thành không được giảm sút so với tuần trước
            var lastProgress = await _unitOfWork.Repository<ProgressUpdate>()
                .Query()
                .Where(p => p.ContractId == contractId)
                .OrderByDescending(p => p.WeekNumber)
                .FirstOrDefaultAsync();

            if (lastProgress != null && request.CompletionPercent < lastProgress.CompletionPercent)
            {
                return ApiResponse<ProgressUpdateDto>.Fail(
                    $"Tỷ lệ hoàn thành tuần mới ({request.CompletionPercent}%) không được nhỏ hơn tuần trước ({lastProgress.CompletionPercent}%).");
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

        public async Task<ApiResponse<ProgressUpdateDto>> AddMilestoneProgressUpdateAsync(
            int milestoneId, CreateProgressUpdateRequest request, int userId)
        {
            var milestone = await _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
            {
                return ApiResponse<ProgressUpdateDto>.Fail("Không tìm thấy mốc thanh toán nghiệm thu.");
            }

            return await AddProgressUpdateAsync(milestone.ContractId, request, userId);
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

            // P1-5: Chống duyệt lặp mốc thanh toán đã hoàn thành
            if (milestone.Status == MilestoneStatus.Completed)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Fail(
                    "Mốc thanh toán này đã được nghiệm thu hoàn thành trước đó. Không thể thực hiện nghiệm thu lại.");
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

                var contractMilestones = await _unitOfWork.Repository<ContractMilestone>()
                    .Query()
                    .Where(m => m.ContractId == milestone.ContractId)
                    .ToListAsync();

                var totalCompletedAmount = contractMilestones
                    .Where(m => m.Status == MilestoneStatus.Completed)
                    .Sum(m => m.Amount);

                // Nghiệp vụ: Chỉ tự động chuyển Hợp đồng sang Completed khi:
                // 1. Toàn bộ các mốc hiện có đều đã Completed
                // 2. VÀ Tổng số tiền đã nghiệm thu phải đạt đủ 100% giá trị hợp đồng
                if (contractMilestones.Count > 0 &&
                    contractMilestones.All(m => m.Status == MilestoneStatus.Completed) &&
                    totalCompletedAmount >= milestone.Contract.Value)
                {
                    milestone.Contract.Status = ContractStatus.Completed;
                    milestone.Contract.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<Contract>().Update(milestone.Contract);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var totalDisbursedAmount = await _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .AsNoTracking()
                .Where(m => m.ContractId == milestone.ContractId && m.Status == MilestoneStatus.Completed)
                .SumAsync(m => (decimal?)m.Amount) ?? 0;

            var totalRemainingAmount = milestone.Contract.Value > totalDisbursedAmount
                ? milestone.Contract.Value - totalDisbursedAmount
                : 0;

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
                MilestoneStatusName = milestone.Status.ToString(),
                ContractStatus = milestone.Contract.Status,
                ContractStatusName = milestone.Contract.Status.ToString(),
                TotalDisbursedAmount = totalDisbursedAmount,
                TotalRemainingAmount = totalRemainingAmount
            };

            var actionMsg = request.IsApproved ? "Phê duyệt" : "Từ chối";
            var completedMsg = milestone.Contract.Status == ContractStatus.Completed
                ? " Hợp đồng đã được tự động chuyển sang trạng thái Completed vì tất cả mốc thanh toán đã hoàn thành và giải ngân đủ 100%."
                : string.Empty;

            return ApiResponse<MilestoneAcceptanceDto>.Ok(
                resultDto,
                $"{actionMsg} biên bản nghiệm thu mốc '{milestone.Title}' thành công.{completedMsg}");
        }

        private static decimal GetTotalDisbursedAmount(Contract contract)
        {
            return contract.Milestones?
                .Where(m => m.Status == MilestoneStatus.Completed)
                .Sum(m => m.Amount) ?? 0;
        }

        private static decimal GetTotalRemainingAmount(Contract contract)
        {
            var remaining = contract.Value - GetTotalDisbursedAmount(contract);
            return remaining > 0 ? remaining : 0;
        }
    }
}
