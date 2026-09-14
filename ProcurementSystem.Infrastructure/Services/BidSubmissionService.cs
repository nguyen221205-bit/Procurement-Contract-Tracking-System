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

        public BidSubmissionService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<BidSubmissionDto>> SubmitBidAsync(
            int bidPackageId, int userId, List<IFormFile> files, List<SubmissionFileType> fileTypes)
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

            // 4. Kiểm tra tư cách nhà thầu
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (contractor == null)
            {
                return ApiResponse<BidSubmissionDto>.Fail("Tài khoản của bạn chưa liên kết với hồ sơ nhà thầu.");
            }

            // 5. Kiểm tra nộp trùng (mỗi nhà thầu chỉ nộp 1 hồ sơ cho 1 gói thầu)
            var alreadySubmitted = await _unitOfWork.Repository<BidSubmission>()
                .ExistsAsync(bs => bs.BidPackageId == bidPackageId && bs.ContractorId == contractor.Id);

            if (alreadySubmitted)
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

            // 7. Tạo hồ sơ dự thầu
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var submission = new BidSubmission
                {
                    BidPackageId = bidPackageId,
                    ContractorId = contractor.Id,
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Submitted"
                };

                await _unitOfWork.Repository<BidSubmission>().AddAsync(submission);
                await _unitOfWork.SaveChangesAsync();

                // 8. Lưu các file đính kèm
                var submissionFiles = new List<SubmissionFile>();
                for (int i = 0; i < files.Count; i++)
                {
                    var filePath = await _fileStorageService.SaveFileAsync(
                        files[i], $"submissions/{submission.Id}");

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
                    TotalScore = submission.TotalScore,
                    Rank = submission.Rank,
                    Status = submission.Status,
                    Files = submissionFiles.Select(MapFileToDto).ToList()
                }, "Nộp hồ sơ dự thầu thành công.");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponse<BidSubmissionDto>> GetSubmissionByIdAsync(
            int id, int userId, bool isInternalStaff)
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

            // Bảo mật: Nếu không phải nhân sự nội bộ, kiểm tra quyền sở hữu
            if (!isInternalStaff)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (contractor == null || contractor.Id != submission.ContractorId)
                {
                    return ApiResponse<BidSubmissionDto>.Fail("Bạn không có quyền xem hồ sơ dự thầu này.");
                }
            }

            return ApiResponse<BidSubmissionDto>.Ok(MapToDto(submission));
        }

        public async Task<ApiResponse<List<BidSubmissionSummaryDto>>> GetSubmissionsByPackageAsync(
            int bidPackageId, int userId, bool isInternalStaff)
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

            // Bảo mật giá thầu: nhân sự nội bộ chỉ xem khi gói thầu đã Closed/Evaluating/Contracted
            if (isInternalStaff && bidPackage.Status == BidPackageStatus.Open)
            {
                return ApiResponse<List<BidSubmissionSummaryDto>>.Fail(
                    "Không thể xem danh sách hồ sơ dự thầu khi gói thầu còn đang mở. Vui lòng đóng thầu trước.");
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
                    TotalScore = bs.TotalScore,
                    Rank = bs.Rank,
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
                return ApiResponse<bool>.Fail("Bạn không có quyền rút hồ sơ dự thầu này.");
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

            // Xóa file vật lý
            foreach (var file in submission.SubmissionFiles)
            {
                _fileStorageService.DeleteFile(file.FilePath);
            }

            // Xóa các bản ghi file
            foreach (var file in submission.SubmissionFiles.ToList())
            {
                _unitOfWork.Repository<SubmissionFile>().Delete(file);
            }

            // Xóa hồ sơ dự thầu
            _unitOfWork.Repository<BidSubmission>().Delete(submission);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Rút hồ sơ dự thầu thành công.");
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
                TotalScore = submission.TotalScore,
                Rank = submission.Rank,
                Status = submission.Status,
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
                TotalScore = submission.TotalScore,
                Rank = submission.Rank,
                Status = submission.Status,
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
    }
}
