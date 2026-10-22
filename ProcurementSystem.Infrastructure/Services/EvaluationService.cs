using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Evaluation;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class EvaluationService : IEvaluationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly INotificationService _notificationService;

        public EvaluationService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<List<EvaluationCriteriaDto>>> GetCriteriaByPackageAsync(int packageId)
        {
            var criteriaList = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Where(c => c.BidPackageId == packageId)
                .AsNoTracking()
                .Select(c => new EvaluationCriteriaDto
                {
                    Id = c.Id,
                    BidPackageId = c.BidPackageId,
                    Name = c.Name,
                    MaxScore = c.MaxScore,
                    Weight = c.Weight,
                    IsPriceCriteria = c.IsPriceCriteria
                })
                .ToListAsync();

            return ApiResponse<List<EvaluationCriteriaDto>>.Ok(criteriaList);
        }

        public async Task<ApiResponse<EvaluationCriteriaDto>> CreateCriteriaAsync(int packageId, CreateCriteriaRequest request, int userId, bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(packageId);
            if (package == null)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Không tìm thấy gói thầu.");
            }

            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Bạn không có quyền tạo tiêu chí cho gói thầu này.");
            }

            // Chỉ cho phép thêm tiêu chí khi gói thầu chưa chuyển sang giai đoạn chấm điểm hoặc ký hợp đồng
            if (package.Status == BidPackageStatus.Evaluating || package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Không thể thêm tiêu chí khi gói thầu đang trong giai đoạn chấm điểm hoặc đã ký hợp đồng.");
            }

            if (request.MaxScore <= 0)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Thang điểm tối đa phải lớn hơn 0.");
            }

            if (request.Weight <= 0)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Trọng số phải lớn hơn 0.");
            }

            // Rào chặn nghiệp vụ: Tổng trọng số các tiêu chí trong gói thầu không được vượt quá 100%
            // R2-3: Mỗi gói thầu tối đa 1 tiêu chí Giá / Tài chính
            if (request.IsPriceCriteria)
            {
                var hasPriceCriteria = await _unitOfWork.Repository<EvaluationCriteria>()
                    .ExistsAsync(c => c.BidPackageId == packageId && c.IsPriceCriteria);
                if (hasPriceCriteria)
                {
                    return ApiResponse<EvaluationCriteriaDto>.Fail("Gói thầu đã có 1 tiêu chí đánh giá Giá / Tài chính. Mỗi gói thầu chỉ được phép thiết lập tối đa 1 tiêu chí giá.");
                }
            }

            // Rào chặn nghiệp vụ: Tổng trọng số các tiêu chí trong gói thầu không được vượt quá 100%
            var currentTotalWeight = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Where(c => c.BidPackageId == packageId)
                .SumAsync(c => c.Weight);

            if (currentTotalWeight + request.Weight > 100)
            {
                var remaining = Math.Max(0, 100 - currentTotalWeight);
                return ApiResponse<EvaluationCriteriaDto>.Fail(
                    $"Tổng trọng số không được vượt quá 100%. Gói thầu hiện đã có {currentTotalWeight}%, bạn chỉ có thể thêm tối đa {remaining}%.");
            }

            var isDuplicate = await _unitOfWork.Repository<EvaluationCriteria>()
                .ExistsAsync(c => c.BidPackageId == packageId && c.Name.ToLower() == request.Name.Trim().ToLower());
            if (isDuplicate)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail($"Tiêu chí '{request.Name.Trim()}' đã tồn tại trong gói thầu.");
            }

            var entity = new EvaluationCriteria
            {
                BidPackageId = packageId,
                Name = request.Name.Trim(),
                MaxScore = request.MaxScore,
                Weight = request.Weight,
                IsPriceCriteria = request.IsPriceCriteria
            };

            await _unitOfWork.Repository<EvaluationCriteria>().AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var dto = new EvaluationCriteriaDto
            {
                Id = entity.Id,
                BidPackageId = entity.BidPackageId,
                Name = entity.Name,
                MaxScore = entity.MaxScore,
                Weight = entity.Weight,
                IsPriceCriteria = entity.IsPriceCriteria
            };

            return ApiResponse<EvaluationCriteriaDto>.Ok(dto, "Tạo tiêu chí đánh giá thành công.");
        }

        public async Task<ApiResponse<EvaluationCriteriaDto>> UpdateCriteriaAsync(int criteriaId, UpdateCriteriaRequest request, int userId, bool isAdmin)
        {
            var criteria = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Include(c => c.BidPackage)
                .FirstOrDefaultAsync(c => c.Id == criteriaId);

            if (criteria == null)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Không tìm thấy tiêu chí đánh giá.");
            }

            if (!isAdmin && criteria.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Bạn không có quyền chỉnh sửa tiêu chí này.");
            }

            if (criteria.BidPackage.Status == BidPackageStatus.Evaluating || criteria.BidPackage.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Không thể chỉnh sửa tiêu chí khi gói thầu đang trong giai đoạn chấm điểm hoặc đã ký hợp đồng.");
            }

            if (request.MaxScore <= 0)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Thang điểm tối đa phải lớn hơn 0.");
            }

            if (request.Weight <= 0)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Trọng số phải lớn hơn 0.");
            }

            // R2-3: Mỗi gói thầu tối đa 1 tiêu chí Giá / Tài chính
            if (request.IsPriceCriteria)
            {
                var hasOtherPriceCriteria = await _unitOfWork.Repository<EvaluationCriteria>()
                    .ExistsAsync(c => c.BidPackageId == criteria.BidPackageId && c.Id != criteriaId && c.IsPriceCriteria);
                if (hasOtherPriceCriteria)
                {
                    return ApiResponse<EvaluationCriteriaDto>.Fail("Gói thầu đã có 1 tiêu chí đánh giá Giá / Tài chính khác. Mỗi gói thầu chỉ được phép thiết lập tối đa 1 tiêu chí giá.");
                }
            }

            // Rào chặn nghiệp vụ: Tổng trọng số các tiêu chí khác trong gói thầu không được vượt quá 100%
            var otherTotalWeight = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Where(c => c.BidPackageId == criteria.BidPackageId && c.Id != criteriaId)
                .SumAsync(c => c.Weight);

            if (otherTotalWeight + request.Weight > 100)
            {
                var remaining = Math.Max(0, 100 - otherTotalWeight);
                return ApiResponse<EvaluationCriteriaDto>.Fail(
                    $"Tổng trọng số không được vượt quá 100%. Các tiêu chí khác đã chiếm {otherTotalWeight}%, trọng số tối đa có thể đặt là {remaining}%.");
            }

            // Kiểm tra trùng tên với tiêu chí khác cùng gói thầu
            var isDuplicate = await _unitOfWork.Repository<EvaluationCriteria>()
                .ExistsAsync(c => c.BidPackageId == criteria.BidPackageId 
                               && c.Id != criteriaId 
                               && c.Name.ToLower() == request.Name.Trim().ToLower());
            if (isDuplicate)
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail($"Tiêu chí '{request.Name.Trim()}' đã tồn tại trong gói thầu.");
            }

            // Kiểm tra xem tiêu chí đã có điểm chấm chưa (nếu có, không cho đổi MaxScore hoặc Weight làm lệch bảng điểm)
            var hasScores = await _unitOfWork.Repository<EvaluationScore>().ExistsAsync(s => s.CriteriaId == criteriaId);
            if (hasScores && (criteria.MaxScore != request.MaxScore || criteria.Weight != request.Weight))
            {
                return ApiResponse<EvaluationCriteriaDto>.Fail("Tiêu chí này đã có điểm đánh giá trong hệ thống, không thể thay đổi thang điểm tối đa hoặc trọng số.");
            }

            criteria.Name = request.Name.Trim();
            criteria.MaxScore = request.MaxScore;
            criteria.Weight = request.Weight;
            criteria.IsPriceCriteria = request.IsPriceCriteria;

            _unitOfWork.Repository<EvaluationCriteria>().Update(criteria);
            await _unitOfWork.SaveChangesAsync();

            var dto = new EvaluationCriteriaDto
            {
                Id = criteria.Id,
                BidPackageId = criteria.BidPackageId,
                Name = criteria.Name,
                MaxScore = criteria.MaxScore,
                Weight = criteria.Weight,
                IsPriceCriteria = criteria.IsPriceCriteria
            };

            return ApiResponse<EvaluationCriteriaDto>.Ok(dto, "Cập nhật tiêu chí đánh giá thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteCriteriaAsync(int criteriaId, int userId, bool isAdmin)
        {
            var criteria = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Include(c => c.BidPackage)
                .FirstOrDefaultAsync(c => c.Id == criteriaId);

            if (criteria == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy tiêu chí đánh giá.");
            }

            if (!isAdmin && criteria.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<bool>.Fail("Bạn không có quyền xóa tiêu chí này.");
            }

            if (criteria.BidPackage.Status == BidPackageStatus.Evaluating || criteria.BidPackage.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<bool>.Fail("Không thể xóa tiêu chí khi gói thầu đang trong giai đoạn chấm điểm hoặc đã ký hợp đồng.");
            }

            // Kiểm tra xem tiêu chí đã có điểm chấm chưa
            var hasScores = await _unitOfWork.Repository<EvaluationScore>().ExistsAsync(s => s.CriteriaId == criteriaId);
            if (hasScores)
            {
                return ApiResponse<bool>.Fail("Tiêu chí này đã có điểm đánh giá trong hệ thống, không thể xóa.");
            }

            _unitOfWork.Repository<EvaluationCriteria>().Delete(criteria);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Xóa tiêu chí đánh giá thành công.");
        }

        public async Task<ApiResponse<List<EvaluationScoreDto>>> ScoreSubmissionAsync(int submissionId, ScoreSubmissionRequest request, int evaluatorId)
        {
            var submission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.BidPackage)
                .Include(s => s.Contractor)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Không tìm thấy hồ sơ dự thầu.");
            }

            // Kiểm tra trạng thái hồ sơ dự thầu
            if (submission.Status == BidSubmissionStatus.Withdrawn || submission.Status == BidSubmissionStatus.Disqualified)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail($"Không thể chấm điểm hồ sơ dự thầu đã ở trạng thái '{submission.Status}'.");
            }

            if (submission.Status == BidSubmissionStatus.Selected || submission.Status == BidSubmissionStatus.Rejected)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Gói thầu đã có kết quả phê duyệt trúng thầu chính thức, không thể thay đổi điểm đánh giá.");
            }

            // Kiểm tra tài khoản giám khảo
            var evaluator = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == evaluatorId);

            if (evaluator == null || !evaluator.IsActive)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Tài khoản giám khảo không tồn tại hoặc đã bị vô hiệu hóa.");
            }

            var package = submission.BidPackage;

            // Ràng buộc bảo mật & nghiệp vụ: Chỉ giám khảo thuộc Tổ chuyên gia mới có quyền chấm điểm gói thầu này (Căn cứ Điều 19 NĐ 24/2024)
            var isAssigned = await _unitOfWork.Repository<BidPackageEvaluator>()
                .ExistsAsync(pe => pe.BidPackageId == package.Id && pe.EvaluatorId == evaluatorId);

            if (!isAssigned)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail(
                    "Căn cứ Điều 19 Nghị định 24/2024/NĐ-CP: Bạn không thuộc Tổ chuyên gia được phân công chính thức cho gói thầu này. Chỉ thành viên Tổ chuyên gia mới có quyền chấm điểm.");
            }

            // Ràng buộc nghiệp vụ: Không được chấm điểm khi gói thầu vẫn đang Open (chưa hết hạn/chưa đóng thầu)
            if (package.Status == BidPackageStatus.Open)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Chưa thể chấm điểm khi gói thầu đang mở thầu. Vui lòng đóng thầu trước khi tiến hành chấm điểm.");
            }

            if (package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Gói thầu đã ký kết hợp đồng, không thể sửa đổi điểm đánh giá.");
            }

            // Tự động chuyển gói thầu sang Evaluating nếu đang Closed (phải đủ điều kiện tiêu chí và tổ chuyên gia)
            if (package.Status == BidPackageStatus.Closed)
            {
                var criteriaListCheck = await _unitOfWork.Repository<EvaluationCriteria>()
                    .Query()
                    .Where(c => c.BidPackageId == package.Id)
                    .ToListAsync();
                var totalWeight = criteriaListCheck.Sum(c => c.Weight);
                if (totalWeight != 100)
                {
                    return ApiResponse<List<EvaluationScoreDto>>.Fail(
                        $"Không thể bắt đầu chấm điểm. Tổng trọng số bộ tiêu chí phải đạt 100% (Hiện tại: {totalWeight}%).");
                }

                var evaluatorsCount = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .CountAsync(pe => pe.BidPackageId == package.Id);
                if (evaluatorsCount < 3 || evaluatorsCount % 2 == 0)
                {
                    return ApiResponse<List<EvaluationScoreDto>>.Fail(
                        $"Không thể bắt đầu chấm điểm. Tổ chuyên gia phải có tối thiểu 3 thành viên và là số lẻ (3, 5, 7,...) theo Luật Đấu thầu (Hiện tại: {evaluatorsCount} thành viên).");
                }

                package.Status = BidPackageStatus.Evaluating;
                package.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<BidPackage>().Update(package);
            }

            if (request.Scores == null || !request.Scores.Any())
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Danh sách điểm chấm không được để trống.");
            }

            // Kiểm tra tiêu chí bị trùng lặp trong cùng 1 phiếu chấm
            var duplicateCriteriaIds = request.Scores
                .GroupBy(s => s.CriteriaId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateCriteriaIds.Any())
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail($"Danh sách chấm điểm có tiêu chí bị trùng lặp (Mã tiêu chí ID: {string.Join(", ", duplicateCriteriaIds)}). Mỗi tiêu chí chỉ được chấm một lần trong một phiếu chấm.");
            }

            // Lấy danh sách tiêu chí của gói thầu
            var criteriaList = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Where(c => c.BidPackageId == package.Id)
                .ToListAsync();

            if (!criteriaList.Any())
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Gói thầu chưa được thiết lập bộ tiêu chí đánh giá. Vui lòng tạo tiêu chí trước.");
            }

            var totalCriteriaWeight = criteriaList.Sum(c => c.Weight);
            if (totalCriteriaWeight != 100)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail(
                    $"Không thể chấm điểm. Gói thầu cần có bộ tiêu chí với tổng trọng số đạt đúng 100% (Hiện tại: {totalCriteriaWeight}%).");
            }

            var criteriaMap = criteriaList.ToDictionary(c => c.Id);

            // Kiểm tra và lưu từng điểm số
            foreach (var scoreItem in request.Scores)
            {
                if (!criteriaMap.TryGetValue(scoreItem.CriteriaId, out var criteria))
                {
                    return ApiResponse<List<EvaluationScoreDto>>.Fail($"Tiêu chí có mã ID {scoreItem.CriteriaId} không thuộc gói thầu này.");
                }

                // R2-3: Tiêu chí Giá được hệ thống tự động tính điểm từ giá dự thầu theo Luật Đấu thầu, không cho phép giám khảo chấm tay
                if (criteria.IsPriceCriteria)
                {
                    return ApiResponse<List<EvaluationScoreDto>>.Fail(
                        $"Tiêu chí '{criteria.Name}' là tiêu chí Giá / Tài chính được hệ thống tự động tính điểm từ giá dự thầu theo công thức luật đấu thầu. Giám khảo không được phép chấm điểm thủ công cho tiêu chí này.");
                }

                if (scoreItem.Score < 0 || scoreItem.Score > criteria.MaxScore)
                {
                    return ApiResponse<List<EvaluationScoreDto>>.Fail($"Điểm số '{scoreItem.Score}' không hợp lệ cho tiêu chí '{criteria.Name}'. Thang điểm cho phép: 0 - {criteria.MaxScore}.");
                }

                var existingScore = await _unitOfWork.Repository<EvaluationScore>()
                    .Query()
                    .FirstOrDefaultAsync(s => s.BidSubmissionId == submissionId
                                           && s.CriteriaId == scoreItem.CriteriaId
                                           && s.EvaluatorId == evaluatorId);

                if (existingScore != null)
                {
                    existingScore.Score = scoreItem.Score;
                    existingScore.Comment = scoreItem.Comment?.Trim();
                    existingScore.ScoredAt = DateTime.UtcNow;
                    _unitOfWork.Repository<EvaluationScore>().Update(existingScore);
                }
                else
                {
                    var newScore = new EvaluationScore
                    {
                        BidSubmissionId = submissionId,
                        CriteriaId = scoreItem.CriteriaId,
                        EvaluatorId = evaluatorId,
                        Score = scoreItem.Score,
                        Comment = scoreItem.Comment?.Trim(),
                        ScoredAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<EvaluationScore>().AddAsync(newScore);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // =========================================================================
            // THUẬT TOÁN TÍNH ĐIỂM TỔNG HỢP THEO TRỌNG SỐ & TỰ ĐỘNG XẾP HẠNG (AUTO-RANKING)
            // =========================================================================
            await CalculateRankingsForPackageAsync(package.Id);

            // Gửi thông báo cho Bên mời thầu (Chủ gói thầu)
            try
            {
                var contractorName = submission.Contractor?.CompanyName ?? "Nhà thầu";
                await _notificationService.CreateNotificationAsync(
                    package.CreatedBy,
                    $"Giám khảo đã chấm điểm - Gói {package.Code}",
                    $"Giám khảo {evaluator.FullName} vừa hoàn tất chấm điểm hồ sơ của {contractorName} cho gói thầu '{package.Name}' ({package.Code}).",
                    NotificationType.Info,
                    sendEmail: false);
            }
            catch
            {
                // Không để lỗi thông báo làm gián đoạn luồng chấm điểm
            }

            // Trả về danh sách điểm chấm chi tiết vừa lưu
            return await GetScoresBySubmissionAsync(submissionId, evaluatorId, false);
        }

        public async Task<ApiResponse<List<EvaluationScoreDto>>> GetScoresBySubmissionAsync(int submissionId, int currentUserId, bool isInternalStaff)
        {
            var submission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.BidPackage)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Không tìm thấy hồ sơ dự thầu.");
            }

            var query = _unitOfWork.Repository<EvaluationScore>()
                .Query()
                .Include(s => s.Criteria)
                .Include(s => s.Evaluator)
                .Where(s => s.BidSubmissionId == submissionId)
                .AsNoTracking();

            // Nguyên tắc độc lập chấm điểm (NĐ 24/2024/NĐ-CP & Luật Đấu thầu):
            // Trong thời gian chấm thầu (Evaluating), thành viên Tổ chuyên gia chỉ được xem điểm đánh giá của chính mình
            // để bảo đảm tính độc lập, khách quan (không bị ảnh hưởng hoặc gây áp lực từ điểm của giám khảo khác).
            if (submission.BidPackage.Status == BidPackageStatus.Evaluating)
            {
                var isPackageEvaluator = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .ExistsAsync(pe => pe.BidPackageId == submission.BidPackageId && pe.EvaluatorId == currentUserId);

                if (isPackageEvaluator)
                {
                    query = query.Where(s => s.EvaluatorId == currentUserId);
                }
                else if (!isInternalStaff)
                {
                    query = query.Where(s => s.EvaluatorId == currentUserId);
                }
            }

            var scores = await query
                .Select(s => new EvaluationScoreDto
                {
                    Id = s.Id,
                    BidSubmissionId = s.BidSubmissionId,
                    CriteriaId = s.CriteriaId,
                    CriteriaName = s.Criteria.Name,
                    MaxScore = s.Criteria.MaxScore,
                    Weight = s.Criteria.Weight,
                    EvaluatorId = s.EvaluatorId,
                    EvaluatorName = s.Evaluator.FullName,
                    Score = s.Score,
                    Comment = s.Comment,
                    ScoredAt = s.ScoredAt
                })
                .ToListAsync();

            return ApiResponse<List<EvaluationScoreDto>>.Ok(scores);
        }

        public async Task<ApiResponse<List<SubmissionRankingDto>>> GetPackageRankingsAsync(int packageId, int currentUserId, bool isInternalStaff)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == packageId);

            if (package == null)
            {
                return ApiResponse<List<SubmissionRankingDto>>.Fail("Không tìm thấy gói thầu.");
            }

            // Nếu không phải Admin/Procurement, người gọi phải thuộc Tổ chuyên gia của gói thầu
            if (!isInternalStaff)
            {
                var isPackageEvaluator = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .ExistsAsync(pe => pe.BidPackageId == packageId && pe.EvaluatorId == currentUserId);

                if (!isPackageEvaluator)
                {
                    return ApiResponse<List<SubmissionRankingDto>>.Fail("Bạn không thuộc Tổ chuyên gia đánh giá của gói thầu này.");
                }
            }

            var isEvaluating = package.Status == BidPackageStatus.Evaluating;
            var restrictEvaluator = isEvaluating && !isInternalStaff;

            var submissions = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Where(s => s.BidPackageId == packageId)
                .Include(s => s.Contractor)
                .Include(s => s.EvaluationScores)
                    .ThenInclude(es => es.Criteria)
                .Include(s => s.EvaluationScores)
                    .ThenInclude(es => es.Evaluator)
                .AsNoTracking()
                .OrderBy(s => s.Rank == null)
                .ThenBy(s => s.Rank)
                .ThenByDescending(s => s.TotalScore)
                .ToListAsync();

            var result = submissions.Select(s =>
            {
                // Khi gói đang trong giai đoạn 'Evaluating', Giám khảo chỉ được xem điểm do chính mình chấm;
                // Ẩn TotalScore và Rank để bảo đảm tính độc lập, khách quan tuyệt đối của từng thành viên Tổ chuyên gia (N14)
                var scores = s.EvaluationScores
                    .Where(es => !restrictEvaluator || es.EvaluatorId == currentUserId)
                    .Select(es => new EvaluationScoreDto
                    {
                        Id = es.Id,
                        BidSubmissionId = es.BidSubmissionId,
                        CriteriaId = es.CriteriaId,
                        CriteriaName = es.Criteria.Name,
                        MaxScore = es.Criteria.MaxScore,
                        Weight = es.Criteria.Weight,
                        EvaluatorId = es.EvaluatorId,
                        EvaluatorName = es.Evaluator.FullName,
                        Score = es.Score,
                        Comment = es.Comment,
                        ScoredAt = es.ScoredAt
                    }).ToList();

                return new SubmissionRankingDto
                {
                    SubmissionId = s.Id,
                    BidPackageId = s.BidPackageId,
                    ContractorId = s.ContractorId,
                    CompanyName = s.Contractor.CompanyName,
                    TaxCode = s.Contractor.TaxCode,
                    BidPrice = s.BidPrice,
                    TotalScore = restrictEvaluator ? null : s.TotalScore,
                    Rank = restrictEvaluator ? null : s.Rank,
                    Status = s.Status,
                    SelectionReason = s.SelectionReason,
                    DisqualificationReason = s.DisqualificationReason,
                    SubmittedAt = s.SubmittedAt,
                    Scores = scores
                };
            }).ToList();

            return ApiResponse<List<SubmissionRankingDto>>.Ok(result);
        }

        public async Task<ApiResponse<bool>> FinalizeEvaluationAsync(int packageId, FinalizeEvaluationRequest request, int userId, bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .FirstOrDefaultAsync(bp => bp.Id == packageId);

            if (package == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy gói thầu.");
            }

            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<bool>.Fail("Bạn không có quyền phê duyệt kết quả đánh giá cho gói thầu này.");
            }

            if (package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<bool>.Fail("Gói thầu đã ký hợp đồng, không thể phê duyệt lại.");
            }

            if (package.Status != BidPackageStatus.Evaluating)
            {
                return ApiResponse<bool>.Fail($"Chỉ được phê duyệt kết quả khi gói thầu ở trạng thái 'Evaluating' (Chấm điểm). Trạng thái hiện tại: '{package.Status}'.");
            }

            var submissions = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.Contractor)
                .Where(s => s.BidPackageId == packageId)
                .ToListAsync();

            if (!submissions.Any())
            {
                return ApiResponse<bool>.Fail("Gói thầu không có hồ sơ dự thầu nào để phê duyệt.");
            }

            var selectedSubmission = submissions.FirstOrDefault(s => s.Id == request.SelectedSubmissionId);
            if (selectedSubmission == null)
            {
                return ApiResponse<bool>.Fail("Hồ sơ dự thầu được chọn không thuộc gói thầu này.");
            }

            if (!selectedSubmission.TotalScore.HasValue || selectedSubmission.Status != BidSubmissionStatus.Evaluated)
            {
                return ApiResponse<bool>.Fail("Hồ sơ dự thầu được chọn chưa hoàn tất quá trình chấm điểm đánh giá.");
            }

            // R3-4 / N16: Chặn phê duyệt trúng thầu cho nhà thầu đã từng bị chấm dứt hợp đồng (Terminated) trong cùng gói thầu này
            var hasTerminatedContract = await _unitOfWork.Repository<Contract>()
                .Query()
                .AnyAsync(c => c.BidPackageId == packageId && c.ContractorId == selectedSubmission.ContractorId && c.Status == ContractStatus.Terminated);

            if (hasTerminatedContract)
            {
                return ApiResponse<bool>.Fail("Không thể phê duyệt trúng thầu cho nhà thầu đã từng bị chấm dứt hợp đồng (Terminated) trong cùng gói thầu này.");
            }

            // Căn cứ NĐ 24/2024/NĐ-CP: Toàn bộ hồ sơ dự thầu hợp lệ phải hoàn tất đánh giá đủ điều kiện
            var activeSubmissions = submissions.Where(s => s.Status != BidSubmissionStatus.Withdrawn && s.Status != BidSubmissionStatus.Disqualified).ToList();
            var incompleteSubmissions = activeSubmissions.Where(s => s.Status != BidSubmissionStatus.Evaluated).ToList();
            if (incompleteSubmissions.Any())
            {
                return ApiResponse<bool>.Fail(
                    $"Căn cứ Nghị định 24/2024/NĐ-CP: Còn {incompleteSubmissions.Count} hồ sơ dự thầu hợp lệ chưa hoàn tất quá trình đánh giá của đầy đủ các thành viên Tổ chuyên gia. Không thể phê duyệt trao thầu trên kết quả chấm chưa hoàn tất.");
            }

            // Căn cứ Điều 61 Luật Đấu thầu 2023: Nếu chọn nhà thầu không xếp hạng 1, bắt buộc phải có lý do giải trình
            if (selectedSubmission.Rank.HasValue && selectedSubmission.Rank.Value > 1)
            {
                if (string.IsNullOrWhiteSpace(request.DecisionReason))
                {
                    return ApiResponse<bool>.Fail(
                        $"Căn cứ Điều 61 Luật Đấu thầu 2023: Bạn đang phê duyệt trao thầu cho hồ sơ xếp thứ hạng #{selectedSubmission.Rank.Value} (không phải xếp Hạng 1). Bắt buộc phải cung cấp lý do / căn cứ giải trình pháp lý để lưu hồ sơ kiểm toán.");
                }
            }

            // Đánh dấu hồ sơ trúng thầu và từ chối các hồ sơ hợp lệ còn lại (bảo toàn nguyên vẹn trạng thái Withdrawn và Disqualified)
            foreach (var sub in submissions)
            {
                if (sub.Id == request.SelectedSubmissionId)
                {
                    sub.Status = BidSubmissionStatus.Selected;
                    sub.SelectionReason = request.DecisionReason;
                    _unitOfWork.Repository<BidSubmission>().Update(sub);
                }
                else if (sub.Status != BidSubmissionStatus.Withdrawn && sub.Status != BidSubmissionStatus.Disqualified)
                {
                    sub.Status = BidSubmissionStatus.Rejected;
                    _unitOfWork.Repository<BidSubmission>().Update(sub);
                }
            }

            // Cập nhật trạng thái gói thầu sang Awarded (Đã trao thầu theo Điều 61, 64 Luật Đấu thầu 2023)
            var oldStatus = package.Status;
            package.Status = BidPackageStatus.Awarded;
            package.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<BidPackage>().Update(package);

            // Ghi vết nhật ký kiểm toán (Audit Log)
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = "FINALIZE_AWARD",
                EntityType = "BidPackage",
                EntityId = packageId,
                OldValues = JsonSerializer.Serialize(new { Status = oldStatus.ToString() }),
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = BidPackageStatus.Awarded.ToString(),
                    WinningSubmissionId = request.SelectedSubmissionId,
                    Rank = selectedSubmission.Rank,
                    DecisionReason = request.DecisionReason
                }),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangesAsync();

            // Gửi thông báo cho nhà thầu trúng thầu
            if (selectedSubmission.Contractor?.UserId != null)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(
                        selectedSubmission.Contractor.UserId,
                        $"Chúc mừng trúng thầu gói {package.Code}",
                        $"Hồ sơ dự thầu của doanh nghiệp bạn đối với gói thầu '{package.Name}' ({package.Code}) đã được phê duyệt trúng thầu. Vui lòng chuẩn bị để tiến hành thương thảo và hoàn thiện hợp đồng kinh tế.",
                        NotificationType.Info,
                        sendEmail: false);
                }
                catch
                {
                }
            }

            // Gửi thông báo hàng loạt cho các nhà thầu khác không được lựa chọn
            var rejectedUserIds = submissions
                .Where(s => s.Id != selectedSubmission.Id && s.Contractor?.UserId != null && s.Status == BidSubmissionStatus.Rejected)
                .Select(s => s.Contractor.UserId)
                .Distinct()
                .ToList();

            if (rejectedUserIds.Count != 0)
            {
                try
                {
                    await _notificationService.CreateBulkNotificationsAsync(
                        rejectedUserIds,
                        $"Thông báo kết quả xét thầu gói {package.Code}",
                        $"Gói thầu '{package.Name}' ({package.Code}) đã hoàn tất công tác xét chọn trao thầu. Hồ sơ của quý đơn vị chưa được lựa chọn trong đợt này. Cảm ơn sự quan tâm của quý doanh nghiệp.",
                        NotificationType.Info,
                        sendEmail: false);
                }
                catch
                {
                }
            }

            // Gửi thông báo cho các Giám khảo thuộc Tổ chuyên gia về việc hoàn tất trao thầu
            try
            {
                var evaluatorUserIds = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .Query()
                    .Where(pe => pe.BidPackageId == packageId)
                    .Select(pe => pe.EvaluatorId)
                    .ToListAsync();

                if (evaluatorUserIds.Count > 0)
                {
                    var winningContractorName = selectedSubmission.Contractor?.CompanyName ?? $"Hồ sơ #{selectedSubmission.Id}";
                    await _notificationService.CreateBulkNotificationsAsync(
                        evaluatorUserIds,
                        $"Hoàn tất phê duyệt trao thầu gói {package.Code}",
                        $"Gói thầu '{package.Name}' ({package.Code}) đã được Bên mời thầu phê duyệt kết quả trao thầu cho đơn vị {winningContractorName}.",
                        NotificationType.Info,
                        sendEmail: false);
                }
            }
            catch
            {
            }

            return ApiResponse<bool>.Ok(true, $"Đã phê duyệt nhà thầu (Mã hồ sơ: #{request.SelectedSubmissionId}) trúng thầu thành công. Gói thầu đã chuyển sang trạng thái 'Awarded' và sẵn sàng để ký hợp đồng.");
        }

        public async Task<ApiResponse<bool>> RevokeAwardAsync(int packageId, RevokeAwardRequest request, int userId, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(request?.Reason) || request.Reason.Trim().Length < 5)
            {
                return ApiResponse<bool>.Fail("Lý do thu hồi kết quả trao thầu là bắt buộc và phải có ít nhất 5 ký tự.");
            }

            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .FirstOrDefaultAsync(bp => bp.Id == packageId);

            if (package == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy gói thầu.");
            }

            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<bool>.Fail("Bạn không có quyền thu hồi kết quả trao thầu cho gói thầu này.");
            }

            // Kiểm tra các hợp đồng liên quan đến gói thầu
            var contracts = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.Milestones)
                .Where(c => c.BidPackageId == packageId)
                .ToListAsync();

            var hasActiveOrCompletedContract = contracts.Any(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Completed);
            if (hasActiveOrCompletedContract)
            {
                return ApiResponse<bool>.Fail(
                    "Không thể thu hồi kết quả trao thầu vì gói thầu đã có hợp đồng ở trạng thái Đang thực hiện (Active) hoặc Hoàn thành (Completed). " +
                    "Căn cứ Điều 64 Luật Đấu thầu 2023, quyết định trúng thầu đã chuyển hóa thành nghĩa vụ hợp đồng có hiệu lực pháp lý.");
            }

            // Kiểm tra trạng thái gói thầu: chỉ cho phép khi Awarded, hoặc Contracted nếu toàn bộ hợp đồng đã Terminated
            var allContractsTerminated = contracts.Any() && contracts.All(c => c.Status == ContractStatus.Terminated);
            if (package.Status != BidPackageStatus.Awarded && !(package.Status == BidPackageStatus.Contracted && allContractsTerminated))
            {
                return ApiResponse<bool>.Fail(
                    $"Chỉ có thể thu hồi kết quả trao thầu khi gói thầu ở trạng thái 'Awarded' (hoặc 'Contracted' khi toàn bộ hợp đồng đã bị chấm dứt - Terminated). Trạng thái hiện tại: '{package.Status}'.");
            }

            // Hủy bỏ toàn bộ hợp đồng Draft liên quan đến gói thầu và thu thập file scan để xóa vật lý (R3-5)
            var draftContracts = contracts.Where(c => c.Status == ContractStatus.Draft).ToList();
            var draftScanFilesToDelete = draftContracts
                .Where(d => !string.IsNullOrEmpty(d.ScannedFilePath))
                .Select(d => d.ScannedFilePath!)
                .ToList();

            foreach (var draft in draftContracts)
            {
                _unitOfWork.Repository<Contract>().Delete(draft);
            }

            // Tìm hồ sơ trúng thầu hiện tại để lấy thông tin ghi nhận audit & notification
            var submissions = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.Contractor)
                    .ThenInclude(c => c.User)
                .Where(s => s.BidPackageId == packageId)
                .ToListAsync();

            var winningSubmission = submissions.FirstOrDefault(s => s.Status == BidSubmissionStatus.Selected);

            // R3-4 / N16: Khôi phục trạng thái hồ sơ dự thầu về Evaluated.
            // Nếu nhà thầu trúng thầu có hợp đồng bị Terminated, hồ sơ phải bị Disqualified kèm lý do, không thể chọn lại.
            foreach (var sub in submissions)
            {
                if (sub.Status == BidSubmissionStatus.Selected)
                {
                    var hadTerminatedContract = contracts.Any(c => c.ContractorId == sub.ContractorId && c.Status == ContractStatus.Terminated);
                    if (hadTerminatedContract)
                    {
                        sub.Status = BidSubmissionStatus.Disqualified;
                        sub.DisqualificationReason = $"Loại bỏ do nhà thầu đã bị chấm dứt hợp đồng (Terminated) đối với gói thầu này. Lý do thu hồi trao thầu: {request.Reason.Trim()}";
                        sub.SelectionReason = null;
                        sub.Rank = null;
                    }
                    else
                    {
                        sub.Status = BidSubmissionStatus.Evaluated;
                        sub.SelectionReason = null;
                    }
                    _unitOfWork.Repository<BidSubmission>().Update(sub);
                }
                else if (sub.Status == BidSubmissionStatus.Rejected && sub.TotalScore.HasValue)
                {
                    // Khôi phục các hồ sơ bị Rejected do quyết định trao thầu trước đó
                    sub.Status = BidSubmissionStatus.Evaluated;
                    _unitOfWork.Repository<BidSubmission>().Update(sub);
                }
            }

            // Cập nhật trạng thái gói thầu quay về Evaluating
            var oldStatus = package.Status;
            package.Status = BidPackageStatus.Evaluating;
            package.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<BidPackage>().Update(package);

            // Ghi nhật ký kiểm toán (Audit Log)
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = "REVOKE_AWARD",
                EntityType = "BidPackage",
                EntityId = packageId,
                OldValues = JsonSerializer.Serialize(new
                {
                    Status = oldStatus.ToString(),
                    RevokedSubmissionId = winningSubmission?.Id,
                    WinningContractorId = winningSubmission?.ContractorId,
                    WinningContractorName = winningSubmission?.Contractor?.CompanyName
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = BidPackageStatus.Evaluating.ToString(),
                    Reason = request.Reason.Trim()
                }),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangesAsync();

            // Gửi thông báo và email đến tài khoản nhà thầu bị thu hồi trao thầu
            if (winningSubmission?.Contractor?.UserId != null)
            {
                await _notificationService.CreateNotificationAsync(
                    winningSubmission.Contractor.UserId,
                    $"Thu hồi quyết định trao thầu gói {package.Code}",
                    $"Quyết định phê duyệt trúng thầu của doanh nghiệp bạn đối với gói thầu '{package.Name}' ({package.Code}) đã bị thu hồi. Lý do: {request.Reason.Trim()}",
                    NotificationType.Warning,
                    sendEmail: true);
            }

            // Nếu người thu hồi là Admin và không phải người tạo gói, gửi thông báo cho người tạo gói thầu
            if (isAdmin && package.CreatedBy != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    package.CreatedBy,
                    $"Kết quả trao thầu gói {package.Code} đã bị thu hồi",
                    $"Quản trị viên đã thực hiện thu hồi quyết định phê duyệt trúng thầu gói thầu '{package.Name}' ({package.Code}). Lý do: {request.Reason.Trim()}",
                    NotificationType.Warning,
                    sendEmail: true);
            }

            // R3-5: Dọn dẹp các tệp scan vật lý của hợp đồng Draft đã xóa
            foreach (var scanFile in draftScanFilesToDelete)
            {
                try
                {
                    _fileStorageService.DeleteFile(scanFile);
                }
                catch
                {
                    // Tránh ném lỗi ngoại vi làm hỏng kết quả thu hồi
                }
            }

            // Tính toán lại thứ hạng tự động cho các hồ sơ hợp lệ còn lại
            await CalculateRankingsForPackageAsync(packageId);

            return ApiResponse<bool>.Ok(true, "Thu hồi kết quả phê duyệt trúng thầu thành công. Gói thầu đã chuyển về trạng thái 'Evaluating' để tiếp tục thẩm định hoặc lựa chọn nhà thầu khác.");
        }

        public async Task<ApiResponse<bool>> DisqualifySubmissionAsync(int submissionId, DisqualifySubmissionRequest request, int userId, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(request?.Reason) || request.Reason.Trim().Length < 5)
            {
                return ApiResponse<bool>.Fail("Lý do loại bỏ hồ sơ dự thầu là bắt buộc và phải có ít nhất 5 ký tự.");
            }

            var submission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.BidPackage)
                .Include(s => s.Contractor)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy hồ sơ dự thầu.");
            }

            if (!isAdmin && submission.BidPackage.CreatedBy != userId)
            {
                return ApiResponse<bool>.Fail("Bạn không có quyền loại bỏ hồ sơ dự thầu này.");
            }

            if (submission.BidPackage.Status != BidPackageStatus.Closed && submission.BidPackage.Status != BidPackageStatus.Evaluating)
            {
                return ApiResponse<bool>.Fail(
                    $"Chỉ có thể loại bỏ hồ sơ dự thầu khi gói thầu ở giai đoạn đóng thầu hoặc đang chấm điểm ('Closed' hoặc 'Evaluating'). Trạng thái hiện tại: '{submission.BidPackage.Status}'.");
            }

            if (submission.Status == BidSubmissionStatus.Selected)
            {
                return ApiResponse<bool>.Fail("Không thể loại bỏ hồ sơ dự thầu đã được phê duyệt trúng thầu (Selected). Vui lòng thực hiện thu hồi trao thầu trước.");
            }

            if (submission.Status == BidSubmissionStatus.Withdrawn)
            {
                return ApiResponse<bool>.Fail("Hồ sơ dự thầu này đã được nhà thầu rút lui trước đó.");
            }

            if (submission.Status == BidSubmissionStatus.Disqualified)
            {
                return ApiResponse<bool>.Fail("Hồ sơ dự thầu này đã ở trạng thái bị loại trước đó.");
            }

            var oldStatus = submission.Status;
            submission.Status = BidSubmissionStatus.Disqualified;
            submission.DisqualificationReason = request.Reason.Trim();
            submission.SelectionReason = null;
            submission.Rank = null;
            _unitOfWork.Repository<BidSubmission>().Update(submission);

            // Ghi nhật ký kiểm toán (Audit Log)
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = "DISQUALIFY_SUBMISSION",
                EntityType = "BidSubmission",
                EntityId = submissionId,
                OldValues = JsonSerializer.Serialize(new { Status = oldStatus }),
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = BidSubmissionStatus.Disqualified,
                    Reason = request.Reason.Trim(),
                    BidPackageId = submission.BidPackageId
                }),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangesAsync();

            // Gửi thông báo và email đến nhà thầu
            if (submission.Contractor?.UserId != null)
            {
                await _notificationService.CreateNotificationAsync(
                    submission.Contractor.UserId,
                    $"Hồ sơ dự thầu bị loại - Gói {submission.BidPackage.Code}",
                    $"Hồ sơ dự thầu của doanh nghiệp bạn đối với gói thầu '{submission.BidPackage.Name}' ({submission.BidPackage.Code}) đã bị loại khỏi quá trình đánh giá. Lý do: {request.Reason.Trim()}",
                    NotificationType.Warning,
                    sendEmail: true);
            }

            // Tính toán lại xếp hạng cho gói thầu nếu đang trong giai đoạn chấm điểm
            if (submission.BidPackage.Status == BidPackageStatus.Evaluating)
            {
                await CalculateRankingsForPackageAsync(submission.BidPackageId);
            }

            return ApiResponse<bool>.Ok(true, $"Đã loại bỏ hồ sơ dự thầu #{submissionId} khỏi quá trình xét thầu.");
        }

        public async Task<ApiResponse<EvaluationSummaryDto>> GetEvaluationSummaryAsync(int packageId, int currentUserId, bool isInternalStaff)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Where(bp => bp.Id == packageId)
                .AsNoTracking()
                .Select(bp => new
                {
                    bp.Id,
                    bp.Code,
                    bp.Name,
                    bp.Budget,
                    bp.Status
                })
                .FirstOrDefaultAsync();

            if (package == null)
            {
                return ApiResponse<EvaluationSummaryDto>.Fail("Không tìm thấy gói thầu.");
            }

            if (!isInternalStaff)
            {
                var isPackageEvaluator = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .ExistsAsync(pe => pe.BidPackageId == packageId && pe.EvaluatorId == currentUserId);

                if (!isPackageEvaluator)
                {
                    return ApiResponse<EvaluationSummaryDto>.Fail("Bạn không thuộc Tổ chuyên gia đánh giá của gói thầu này.");
                }
            }

            var isEvaluating = package.Status == BidPackageStatus.Evaluating;
            var restrictEvaluator = isEvaluating && !isInternalStaff;

            // Thống kê tiêu chí
            var criteriaStats = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Where(c => c.BidPackageId == packageId)
                .AsNoTracking()
                .GroupBy(c => c.BidPackageId)
                .Select(g => new
                {
                    TotalCriteria = g.Count(),
                    TotalWeight = g.Sum(c => c.Weight)
                })
                .FirstOrDefaultAsync();

            // Thống kê hồ sơ dự thầu
            var submissionsQuery = _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Where(s => s.BidPackageId == packageId)
                .AsNoTracking();

            var totalSubmissions = await submissionsQuery.CountAsync();
            var evaluatedSubmissions = await submissionsQuery
                .CountAsync(s => s.Status == BidSubmissionStatus.Evaluated || s.Status == BidSubmissionStatus.Selected || s.Status == BidSubmissionStatus.Rejected);
            var pendingSubmissions = totalSubmissions - evaluatedSubmissions;

            // Thống kê điểm số (chỉ lấy các hồ sơ đã có điểm)
            // Nếu là Giám khảo trong giai đoạn Evaluating, ẩn thống kê điểm để chống thiên vị và bảo vệ tính độc lập độc đoán
            decimal? highestScore = null;
            decimal? lowestScore = null;
            decimal? averageScore = null;

            if (!restrictEvaluator)
            {
                var scoredQuery = submissionsQuery.Where(s => s.TotalScore.HasValue);
                if (await scoredQuery.AnyAsync())
                {
                    highestScore = await scoredQuery.MaxAsync(s => s.TotalScore);
                    lowestScore = await scoredQuery.MinAsync(s => s.TotalScore);
                    var avg = await scoredQuery.AverageAsync(s => s.TotalScore!.Value);
                    averageScore = Math.Round(avg, 2);
                }
            }

            // Thông tin hồ sơ trúng thầu (nếu đã phê duyệt)
            var winningSub = await submissionsQuery
                .Where(s => s.Status == BidSubmissionStatus.Selected)
                .Select(s => new
                {
                    s.Id,
                    s.ContractorId,
                    CompanyName = s.Contractor.CompanyName,
                    TaxCode = s.Contractor.TaxCode,
                    s.BidPrice,
                    s.TotalScore
                })
                .FirstOrDefaultAsync();

            var summary = new EvaluationSummaryDto
            {
                BidPackageId = package.Id,
                BidPackageCode = package.Code,
                BidPackageName = package.Name,
                Budget = package.Budget,
                PackageStatus = package.Status.ToString(),
                TotalCriteria = criteriaStats?.TotalCriteria ?? 0,
                TotalWeight = criteriaStats?.TotalWeight ?? 0,
                TotalSubmissions = totalSubmissions,
                EvaluatedSubmissions = evaluatedSubmissions,
                PendingSubmissions = pendingSubmissions,
                HighestScore = highestScore,
                LowestScore = lowestScore,
                AverageScore = averageScore,
                IsFinalized = winningSub != null,
                WinningSubmissionId = restrictEvaluator ? null : winningSub?.Id,
                WinningContractorId = restrictEvaluator ? null : winningSub?.ContractorId,
                WinningContractorName = restrictEvaluator ? null : winningSub?.CompanyName,
                WinningContractorTaxCode = restrictEvaluator ? null : winningSub?.TaxCode,
                WinningScore = restrictEvaluator ? null : winningSub?.TotalScore,
                WinningBidPrice = restrictEvaluator ? null : winningSub?.BidPrice
            };

            return ApiResponse<EvaluationSummaryDto>.Ok(summary);
        }

        public async Task<ApiResponse<AwardedBidDto>> GetAwardedBidAsync(int packageId)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Where(bp => bp.Id == packageId)
                .AsNoTracking()
                .Select(bp => new
                {
                    bp.Id,
                    bp.Code,
                    bp.Name,
                    bp.Budget,
                    bp.Status
                })
                .FirstOrDefaultAsync();

            if (package == null)
            {
                return ApiResponse<AwardedBidDto>.Fail("Không tìm thấy gói thầu.");
            }

            // Tìm hồ sơ trúng thầu đã được phê duyệt chính thức (Status == "Selected")
            var awardedSubmission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Where(s => s.BidPackageId == packageId && s.Status == BidSubmissionStatus.Selected)
                .Include(s => s.Contractor)
                    .ThenInclude(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (awardedSubmission == null)
            {
                return ApiResponse<AwardedBidDto>.Fail(
                    "Gói thầu chưa có kết quả phê duyệt trúng thầu chính thức. Vui lòng hoàn tất quá trình đánh giá và phê duyệt nhà thầu trúng thầu trước khi lập hợp đồng.");
            }

            // Kiểm tra xem gói thầu này đã được lập hợp đồng trong hệ thống chưa (bỏ qua hợp đồng đã Terminated)
            var existingContract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Where(c => c.BidPackageId == packageId && c.Status != ContractStatus.Terminated)
                .AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    c.ContractNumber,
                    c.Status
                })
                .FirstOrDefaultAsync();

            var contractor = awardedSubmission.Contractor;
            var contractorUser = contractor?.User;

            var dto = new AwardedBidDto
            {
                BidPackageId = package.Id,
                PackageCode = package.Code,
                PackageName = package.Name,
                Budget = package.Budget,
                PackageStatus = package.Status.ToString(),

                ContractorId = awardedSubmission.ContractorId,
                CompanyName = contractor?.CompanyName ?? string.Empty,
                TaxCode = contractor?.TaxCode,
                Email = contractorUser?.Email,
                Phone = contractorUser?.Phone,
                Address = contractor?.Address,

                SubmissionId = awardedSubmission.Id,
                WinningBidPrice = awardedSubmission.BidPrice,
                TotalScore = awardedSubmission.TotalScore,
                Rank = awardedSubmission.Rank,
                SelectionReason = awardedSubmission.SelectionReason,
                AwardedAt = awardedSubmission.SubmittedAt,

                HasContract = existingContract != null,
                ExistingContractId = existingContract?.Id,
                ExistingContractNumber = existingContract?.ContractNumber,
                ContractStatus = existingContract?.Status.ToString(),
                IsReadyForContract = existingContract == null && package.Status != BidPackageStatus.Contracted
            };

            return ApiResponse<AwardedBidDto>.Ok(dto, "Lấy thông tin nhà thầu trúng thầu thành công.");
        }

        private async Task CalculateRankingsForPackageAsync(int packageId)
        {
            var criteriaList = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Where(c => c.BidPackageId == packageId)
                .AsNoTracking()
                .Select(c => new { c.Id, c.Name, c.Weight, c.MaxScore, c.IsPriceCriteria })
                .ToListAsync();

            if (!criteriaList.Any()) return;

            var totalWeight = criteriaList.Sum(c => c.Weight);
            if (totalWeight <= 0) return;

            var submissions = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.EvaluationScores)
                .Where(s => s.BidPackageId == packageId)
                .ToListAsync();

            // R2-2: Lấy danh sách ID các giám khảo chính thức được phân công trong Tổ chuyên gia
            var assignedEvaluatorIds = await _unitOfWork.Repository<BidPackageEvaluator>()
                .Query()
                .Where(pe => pe.BidPackageId == packageId)
                .Select(pe => pe.EvaluatorId)
                .ToListAsync();

            // Tìm giá dự thầu thấp nhất giữa các hồ sơ hợp lệ có giá dự thầu để áp dụng công thức chấm điểm Giá (Điều 29 NĐ 24/2024/NĐ-CP)
            var activeSubmissionsWithPrice = submissions
                .Where(s => s.Status != BidSubmissionStatus.Withdrawn && s.Status != BidSubmissionStatus.Disqualified && s.BidPrice.HasValue && s.BidPrice.Value > 0)
                .ToList();
            decimal? minBidPrice = activeSubmissionsWithPrice.Any()
                ? activeSubmissionsWithPrice.Min(s => s.BidPrice!.Value)
                : null;

            // R2-3: Danh sách các tiêu chí cần chấm tay (tiêu chí có IsPriceCriteria == true được tính toán tự động từ giá dự thầu)
            var manualCriteria = criteriaList.Where(c => !c.IsPriceCriteria).ToList();

            foreach (var sub in submissions)
            {
                // Bỏ qua nếu hồ sơ đã rút hoặc bị loại
                if (sub.Status == BidSubmissionStatus.Withdrawn || sub.Status == BidSubmissionStatus.Disqualified) continue;

                if (!sub.EvaluationScores.Any())
                {
                    if (sub.Status == BidSubmissionStatus.Evaluated)
                    {
                        sub.Status = BidSubmissionStatus.Submitted;
                    }
                    sub.Rank = null;
                    sub.TotalScore = null;
                    _unitOfWork.Repository<BidSubmission>().Update(sub);
                    continue;
                }

                // Tính điểm chuẩn hóa từng tiêu chí về thang 100 theo MaxScore (P1-1)
                decimal weightedScoreSum = 0;
                foreach (var criteria in criteriaList)
                {
                    decimal criteriaScore;
                    // R2-3: Nếu là tiêu chí Giá: Tự động tính điểm tỷ lệ nghịch theo Luật Đấu thầu
                    if (criteria.IsPriceCriteria && minBidPrice.HasValue && sub.BidPrice.HasValue && sub.BidPrice.Value > 0)
                    {
                        criteriaScore = Math.Round((minBidPrice.Value / sub.BidPrice.Value) * criteria.MaxScore, 2);
                    }
                    else
                    {
                        // Chỉ tính điểm của các giám khảo thuộc Tổ chuyên gia (bỏ qua điểm của người ngoài tổ)
                        var scoresForCriteria = sub.EvaluationScores
                            .Where(es => es.CriteriaId == criteria.Id && (assignedEvaluatorIds.Count == 0 || assignedEvaluatorIds.Contains(es.EvaluatorId)))
                            .Select(es => es.Score)
                            .ToList();

                        criteriaScore = scoresForCriteria.Any() ? (decimal)scoresForCriteria.Average() : 0m;
                    }

                    // Chuẩn hóa điểm từng tiêu chí về thang 100%: (criteriaScore / maxScore) * 100
                    var normalizedScore = criteria.MaxScore > 0
                        ? (criteriaScore / criteria.MaxScore) * 100m
                        : criteriaScore;

                    weightedScoreSum += normalizedScore * criteria.Weight;
                }

                // Điểm tổng hợp theo trọng số (thang 100)
                var finalTotalScore = weightedScoreSum / totalWeight;
                sub.TotalScore = Math.Round(finalTotalScore, 2);

                // R2-2: Căn cứ NĐ 24/2024/NĐ-CP: Điều kiện "chấm đủ":
                // 1. Phải có Tổ chuyên gia (assignedEvaluatorIds.Count > 0)
                // 2. Với MỖI giám khảo trong assignedEvaluatorIds, giám khảo đó PHẢI chấm đủ TẤT CẢ các tiêu chí cần chấm tay (manualCriteria)
                // Cặp (EvaluatorId, CriteriaId) phải tồn tại đầy đủ
                bool isFullyEvaluated = assignedEvaluatorIds.Any() && assignedEvaluatorIds.All(evalId =>
                    manualCriteria.All(crit =>
                        sub.EvaluationScores.Any(es => es.EvaluatorId == evalId && es.CriteriaId == crit.Id)
                    )
                );

                if (isFullyEvaluated)
                {
                    if (sub.Status != BidSubmissionStatus.Selected && sub.Status != BidSubmissionStatus.Rejected)
                    {
                        sub.Status = BidSubmissionStatus.Evaluated;
                    }
                }
                else
                {
                    // Nếu chưa chấm đủ thì không được giữ trạng thái Evaluated và không được xếp Rank
                    if (sub.Status == BidSubmissionStatus.Evaluated)
                    {
                        sub.Status = BidSubmissionStatus.Submitted;
                    }
                    sub.Rank = null;
                }

                _unitOfWork.Repository<BidSubmission>().Update(sub);
            }

            // P2-9: Tự động sắp xếp phân hạng Rank 1, 2, 3... cho các hồ sơ đã hoàn tất đánh giá:
            // Tiêu chuẩn phá hòa điểm: Điểm tổng hợp cao nhất -> Giá dự thầu thấp nhất -> Thời gian nộp sớm nhất
            var scoredSubmissions = submissions
                .Where(s => s.TotalScore.HasValue && (s.Status == BidSubmissionStatus.Evaluated || s.Status == BidSubmissionStatus.Selected))
                .OrderByDescending(s => s.TotalScore!.Value)
                .ThenBy(s => s.BidPrice ?? decimal.MaxValue)
                .ThenBy(s => s.SubmittedAt)
                .ToList();

            int currentRank = 1;
            foreach (var sub in scoredSubmissions)
            {
                sub.Rank = currentRank++;
                _unitOfWork.Repository<BidSubmission>().Update(sub);
            }

            await _unitOfWork.SaveChangesAsync();
        }


        public async Task<ApiResponse<BidEvaluationReportDto>> GetEvaluationReportAsync(int packageId, int currentUserId, bool isInternalStaff)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Where(bp => bp.Id == packageId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (package == null)
            {
                return ApiResponse<BidEvaluationReportDto>.Fail("Không tìm thấy gói thầu.");
            }

            if (!isInternalStaff)
            {
                var isPackageEvaluator = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .ExistsAsync(pe => pe.BidPackageId == packageId && pe.EvaluatorId == currentUserId);

                if (!isPackageEvaluator)
                {
                    return ApiResponse<BidEvaluationReportDto>.Fail("Bạn không thuộc Tổ chuyên gia đánh giá của gói thầu này.");
                }
            }

            // Lấy thông tin Bên mời thầu
            var procuringEntity = await _unitOfWork.Repository<ProcuringEntity>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(pe => pe.UserId == package.CreatedBy);

            var procuringEntityName = procuringEntity?.OrganizationName 
                ?? package.Creator?.FullName 
                ?? "Ban Quản lý Dự án & Đấu thầu";
            var procuringEntityTaxCode = procuringEntity?.TaxCode;

            // Lấy danh sách Tổ chuyên gia
            var packageEvaluators = await _unitOfWork.Repository<BidPackageEvaluator>()
                .Query()
                .Include(pe => pe.Evaluator)
                .Where(pe => pe.BidPackageId == packageId)
                .AsNoTracking()
                .ToListAsync();

            // Lấy tiêu chí đánh giá
            var criteriaList = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Where(c => c.BidPackageId == packageId)
                .OrderBy(c => c.IsPriceCriteria)
                .ThenBy(c => c.Id)
                .AsNoTracking()
                .ToListAsync();

            // Lấy danh sách hồ sơ dự thầu kèm điểm số
            var submissions = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.Contractor)
                .Include(s => s.EvaluationScores)
                    .ThenInclude(es => es.Criteria)
                .Include(s => s.EvaluationScores)
                    .ThenInclude(es => es.Evaluator)
                .Where(s => s.BidPackageId == packageId)
                .AsNoTracking()
                .ToListAsync();

            var committeeMembers = new List<EvaluationCommitteeMemberDto>();
            int memberIndex = 0;
            foreach (var pe in packageEvaluators)
            {
                var evaluatorName = pe.Evaluator?.FullName ?? "Chuyên gia xét thầu";
                var hasScoredAll = submissions.Any() && submissions
                    .Where(s => s.Status != BidSubmissionStatus.Withdrawn && s.Status != BidSubmissionStatus.Disqualified)
                    .All(s => s.EvaluationScores.Any(es => es.EvaluatorId == pe.EvaluatorId));

                string role = memberIndex == 0 ? "Tổ trưởng Tổ chuyên gia" : 
                              (memberIndex % 2 == 1 ? "Thành viên - Chuyên gia Đánh giá Kỹ thuật" : "Thành viên - Chuyên gia Thẩm định Tài chính");

                committeeMembers.Add(new EvaluationCommitteeMemberDto
                {
                    EvaluatorId = pe.EvaluatorId,
                    FullName = evaluatorName,
                    Email = pe.Evaluator?.Email ?? string.Empty,
                    Phone = pe.Evaluator?.Phone,
                    CommitteeRole = role,
                    HasCompletedScoring = hasScoredAll
                });
                memberIndex++;
            }

            var rankingItems = new List<EvaluationReportRankingItemDto>();
            var sortedSubmissions = submissions
                .OrderBy(s => s.Rank.HasValue ? s.Rank.Value : int.MaxValue)
                .ThenByDescending(s => s.TotalScore ?? 0)
                .ThenBy(s => s.BidPrice ?? decimal.MaxValue)
                .ToList();

            foreach (var s in sortedSubmissions)
            {
                decimal? priceSavings = null;
                decimal? priceSavingsPercent = null;
                if (s.BidPrice.HasValue && package.Budget > 0)
                {
                    priceSavings = package.Budget - s.BidPrice.Value;
                    priceSavingsPercent = Math.Round((priceSavings.Value / package.Budget) * 100, 2);
                }

                // Tách điểm kỹ thuật và điểm tài chính
                decimal? techScore = null;
                decimal? priceScore = null;
                var nonPriceCriteriaIds = criteriaList.Where(c => !c.IsPriceCriteria).Select(c => c.Id).ToHashSet();
                var priceCriteriaIds = criteriaList.Where(c => c.IsPriceCriteria).Select(c => c.Id).ToHashSet();

                var techScores = s.EvaluationScores.Where(es => nonPriceCriteriaIds.Contains(es.CriteriaId)).ToList();
                if (techScores.Any())
                {
                    techScore = Math.Round(techScores.Average(es => es.Score), 2);
                }

                var priceScores = s.EvaluationScores.Where(es => priceCriteriaIds.Contains(es.CriteriaId)).ToList();
                if (priceScores.Any())
                {
                    priceScore = Math.Round(priceScores.Average(es => es.Score), 2);
                }

                rankingItems.Add(new EvaluationReportRankingItemDto
                {
                    SubmissionId = s.Id,
                    ContractorId = s.ContractorId,
                    CompanyName = s.Contractor?.CompanyName ?? "Nhà thầu",
                    TaxCode = s.Contractor?.TaxCode,
                    BidPrice = s.BidPrice,
                    PriceSavings = priceSavings,
                    PriceSavingsPercent = priceSavingsPercent,
                    TotalScore = s.TotalScore,
                    TechnicalScore = techScore,
                    PriceScore = priceScore,
                    Rank = s.Rank,
                    Status = s.Status.ToString(),
                    DisqualificationReason = s.DisqualificationReason,
                    SubmittedAt = s.SubmittedAt,
                    Scores = s.EvaluationScores.Select(es => new EvaluationScoreDto
                    {
                        Id = es.Id,
                        BidSubmissionId = es.BidSubmissionId,
                        CriteriaId = es.CriteriaId,
                        CriteriaName = es.Criteria?.Name ?? string.Empty,
                        MaxScore = es.Criteria?.MaxScore ?? 0,
                        Weight = es.Criteria?.Weight ?? 0,
                        EvaluatorId = es.EvaluatorId,
                        EvaluatorName = es.Evaluator?.FullName ?? string.Empty,
                        Score = es.Score,
                        Comment = es.Comment,
                        ScoredAt = es.ScoredAt
                    }).ToList()
                });
            }

            var winningSub = sortedSubmissions.FirstOrDefault(s => s.Status == BidSubmissionStatus.Selected || (s.Rank == 1 && s.TotalScore.HasValue));
            bool isFinalized = package.Status == BidPackageStatus.Awarded || submissions.Any(s => s.Status == BidSubmissionStatus.Selected);

            string conclusion = isFinalized && winningSub != null
                ? $"Căn cứ kết quả đánh giá toàn diện về năng lực, kinh nghiệm, giải pháp kỹ thuật và giá dự thầu, Tổ chuyên gia thống nhất kiến nghị Bên mời thầu / Người có thẩm quyền phê duyệt trúng thầu cho đơn vị: {winningSub.Contractor?.CompanyName} với giá trúng thầu {winningSub.BidPrice:N0} VND."
                : (winningSub != null
                    ? $"Căn cứ tổng hợp kết quả chấm điểm của các thành viên Tổ chuyên gia, Nhà thầu {winningSub.Contractor?.CompanyName} có tổng điểm đánh giá cao nhất ({winningSub.TotalScore:N2}/100 điểm) và xếp hạng thứ nhất (#1), đủ điều kiện kiến nghị trúng thầu sau khi hoàn tất thương thảo hợp đồng."
                    : "Hội đồng chuyên gia đang trong quá trình đánh giá và tổng hợp kết quả thẩm định E-HSDT.");

            var reportDto = new BidEvaluationReportDto
            {
                ReportNumber = $"BC-ĐGHSDT-{package.Code}",
                GeneratedAt = DateTime.UtcNow,
                Location = "Hà Nội",
                PackageId = package.Id,
                PackageCode = package.Code,
                PackageName = package.Name,
                Budget = package.Budget,
                PackageStatus = package.Status.ToString(),
                Deadline = package.Deadline,
                ProcuringEntityName = procuringEntityName,
                ProcuringEntityTaxCode = procuringEntityTaxCode,
                BiddingMethod = "Đấu thầu rộng rãi qua mạng (E-Bidding)",
                BiddingProcedure = "Một giai đoạn một túi hồ sơ",
                CommitteeMembers = committeeMembers,
                Criteria = criteriaList.Select(c => new EvaluationCriteriaDto
                {
                    Id = c.Id,
                    BidPackageId = c.BidPackageId,
                    Name = c.Name,
                    MaxScore = c.MaxScore,
                    Weight = c.Weight,
                    IsPriceCriteria = c.IsPriceCriteria
                }).ToList(),
                TotalSubmissions = submissions.Count,
                EvaluatedSubmissions = submissions.Count(s => s.Status == BidSubmissionStatus.Evaluated || s.Status == BidSubmissionStatus.Selected),
                Rankings = rankingItems,
                IsFinalized = isFinalized,
                WinningContractorName = winningSub?.Contractor?.CompanyName,
                WinningContractorTaxCode = winningSub?.Contractor?.TaxCode,
                WinningBidPrice = winningSub?.BidPrice,
                WinningTotalScore = winningSub?.TotalScore,
                DecisionReason = winningSub?.SelectionReason ?? "Đạt điểm đánh giá cao nhất và đáp ứng toàn diện hồ sơ mời thầu.",
                ConclusionSummary = conclusion
            };

            return ApiResponse<BidEvaluationReportDto>.Ok(reportDto, "Lấy dữ liệu Báo cáo Đánh giá HSDT thành công.");
        }

        public async Task<ApiResponse<bool>> RecalculatePackageRankingsAsync(int packageId)
        {
            await CalculateRankingsForPackageAsync(packageId);
            return ApiResponse<bool>.Ok(true, "Đã tính toán lại xếp hạng đánh giá cho gói thầu thành công.");
        }
    }
}
