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
    public partial class ContractService : IContractMilestoneAcceptanceService
    {
        public async Task<ApiResponse<ContractMilestoneDto>> AddMilestoneAsync(
            int contractId, CreateMilestoneRequest request, int userId, bool isAdmin)
        {
            var contract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Milestones)
                    .ThenInclude(m => m.Acceptances)
                .FirstOrDefaultAsync(c => c.Id == contractId);
            if (contract == null)
            {
                return ApiResponse<ContractMilestoneDto>.Fail("Không tìm thấy hợp đồng.");
            }

            // Kiểm tra quyền sở hữu hợp đồng (chỉ Admin hoặc Procurement tạo gói mới được thêm mốc)
            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
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
            await _unitOfWork.BeginTransactionAsync();
            try
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
                if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
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

                bool contentChanged = milestone.Title != request.Title ||
                                      milestone.DueDate != request.DueDate ||
                                      milestone.Amount != request.Amount;
                if (contentChanged)
                {
                    milestone.Version++;
                    // R7: Truy vấn TRỰC TIẾP Acceptance từ DB, độc lập với DbContext tracking
                    var activeTechnicalAcceptances = await _unitOfWork.Repository<Acceptance>()
                        .Query()
                        .Where(a => a.MilestoneId == milestoneId && a.Status == AcceptanceStatus.TechnicalApproved)
                        .ToListAsync();
                    foreach (var activeTechnicalAcceptance in activeTechnicalAcceptances)
                    {
                        activeTechnicalAcceptance.Status = AcceptanceStatus.ReviewInvalidated;
                        activeTechnicalAcceptance.IsTechnicalPassed = false;
                        activeTechnicalAcceptance.Note = (string.IsNullOrWhiteSpace(activeTechnicalAcceptance.Note) ? "" : activeTechnicalAcceptance.Note + " | ") +
                            $"[HỆ THỐNG VÔ HIỆU HÓA {DateTime.UtcNow:dd/MM/yyyy HH:mm}] Mốc thanh toán đã bị sửa đổi nội dung sau khi Maker duyệt. Yêu cầu kiểm tra kỹ thuật lại trên phiên bản mốc v{milestone.Version}.";
                        _unitOfWork.Repository<Acceptance>().UpdateScalar(activeTechnicalAcceptance);
                        var auditLog = new AuditLog
                        {
                            UserId = userId,
                            Action = "INVALIDATE_MILESTONE_REVIEW",
                            EntityType = "Acceptance",
                            EntityId = activeTechnicalAcceptance.Id,
                            NewValues = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                MilestoneId = milestone.Id,
                                NewMilestoneVersion = milestone.Version,
                                Reason = "Nội dung mốc thanh toán bị thay đổi sau khi xác nhận kỹ thuật."
                            }),
                            Timestamp = DateTime.UtcNow
                        };
                        await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
                    }

                }

                milestone.Title = request.Title;
                milestone.DueDate = request.DueDate;
                milestone.Amount = request.Amount;
                contract.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<ContractMilestone>().UpdateScalar(milestone);
                _unitOfWork.Repository<Contract>().UpdateScalar(contract);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return ApiResponse<ContractMilestoneDto>.Ok(
                    MapMilestoneToDto(milestone),
                    "Cập nhật mốc thanh toán thành công.");

            }
            catch (DbUpdateConcurrencyException)
            {
                return ApiResponse<ContractMilestoneDto>.Fail("Quyền thành viên hoặc dữ liệu đã thay đổi. Vui lòng tải lại.", statusCode: 409);
            }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
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
            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
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

        public async Task<ApiResponse<MilestoneAcceptanceDto>> TechnicalReviewMilestoneAsync(
            int milestoneId, TechnicalReviewMilestoneRequest request, int userId, bool isAdmin)
        {
            var milestone = await _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .Include(m => m.Contract)
                    .ThenInclude(c => c.BidPackage)
                .Include(m => m.Contract)
                    .ThenInclude(c => c.Contractor)
                .Include(m => m.Acceptances)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);
            if (milestone == null)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Fail("Không tìm thấy mốc thanh toán nghiệm thu.");
            }

            if (milestone.Contract.Status != ContractStatus.Active)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Fail(
                    "Chỉ có thể xác nhận kỹ thuật khi hợp đồng đang ở trạng thái Hoạt động (Active).");
            }

            if (milestone.Status == MilestoneStatus.Completed)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Fail(
                    "Mốc thanh toán này đã được nghiệm thu hoàn thành trước đó.");
            }

            // R4: Admin nền tảng KHÔNG ĐƯỢC trực tiếp xác nhận kỹ thuật hiện trường của Bên mời thầu
            if (isAdmin)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Forbidden(
                    "Quản trị viên hệ thống (Admin) không trực tiếp thực hiện xác nhận kỹ thuật hiện trường của Bên mời thầu.");
            }

            // R4-01 & R4-02: Xác định Bên mời thầu quản lý hợp đồng trực tiếp từ BidPackage.ProcuringEntityId (không suy diễn động theo membership hiện tại của người tạo và không dùng cyclic Include)
            var pe = await ResolveContractProcuringEntityAsync(milestone.Contract.BidPackage);
            if (pe == null)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Fail(
                    "Không xác định được Bên mời thầu quản lý hợp đồng này (gói thầu chưa gắn định danh đơn vị mời thầu hợp lệ).");
            }

            // R4: Maker BẮT BUỘC phải là thành viên đang hoạt động với vai trò Officer của Bên mời thầu
            var isAuthorizedMaker = await _unitOfWork.Repository<ProcuringEntityMember>()
                .Query()
                .AsNoTracking()
                .AnyAsync(m =>
                    m.ProcuringEntityId == pe.Id &&
                    m.UserId == userId &&
                    m.Status == MemberStatus.Active &&
                    m.RoleInEntity == ProcuringEntityMemberRole.Officer);
            if (!isAuthorizedMaker)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Forbidden(
                    "Bạn không có quyền xác nhận kỹ thuật cho mốc thanh toán này. Thao tác này chỉ dành riêng cho Cán bộ kỹ thuật hiện trường (Officer) đang hoạt động của Bên mời thầu.");
            }

            var reviewer = await _unitOfWork.Repository<User>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            // R6 / F03: Tính toán Document Hash của tệp scan hợp đồng căn cứ qua FileStorageService trên byte stream vật lý
            string? docHash = null;
            string? docPath = milestone.Contract.ScannedFilePath;
            if (!string.IsNullOrWhiteSpace(docPath))
            {
                docHash = await ComputeFileHashSafeAsync(docPath);
                if (string.IsNullOrEmpty(docHash))
                {
                    if (!docPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !docPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                        !docPath.StartsWith("supabase://", StringComparison.OrdinalIgnoreCase))
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Fail(
                            $"Không tìm thấy tệp hồ sơ scan hợp đồng vật lý ({docPath}) trên máy chủ để lập hàm băm chứng thực. Kiểm tra kỹ thuật bị từ chối.");
                    }
                    else
                    {
                        using var sha = System.Security.Cryptography.SHA256.Create();
                        var identityBytes = System.Text.Encoding.UTF8.GetBytes($"{docPath}:{milestone.Id}:{milestone.Version}");
                        docHash = Convert.ToHexString(sha.ComputeHash(identityBytes)).ToLowerInvariant();
                    }
                }
            }

            // R5: Quản lý Revision - Không ghi đè in-place lên bản ghi cũ mà tạo bản ghi revision mới
            var existingAcceptances = await _unitOfWork.Repository<Acceptance>()
                .Query()
                .Where(a => a.MilestoneId == milestone.Id)
                .ToListAsync();
            var nextRevision = existingAcceptances.Count > 0
                ? existingAcceptances.Max(a => a.Revision) + 1
                : 1;
            foreach (var old in existingAcceptances.Where(a => a.Status == AcceptanceStatus.Pending || a.Status == AcceptanceStatus.TechnicalApproved))
            {
                old.Status = AcceptanceStatus.ReviewInvalidated;
                _unitOfWork.Repository<Acceptance>().Update(old);
            }

            var acceptance = new Acceptance
            {
                ContractId = milestone.ContractId,
                MilestoneId = milestone.Id,
                Revision = nextRevision,
                TechnicalReviewedBy = userId,
                TechnicalReviewedAt = DateTime.UtcNow,
                TechnicalNote = request.Note,
                IsTechnicalPassed = request.IsPassed,
                Status = request.IsPassed ? AcceptanceStatus.TechnicalApproved : AcceptanceStatus.Rejected,
                MilestoneVersion = milestone.Version,
                SnapshotAmount = milestone.Amount,
                SnapshotTitle = milestone.Title,
                SnapshotDueDate = milestone.DueDate,
                SnapshotDocumentHash = docHash,
                SnapshotDocumentPath = docPath,
                ApprovedBy = null, // Chưa có Checker phê duyệt!
                ApprovedAt = null
            };
            if (!request.IsPassed)
            {
                acceptance.Note = request.Note;
            }

            // R4-03: Đưa mốc thanh toán vào diện kiểm tra concurrency và bắt xung đột (MilestoneId, Revision)
            _unitOfWork.Repository<ContractMilestone>().Update(milestone);
            await _unitOfWork.Repository<Acceptance>().AddAsync(acceptance);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Fail(
                    "Xung đột dữ liệu đồng thời: Đã có một biên bản kiểm tra kỹ thuật khác vừa được ghi nhận hoặc mốc thanh toán đã thay đổi. Vui lòng tải lại trang.",
                    statusCode: 409);
            }
            var totalDisbursed = GetTotalDisbursedAmount(milestone.Contract);
            var totalRemaining = GetTotalRemainingAmount(milestone.Contract);
            return ApiResponse<MilestoneAcceptanceDto>.Ok(new MilestoneAcceptanceDto
            {
                Id = acceptance.Id,
                ContractId = milestone.ContractId,
                MilestoneId = milestone.Id,
                Revision = acceptance.Revision,
                TechnicalReviewedBy = userId,
                TechnicalReviewerName = reviewer?.FullName,
                TechnicalReviewedAt = acceptance.TechnicalReviewedAt,
                TechnicalNote = acceptance.TechnicalNote,
                IsTechnicalPassed = acceptance.IsTechnicalPassed,
                MilestoneVersion = acceptance.MilestoneVersion,
                SnapshotAmount = acceptance.SnapshotAmount,
                SnapshotTitle = acceptance.SnapshotTitle,
                SnapshotDueDate = acceptance.SnapshotDueDate,
                SnapshotDocumentHash = acceptance.SnapshotDocumentHash,
                SnapshotDocumentPath = acceptance.SnapshotDocumentPath,
                ApprovedBy = null,
                ApproverName = null,
                ApprovedAt = null,
                Status = acceptance.Status,
                StatusName = acceptance.Status.ToString(),
                Note = acceptance.Note,
                MilestoneStatus = milestone.Status,
                MilestoneStatusName = milestone.Status.ToString(),
                ContractStatus = milestone.Contract.Status,
                ContractStatusName = milestone.Contract.Status.ToString(),
                TotalDisbursedAmount = totalDisbursed,
                TotalRemainingAmount = totalRemaining
            }, request.IsPassed
                ? "Xác nhận kỹ thuật mốc thanh toán ĐẠT thành công. Hồ sơ đã được chuyển lên cấp có thẩm quyền phê duyệt giải ngân."
                : "Đã ghi nhận kết quả kiểm tra kỹ thuật KHÔNG ĐẠT.");
        }

        public async Task<ApiResponse<MilestoneAcceptanceDto>> ApproveMilestoneAcceptanceAsync(
            int milestoneId, ApproveMilestoneAcceptanceRequest request, int userId, bool isAdmin)
        {
            try
            {
                ContractMilestone? milestone = null;
                Acceptance? acceptance = null;
                SigningAuthority? activeAuthority = null;
                User? approver = null;

                // F05: Bao bọc toàn bộ chu trình thẩm định - phê duyệt - ghi nhận kiểm toán qua ExecuteTransactionalAsync
                var txResult = await _unitOfWork.ExecuteTransactionalAsync<MilestoneAcceptanceDto>(async () =>
                {
                    milestone = await _unitOfWork.Repository<ContractMilestone>()
                        .Query()
                        .Include(m => m.Contract)
                            .ThenInclude(c => c.BidPackage)
                        .Include(m => m.Contract)
                            .ThenInclude(c => c.Contractor)
                        .Include(m => m.Acceptances)
                            .ThenInclude(a => a.TechnicalReviewer)
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
                        return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                            "Mốc thanh toán này đã được nghiệm thu hoàn thành trước đó. Không thể thực hiện nghiệm thu lại.");
                    }

                    // R4: Quản trị viên hệ thống (Admin) TUYỆT ĐỐI KHÔNG ĐƯỢC phê duyệt giải ngân thay cho Bên mời thầu
                    if (isAdmin)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Forbidden(
                            "Quản trị viên hệ thống (Admin) không được phép phê duyệt giải ngân hợp đồng thay cho Bên mời thầu.");
                    }

                    // R4-01 & R4-02: Xác định Bên mời thầu quản lý hợp đồng trực tiếp từ BidPackage.ProcuringEntityId
                    var pe = await ResolveContractProcuringEntityAsync(milestone.Contract.BidPackage);
                    if (pe == null)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Fail(
                            "Không xác định được Bên mời thầu quản lý hợp đồng này (gói thầu chưa gắn định danh đơn vị mời thầu hợp lệ).");
                    }

                    // R3 / F01 / T02: Checker BẮT BUỘC phải là thành viên đang hoạt động (MemberStatus.Active) của Bên mời thầu
                    var currentMember = await _unitOfWork.Repository<ProcuringEntityMember>()
                        .Query()
                        .FirstOrDefaultAsync(m =>
                            m.ProcuringEntityId == pe.Id &&
                            m.UserId == userId &&
                            m.Status == MemberStatus.Active);
                    if (currentMember == null)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Forbidden(
                            "Bạn không còn là thành viên đang hoạt động của Bên mời thầu này. Quyền phê duyệt giải ngân bị từ chối.");
                    }

                    // T02: Đưa currentMember vào theo dõi cập nhật để kích hoạt kiểm tra concurrency RowVersion
                    _unitOfWork.Repository<ProcuringEntityMember>().UpdateScalar(currentMember);

                    // NÚT THẮT 1 & 2: Xác thực thẩm quyền ký duyệt độc lập (SigningAuthority)
                    var now = DateTime.UtcNow;
                    var contractScope = $"Contract:{milestone.ContractId}";
                    activeAuthority = await _unitOfWork.Repository<SigningAuthority>()
                        .Query()
                        .Include(a => a.User)
                        .Where(a =>
                            a.ProcuringEntityId == pe.Id &&
                            a.UserId == userId &&
                            a.Status == SigningAuthorityStatus.Active &&
                            a.ValidFrom <= now &&
                            a.ValidTo >= now &&
                            (a.AuthorityType == SigningAuthorityType.DisbursementApproval ||
                             a.AuthorityType == SigningAuthorityType.FullLegalRepresentative) &&
                            (a.Scope == "EntityWide" || a.Scope == contractScope))
                        .OrderByDescending(a => a.Scope == contractScope ? 1 : 0)
                        .ThenByDescending(a => a.ValidTo)
                        .FirstOrDefaultAsync();
                    if (activeAuthority == null)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Forbidden(
                            "Bạn không có thẩm quyền ký phê duyệt giải ngân hợp lệ cho hợp đồng này. Quyền phê duyệt giải ngân yêu cầu văn bản ủy quyền / quyết định bổ nhiệm đã được Quản trị viên hệ thống thẩm định và đang còn hiệu lực.");
                    }

                    // F05: Đưa activeAuthority vào theo dõi cập nhật để kích hoạt kiểm tra concurrency RowVersion
                    _unitOfWork.Repository<SigningAuthority>().UpdateScalar(activeAuthority);

                    // F02: Tìm biên bản nghiệm thu kỹ thuật gắn chính xác với AcceptanceId mà Checker đã xem xét
                    acceptance = await _unitOfWork.Repository<Acceptance>()
                        .Query()
                        .Include(a => a.TechnicalReviewer)
                        .FirstOrDefaultAsync(a => a.Id == request.AcceptanceId);
                    if (acceptance == null)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Fail(
                            "Mốc thanh toán này chưa được Cán bộ kỹ thuật kiểm tra và xác nhận đạt yêu cầu tại hiện trường. Không thể giải ngân.");
                    }

                    if (acceptance.MilestoneId != milestone.Id)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Fail(
                            "Biên bản nghiệm thu kỹ thuật không thuộc mốc thanh toán này.",
                            statusCode: 400);
                    }

                    // F02: Kiểm tra số lần thẩm tra kỳ vọng (ExpectedRevision)
                    if (acceptance.Revision != request.ExpectedRevision)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                            $"Xung đột phiên bản nghiệm thu kỹ thuật: Phiên bản thẩm tra bạn xem là r{request.ExpectedRevision}, nhưng bản ghi có r{acceptance.Revision}. Vui lòng tải lại trang.");
                    }

                    // Kiểm tra trạng thái biên bản: Nếu đã bị vô hiệu hóa do sửa mốc / thay bản scan (F04)
                    if (acceptance.Status == AcceptanceStatus.ReviewInvalidated)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                            "Biên bản nghiệm thu kỹ thuật trước đó đã bị vô hiệu hóa do mốc thanh toán hoặc hồ sơ hợp đồng đã bị thay đổi sau khi kiểm tra. Yêu cầu Cán bộ kỹ thuật kiểm tra và xác nhận lại trên phiên bản mốc hiện tại trước khi phê duyệt.");
                    }

                    // Kiểm tra trạng thái biên bản: Nếu đã có quyết định hoàn tất trước đó
                    if (acceptance.Status == AcceptanceStatus.Approved || acceptance.Status == AcceptanceStatus.Rejected)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                            "Biên bản nghiệm thu kỹ thuật này đã có quyết định phê duyệt trước đó và không thể thay đổi.");
                    }

                    // F02: Kiểm tra xem biên bản này có còn là đợt thẩm tra mới nhất chưa bị thay thế hay không
                    var latestActiveAcceptance = await _unitOfWork.Repository<Acceptance>()
                        .Query()
                        .AsNoTracking()
                        .Where(a => a.MilestoneId == milestone.Id && a.Status != AcceptanceStatus.ReviewInvalidated)
                        .OrderByDescending(a => a.Revision)
                        .ThenByDescending(a => a.Id)
                        .FirstOrDefaultAsync();
                    if (latestActiveAcceptance == null || latestActiveAcceptance.Id != acceptance.Id)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                            $"Biên bản thẩm tra kỹ thuật (r{acceptance.Revision}) đã bị thay thế bởi đợt thẩm tra kỹ thuật mới hơn (r{latestActiveAcceptance?.Revision}). Vui lòng tải lại trang và xem xét đợt thẩm tra mới nhất trước khi quyết định.");
                    }

                    // T05: CÁC ĐIỀU KIỆN RÀNG BUỘC CHUNG CHO CẢ PHÊ DUYỆT VÀ TỪ CHỐI (MAKER-CHECKER & STALE VERSION)
                    if (acceptance.TechnicalReviewedBy.HasValue && acceptance.TechnicalReviewedBy.Value == userId)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Fail(
                            "Vi phạm nguyên tắc kiểm soát chéo (Maker - Checker): Cán bộ đã xác nhận kỹ thuật hiện trường không được phép tự phê duyệt giải ngân hoặc từ chối giải ngân. Thao tác này phải do Lãnh đạo hoặc Người có thẩm quyền khác thực hiện.");
                    }

                    if (request.ExpectedVersion != acceptance.MilestoneVersion)
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                            $"Xung đột phiên bản mốc thanh toán: Phiên bản bạn đang thao tác là v{request.ExpectedVersion}, nhưng phiên bản nghiệm thu kỹ thuật hiện tại là v{acceptance.MilestoneVersion}. Vui lòng tải lại trang để xem thông tin mới nhất.");
                    }

                    if (acceptance.MilestoneVersion != milestone.Version ||
                        acceptance.SnapshotAmount != milestone.Amount ||
                        acceptance.SnapshotTitle != milestone.Title ||
                        (acceptance.SnapshotDueDate.HasValue && acceptance.SnapshotDueDate.Value != milestone.DueDate))
                    {
                        return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                            "Phát hiện sai lệch giữa dữ liệu nghiệm thu kỹ thuật (Snapshot) và thông tin mốc hiện tại. Mốc thanh toán đã bị thay đổi sau khi kiểm tra kỹ thuật. Yêu cầu kiểm tra kỹ thuật lại.");
                    }

                    if (!string.IsNullOrWhiteSpace(acceptance.SnapshotDocumentHash))
                    {
                        var expectedHash = acceptance.SnapshotDocumentHash.Trim().ToLowerInvariant();

                        string? currentContractDocHash = null;
                        if (!string.IsNullOrWhiteSpace(milestone.Contract.ScannedFilePath))
                        {
                            currentContractDocHash = await ComputeFileHashSafeAsync(milestone.Contract.ScannedFilePath);
                        }

                        string? snapshotFileDocHash = null;
                        if (!string.IsNullOrWhiteSpace(acceptance.SnapshotDocumentPath))
                        {
                            snapshotFileDocHash = await ComputeFileHashSafeAsync(acceptance.SnapshotDocumentPath);
                        }

                        if (!string.IsNullOrEmpty(currentContractDocHash) &&
                            !string.Equals(currentContractDocHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                        {
                            return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                                "Tệp hồ sơ scan hợp đồng hiện hành đã bị thay đổi sau khi Cán bộ kỹ thuật kiểm tra hiện trường (mã băm tài liệu không khớp). Yêu cầu kiểm tra kỹ thuật lại bản scan mới nhất.");
                        }

                        if (!string.IsNullOrEmpty(snapshotFileDocHash) &&
                            !string.Equals(snapshotFileDocHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                        {
                            return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                                "Tệp snapshot nghiệm thu đã bị thay đổi sau khi Cán bộ kỹ thuật kiểm tra hiện trường (mã băm tài liệu không khớp). Yêu cầu kiểm tra kỹ thuật lại.");
                        }

                        if (!string.Equals(milestone.Contract.ScannedFilePath?.Trim(), acceptance.SnapshotDocumentPath?.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                                "Đường dẫn tệp scan hợp đồng đã thay đổi so với biên bản kiểm tra kỹ thuật ban đầu. Yêu cầu kiểm tra kỹ thuật lại bản scan mới.");
                        }
                    }

                    if (request.IsApproved)
                    {
                        if (acceptance.Status != AcceptanceStatus.TechnicalApproved || acceptance.IsTechnicalPassed != true || !acceptance.TechnicalReviewedBy.HasValue)
                        {
                            return ApiResponse<MilestoneAcceptanceDto>.Fail(
                                "Mốc thanh toán này chưa được Cán bộ kỹ thuật kiểm tra và xác nhận đạt yêu cầu tại hiện trường. Không thể giải ngân.");
                        }

                        milestone.Status = MilestoneStatus.Completed;
                        milestone.Version++;
                        _unitOfWork.Repository<ContractMilestone>().Update(milestone);
                        var contractMilestones = await _unitOfWork.Repository<ContractMilestone>()
                            .Query()
                            .Where(m => m.ContractId == milestone.ContractId)
                            .ToListAsync();
                        var totalCompletedAmount = contractMilestones
                            .Where(m => m.Status == MilestoneStatus.Completed)
                            .Sum(m => m.Amount);
                        if (contractMilestones.Count > 0 &&
                            contractMilestones.All(m => m.Status == MilestoneStatus.Completed) &&
                            totalCompletedAmount >= milestone.Contract.Value)
                        {
                            milestone.Contract.Status = ContractStatus.Completed;
                            milestone.Contract.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.Repository<Contract>().Update(milestone.Contract);
                        }
                    }

                    approver = await _unitOfWork.Repository<User>()
                        .Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == userId);
                    acceptance.ApprovedBy = userId;
                    acceptance.ApprovedAt = DateTime.UtcNow;
                    acceptance.Status = request.IsApproved ? AcceptanceStatus.Approved : AcceptanceStatus.Rejected;
                    acceptance.Note = request.Note;
                    acceptance.SigningAuthorityId = activeAuthority.Id;
                    _unitOfWork.Repository<Acceptance>().Update(acceptance);

                    await _unitOfWork.SaveChangesAsync();

                    var decisionAuditLog = new AuditLog
                    {
                        UserId = userId,
                        Action = request.IsApproved ? "APPROVE_MILESTONE_ACCEPTANCE" : "REJECT_MILESTONE_ACCEPTANCE",
                        EntityType = "Acceptance",
                        EntityId = acceptance.Id,
                        NewValues = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            MilestoneId = milestone.Id,
                            ContractId = milestone.ContractId,
                            Revision = acceptance.Revision,
                            MilestoneVersion = milestone.Version,
                            Decision = request.IsApproved ? "Approved" : "Rejected",
                            SigningAuthorityId = activeAuthority.Id,
                            SigningAuthorityDecisionNumber = activeAuthority.DecisionNumber,
                            ProcuringEntityMemberId = currentMember.Id,
                            Note = request.Note,
                            Timestamp = DateTime.UtcNow
                        }),
                        Timestamp = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<AuditLog>().AddAsync(decisionAuditLog);
                    await _unitOfWork.SaveChangesAsync();

                    return ApiResponse<MilestoneAcceptanceDto>.Ok(null!);
                }, System.Data.IsolationLevel.ReadCommitted);

                if (!txResult.Success)
                {
                    return txResult;
                }

                // Gửi thông báo cho Nhà thầu về kết quả nghiệm thu mốc SAU KHI TRANSACTION ĐÃ COMMIT
                try
                {
                    if (milestone!.Contract?.Contractor?.UserId > 0)
                    {
                        var notifTitle = request.IsApproved
                            ? "Nghiệm thu giai đoạn hợp đồng thành công"
                            : "Nghiệm thu giai đoạn hợp đồng bị từ chối";
                        var rejectReason = !request.IsApproved && !string.IsNullOrWhiteSpace(request.Note)
                            ? $" - Lý do từ chối: {request.Note}"
                            : string.Empty;
                        var notifMsg = request.IsApproved
                            ? $"Mốc thực hiện '{milestone.Title}' thuộc hợp đồng số {milestone.Contract.ContractNumber} đã được phê duyệt nghiệm thu."
                            : $"Mốc thực hiện '{milestone.Title}' thuộc hợp đồng số {milestone.Contract.ContractNumber} đã bị từ chối nghiệm thu.{rejectReason}";
                        var notifType = request.IsApproved ? NotificationType.Info : NotificationType.Warning;

                        await _notificationService.CreateNotificationAsync(
                            milestone.Contract.Contractor.UserId,
                            notifTitle,
                            notifMsg,
                            notifType,
                            sendEmail: true);
                    }
                }
                catch
                {
                    // Bọc try-catch riêng biệt để không ảnh hưởng đến kết quả nghiệm thu
                }

                if ((!request.IsApproved || milestone!.Contract?.Status == ContractStatus.Completed) && milestone.Contract != null)
                {
                    await _contractorService.RecalculateContractorRatingAsync(milestone.Contract.ContractorId);
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
                    Id = acceptance!.Id,
                    ContractId = acceptance.ContractId,
                    MilestoneId = acceptance.MilestoneId,
                    Revision = acceptance.Revision,
                    TechnicalReviewedBy = acceptance.TechnicalReviewedBy,
                    TechnicalReviewerName = acceptance.TechnicalReviewer?.FullName,
                    TechnicalReviewedAt = acceptance.TechnicalReviewedAt,
                    TechnicalNote = acceptance.TechnicalNote,
                    IsTechnicalPassed = acceptance.IsTechnicalPassed,
                    MilestoneVersion = acceptance.MilestoneVersion,
                    SnapshotAmount = acceptance.SnapshotAmount,
                    SnapshotTitle = acceptance.SnapshotTitle,
                    SnapshotDueDate = acceptance.SnapshotDueDate,
                    SnapshotDocumentHash = acceptance.SnapshotDocumentHash,
                    SnapshotDocumentPath = acceptance.SnapshotDocumentPath,
                    SigningAuthorityId = acceptance.SigningAuthorityId,
                    SigningAuthorityDecisionNumber = activeAuthority!.DecisionNumber,
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
            catch (DbUpdateConcurrencyException)
            {
                return ApiResponse<MilestoneAcceptanceDto>.Conflict(
                    "Xung đột dữ liệu đồng thời: Mốc thanh toán hoặc quyền hạn liên quan đã bị thay đổi bởi giao dịch khác. Vui lòng tải lại trang.");
            }
        }

        /// <summary>
        /// T01: Tải tệp tài liệu snapshot nghiệm thu kỹ thuật an toàn (kiểm soát chặt chẽ phạm vi đơn vị và nhà thầu sở hữu)
        /// </summary>
        public async Task<ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>> GetAcceptanceSnapshotDocumentDownloadAsync(
            int milestoneId, int acceptanceId, int userId, bool isAdmin, bool isProcurement, bool isContractor)
        {
            var acceptance = await _unitOfWork.Repository<Acceptance>()
                .Query()
                .Include(a => a.Contract)
                    .ThenInclude(c => c.Contractor)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.BidPackage)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == acceptanceId && a.MilestoneId == milestoneId);

            if (acceptance == null)
            {
                return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Fail(
                    "Không tìm thấy biên bản nghiệm thu kỹ thuật.", statusCode: 404);
            }

            if (string.IsNullOrWhiteSpace(acceptance.SnapshotDocumentPath))
            {
                return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Fail(
                    "Biên bản nghiệm thu này không có tài liệu snapshot đính kèm.", statusCode: 404);
            }

            // Authorization policy:
            // 1. Platform Admin: có quyền xem/audit trên toàn hệ thống
            if (isAdmin)
            {
                // Cho phép Admin đọc kiểm toán
            }
            // 2. Contractor: chỉ được phép tải snapshot của chính hợp đồng mình trúng thầu
            else if (isContractor)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (contractor == null || contractor.Id != acceptance.Contract.ContractorId)
                {
                    return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Forbidden(
                        "Bạn không có quyền tải tài liệu snapshot của nghiệm thu này. Bạn không phải là nhà thầu sở hữu hợp đồng.");
                }
            }
            // 3. Procurement: BẮT BUỘC phải là thành viên đang hoạt động (MemberStatus.Active) của Bên mời thầu quản lý hợp đồng này
            else if (isProcurement)
            {
                // R4-01: Xác định Bên mời thầu quản lý hợp đồng trực tiếp từ BidPackage.ProcuringEntityId
                var pe = await ResolveContractProcuringEntityAsync(acceptance.Contract?.BidPackage);
                if (pe == null)
                {
                    return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Forbidden(
                        "Không xác định được Bên mời thầu quản lý hợp đồng này.");
                }

                var isEntityMember = await _unitOfWork.Repository<ProcuringEntityMember>()
                    .Query()
                    .AsNoTracking()
                    .AnyAsync(m =>
                        m.ProcuringEntityId == pe.Id &&
                        m.UserId == userId &&
                        m.Status == MemberStatus.Active);

                if (!isEntityMember)
                {
                    return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Forbidden(
                        "Bạn không có quyền tải tài liệu snapshot của nghiệm thu này. Bạn không phải là thành viên đang hoạt động của Bên mời thầu quản lý hợp đồng.");
                }
            }
            else
            {
                return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Forbidden(
                    "Bạn không có quyền truy cập tài liệu snapshot của nghiệm thu này.");
            }

            if (!string.IsNullOrWhiteSpace(acceptance.SnapshotDocumentHash))
            {
                var actualHash = await ComputeFileHashSafeAsync(acceptance.SnapshotDocumentPath);
                if (actualHash != null && !string.Equals(actualHash, acceptance.SnapshotDocumentHash.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Fail(
                        "Tệp tài liệu snapshot trên máy chủ không khớp với mã băm SHA256 đã lưu trữ lúc nghiệm thu kỹ thuật.",
                        statusCode: 409);
                }
            }

            var fileName = $"NghiemThu_Moc{milestoneId}_Rev{acceptance.Revision}.pdf";
            ProcurementSystem.Core.DTOs.Common.FileDownloadResult downloadResult;
            try
            {
                downloadResult = await _fileStorageService.ResolveDownloadAsync(acceptance.SnapshotDocumentPath, fileName, "application/pdf");
            }
            catch (Exception ex)
            {
                return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Fail($"Đường dẫn tệp không hợp lệ: {ex.Message}");
            }

            return ApiResponse<ProcurementSystem.Core.DTOs.Common.FileDownloadResult>.Ok(downloadResult);
        }
    }
}
