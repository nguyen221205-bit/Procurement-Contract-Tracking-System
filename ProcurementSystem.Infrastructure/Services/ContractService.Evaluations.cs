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
    public partial class ContractService : IContractorEvaluationService
    {
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

            if (!await PackageWriteAuthorization.CanWriteAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
            {
                return ApiResponse<ContractorEvaluationDto>.Forbidden("Bạn chỉ có quyền đánh giá nghiệm thu cho gói thầu do chính mình phụ trách.");
            }

            if (contract.Status != ContractStatus.Completed && contract.Status != ContractStatus.Terminated)
            {
                return ApiResponse<ContractorEvaluationDto>.Fail("Chỉ được đánh giá nghiệm thu tổng kết khi hợp đồng đã ở trạng thái Hoàn thành (Completed) hoặc Chấm dứt (Terminated).");
            }

            var total = Math.Round(request.QualityScore + request.ScheduleScore + request.ComplianceScore, 2);
            var grade = Core.Helpers.ContractorRatingPolicy.Grade(total);
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

            if (isInternalStaff && !isAdmin &&
                !await PackageWriteAuthorization.CanReadEntityStaffAsync(_unitOfWork, evaluation.Contract?.BidPackage, userId, isAdmin))
            {
                return ApiResponse<ContractorEvaluationDto?>.Forbidden("Bạn chỉ có quyền xem đánh giá của hợp đồng thuộc đơn vị mời thầu của mình.");
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
    }
}
