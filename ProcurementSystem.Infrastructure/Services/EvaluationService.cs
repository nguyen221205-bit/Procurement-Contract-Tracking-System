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

        public EvaluationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
                    Weight = c.Weight
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
                Weight = request.Weight
            };

            await _unitOfWork.Repository<EvaluationCriteria>().AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var dto = new EvaluationCriteriaDto
            {
                Id = entity.Id,
                BidPackageId = entity.BidPackageId,
                Name = entity.Name,
                MaxScore = entity.MaxScore,
                Weight = entity.Weight
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

            _unitOfWork.Repository<EvaluationCriteria>().Update(criteria);
            await _unitOfWork.SaveChangesAsync();

            var dto = new EvaluationCriteriaDto
            {
                Id = criteria.Id,
                BidPackageId = criteria.BidPackageId,
                Name = criteria.Name,
                MaxScore = criteria.MaxScore,
                Weight = criteria.Weight
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
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Không tìm thấy hồ sơ dự thầu.");
            }

            // Kiểm tra trạng thái hồ sơ dự thầu
            if (submission.Status == "Withdrawn")
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Không thể chấm điểm hồ sơ dự thầu đã rút lui khỏi gói thầu.");
            }

            if (submission.Status == "Selected" || submission.Status == "Rejected")
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

            // Nguyên tắc độc lập chấm điểm (NĐ 24/2024/NĐ-CP): Trong giai đoạn Evaluating, giám khảo chỉ được xem điểm của chính mình
            if (submission.BidPackage.Status == BidPackageStatus.Evaluating && !isInternalStaff)
            {
                query = query.Where(s => s.EvaluatorId == currentUserId);
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

        public async Task<ApiResponse<List<SubmissionRankingDto>>> GetPackageRankingsAsync(int packageId)
        {
            var result = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Where(s => s.BidPackageId == packageId)
                .AsNoTracking()
                .OrderBy(s => s.Rank == null)
                .ThenBy(s => s.Rank)
                .ThenByDescending(s => s.TotalScore)
                .Select(s => new SubmissionRankingDto
                {
                    SubmissionId = s.Id,
                    BidPackageId = s.BidPackageId,
                    ContractorId = s.ContractorId,
                    CompanyName = s.Contractor.CompanyName,
                    TaxCode = s.Contractor.TaxCode,
                    BidPrice = s.BidPrice,
                    TotalScore = s.TotalScore,
                    Rank = s.Rank,
                    Status = s.Status,
                    SubmittedAt = s.SubmittedAt,
                    Scores = s.EvaluationScores.Select(es => new EvaluationScoreDto
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
                    }).ToList()
                })
                .ToListAsync();

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

            if (!selectedSubmission.TotalScore.HasValue || selectedSubmission.Status != "Evaluated")
            {
                return ApiResponse<bool>.Fail("Hồ sơ dự thầu được chọn chưa hoàn tất quá trình chấm điểm đánh giá.");
            }

            // Căn cứ NĐ 24/2024/NĐ-CP: Toàn bộ hồ sơ dự thầu hợp lệ phải hoàn tất đánh giá đủ điều kiện
            var activeSubmissions = submissions.Where(s => s.Status != "Withdrawn" && s.Status != "Disqualified").ToList();
            var incompleteSubmissions = activeSubmissions.Where(s => s.Status != "Evaluated").ToList();
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

            // Đánh dấu hồ sơ trúng thầu và từ chối các hồ sơ còn lại
            foreach (var sub in submissions)
            {
                if (sub.Id == request.SelectedSubmissionId)
                {
                    sub.Status = "Selected";
                    sub.SelectionReason = request.DecisionReason;
                }
                else
                {
                    sub.Status = "Rejected";
                }
                _unitOfWork.Repository<BidSubmission>().Update(sub);
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

            return ApiResponse<bool>.Ok(true, $"Đã phê duyệt nhà thầu (Mã hồ sơ: #{request.SelectedSubmissionId}) trúng thầu thành công. Gói thầu đã chuyển sang trạng thái 'Awarded' và sẵn sàng để ký hợp đồng.");
        }

        public async Task<ApiResponse<EvaluationSummaryDto>> GetEvaluationSummaryAsync(int packageId)
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
                .CountAsync(s => s.Status == "Evaluated" || s.Status == "Selected" || s.Status == "Rejected");
            var pendingSubmissions = totalSubmissions - evaluatedSubmissions;

            // Thống kê điểm số (chỉ lấy các hồ sơ đã có điểm)
            var scoredQuery = submissionsQuery.Where(s => s.TotalScore.HasValue);
            decimal? highestScore = null;
            decimal? lowestScore = null;
            decimal? averageScore = null;

            if (await scoredQuery.AnyAsync())
            {
                highestScore = await scoredQuery.MaxAsync(s => s.TotalScore);
                lowestScore = await scoredQuery.MinAsync(s => s.TotalScore);
                var avg = await scoredQuery.AverageAsync(s => s.TotalScore!.Value);
                averageScore = Math.Round(avg, 2);
            }

            // Thông tin hồ sơ trúng thầu (nếu đã phê duyệt)
            var winningSub = await submissionsQuery
                .Where(s => s.Status == "Selected")
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
                WinningSubmissionId = winningSub?.Id,
                WinningContractorId = winningSub?.ContractorId,
                WinningContractorName = winningSub?.CompanyName,
                WinningContractorTaxCode = winningSub?.TaxCode,
                WinningScore = winningSub?.TotalScore,
                WinningBidPrice = winningSub?.BidPrice
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
                .Where(s => s.BidPackageId == packageId && s.Status == "Selected")
                .Include(s => s.Contractor)
                    .ThenInclude(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (awardedSubmission == null)
            {
                return ApiResponse<AwardedBidDto>.Fail(
                    "Gói thầu chưa có kết quả phê duyệt trúng thầu chính thức. Vui lòng hoàn tất quá trình đánh giá và phê duyệt nhà thầu trúng thầu trước khi lập hợp đồng.");
            }

            // Kiểm tra xem gói thầu này đã được lập hợp đồng trong hệ thống chưa
            var existingContract = await _unitOfWork.Repository<Contract>()
                .Query()
                .Where(c => c.BidPackageId == packageId)
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
                .Select(c => new { c.Id, c.Name, c.Weight, c.MaxScore })
                .ToListAsync();

            if (!criteriaList.Any()) return;

            var totalWeight = criteriaList.Sum(c => c.Weight);
            if (totalWeight <= 0) return;

            var submissions = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.EvaluationScores)
                .Where(s => s.BidPackageId == packageId)
                .ToListAsync();

            // Đếm số giám khảo được phân công cho gói thầu để xác định điều kiện hoàn tất chấm điểm
            var assignedEvaluatorsCount = await _unitOfWork.Repository<BidPackageEvaluator>()
                .Query()
                .CountAsync(pe => pe.BidPackageId == packageId);

            var criteriaCount = criteriaList.Count;
            var requiredScoreCount = (assignedEvaluatorsCount > 0 ? assignedEvaluatorsCount : 1) * criteriaCount;

            // Tìm giá dự thầu thấp nhất giữa các hồ sơ hợp lệ có giá dự thầu để áp dụng công thức chấm điểm Giá (Điều 29 NĐ 24/2024/NĐ-CP)
            var activeSubmissionsWithPrice = submissions
                .Where(s => s.Status != "Withdrawn" && s.Status != "Disqualified" && s.BidPrice.HasValue && s.BidPrice.Value > 0)
                .ToList();
            decimal? minBidPrice = activeSubmissionsWithPrice.Any()
                ? activeSubmissionsWithPrice.Min(s => s.BidPrice!.Value)
                : null;

            bool IsPriceCriteria(string name)
            {
                var lower = name.ToLowerInvariant();
                return lower.Contains("giá") || lower.Contains("tài chính") || lower.Contains("chi phí") || lower.Contains("price");
            }

            foreach (var sub in submissions)
            {
                // Bỏ qua nếu hồ sơ đã rút hoặc bị loại
                if (sub.Status == "Withdrawn" || sub.Status == "Disqualified") continue;

                if (!sub.EvaluationScores.Any()) continue;

                // Tính điểm chuẩn hóa từng tiêu chí về thang 100 theo MaxScore (P1-1)
                decimal weightedScoreSum = 0;
                foreach (var criteria in criteriaList)
                {
                    decimal criteriaScore;
                    // Nếu là tiêu chí Giá: Tự động tính điểm tỷ lệ nghịch theo Luật Đấu thầu
                    if (IsPriceCriteria(criteria.Name) && minBidPrice.HasValue && sub.BidPrice.HasValue && sub.BidPrice.Value > 0)
                    {
                        criteriaScore = Math.Round((minBidPrice.Value / sub.BidPrice.Value) * criteria.MaxScore, 2);
                    }
                    else
                    {
                        var scoresForCriteria = sub.EvaluationScores
                            .Where(es => es.CriteriaId == criteria.Id)
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

                // Căn cứ NĐ 24/2024/NĐ-CP: CHỈ gán trạng thái 'Evaluated' khi ĐỦ 100% giám khảo chấm đủ 100% tiêu chí
                if (sub.EvaluationScores.Count >= requiredScoreCount && sub.Status != "Selected" && sub.Status != "Rejected")
                {
                    sub.Status = "Evaluated";
                }
            }

            // P2-9: Tự động sắp xếp phân hạng Rank 1, 2, 3... cho các hồ sơ đã hoàn tất đánh giá:
            // Tiêu chuẩn phá hòa điểm: Điểm tổng hợp cao nhất -> Giá dự thầu thấp nhất -> Thời gian nộp sớm nhất
            var scoredSubmissions = submissions
                .Where(s => s.TotalScore.HasValue && (s.Status == "Evaluated" || s.Status == "Selected"))
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
    }
}
