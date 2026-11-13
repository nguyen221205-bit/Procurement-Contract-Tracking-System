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
    public partial class ContractService : IContractService
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

        public async Task<ApiResponse<ContractListDto>> GetContractsAsync(
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
                    return ApiResponse<ContractListDto>.Fail(
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

            var summary = await query.GroupBy(c => 1).Select(g => new ContractListSummaryDto
            {
                TotalContracts = g.Count(),
                ActiveContracts = g.Count(c => c.Status == ContractStatus.Active),
                DraftContracts = g.Count(c => c.Status == ContractStatus.Draft),
                CompletedContracts = g.Count(c => c.Status == ContractStatus.Completed),
                TotalValue = g.Sum(c => c.Value)
            }).SingleOrDefaultAsync() ?? new ContractListSummaryDto();
            var contractIds = query.Select(c => c.Id);
            summary.TotalDisbursedAmount = await _unitOfWork.Repository<ContractMilestone>().Query()
                .Where(m => contractIds.Contains(m.ContractId) && m.Status == MilestoneStatus.Completed)
                .SumAsync(m => (decimal?)m.Amount) ?? 0m;
            summary.DisbursementRate = summary.TotalValue > 0
                ? Math.Round(summary.TotalDisbursedAmount / summary.TotalValue * 100m, 2) : 0m;
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
            return ApiResponse<ContractListDto>.Ok(new ContractListDto(paginatedResult, summary));
        }

        public async Task<ApiResponse<ContractDto>> GetContractByIdAsync(
            int id, int userId, bool isInternalStaff, bool isAdmin = false)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Include(c => c.Milestones)
                    .ThenInclude(m => m.Acceptances)
                        .ThenInclude(a => a.TechnicalReviewer)
                .Include(c => c.Milestones)
                    .ThenInclude(m => m.Acceptances)
                        .ThenInclude(a => a.Approver)
                .Include(c => c.Milestones)
                    .ThenInclude(m => m.Acceptances)
                        .ThenInclude(a => a.AppliedSigningAuthority)
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

            var dto = MapToDto(contract);
            if (isInternalStaff)
            {
                dto.CanManageContract = await PackageWriteAuthorization.CanWriteReadOnlyAsync(
                    _unitOfWork, contract.BidPackage, userId, isAdmin);

                if (!isAdmin && contract.BidPackage != null)
                {
                    var pe = await ResolveContractProcuringEntityAsync(contract.BidPackage);
                    if (pe != null)
                    {
                        var member = await _unitOfWork.Repository<ProcuringEntityMember>()
                            .Query()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(m =>
                                m.ProcuringEntityId == pe.Id &&
                                m.UserId == userId &&
                                m.Status == MemberStatus.Active);
                        if (member != null)
                        {
                            dto.CanTechnicalReview = member.RoleInEntity == ProcuringEntityMemberRole.Officer;
                            var now = DateTime.UtcNow;
                            var contractScope = $"Contract:{contract.Id}";
                            dto.CanApproveDisbursement = await _unitOfWork.Repository<SigningAuthority>()
                                .Query()
                                .AsNoTracking()
                                .AnyAsync(a =>
                                    a.ProcuringEntityId == pe.Id &&
                                    a.UserId == userId &&
                                    a.Status == SigningAuthorityStatus.Active &&
                                    a.ValidFrom <= now &&
                                    a.ValidTo >= now &&
                                    (a.AuthorityType == SigningAuthorityType.DisbursementApproval ||
                                     a.AuthorityType == SigningAuthorityType.FullLegalRepresentative) &&
                                    (a.Scope == "EntityWide" || a.Scope == contractScope));
                        }
                    }
                }
            }

            return ApiResponse<ContractDto>.Ok(dto);
        }

        public async Task<ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>> GetScannedContractForDownloadAsync(
            int contractId, int userId, bool isInternalStaff, bool isAdmin = false)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId);
            if (contract == null)
            {
                return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.NotFound("Không tìm thấy hợp đồng.");
            }

            if (isInternalStaff && !isAdmin &&
                !await PackageWriteAuthorization.CanReadEntityStaffAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
            {
                return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Forbidden(
                    "Bạn không có quyền tải tệp scan của hợp đồng thuộc đơn vị mời thầu khác.");
            }

            if (!isInternalStaff)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (contractor == null || contractor.Id != contract.ContractorId)
                {
                    return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Forbidden(
                        "Bạn không có quyền tải tệp scan của hợp đồng này.");
                }
            }

            if (string.IsNullOrWhiteSpace(contract.ScannedFilePath))
            {
                return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.NotFound(
                    "Hợp đồng chưa có tệp scan đính kèm.");
            }

            var fileName = $"HopDong_{contract.ContractNumber}.pdf";
            var downloadResult = await _fileStorageService.ResolveDownloadAsync(contract.ScannedFilePath, fileName, "application/pdf");
            if (!downloadResult.IsRedirect && !downloadResult.IsStream &&
                (string.IsNullOrEmpty(downloadResult.PhysicalPath) || !File.Exists(downloadResult.PhysicalPath)))
            {
                return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.NotFound(
                    "Tệp tài liệu scan không tồn tại trên hệ thống lưu trữ.");
            }

            return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Ok(downloadResult);
        }

        public async Task<ApiResponse<AwardedBidInfoDto>> GetAwardedBidForContractAsync(int packageId)
        {
            // Tìm gói thầu
            var bidPackage = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == packageId);
            if (bidPackage == null || bidPackage.OwnershipNeedsReview)
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
            if (bidPackage == null || bidPackage.OwnershipNeedsReview)
            {
                return ApiResponse<ContractDto>.Fail("Gói thầu không tồn tại.");
            }

            // Kiểm tra quyền sở hữu gói thầu (chỉ Admin hoặc Procurement tạo gói mới được lập hợp đồng)
            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, bidPackage, userId, isAdmin))
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
            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
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
            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
            {
                return ApiResponse<ContractDto>.Forbidden("Bạn chỉ có quyền thay đổi trạng thái hợp đồng thuộc gói thầu do chính mình phụ trách.");
            }

            var transitionContext = new ContractTransitionContext(contract, request, userId, isAdmin, DateTime.UtcNow);
            var transitionValidation = ContractStateMachine.ValidateAndApplyTransitionMetadata(transitionContext);
            if (!transitionValidation.IsValid)
            {
                return transitionValidation.IsForbidden
                    ? ApiResponse<ContractDto>.Forbidden(transitionValidation.ErrorMessage ?? "Bạn không có thẩm quyền thực hiện chuyển trạng thái này.")
                    : ApiResponse<ContractDto>.Fail(transitionValidation.ErrorMessage ?? "Chuyển trạng thái hợp đồng không hợp lệ.");
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
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var contract = await _unitOfWork.Repository<Contract>()
                    .Query()
                    .Include(c => c.BidPackage)
                    .FirstOrDefaultAsync(c => c.Id == contractId);
                if (contract == null)
                {
                    return ApiResponse<string>.Fail("Không tìm thấy hợp đồng.");
                }

                // F04: Không cho phép thay đổi file scan của hợp đồng đã hoàn thành hoặc đã chấm dứt
                if (contract.Status == ContractStatus.Completed || contract.Status == ContractStatus.Terminated)
                {
                    return ApiResponse<string>.Fail("Không thể thay đổi tệp scan của hợp đồng đã hoàn thành hoặc đã chấm dứt.");
                }

                // Kiểm tra quyền sở hữu hợp đồng (chỉ Admin hoặc Procurement tạo gói mới được tải lên bản scan)
                if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
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
                _unitOfWork.Repository<Contract>().UpdateScalar(contract);
                // F04: Trong cùng đơn vị thay đổi nguyên tử, khi thay đổi tài liệu scan hợp đồng,
                // vô hiệu hóa toàn bộ các biên bản kiểm tra kỹ thuật đang chờ (Pending / TechnicalApproved) của hợp đồng này
                // và tăng Milestone.Version++ để bắt buộc Cán bộ kỹ thuật kiểm tra lại trên bản scan mới.
                var pendingAcceptances = await _unitOfWork.Repository<Acceptance>()
                    .Query()
                    .Include(a => a.Milestone)
                    .Where(a => a.ContractId == contractId && (a.Status == AcceptanceStatus.Pending || a.Status == AcceptanceStatus.TechnicalApproved))
                    .ToListAsync();
                foreach (var acc in pendingAcceptances)
                {
                    acc.Status = AcceptanceStatus.ReviewInvalidated;
                    acc.Note = string.IsNullOrWhiteSpace(acc.Note)
                        ? "[HỆ THỐNG VÔ HIỆU HÓA] Bản scan hợp đồng căn cứ đã bị thay thế."
                        : acc.Note + " | [HỆ THỐNG VÔ HIỆU HÓA] Bản scan hợp đồng căn cứ đã bị thay thế.";
                    _unitOfWork.Repository<Acceptance>().UpdateScalar(acc);
                    if (acc.Milestone != null)
                    {
                        acc.Milestone.Version++;
                        _unitOfWork.Repository<ContractMilestone>().UpdateScalar(acc.Milestone);
                    }

                }

                try
                {
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }

                catch
                {
                    // Nếu lưu DB thất bại, dọn dẹp ngay file mới tải lên để chống file mồ côi
                    _fileStorageService.DeleteFile(filePath);
                    throw;
                }

                // R6: Chỉ xóa file cũ nếu file đó CHƯA TỪNG được snapshot/tham chiếu trong bất kỳ biên bản nghiệm thu nào
                if (!string.IsNullOrWhiteSpace(oldFilePath))
                {
                    var isReferencedByAcceptance = await _unitOfWork.Repository<Acceptance>()
                        .Query()
                        .AsNoTracking()
                        .AnyAsync(a => a.SnapshotDocumentPath == oldFilePath);
                    if (!isReferencedByAcceptance)
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

                }

                return ApiResponse<string>.Ok(filePath, "Upload file scan hợp đồng thành công.");

            }
            catch (DbUpdateConcurrencyException)
            {
                return ApiResponse<string>.Fail("Quyền thành viên hoặc dữ liệu đã thay đổi. Vui lòng tải lại.", statusCode: 409);
            }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
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
            var latestAcceptance = milestone.Acceptances?
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();
            return new ContractMilestoneDto
            {
                Id = milestone.Id,
                ContractId = milestone.ContractId,
                Title = milestone.Title,
                DueDate = milestone.DueDate,
                Amount = milestone.Amount,
                Status = milestone.Status,
                StatusName = milestone.Status.ToString(),
                Version = milestone.Version,
                AcceptanceId = latestAcceptance?.Id,
                AcceptanceStatus = latestAcceptance?.Status,
                AcceptanceStatusName = latestAcceptance?.Status.ToString(),
                TechnicalReviewedBy = latestAcceptance?.TechnicalReviewedBy,
                TechnicalReviewerName = latestAcceptance?.TechnicalReviewer?.FullName,
                TechnicalReviewedAt = latestAcceptance?.TechnicalReviewedAt,
                TechnicalNote = latestAcceptance?.TechnicalNote,
                IsTechnicalPassed = latestAcceptance?.IsTechnicalPassed,
                MilestoneVersion = latestAcceptance?.MilestoneVersion,
                SnapshotAmount = latestAcceptance?.SnapshotAmount,
                SnapshotTitle = latestAcceptance?.SnapshotTitle,
                SnapshotDueDate = latestAcceptance?.SnapshotDueDate,
                AcceptanceRevision = latestAcceptance?.Revision,
                SnapshotDocumentHash = latestAcceptance?.SnapshotDocumentHash,
                SnapshotDocumentPath = latestAcceptance?.SnapshotDocumentPath,
                SigningAuthorityId = latestAcceptance?.SigningAuthorityId,
                SigningAuthorityDecisionNumber = latestAcceptance?.AppliedSigningAuthority?.DecisionNumber,
                ApprovedBy = latestAcceptance?.ApprovedBy,
                ApproverName = latestAcceptance?.Approver?.FullName,
                ApprovedAt = latestAcceptance?.ApprovedAt,
                AcceptanceNote = latestAcceptance?.Note
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

        /// <summary>
        /// R4-01 & R4-02: Xác định Bên mời thầu quản lý hợp đồng trực tiếp từ BidPackage.ProcuringEntityId cố định.
        /// Tuyệt đối không suy diễn từ membership hiện tại của BidPackage.CreatedBy.
        /// Nếu dữ liệu legacy có ProcuringEntityId = null nhưng người tạo là chủ sở hữu duy nhất chưa từng chuyển đơn vị,
        /// hỗ trợ truy xuất an toàn có kiểm chứng không mơ hồ; ngược lại fail-closed (trả về null).
        /// </summary>
                /// <summary>
        /// Tính toán mã băm SHA256 an toàn từ tệp tài liệu (hỗ trợ cả tệp cục bộ và tệp lưu trữ từ xa/cloud).
        /// Trả về chuỗi hex SHA256 viết thường hoặc null nếu không thể truy cập tệp.
        /// </summary>
        private async Task<string?> ComputeFileHashSafeAsync(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;

            // 1. Tệp Cloud / Remote URL (http/https)
            if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                    using var stream = await httpClient.GetStreamAsync(filePath);
                    return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
                }
                catch
                {
                    return null;
                }
            }

            // 2. Tệp lưu trữ đám mây (supabase://) thông qua abstraction IFileStorageService
            if (_fileStorageService.IsCloudPath(filePath))
            {
                try
                {
                    using var downloadResult = await _fileStorageService.ResolveDownloadAsync(filePath, Path.GetFileName(filePath));
                    if (downloadResult.FileStream != null)
                    {
                        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(downloadResult.FileStream)).ToLowerInvariant();
                    }
                    if (!string.IsNullOrWhiteSpace(downloadResult.PhysicalPath) && File.Exists(downloadResult.PhysicalPath))
                    {
                        using var fallbackStream = File.OpenRead(downloadResult.PhysicalPath);
                        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fallbackStream)).ToLowerInvariant();
                    }
                }
                catch
                {
                    return null;
                }
            }

            // 3. Tệp lưu trữ cục bộ
            try
            {
                var physPath = _fileStorageService.GetPhysicalFilePath(filePath);
                if (File.Exists(physPath))
                {
                    using var s = File.OpenRead(physPath);
                    return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(s)).ToLowerInvariant();
                }
            }
            catch
            {
                // Bỏ qua ngoại lệ đường dẫn không hợp lệ / không phải tệp cục bộ
            }

            return null;
        }

        private async Task<ProcuringEntity?> ResolveContractProcuringEntityAsync(BidPackage? bidPackage)
        {
            if (bidPackage == null || bidPackage.OwnershipNeedsReview)
            {
                return null;
            }

            if (bidPackage.ProcuringEntityId.HasValue)
            {
                return await _unitOfWork.Repository<ProcuringEntity>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == bidPackage.ProcuringEntityId.Value);
            }

            return null;
        }
    }
}
