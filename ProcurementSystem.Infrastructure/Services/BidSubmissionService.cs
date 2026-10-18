using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidSubmission;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class BidSubmissionService : IBidSubmissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly INotificationService _notificationService;

        public BidSubmissionService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<BidSubmissionDto>> SubmitBidAsync(
            int bidPackageId, int userId, decimal bidPrice, List<IFormFile> files, List<SubmissionFileType> fileTypes)
        {
            // 1. Kiểm tra gói thầu tồn tại
            var bidPackage = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == bidPackageId);

            if (bidPackage == null)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Gói thầu không tồn tại.");
            }

            // 2. Kiểm tra trạng thái gói thầu phải đang Open
            if (bidPackage.Status != BidPackageStatus.Open)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Gói thầu không ở trạng thái mở thầu. Không thể nộp hồ sơ.");
            }

            // 3. Kiểm tra hạn chót (Deadline)
            if (DateTime.UtcNow > bidPackage.Deadline)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Đã hết thời hạn nộp hồ sơ dự thầu.");
            }

            // 3.1 Kiểm tra tính hợp lệ của Giá dự thầu (Không được vượt giá gói thầu)
            if (bidPrice <= 0)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Giá dự thầu phải lớn hơn 0.");
            }

            if (bidPrice > bidPackage.Budget)
            {
                return ApiResponse<BidSubmissionDto>.Fail(
                    $"Giá dự thầu ({bidPrice:N0} VNĐ) không được vượt quá Ngân sách dự toán của gói thầu ({bidPackage.Budget:N0} VNĐ) theo quy định.");
            }

            // 4. Kiểm tra tư cách nhà thầu
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (contractor == null)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Tài khoản của bạn chưa liên kết với hồ sơ nhà thầu.");
            }

            if (contractor.VerificationStatus != "Approved")
            {
                var reasonMessage = contractor.VerificationStatus == "Pending"
                    ? "Tài khoản doanh nghiệp của bạn đang trong trạng thái chờ Quản trị viên (Admin) thẩm định Giấy phép kinh doanh. Vui lòng liên hệ Quản trị viên hệ thống hoặc quay lại sau khi tài khoản được kích hoạt."
                    : "Hồ sơ đăng ký doanh nghiệp của bạn đã bị từ chối phê duyệt. Vui lòng liên hệ Quản trị viên hệ thống để được hỗ trợ.";
                return ApiResponse<BidSubmissionDto>.Fail(reasonMessage);
            }

            // 5. Kiểm tra nộp trùng hoặc nộp lại (P2-4)
            var existingSubmission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.SubmissionFiles)
                .FirstOrDefaultAsync(bs => bs.BidPackageId == bidPackageId && bs.ContractorId == contractor.Id);

            if (existingSubmission != null && existingSubmission.Status != BidSubmissionStatus.Withdrawn)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Bạn đã nộp hồ sơ dự thầu cho gói thầu này rồi.");
            }

            // 6. Kiểm tra file đính kèm
            if (files == null || files.Count == 0)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Phải đính kèm ít nhất 1 file hồ sơ dự thầu.");
            }

            if (files.Count != fileTypes.Count)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Số lượng file và số lượng loại file phải khớp nhau.");
            }

            // P2-5: Kiểm tra bắt buộc phải có tối thiểu Báo giá (Quotation) và Hồ sơ năng lực (Capability)
            if (!fileTypes.Contains(SubmissionFileType.Quotation) || !fileTypes.Contains(SubmissionFileType.Capability))
            {
                return ApiResponse<BidSubmissionDto>.Fail(
                    "Hồ sơ dự thầu bắt buộc phải đính kèm tối thiểu: Báo giá tài chính (Quotation) và Hồ sơ năng lực (Capability).");
            }

            // 7. Tạo hoặc cập nhật hồ sơ dự thầu (hỗ trợ nộp lại sau khi đã rút thầu Withdrawn)
            var newlyUploadedFiles = new List<string>();
            List<string>? oldFilePaths = null;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                BidSubmission submission;
                if (existingSubmission != null)
                {
                    submission = existingSubmission;
                    submission.BidPrice = bidPrice;
                    submission.SubmittedAt = DateTime.UtcNow;
                    submission.Status = BidSubmissionStatus.Submitted;
                    submission.TotalScore = null;
                    submission.Rank = null;
                    _unitOfWork.Repository<BidSubmission>().Update(submission);

                    // Thu thập danh sách file cũ để xóa vật lý sau khi commit thành công
                    oldFilePaths = submission.SubmissionFiles
                        .Select(f => f.FilePath)
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();

                    // Xóa các liên kết file cũ trong cơ sở dữ liệu
                    foreach (var oldFile in submission.SubmissionFiles.ToList())
                    {
                        _unitOfWork.Repository<SubmissionFile>().Delete(oldFile);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    submission = new BidSubmission
                    {
                        BidPackageId = bidPackageId,
                        ContractorId = contractor.Id,
                        SubmittedAt = DateTime.UtcNow,
                        BidPrice = bidPrice,
                        Status = BidSubmissionStatus.Submitted
                    };

                    await _unitOfWork.Repository<BidSubmission>().AddAsync(submission);
                    await _unitOfWork.SaveChangesAsync();
                }

                // 8. Lưu các file đính kèm
                var submissionFiles = new List<SubmissionFile>();
                for (int i = 0; i < files.Count; i++)
                {
                    var filePath = await _fileStorageService.SaveFileAsync(
                        files[i], $"submissions/{submission.Id}");
                    newlyUploadedFiles.Add(filePath);

                    var submissionFile = new SubmissionFile
                    {
                        BidSubmissionId = submission.Id,
                        FileType = fileTypes[i],
                        FileName = files[i].FileName,
                        FilePath = filePath,
                        UploadedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Repository<SubmissionFile>().AddAsync(submissionFile);
                    submissionFiles.Add(submissionFile);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Dọn dẹp các file vật lý cũ sau khi transaction đã commit thành công
                if (oldFilePaths != null && oldFilePaths.Count > 0)
                {
                    foreach (var oldPath in oldFilePaths)
                    {
                        try
                        {
                            _fileStorageService.DeleteFile(oldPath);
                        }
                        catch
                        {
                            // Bỏ qua lỗi xóa file cũ để không làm gián đoạn phản hồi
                        }
                    }
                }

                // Gửi thông báo In-app & Email cho Nhà thầu và Bên mời thầu (bọc try-catch độc lập)
                try
                {
                    // 1. Thông báo cho Nhà thầu nộp thầu
                    await _notificationService.CreateNotificationAsync(
                        contractor.UserId,
                        "Xác nhận nộp E-HSDT thành công",
                        $"Hồ sơ dự thầu cho gói thầu {bidPackage.Code} - {bidPackage.Name} đã được nộp và niêm phong an toàn vào lúc {DateTime.UtcNow:HH:mm dd/MM/yyyy} (UTC).",
                        ProcurementSystem.Core.Enums.NotificationType.Info,
                        sendEmail: true);

                    // 2. Thông báo cho Bên mời thầu sở hữu gói thầu (Bảo vệ Sealed Bid - KHÔNG làm lộ danh tính nhà thầu)
                    if (bidPackage.CreatedBy > 0)
                    {
                        await _notificationService.CreateNotificationAsync(
                            bidPackage.CreatedBy,
                            "Hồ sơ dự thầu mới đã nộp",
                            $"Gói thầu {bidPackage.Code} - {bidPackage.Name} vừa tiếp nhận 01 hồ sơ dự thầu mới đã được niêm phong an toàn.",
                            ProcurementSystem.Core.Enums.NotificationType.Info,
                            sendEmail: true);
                    }
                }
                catch
                {
                    // Bọc try-catch riêng biệt để sự cố thông báo không ảnh hưởng kết quả nộp thầu
                }

                // 9. Trả về kết quả
                return ApiResponse<BidSubmissionDto>.Ok(new BidSubmissionDto
                {
                    Id = submission.Id,
                    BidPackageId = bidPackage.Id,
                    BidPackageCode = bidPackage.Code,
                    BidPackageName = bidPackage.Name,
                    ContractorId = contractor.Id,
                    CompanyName = contractor.CompanyName,
                    TaxCode = contractor.TaxCode,
                    SubmittedAt = submission.SubmittedAt,
                    BidPrice = submission.BidPrice,
                    TotalScore = submission.TotalScore,
                    Rank = submission.Rank,
                    Status = submission.Status,
                    Files = submissionFiles.Select(MapFileToDto).ToList()
                }, "Nộp hồ sơ dự thầu thành công.");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();

                // Xóa các file mới tải lên trong mẻ này để chống file mồ côi
                foreach (var newPath in newlyUploadedFiles)
                {
                    try
                    {
                        _fileStorageService.DeleteFile(newPath);
                    }
                    catch
                    {
                        // Bỏ qua lỗi xóa dọn dẹp
                    }
                }

                throw;
            }
        }

        public async Task<ApiResponse<BidSubmissionDto>> GetSubmissionByIdAsync(
            int id, int userId, IList<string> userRoles)
        {
            var submission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(bs => bs.BidPackage)
                .Include(bs => bs.Contractor)
                .Include(bs => bs.SubmissionFiles)
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.Id == id);

            if (submission == null)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Không tìm thấy hồ sơ dự thầu.");
            }

            var isAdmin = userRoles.Contains("Admin");
            var isProcurement = userRoles.Contains("Procurement");
            var isEvaluator = userRoles.Contains("Evaluator");
            var isInternalStaff = isAdmin || isProcurement || isEvaluator;

            // Kiểm tra tư cách nhà thầu sở hữu hồ sơ
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);
            var isOwner = contractor != null && contractor.Id == submission.ContractorId;

            // Bảo mật Sealed-Bid: Căn cứ Điều 16 Luật Đấu thầu 2023, hồ sơ phải được niêm phong khi gói thầu còn Open
            if (submission.BidPackage.Status == BidPackageStatus.Open)
            {
                if (isInternalStaff)
                {
                    return ApiResponse<BidSubmissionDto>.Fail(
                        "Căn cứ Điều 16 Luật Đấu thầu 2023: Hồ sơ dự thầu đang trong thời hạn niêm phong bảo mật. Nhân sự nội bộ không được phép mở xem hồ sơ trước thời điểm đóng thầu.");
                }

                if (!isOwner)
                {
                    return ApiResponse<BidSubmissionDto>.Forbidden("Bạn không có quyền xem hồ sơ dự thầu này.");
                }

                return ApiResponse<BidSubmissionDto>.Ok(MapToDto(submission));
            }

            // Gói thầu đã đóng/chấm điểm: Kiểm soát IDOR chi tiết theo quyền hạn
            var hasAccess = false;
            if (isAdmin)
            {
                hasAccess = true;
            }
            else if (isProcurement && submission.BidPackage.CreatedBy == userId)
            {
                // Bên mời thầu phụ trách gói thầu này
                hasAccess = true;
            }
            else if (isEvaluator)
            {
                // Chỉ chuyên gia được phân công vào tổ chuyên gia của gói thầu
                var isAssigned = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .ExistsAsync(pe => pe.BidPackageId == submission.BidPackageId && pe.EvaluatorId == userId);
                if (isAssigned)
                {
                    hasAccess = true;
                }
            }
            else if (isOwner)
            {
                hasAccess = true;
            }

            if (!hasAccess)
            {
                return ApiResponse<BidSubmissionDto>.Forbidden("Bạn không có quyền xem hồ sơ dự thầu này.");
            }

            var dto = MapToDto(submission);

            // Bảo mật điểm số chấm thầu:
            // Khi gói thầu đang Evaluating: Ẩn TotalScore và Rank tạm thời với Evaluator và Contractor
            if (submission.BidPackage.Status == BidPackageStatus.Evaluating)
            {
                if (!isAdmin && !(isProcurement && submission.BidPackage.CreatedBy == userId))
                {
                    dto.TotalScore = null;
                    dto.Rank = null;
                }
            }
            else if (submission.BidPackage.Status == BidPackageStatus.Closed || submission.BidPackage.Status == BidPackageStatus.Open)
            {
                dto.TotalScore = null;
                dto.Rank = null;
            }

            return ApiResponse<BidSubmissionDto>.Ok(dto);
        }

        public async Task<ApiResponse<List<BidSubmissionSummaryDto>>> GetSubmissionsByPackageAsync(
            int bidPackageId, int userId, IList<string> userRoles)
        {
            // Kiểm tra gói thầu tồn tại
            var bidPackage = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == bidPackageId);

            if (bidPackage == null)
            {
                return ApiResponse<List<BidSubmissionSummaryDto>>.Fail("Gói thầu không tồn tại.");
            }

            var isAdmin = userRoles.Contains("Admin");
            var isProcurement = userRoles.Contains("Procurement");
            var isEvaluator = userRoles.Contains("Evaluator");
            var isInternalStaff = isAdmin || isProcurement || isEvaluator;

            // Bảo mật giá thầu: nhân sự nội bộ chỉ xem khi gói thầu đã Closed/Evaluating/Contracted
            if (isInternalStaff && bidPackage.Status == BidPackageStatus.Open)
            {
                return ApiResponse<List<BidSubmissionSummaryDto>>.Fail(
                    "Không thể xem danh sách hồ sơ dự thầu khi gói thầu còn đang mở. Vui lòng đóng thầu trước.");
            }

            // Kiểm soát IDOR: Kiểm tra quyền xem danh sách hồ sơ của gói thầu
            var hasAccess = false;
            if (isAdmin)
            {
                hasAccess = true;
            }
            else if (isProcurement && bidPackage.CreatedBy == userId)
            {
                hasAccess = true;
            }
            else if (isEvaluator)
            {
                var isAssigned = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .ExistsAsync(pe => pe.BidPackageId == bidPackageId && pe.EvaluatorId == userId);
                if (isAssigned)
                {
                    hasAccess = true;
                }
            }
            else if (!isInternalStaff)
            {
                // Nhà thầu chỉ xem hồ sơ của chính mình
                hasAccess = true;
            }

            if (!hasAccess)
            {
                return ApiResponse<List<BidSubmissionSummaryDto>>.Forbidden(
                    "Bạn không có quyền xem danh sách hồ sơ của gói thầu này.");
            }

            var query = _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(bs => bs.BidPackage)
                .Include(bs => bs.Contractor)
                .Include(bs => bs.SubmissionFiles)
                .Where(bs => bs.BidPackageId == bidPackageId)
                .AsNoTracking();

            // Nếu là nhà thầu, chỉ xem hồ sơ của chính mình
            if (!isInternalStaff)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (contractor == null)
                {
                    return ApiResponse<List<BidSubmissionSummaryDto>>.Fail("Không tìm thấy hồ sơ nhà thầu.");
                }

                query = query.Where(bs => bs.ContractorId == contractor.Id);
            }

            var submissions = await query
                .OrderByDescending(bs => bs.SubmittedAt)
                .ToListAsync();

            var result = submissions.Select(MapToSummaryDto).ToList();

            // Bảo mật điểm số chấm thầu:
            // Khi gói thầu đang Evaluating: Ẩn TotalScore và Rank tạm thời với Evaluator và Contractor
            if (bidPackage.Status == BidPackageStatus.Evaluating)
            {
                if (!isAdmin && !(isProcurement && bidPackage.CreatedBy == userId))
                {
                    foreach (var item in result)
                    {
                        item.TotalScore = null;
                        item.Rank = null;
                    }
                }
            }
            else if (bidPackage.Status == BidPackageStatus.Closed || bidPackage.Status == BidPackageStatus.Open)
            {
                foreach (var item in result)
                {
                    item.TotalScore = null;
                    item.Rank = null;
                }
            }

            return ApiResponse<List<BidSubmissionSummaryDto>>.Ok(result);
        }

        public async Task<ApiResponse<PaginatedList<BidSubmissionSummaryDto>>> GetMySubmissionsAsync(
            int userId, int pageNumber, int pageSize)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (contractor == null)
            {
                return ApiResponse<PaginatedList<BidSubmissionSummaryDto>>.Fail(
                    "Không tìm thấy hồ sơ nhà thầu liên kết với tài khoản này.");
            }

            var query = _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(bs => bs.BidPackage)
                .Include(bs => bs.Contractor)
                .Include(bs => bs.SubmissionFiles)
                .Where(bs => bs.ContractorId == contractor.Id)
                .OrderByDescending(bs => bs.SubmittedAt)
                .AsNoTracking()
                .Select(bs => new BidSubmissionSummaryDto
                {
                    Id = bs.Id,
                    BidPackageId = bs.BidPackageId,
                    BidPackageCode = bs.BidPackage.Code,
                    BidPackageName = bs.BidPackage.Name,
                    ContractorId = bs.ContractorId,
                    CompanyName = bs.Contractor.CompanyName,
                    SubmittedAt = bs.SubmittedAt,
                    BidPrice = bs.BidPrice,
                    TotalScore = (bs.BidPackage.Status == BidPackageStatus.Awarded || bs.BidPackage.Status == BidPackageStatus.Contracted) ? bs.TotalScore : null,
                    Rank = (bs.BidPackage.Status == BidPackageStatus.Awarded || bs.BidPackage.Status == BidPackageStatus.Contracted) ? bs.Rank : null,
                    Status = bs.Status,
                    FileCount = bs.SubmissionFiles.Count
                });

            var paginatedResult = await PaginatedList<BidSubmissionSummaryDto>.CreateAsync(
                query, pageNumber, pageSize);

            return ApiResponse<PaginatedList<BidSubmissionSummaryDto>>.Ok(paginatedResult);
        }

        public async Task<ApiResponse<bool>> WithdrawSubmissionAsync(int submissionId, int userId)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (contractor == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy hồ sơ nhà thầu.");
            }

            var submission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(bs => bs.BidPackage)
                .Include(bs => bs.SubmissionFiles)
                .FirstOrDefaultAsync(bs => bs.Id == submissionId);

            if (submission == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy hồ sơ dự thầu.");
            }

            // Kiểm tra quyền sở hữu
            if (submission.ContractorId != contractor.Id)
            {
                return ApiResponse<bool>.Forbidden("Bạn không có quyền rút hồ sơ dự thầu này.");
            }

            // Kiểm tra gói thầu còn Open và chưa hết hạn
            if (submission.BidPackage.Status != BidPackageStatus.Open)
            {
                return ApiResponse<bool>.Fail("Không thể rút hồ sơ khi gói thầu đã đóng thầu.");
            }

            if (DateTime.UtcNow > submission.BidPackage.Deadline)
            {
                return ApiResponse<bool>.Fail("Đã hết thời hạn. Không thể rút hồ sơ dự thầu.");
            }

            // N25 & R4-6: Chỉ cho phép rút hồ sơ dự thầu khi hồ sơ đang ở trạng thái hợp lệ duy nhất là Submitted.
            // Chặn triệt để rút hồ sơ đã bị Disqualified (hoặc Evaluated, Selected, Rejected, Withdrawn) để xóa dấu vết kỷ luật.
            if (submission.Status != BidSubmissionStatus.Submitted)
            {
                return ApiResponse<bool>.Fail("Chỉ có thể rút hồ sơ dự thầu khi hồ sơ đang ở trạng thái Đã nộp (Submitted).");
            }

            // P2-4: Chuyển sang Soft Delete để bảo toàn chứng cứ kiểm toán đấu thầu
            submission.Status = BidSubmissionStatus.Withdrawn;
            _unitOfWork.Repository<BidSubmission>().Update(submission);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Rút hồ sơ dự thầu thành công. Bạn có thể nộp lại hồ sơ mới trước thời điểm đóng thầu.");
        }

        private static BidSubmissionDto MapToDto(BidSubmission submission)
        {
            return new BidSubmissionDto
            {
                Id = submission.Id,
                BidPackageId = submission.BidPackageId,
                BidPackageCode = submission.BidPackage?.Code ?? string.Empty,
                BidPackageName = submission.BidPackage?.Name ?? string.Empty,
                ContractorId = submission.ContractorId,
                CompanyName = submission.Contractor?.CompanyName ?? string.Empty,
                TaxCode = submission.Contractor?.TaxCode,
                SubmittedAt = submission.SubmittedAt,
                BidPrice = submission.BidPrice,
                TotalScore = submission.TotalScore,
                Rank = submission.Rank,
                Status = submission.Status,
                SelectionReason = submission.SelectionReason,
                DisqualificationReason = submission.DisqualificationReason,
                Files = submission.SubmissionFiles?.Select(MapFileToDto).ToList() ?? new()
            };
        }

        private static BidSubmissionSummaryDto MapToSummaryDto(BidSubmission submission)
        {
            return new BidSubmissionSummaryDto
            {
                Id = submission.Id,
                BidPackageId = submission.BidPackageId,
                BidPackageCode = submission.BidPackage?.Code ?? string.Empty,
                BidPackageName = submission.BidPackage?.Name ?? string.Empty,
                ContractorId = submission.ContractorId,
                CompanyName = submission.Contractor?.CompanyName ?? string.Empty,
                SubmittedAt = submission.SubmittedAt,
                BidPrice = submission.BidPrice,
                TotalScore = submission.TotalScore,
                Rank = submission.Rank,
                Status = submission.Status,
                SelectionReason = submission.SelectionReason,
                DisqualificationReason = submission.DisqualificationReason,
                FileCount = submission.SubmissionFiles?.Count ?? 0
            };
        }

        private static SubmissionFileDto MapFileToDto(SubmissionFile file)
        {
            return new SubmissionFileDto
            {
                Id = file.Id,
                FileType = file.FileType,
                FileTypeName = file.FileType.ToString(),
                FileName = file.FileName,
                FilePath = file.FilePath,
                UploadedAt = file.UploadedAt
            };
        }

        public async Task<ApiResponse<SubmissionFileDownloadDto>> GetSubmissionFileForDownloadAsync(
            int fileId, int userId, IList<string> userRoles)
        {
            var file = await _unitOfWork.Repository<SubmissionFile>()
                .Query()
                .Include(f => f.BidSubmission)
                .ThenInclude(bs => bs.BidPackage)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null)
            {
                return ApiResponse<SubmissionFileDownloadDto>.Fail("Không tìm thấy tệp tài liệu đính kèm.");
            }

            var isAdmin = userRoles.Contains("Admin");
            var isProcurement = userRoles.Contains("Procurement");
            var isEvaluator = userRoles.Contains("Evaluator");
            var isInternalStaff = isAdmin || isProcurement || isEvaluator;

            // Kiểm tra tư cách nhà thầu sở hữu hồ sơ
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);
            var isOwner = contractor != null && contractor.Id == file.BidSubmission.ContractorId;

            // Bảo mật Sealed-Bid: Căn cứ Điều 16 Luật Đấu thầu 2023, cấm tải tệp hồ sơ khi gói còn Open
            if (file.BidSubmission.BidPackage.Status == BidPackageStatus.Open)
            {
                if (isInternalStaff)
                {
                    return ApiResponse<SubmissionFileDownloadDto>.Fail(
                        "Căn cứ Điều 16 Luật Đấu thầu 2023: Không thể tải tệp đề xuất dự thầu khi gói thầu đang trong giai đoạn tiếp nhận hồ sơ (chưa đóng thầu).");
                }

                if (!isOwner)
                {
                    return ApiResponse<SubmissionFileDownloadDto>.Forbidden(
                        "Bạn không có quyền tải tệp tài liệu này.");
                }
            }
            else
            {
                // Gói thầu đã đóng/chấm điểm: Kiểm soát IDOR chi tiết
                var hasAccess = false;
                if (isAdmin)
                {
                    hasAccess = true;
                }
                else if (isProcurement && file.BidSubmission.BidPackage.CreatedBy == userId)
                {
                    hasAccess = true;
                }
                else if (isEvaluator)
                {
                    var isAssigned = await _unitOfWork.Repository<BidPackageEvaluator>()
                        .ExistsAsync(pe => pe.BidPackageId == file.BidSubmission.BidPackageId && pe.EvaluatorId == userId);
                    if (isAssigned)
                    {
                        hasAccess = true;
                    }
                }
                else if (isOwner)
                {
                    hasAccess = true;
                }

                if (!hasAccess)
                {
                    return ApiResponse<SubmissionFileDownloadDto>.Forbidden(
                        "Bạn không có quyền tải tệp tài liệu này.");
                }
            }

            string physicalPath;
            try
            {
                physicalPath = _fileStorageService.GetPhysicalFilePath(file.FilePath);
            }
            catch (Exception ex)
            {
                return ApiResponse<SubmissionFileDownloadDto>.Fail($"Đường dẫn tệp không hợp lệ: {ex.Message}");
            }

            if (!File.Exists(physicalPath))
            {
                return ApiResponse<SubmissionFileDownloadDto>.Fail("Tệp tài liệu không tồn tại trên hệ thống lưu trữ.");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };

            var dto = new SubmissionFileDownloadDto
            {
                FileName = file.FileName,
                ContentType = contentType,
                PhysicalPath = physicalPath
            };

            return ApiResponse<SubmissionFileDownloadDto>.Ok(dto);
        }
    }
}
