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
            var evaluator = await _unitOfWork.Repository<User>().GetByIdAsync(evaluatorId);
            if (evaluator == null || !evaluator.IsActive)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Tài khoản giám khảo không tồn tại hoặc đã bị vô hiệu hóa.");
            }

            var package = submission.BidPackage;

            // Ràng buộc nghiệp vụ: Không được chấm điểm khi gói thầu vẫn đang Open (chưa hết hạn/chưa đóng thầu)
            if (package.Status == BidPackageStatus.Open)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Chưa thể chấm điểm khi gói thầu đang mở thầu. Vui lòng đóng thầu trước khi tiến hành chấm điểm.");
            }

            if (package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail("Gói thầu đã ký kết hợp đồng, không thể sửa đổi điểm đánh giá.");
            }

            // Tự động chuyển gói thầu sang Evaluating nếu đang Closed
            if (package.Status == BidPackageStatus.Closed)
            {
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
            return await GetScoresBySubmissionAsync(submissionId);
        }

        public async Task<ApiResponse<List<EvaluationScoreDto>>> GetScoresBySubmissionAsync(int submissionId)
        {
            var scores = await _unitOfWork.Repository<EvaluationScore>()
                .Query()
                .Include(s => s.Criteria)
                .Include(s => s.Evaluator)
                .Where(s => s.BidSubmissionId == submissionId)
                .AsNoTracking()
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

        public async Task<ApiResponse<bool>> FinalizeEvaluationAsync(int packageId, int selectedSubmissionId, int userId, bool isAdmin)
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

            var selectedSubmission = submissions.FirstOrDefault(s => s.Id == selectedSubmissionId);
            if (selectedSubmission == null)
            {
                return ApiResponse<bool>.Fail("Hồ sơ dự thầu được chọn không thuộc gói thầu này.");
            }

            if (!selectedSubmission.TotalScore.HasValue || selectedSubmission.Status != "Evaluated")
            {
                return ApiResponse<bool>.Fail("Hồ sơ dự thầu được chọn chưa hoàn tất quá trình chấm điểm đánh giá.");
            }

            // Đánh dấu hồ sơ trúng thầu và từ chối các hồ sơ còn lại
            foreach (var sub in submissions)
            {
                if (sub.Id == selectedSubmissionId)
                {
                    sub.Status = "Selected";
                }
                else
                {
                    sub.Status = "Rejected";
                }
                _unitOfWork.Repository<BidSubmission>().Update(sub);
            }

            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, $"Đã phê duyệt nhà thầu (Mã hồ sơ: #{selectedSubmissionId}) trúng thầu thành công. Gói thầu sẵn sàng để ký hợp đồng.");
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
                WinningScore = winningSub?.TotalScore
            };

            return ApiResponse<EvaluationSummaryDto>.Ok(summary);
        }

        private async Task CalculateRankingsForPackageAsync(int packageId)
        {
            var criteriaList = await _unitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Where(c => c.BidPackageId == packageId)
                .AsNoTracking()
                .Select(c => new { c.Id, c.Weight })
                .ToListAsync();

            if (!criteriaList.Any()) return;

            var totalWeight = criteriaList.Sum(c => c.Weight);
            if (totalWeight <= 0) return;

            var criteriaWeights = criteriaList.ToDictionary(c => c.Id, c => c.Weight);

            var submissions = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.EvaluationScores)
                .Where(s => s.BidPackageId == packageId)
                .ToListAsync();

            foreach (var sub in submissions)
            {
                if (!sub.EvaluationScores.Any()) continue;

                // Tính điểm bình quân từng tiêu chí (nếu có nhiều giám khảo chấm cùng tiêu chí)
                decimal weightedScoreSum = 0;
                foreach (var (criteriaId, weight) in criteriaWeights)
                {
                    var scoresForCriteria = sub.EvaluationScores
                        .Where(es => es.CriteriaId == criteriaId)
                        .Select(es => es.Score)
                        .ToList();

                    if (scoresForCriteria.Any())
                    {
                        var avgScore = scoresForCriteria.Average();
                        weightedScoreSum += avgScore * weight;
                    }
                }

                // Điểm tổng hợp theo trọng số = Tổng (Điểm bình quân tiêu chí * Trọng số) / Tổng trọng số
                var finalTotalScore = weightedScoreSum / totalWeight;
                sub.TotalScore = Math.Round(finalTotalScore, 2);
                if (sub.Status != "Selected" && sub.Status != "Rejected")
                {
                    sub.Status = "Evaluated";
                }
            }

            // Tự động sắp xếp phân hạng Rank 1, 2, 3...
            var scoredSubmissions = submissions
                .Where(s => s.TotalScore.HasValue)
                .OrderByDescending(s => s.TotalScore!.Value)
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
