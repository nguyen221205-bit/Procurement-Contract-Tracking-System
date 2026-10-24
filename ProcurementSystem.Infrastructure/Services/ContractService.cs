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
        private readonly IContractorService _contractorService;
        private readonly INotificationService _notificationService;

        public ContractService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IContractorService contractorService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _contractorService = contractorService;
            _notificationService = notificationService;
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
                .Include(c => c.Guarantees)
                .Include(c => c.Addenda)
                    .ThenInclude(a => a.Creator)
                .Include(c => c.Addenda)
                    .ThenInclude(a => a.Approver)
                .Include(c => c.Evaluations)
                    .ThenInclude(e => e.EvaluatorUser)
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

            // Kiểm tra đã có hợp đồng chưa (bỏ qua hợp đồng đã Terminated)
            var existingContract = await _unitOfWork.Repository<Contract>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.BidPackageId == packageId && c.Status != ContractStatus.Terminated);

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
            CreateContractRequest request, int userId, bool isAdmin)
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

            // Kiểm tra quyền sở hữu gói thầu (chỉ Admin hoặc Procurement tạo gói mới được lập hợp đồng)
            if (!isAdmin && bidPackage.CreatedBy != userId)
            {
                return ApiResponse<ContractDto>.Forbidden("Bạn chỉ có quyền tạo hợp đồng cho gói thầu do chính mình phụ trách.");
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

            // 3. Kiểm tra gói thầu chưa có hợp đồng đang hoạt động (quan hệ 1-1, bỏ qua hợp đồng đã Terminated)
            var existingContract = await _unitOfWork.Repository<Contract>()
                .ExistsAsync(c => c.BidPackageId == request.BidPackageId && c.Status != ContractStatus.Terminated);

            if (existingContract)
            {
                return ApiResponse<ContractDto>.Fail(
                    "Gói thầu này đã có hợp đồng đang thực hiện hoặc dự thảo. Mỗi gói thầu chỉ được phép ký tối đa 1 hợp đồng chính thức còn hiệu lực.");
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
            int id, UpdateContractRequest request, int userId, bool isAdmin)
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

            // Kiểm tra quyền quản lý hợp đồng (chỉ Admin hoặc Procurement tạo gói mới được cập nhật)
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
            int id, ChangeContractStatusRequest request, int userId, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Include(c => c.Milestones)
                    .ThenInclude(m => m.Acceptances)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return ApiResponse<ContractDto>.Fail("Không tìm thấy hợp đồng.");
            }

            // Kiểm tra quyền sở hữu hợp đồng (chỉ Admin hoặc Procurement tạo gói mới được đổi trạng thái)
            if (!isAdmin && contract.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<ContractDto>.Forbidden("Bạn chỉ có quyền thay đổi trạng thái hợp đồng thuộc gói thầu do chính mình phụ trách.");
            }

            // Kiểm tra luồng trạng thái hợp lệ
            var validTransitions = new Dictionary<ContractStatus, List<ContractStatus>>
            {
                { ContractStatus.Draft, new List<ContractStatus> { ContractStatus.Active } },
                { ContractStatus.Active, new List<ContractStatus> { ContractStatus.Completed, ContractStatus.Terminated } },
                { ContractStatus.Completed, new List<ContractStatus>() }, // Cấm Completed -> Active (P1-4)
                { ContractStatus.Terminated, new List<ContractStatus> { ContractStatus.Active } } // Cho phép Admin khôi phục khi có giải trình hợp lệ
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

            // Nghiệp vụ: Chấm dứt hợp đồng (Active -> Terminated) bắt buộc có đầy đủ căn cứ pháp lý và không được dở dang mốc nghiệm thu
            if (request.NewStatus == ContractStatus.Terminated)
            {
                // 1. Kiểm tra mốc nghiệm thu dở dang đang có biên bản chờ duyệt
                var pendingAcceptanceMilestone = contract.Milestones?
                    .FirstOrDefault(m => m.Acceptances != null && m.Acceptances.Any(a => a.Status == AcceptanceStatus.Pending));
                if (pendingAcceptanceMilestone != null)
                {
                    return ApiResponse<ContractDto>.Fail(
                        $"Không thể chấm dứt hợp đồng khi mốc nghiệm thu '{pendingAcceptanceMilestone.Title}' đang có biên bản nghiệm thu chờ thẩm định duyệt. " +
                        "Vui lòng xử lý dứt điểm duyệt hoặc từ chối biên bản nghiệm thu này trước khi thực hiện chấm dứt hợp đồng.");
                }

                // 2. Bắt buộc chọn loại căn cứ pháp lý
                if (string.IsNullOrWhiteSpace(request.TerminationReasonCategory))
                {
                    return ApiResponse<ContractDto>.Fail(
                        "Căn cứ Luật Đấu thầu: Vui lòng chọn phân loại căn cứ pháp lý chấm dứt hợp đồng (Vi phạm nghĩa vụ hợp đồng, Bất khả kháng, Thỏa thuận hai bên hoặc Quyết định của cấp có thẩm quyền).");
                }

                // 3. Bắt buộc có Số hiệu Quyết định / Văn bản pháp lý
                if (string.IsNullOrWhiteSpace(request.DecisionNumber))
                {
                    return ApiResponse<ContractDto>.Fail(
                        "Vui lòng cung cấp Số hiệu Quyết định / Văn bản / Biên bản chấm dứt hợp đồng làm căn cứ pháp lý lưu trữ hồ sơ.");
                }

                // 4. Bắt buộc nội dung giải trình tối thiểu 20 ký tự
                if (string.IsNullOrWhiteSpace(request.Note) || request.Note.Trim().Length < 20)
                {
                    return ApiResponse<ContractDto>.Fail(
                        "Lý do và nội dung giải trình chấm dứt hợp đồng phải có tối thiểu 20 ký tự, nêu rõ nguyên nhân và căn cứ pháp lý theo quy định.");
                }

                // Ghi nhận lịch sử vào Terms
                var terminationLog = $"[CHẤM DỨT HĐ {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC] Căn cứ: {request.TerminationReasonCategory} | Số hiệu QĐ: {request.DecisionNumber.Trim()} | Lý do: {request.Note.Trim()}";
                contract.Terms = string.IsNullOrWhiteSpace(contract.Terms)
                    ? terminationLog
                    : $"{contract.Terms}\n\n{terminationLog}";
            }

            // Nghiệp vụ: Khôi phục hợp đồng (Terminated -> Active) chỉ cho phép Admin thực hiện và phải có giải trình tối thiểu 20 ký tự
            if (contract.Status == ContractStatus.Terminated && request.NewStatus == ContractStatus.Active)
            {
                if (!isAdmin)
                {
                    return ApiResponse<ContractDto>.Forbidden("Chỉ Quản trị viên hệ thống (Admin) mới có thẩm quyền khôi phục hợp đồng đã chấm dứt.");
                }

                if (string.IsNullOrWhiteSpace(request.Note) || request.Note.Trim().Length < 20)
                {
                    return ApiResponse<ContractDto>.Fail(
                        "Vui lòng nêu rõ lý do và căn cứ pháp lý khôi phục hiệu lực hợp đồng (tối thiểu 20 ký tự giải trình).");
                }

                var reactivateLog = $"[KHÔI PHỤC HĐ {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC] Bởi Admin (ID: {userId}) | Lý do: {request.Note.Trim()}";
                contract.Terms = string.IsNullOrWhiteSpace(contract.Terms)
                    ? reactivateLog
                    : $"{contract.Terms}\n\n{reactivateLog}";
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

            // Gửi thông báo cho Nhà thầu khi hợp đồng có hiệu lực (Draft -> Active)
            if (request.NewStatus == ContractStatus.Active)
            {
                try
                {
                    if (contract.Contractor?.UserId > 0)
                    {
                        var contractTitle = contract.BidPackage?.Name ?? contract.ContractNumber;
                        await _notificationService.CreateNotificationAsync(
                            contract.Contractor.UserId,
                            "Hợp đồng kinh tế có hiệu lực",
                            $"Hợp đồng số {contract.ContractNumber} ({contractTitle}) đã chính thức có hiệu lực thi công.",
                            NotificationType.Info,
                            sendEmail: true);
                    }
                }
                catch
                {
                    // Bọc try-catch riêng biệt để không ảnh hưởng đến kết quả đổi trạng thái
                }
            }

            // Gửi thông báo cho Nhà thầu khi hợp đồng bị chấm dứt
            if (request.NewStatus == ContractStatus.Terminated && contract.Contractor?.UserId > 0)
            {
                try
                {
                    var contractTitle = contract.BidPackage?.Name ?? contract.ContractNumber;
                    await _notificationService.CreateNotificationAsync(
                        contract.Contractor.UserId,
                        "Thông báo chấm dứt hợp đồng kinh tế",
                        $"Hợp đồng số {contract.ContractNumber} ({contractTitle}) đã bị chấm dứt theo văn bản/QĐ số {request.DecisionNumber}. Căn cứ: {request.TerminationReasonCategory}. Lý do: {request.Note}",
                        NotificationType.Warning,
                        sendEmail: true);
                }
                catch
                {
                    // Bọc try-catch riêng biệt để không ảnh hưởng đến kết quả đổi trạng thái
                }
            }

            // R3-9: Tự động tính toán lại điểm đánh giá uy tín (Rating) nhà thầu khi hợp đồng kết thúc, chấm dứt hoặc khôi phục
            if (request.NewStatus == ContractStatus.Terminated || 
                request.NewStatus == ContractStatus.Completed || 
                (oldStatus == ContractStatus.Terminated && request.NewStatus == ContractStatus.Active))
            {
                await _contractorService.RecalculateContractorRatingAsync(contract.ContractorId);
            }

            return ApiResponse<ContractDto>.Ok(
                MapToDto(contract),
                $"Chuyển trạng thái hợp đồng từ '{oldStatus}' sang '{request.NewStatus}' thành công.");
        }

        public async Task<ApiResponse<string>> UploadScannedContractAsync(
            int contractId, IFormFile file, int userId, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<string>.Fail("Không tìm thấy hợp đồng.");
            }

            // Kiểm tra quyền sở hữu hợp đồng (chỉ Admin hoặc Procurement tạo gói mới được tải lên bản scan)
            if (!isAdmin && contract.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<string>.Forbidden("Bạn chỉ có quyền tải lên bản scan hợp đồng cho gói thầu do chính mình phụ trách.");
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

            var oldFilePath = contract.ScannedFilePath;

            // Lưu file mới
            var filePath = await _fileStorageService.SaveFileAsync(
                file, $"contracts/{contractId}");

            contract.ScannedFilePath = filePath;
            contract.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Contract>().Update(contract);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Nếu lưu DB thất bại, dọn dẹp ngay file mới tải lên để chống file mồ côi
                _fileStorageService.DeleteFile(filePath);
                throw;
            }

            // Xóa file cũ sau khi đã lưu DB thành công
            if (!string.IsNullOrWhiteSpace(oldFilePath))
            {
                try
                {
                    _fileStorageService.DeleteFile(oldFilePath);
                }
                catch
                {
                    // Bỏ qua lỗi xóa file cũ
                }
            }

            return ApiResponse<string>.Ok(filePath, "Upload file scan hợp đồng thành công.");
        }

        public async Task<ApiResponse<ContractMilestoneDto>> AddMilestoneAsync(
            int contractId, CreateMilestoneRequest request, int userId, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Milestones)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<ContractMilestoneDto>.Fail("Không tìm thấy hợp đồng.");
            }

            // Kiểm tra quyền sở hữu hợp đồng (chỉ Admin hoặc Procurement tạo gói mới được thêm mốc)
            if (!isAdmin && contract.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<ContractMilestoneDto>.Forbidden("Bạn chỉ có quyền thêm mốc thanh toán cho hợp đồng do chính mình phụ trách.");
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
            int contractId, int milestoneId, CreateMilestoneRequest request, int userId, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Milestones)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<ContractMilestoneDto>.Fail("Không tìm thấy hợp đồng.");
            }

            // Kiểm tra quyền sở hữu hợp đồng (chỉ Admin hoặc Procurement tạo gói mới được cập nhật mốc)
            if (!isAdmin && contract.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<ContractMilestoneDto>.Forbidden("Bạn chỉ có quyền cập nhật mốc thanh toán cho hợp đồng do chính mình phụ trách.");
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
            int contractId, int milestoneId, int userId, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy hợp đồng.");
            }

            // Kiểm tra quyền sở hữu hợp đồng (chỉ Admin hoặc Procurement tạo gói mới được xóa mốc)
            if (!isAdmin && contract.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<bool>.Forbidden("Bạn chỉ có quyền xóa mốc thanh toán của hợp đồng do chính mình phụ trách.");
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
                Milestones = contract.Milestones?.Select(MapMilestoneToDto).ToList() ?? new(),
                Guarantees = contract.Guarantees?.Select(g => new ContractGuaranteeDto
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
                }).OrderByDescending(g => g.CreatedAt).ToList() ?? new(),
                Addenda = contract.Addenda?.Select(a => new ContractAddendumDto
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
                }).OrderByDescending(a => a.SignedDate).ToList() ?? new(),
                Evaluation = contract.Evaluations?.OrderByDescending(e => e.EvaluatedAt).Select(e => new ContractorEvaluationDto
                {
                    Id = e.Id,
                    ContractId = e.ContractId,
                    ContractNumber = contract.ContractNumber,
                    BidPackageName = contract.BidPackage?.Name,
                    ContractorId = e.ContractorId,
                    ContractorName = contract.Contractor?.CompanyName ?? string.Empty,
                    EvaluatorUserId = e.EvaluatorUserId,
                    EvaluatorName = e.EvaluatorUser?.FullName ?? "Bên mời thầu",
                    QualityScore = e.QualityScore,
                    ScheduleScore = e.ScheduleScore,
                    ComplianceScore = e.ComplianceScore,
                    TotalScore = e.TotalScore,
                    RatingGrade = e.RatingGrade,
                    Comments = e.Comments,
                    EvaluatedAt = e.EvaluatedAt
                }).FirstOrDefault()
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
            int milestoneId, ApproveMilestoneAcceptanceRequest request, int userId, bool isAdmin)
        {
            var milestone = await _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .Include(m => m.Contract)
                    .ThenInclude(c => c.BidPackage)
                .Include(m => m.Contract)
                    .ThenInclude(c => c.Contractor)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Fail("Không tìm thấy mốc thanh toán nghiệm thu.");
            }

            // Kiểm tra quyền sở hữu hợp đồng (chỉ Admin hoặc Procurement tạo gói mới được nghiệm thu mốc)
            if (!isAdmin && milestone.Contract.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Forbidden("Bạn chỉ có quyền nghiệm thu mốc thanh toán cho hợp đồng do chính mình phụ trách.");
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

            // Gửi thông báo cho Nhà thầu về kết quả nghiệm thu mốc
            try
            {
                if (milestone.Contract?.Contractor?.UserId > 0)
                {
                    if (request.IsApproved)
                    {
                        await _notificationService.CreateNotificationAsync(
                            milestone.Contract.Contractor.UserId,
                            "Nghiệm thu giai đoạn hợp đồng thành công",
                            $"Mốc thực hiện '{milestone.Title}' thuộc hợp đồng số {milestone.Contract.ContractNumber} đã được phê duyệt nghiệm thu.",
                            NotificationType.Info,
                            sendEmail: true);
                    }
                    else
                    {
                        var rejectReason = !string.IsNullOrWhiteSpace(request.Note) ? $"\nLý do từ chối: {request.Note}" : string.Empty;
                        await _notificationService.CreateNotificationAsync(
                            milestone.Contract.Contractor.UserId,
                            "Nghiệm thu giai đoạn hợp đồng bị từ chối",
                            $"Mốc thực hiện '{milestone.Title}' thuộc hợp đồng số {milestone.Contract.ContractNumber} đã bị từ chối nghiệm thu.{rejectReason}",
                            NotificationType.Warning,
                            sendEmail: true);
                    }
                }
            }
            catch
            {
                // Bọc try-catch riêng biệt để không ảnh hưởng đến kết quả nghiệm thu
            }

            // R3-9: Tự động tính toán lại điểm uy tín (Rating) khi nghiệm thu mốc bị từ chối hoặc hợp đồng hoàn tất nghiệm thu 100%
            if (!request.IsApproved || milestone.Contract?.Status == ContractStatus.Completed)
            {
                if (milestone.Contract != null)
                {
                    await _contractorService.RecalculateContractorRatingAsync(milestone.Contract.ContractorId);
                }
            }

            var totalDisbursedAmount = await _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .AsNoTracking()
                .Where(m => m.ContractId == milestone.ContractId && m.Status == MilestoneStatus.Completed)
                .SumAsync(m => (decimal?)m.Amount) ?? 0;

            var totalRemainingAmount = (milestone.Contract?.Value ?? 0) > totalDisbursedAmount
                ? (milestone.Contract?.Value ?? 0) - totalDisbursedAmount
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
                ContractStatus = milestone.Contract!.Status,
                ContractStatusName = milestone.Contract!.Status.ToString(),
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

        #region Contract Guarantees (Bảo lãnh hợp đồng)
        public async Task<ApiResponse<List<ContractGuaranteeDto>>> GetContractGuaranteesAsync(
            int contractId, int userId, bool isInternalStaff, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.Guarantees)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<List<ContractGuaranteeDto>>.Fail("Không tìm thấy hợp đồng.");
            }

            if (isInternalStaff && !isAdmin && contract.BidPackage?.CreatedBy != userId)
            {
                return ApiResponse<List<ContractGuaranteeDto>>.Forbidden("Bạn chỉ có quyền xem bảo lãnh của hợp đồng do chính mình phụ trách.");
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

            if (!isAdmin && contract.BidPackage.CreatedBy != userId)
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

            var pct = request.Percentage > 0
                ? request.Percentage
                : (contract.Value > 0 ? Math.Round((request.Amount / contract.Value) * 100, 2) : 0);

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

            if (!isAdmin && guarantee.Contract.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<ContractGuaranteeDto>.Forbidden("Bạn không có quyền cập nhật trạng thái bảo lãnh của hợp đồng này.");
            }

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

            if (!isAdmin && guarantee.Contract.BidPackage.CreatedBy != userId)
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

        public async Task<(string PhysicalPath, string ContentType, string FileName)?> GetGuaranteeDocumentForDownloadAsync(
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

            if (isInternalStaff && !isAdmin && guarantee.Contract.BidPackage?.CreatedBy != userId)
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

            var physicalPath = _fileStorageService.GetPhysicalFilePath(guarantee.ScannedDocumentPath);
            if (!File.Exists(physicalPath))
            {
                return null;
            }

            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            var fileName = $"BaoLanh_{guarantee.GuaranteeNumber}{ext}";
            return (physicalPath, contentType, fileName);
        }
        #endregion

        #region Contract Addenda (Phụ lục hợp đồng)
        public async Task<ApiResponse<List<ContractAddendumDto>>> GetContractAddendaAsync(
            int contractId, int userId, bool isInternalStaff, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
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

            if (isInternalStaff && !isAdmin && contract.BidPackage?.CreatedBy != userId)
            {
                return ApiResponse<List<ContractAddendumDto>>.Forbidden("Bạn chỉ có quyền xem phụ lục của hợp đồng do chính mình phụ trách.");
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

            if (!isAdmin && contract.BidPackage.CreatedBy != userId)
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

            if (!isAdmin && addendum.Contract.BidPackage.CreatedBy != userId)
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

        public async Task<(string PhysicalPath, string ContentType, string FileName)?> GetAddendumDocumentForDownloadAsync(
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

            if (isInternalStaff && !isAdmin && addendum.Contract.BidPackage?.CreatedBy != userId)
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

            var physicalPath = _fileStorageService.GetPhysicalFilePath(addendum.ScannedDocumentPath);
            if (!File.Exists(physicalPath))
            {
                return null;
            }

            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            var fileName = $"PhuLuc_{addendum.AddendumNumber}{ext}";
            return (physicalPath, contentType, fileName);
        }
        #endregion

        #region Contractor Evaluation (Đánh giá uy tín & xếp hạng nhà thầu)
        public async Task<ApiResponse<ContractorEvaluationDto>> EvaluateContractorAsync(
            int contractId, CreateContractorEvaluationRequest request, int userId, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return ApiResponse<ContractorEvaluationDto>.Fail("Không tìm thấy hợp đồng.");
            }

            if (!isAdmin && contract.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<ContractorEvaluationDto>.Forbidden("Bạn chỉ có quyền đánh giá nghiệm thu cho gói thầu do chính mình phụ trách.");
            }

            if (contract.Status != ContractStatus.Completed && contract.Status != ContractStatus.Terminated)
            {
                return ApiResponse<ContractorEvaluationDto>.Fail("Chỉ được đánh giá nghiệm thu tổng kết khi hợp đồng đã ở trạng thái Hoàn thành (Completed) hoặc Chấm dứt (Terminated).");
            }

            var total = Math.Round(request.QualityScore + request.ScheduleScore + request.ComplianceScore, 2);
            string grade = total switch
            {
                >= 85 => "A",
                >= 70 => "B",
                >= 50 => "C",
                _ => "D"
            };

            var existingEval = await _unitOfWork.Repository<ContractorEvaluation>()
                .Query()
                .FirstOrDefaultAsync(e => e.ContractId == contractId);

            if (existingEval != null)
            {
                existingEval.QualityScore = request.QualityScore;
                existingEval.ScheduleScore = request.ScheduleScore;
                existingEval.ComplianceScore = request.ComplianceScore;
                existingEval.TotalScore = total;
                existingEval.RatingGrade = grade;
                existingEval.Comments = request.Comments?.Trim();
                existingEval.EvaluatorUserId = userId;
                existingEval.EvaluatedAt = DateTime.UtcNow;

                _unitOfWork.Repository<ContractorEvaluation>().Update(existingEval);
            }
            else
            {
                existingEval = new ContractorEvaluation
                {
                    ContractId = contractId,
                    ContractorId = contract.ContractorId,
                    EvaluatorUserId = userId,
                    QualityScore = request.QualityScore,
                    ScheduleScore = request.ScheduleScore,
                    ComplianceScore = request.ComplianceScore,
                    TotalScore = total,
                    RatingGrade = grade,
                    Comments = request.Comments?.Trim(),
                    EvaluatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<ContractorEvaluation>().AddAsync(existingEval);
            }

            await _unitOfWork.SaveChangesAsync();

            // Tự động tính toán lại điểm Contractor.Rating theo nguồn chân lý chuẩn hóa (F5-1)
            await _contractorService.RecalculateContractorRatingAsync(contract.ContractorId);

            // Gửi thông báo cho nhà thầu
            try
            {
                if (contract.Contractor != null && contract.Contractor.UserId > 0)
                {
                    await _notificationService.CreateNotificationAsync(
                        contract.Contractor.UserId,
                        $"Đánh giá hoàn thành hợp đồng #{contract.ContractNumber}",
                        $"Đơn vị của bạn được xếp Hạng {grade} ({total}/100 điểm) cho hợp đồng '{contract.ContractNumber}'.",
                        NotificationType.Info,
                        sendEmail: false);
                }
            }
            catch (Exception)
            {
            }

            var evaluatorUser = await _unitOfWork.Repository<User>().Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

            var dto = new ContractorEvaluationDto
            {
                Id = existingEval.Id,
                ContractId = existingEval.ContractId,
                ContractNumber = contract.ContractNumber,
                BidPackageName = contract.BidPackage?.Name,
                ContractorId = existingEval.ContractorId,
                ContractorName = contract.Contractor?.CompanyName ?? string.Empty,
                EvaluatorUserId = existingEval.EvaluatorUserId,
                EvaluatorName = evaluatorUser?.FullName ?? "Bên mời thầu",
                QualityScore = existingEval.QualityScore,
                ScheduleScore = existingEval.ScheduleScore,
                ComplianceScore = existingEval.ComplianceScore,
                TotalScore = existingEval.TotalScore,
                RatingGrade = existingEval.RatingGrade,
                Comments = existingEval.Comments,
                EvaluatedAt = existingEval.EvaluatedAt
            };

            return ApiResponse<ContractorEvaluationDto>.Ok(dto, $"Đánh giá nghiệm thu nhà thầu thành công. Xếp hạng: Hạng {grade} ({total}/100 điểm).");
        }

        public async Task<ApiResponse<ContractorEvaluationDto?>> GetContractEvaluationAsync(
            int contractId, int userId, bool isInternalStaff, bool isAdmin)
        {
            var evaluation = await _unitOfWork.Repository<ContractorEvaluation>()
                .Query()
                .Include(e => e.Contract)
                    .ThenInclude(c => c.BidPackage)
                .Include(e => e.Contractor)
                .Include(e => e.EvaluatorUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ContractId == contractId);

            if (evaluation == null)
            {
                return ApiResponse<ContractorEvaluationDto?>.Ok(null, "Hợp đồng chưa có đánh giá nghiệm thu.");
            }

            if (isInternalStaff && !isAdmin && evaluation.Contract?.BidPackage?.CreatedBy != userId)
            {
                return ApiResponse<ContractorEvaluationDto?>.Forbidden("Bạn chỉ có quyền xem đánh giá của hợp đồng do chính mình phụ trách.");
            }

            if (!isInternalStaff)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (contractor == null || contractor.Id != evaluation.ContractorId)
                {
                    return ApiResponse<ContractorEvaluationDto?>.Forbidden("Bạn không có quyền xem đánh giá này.");
                }
            }

            var dto = new ContractorEvaluationDto
            {
                Id = evaluation.Id,
                ContractId = evaluation.ContractId,
                ContractNumber = evaluation.Contract?.ContractNumber ?? string.Empty,
                BidPackageName = evaluation.Contract?.BidPackage?.Name,
                ContractorId = evaluation.ContractorId,
                ContractorName = evaluation.Contractor?.CompanyName ?? string.Empty,
                EvaluatorUserId = evaluation.EvaluatorUserId,
                EvaluatorName = evaluation.EvaluatorUser?.FullName ?? "Bên mời thầu",
                QualityScore = evaluation.QualityScore,
                ScheduleScore = evaluation.ScheduleScore,
                ComplianceScore = evaluation.ComplianceScore,
                TotalScore = evaluation.TotalScore,
                RatingGrade = evaluation.RatingGrade,
                Comments = evaluation.Comments,
                EvaluatedAt = evaluation.EvaluatedAt
            };

            return ApiResponse<ContractorEvaluationDto?>.Ok(dto);
        }

        public async Task<ApiResponse<ContractorCreditProfileDto>> GetContractorCreditProfileAsync(
            int contractorId, int userId, bool isInternalStaff)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractorId);

            if (contractor == null)
            {
                return ApiResponse<ContractorCreditProfileDto>.Fail("Không tìm thấy nhà thầu.");
            }

            if (!isInternalStaff)
            {
                var currentContractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (currentContractor == null || currentContractor.Id != contractorId)
                {
                    return ApiResponse<ContractorCreditProfileDto>.Forbidden("Bạn không có quyền xem hồ sơ uy tín của nhà thầu khác.");
                }
            }

            var contracts = await _unitOfWork.Repository<Contract>()
                .Query()
                .Where(c => c.ContractorId == contractorId)
                .AsNoTracking()
                .ToListAsync();

            var evals = await _unitOfWork.Repository<ContractorEvaluation>()
                .Query()
                .Include(e => e.Contract)
                    .ThenInclude(c => c.BidPackage)
                .Include(e => e.EvaluatorUser)
                .Where(e => e.ContractorId == contractorId)
                .OrderByDescending(e => e.EvaluatedAt)
                .AsNoTracking()
                .ToListAsync();

            var totalContracts = contracts.Count;
            var completedContracts = contracts.Count(c => c.Status == ContractStatus.Completed);
            var terminatedContracts = contracts.Count(c => c.Status == ContractStatus.Terminated);

            var avgQuality = evals.Count > 0 ? Math.Round(evals.Average(e => e.QualityScore), 2) : 0;
            var avgSchedule = evals.Count > 0 ? Math.Round(evals.Average(e => e.ScheduleScore), 2) : 0;
            var avgCompliance = evals.Count > 0 ? Math.Round(evals.Average(e => e.ComplianceScore), 2) : 0;

            var profile = new ContractorCreditProfileDto
            {
                ContractorId = contractor.Id,
                CompanyName = contractor.CompanyName,
                TaxCode = contractor.TaxCode,
                Rating = contractor.Rating,
                TotalContractsCount = totalContracts,
                CompletedContractsCount = completedContracts,
                TerminatedContractsCount = terminatedContracts,
                AverageQualityScore = avgQuality,
                AverageScheduleScore = avgSchedule,
                AverageComplianceScore = avgCompliance,
                Evaluations = evals.Select(e => new ContractorEvaluationDto
                {
                    Id = e.Id,
                    ContractId = e.ContractId,
                    ContractNumber = e.Contract?.ContractNumber ?? string.Empty,
                    BidPackageName = e.Contract?.BidPackage?.Name,
                    ContractorId = e.ContractorId,
                    ContractorName = contractor.CompanyName,
                    EvaluatorUserId = e.EvaluatorUserId,
                    EvaluatorName = e.EvaluatorUser?.FullName ?? "Bên mời thầu",
                    QualityScore = e.QualityScore,
                    ScheduleScore = e.ScheduleScore,
                    ComplianceScore = e.ComplianceScore,
                    TotalScore = e.TotalScore,
                    RatingGrade = e.RatingGrade,
                    Comments = e.Comments,
                    EvaluatedAt = e.EvaluatedAt
                }).ToList()
            };

            return ApiResponse<ContractorCreditProfileDto>.Ok(profile);
        }
        #endregion

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
