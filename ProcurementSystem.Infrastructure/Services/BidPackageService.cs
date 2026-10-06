using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidPackage;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class BidPackageService : IBidPackageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public BidPackageService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<PaginatedList<BidPackageSummaryDto>>> GetBidPackagesAsync(BidPackageFilterParams filter)
        {
            var query = _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .AsNoTracking();

            // 1. Tìm kiếm theo mã hoặc tên gói thầu
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var keyword = filter.Search.Trim().ToLower();
                query = query.Where(bp => bp.Code.ToLower().Contains(keyword)
                                       || bp.Name.ToLower().Contains(keyword));
            }

            // 2. Lọc theo loại gói thầu (Goods, Construction, Service)
            if (filter.Type.HasValue)
            {
                query = query.Where(bp => bp.Type == filter.Type.Value);
            }

            // 3. Lọc theo trạng thái
            if (filter.Status.HasValue)
            {
                query = query.Where(bp => bp.Status == filter.Status.Value);
            }

            // 4. Lọc theo khoảng ngân sách
            if (filter.MinBudget.HasValue)
            {
                query = query.Where(bp => bp.Budget >= filter.MinBudget.Value);
            }
            if (filter.MaxBudget.HasValue)
            {
                query = query.Where(bp => bp.Budget <= filter.MaxBudget.Value);
            }

            // 5. Lọc theo thời hạn nộp thầu
            if (filter.FromDeadline.HasValue)
            {
                query = query.Where(bp => bp.Deadline >= filter.FromDeadline.Value);
            }
            if (filter.ToDeadline.HasValue)
            {
                query = query.Where(bp => bp.Deadline <= filter.ToDeadline.Value);
            }

            // 6. Lọc theo Giám khảo được phân công vào Tổ chuyên gia
            if (filter.EvaluatorId.HasValue)
            {
                query = query.Where(bp => bp.PackageEvaluators.Any(pe => pe.EvaluatorId == filter.EvaluatorId.Value));
            }

            // Mặc định sắp xếp gói thầu mới tạo lên đầu
            query = query.OrderByDescending(bp => bp.CreatedAt);

            var totalCount = await query.CountAsync();
            var pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var items = await query.Skip((pageIndex - 1) * pageSize)
                                   .Take(pageSize)
                                   .Select(bp => new BidPackageSummaryDto
                                   {
                                       Id = bp.Id,
                                       Code = bp.Code,
                                       Name = bp.Name,
                                       Type = bp.Type,
                                       Budget = bp.Budget,
                                       Deadline = bp.Deadline,
                                       Status = bp.Status,
                                       CreatedByName = bp.Creator != null ? bp.Creator.FullName : null,
                                       CreatedAt = bp.CreatedAt,
                                       DocumentsCount = bp.BidDocuments.Count,
                                       SubmissionsCount = bp.BidSubmissions.Count,
                                       IsAwarded = bp.BidSubmissions.Any(s => s.Status == "Selected"),
                                       EvaluatorIds = bp.PackageEvaluators.Select(pe => pe.EvaluatorId).ToList()
                                   })
                                   .ToListAsync();

            var paginatedList = new PaginatedList<BidPackageSummaryDto>(items, totalCount, pageIndex, pageSize);
            return ApiResponse<PaginatedList<BidPackageSummaryDto>>.Ok(paginatedList);
        }

        public async Task<ApiResponse<BidPackageDto>> GetBidPackageByIdAsync(int id)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (package == null)
            {
                return ApiResponse<BidPackageDto>.Fail("Không tìm thấy gói thầu.");
            }

            var dto = MapToBidPackageDto(package);
            return ApiResponse<BidPackageDto>.Ok(dto);
        }

        public async Task<ApiResponse<BidPackageDto>> CreateBidPackageAsync(CreateBidPackageRequest request, int userId)
        {
            // 1. Kiểm tra hạn nộp thầu phải trong tương lai
            if (request.Deadline <= DateTime.UtcNow)
            {
                return ApiResponse<BidPackageDto>.Fail("Thời hạn nộp thầu phải lớn hơn thời điểm hiện tại.");
            }

            // 2. Xác định mã gói thầu (tự sinh nếu để trống)
            string code;
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                var year = DateTime.UtcNow.Year;
                var count = await _unitOfWork.Repository<BidPackage>()
                    .Query()
                    .CountAsync(p => p.CreatedAt.Year == year);

                int seq = count + 1;
                do
                {
                    code = $"PKG-{year}-{seq:D4}";
                    seq++;
                } while (await _unitOfWork.Repository<BidPackage>().ExistsAsync(bp => bp.Code == code));
            }
            else
            {
                code = request.Code.Trim().ToUpper();
                var isExist = await _unitOfWork.Repository<BidPackage>().ExistsAsync(bp => bp.Code == code);
                if (isExist)
                {
                    return ApiResponse<BidPackageDto>.Fail($"Mã gói thầu '{code}' đã tồn tại trong hệ thống.");
                }
            }

            var entity = new BidPackage
            {
                Code = code,
                Name = request.Name.Trim(),
                Type = request.Type,
                Budget = request.Budget,
                Deadline = request.Deadline,
                Status = BidPackageStatus.Open,
                Description = request.Description?.Trim(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<BidPackage>().AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            // Load lại creator để hiển thị DTO
            var createdPackage = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .AsNoTracking()
                .FirstAsync(bp => bp.Id == entity.Id);

            return ApiResponse<BidPackageDto>.Ok(MapToBidPackageDto(createdPackage), "Tạo gói thầu thành công.");
        }

        public async Task<ApiResponse<BidPackageDto>> UpdateBidPackageAsync(int id, UpdateBidPackageRequest request, int userId, bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (package == null)
            {
                return ApiResponse<BidPackageDto>.Fail("Không tìm thấy gói thầu.");
            }

            // Phân quyền: Chỉ người tạo hoặc Admin mới được sửa
            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<BidPackageDto>.Fail("Bạn không có quyền chỉnh sửa gói thầu này.");
            }

            // P2-1: Chỉ cho phép chỉnh sửa thông tin khi gói thầu đang mở thầu (Open)
            if (package.Status != BidPackageStatus.Open)
            {
                return ApiResponse<BidPackageDto>.Fail($"Không thể chỉnh sửa gói thầu khi đang ở trạng thái '{package.Status}'. Chỉ được phép chỉnh sửa khi gói thầu đang mở thầu (Open).");
            }

            // P2-1: Nếu đã có nhà thầu nộp hồ sơ, không cho phép giảm ngân sách xuống dưới giá dự thầu của các hồ sơ đã nộp
            var existingSubmissions = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Where(s => s.BidPackageId == id && s.Status != "Withdrawn")
                .ToListAsync();

            if (existingSubmissions.Any())
            {
                var minBidPrice = existingSubmissions.Where(s => s.BidPrice.HasValue).Min(s => s.BidPrice!.Value);
                if (request.Budget < minBidPrice)
                {
                    return ApiResponse<BidPackageDto>.Fail(
                        $"Không thể điều chỉnh ngân sách xuống {request.Budget:N0} VNĐ vì đã có hồ sơ dự thầu tham gia với giá {minBidPrice:N0} VNĐ. Ngân sách mới không được thấp hơn giá dự thầu của các nhà thầu đã nộp.");
                }
            }

            if (request.Deadline <= DateTime.UtcNow)
            {
                return ApiResponse<BidPackageDto>.Fail("Thời hạn nộp thầu phải lớn hơn thời điểm hiện tại.");
            }

            package.Name = request.Name.Trim();
            package.Type = request.Type;
            package.Budget = request.Budget;
            package.Deadline = request.Deadline;
            package.Description = request.Description?.Trim();
            package.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<BidPackage>().Update(package);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<BidPackageDto>.Ok(MapToBidPackageDto(package), "Cập nhật thông tin gói thầu thành công.");
        }

        public async Task<ApiResponse<BidPackageDto>> ChangeStatusAsync(int id, ChangeBidPackageStatusRequest request, int userId, bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Creator)
                .Include(bp => bp.BidDocuments)
                .Include(bp => bp.BidSubmissions)
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (package == null)
            {
                return ApiResponse<BidPackageDto>.Fail("Không tìm thấy gói thầu.");
            }

            // Phân quyền: Chỉ người tạo hoặc Admin mới được đổi trạng thái
            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<BidPackageDto>.Fail("Bạn không có quyền thay đổi trạng thái gói thầu này.");
            }

            if (package.Status == request.NewStatus)
            {
                return ApiResponse<BidPackageDto>.Ok(MapToBidPackageDto(package), "Trạng thái không thay đổi.");
            }

            // State Machine Validation
            if (package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<BidPackageDto>.Fail("Gói thầu đã ký hợp đồng, không thể thay đổi trạng thái.");
            }

            // Kiểm tra quy tắc chuyển trạng thái hợp lệ
            var isValidTransition = (package.Status, request.NewStatus) switch
            {
                // Từ Open có thể chuyển sang Closed (Đóng thầu)
                (BidPackageStatus.Open, BidPackageStatus.Closed) => true,

                // Từ Closed có thể chuyển sang Evaluating (Chấm điểm) hoặc mở lại Open (Gia hạn thầu)
                (BidPackageStatus.Closed, BidPackageStatus.Evaluating) => true,
                (BidPackageStatus.Closed, BidPackageStatus.Open) => true,

                // Từ Evaluating có thể sang Contracted (Hoàn tất đấu thầu & ký HĐ) hoặc quay lại Closed
                (BidPackageStatus.Evaluating, BidPackageStatus.Contracted) => true,
                (BidPackageStatus.Evaluating, BidPackageStatus.Closed) => true,

                _ => false
            };

            if (!isValidTransition)
            {
                return ApiResponse<BidPackageDto>.Fail($"Không thể chuyển trạng thái từ '{package.Status}' sang '{request.NewStatus}'. Quy trình hợp lệ: Mở thầu -> Đóng thầu -> Chấm điểm -> Ký hợp đồng.");
            }

            // P2-3: Đóng thầu trước thời hạn (Deadline) bắt buộc phải có lý do
            if (package.Status == BidPackageStatus.Open && request.NewStatus == BidPackageStatus.Closed)
            {
                if (package.Deadline > DateTime.UtcNow && string.IsNullOrWhiteSpace(request.Reason))
                {
                    return ApiResponse<BidPackageDto>.Fail(
                        "Gói thầu chưa hết thời hạn nộp thầu. Nếu muốn đóng thầu trước thời hạn, bắt buộc phải cung cấp lý do/căn cứ pháp lý.");
                }
            }

            // Lựa chọn A: Bắt buộc tiêu chí phải đủ 100% trước khi bắt đầu giai đoạn Chấm điểm (Evaluating)
            if (request.NewStatus == BidPackageStatus.Evaluating)
            {
                var criteriaList = await _unitOfWork.Repository<EvaluationCriteria>()
                    .Query()
                    .Where(c => c.BidPackageId == id)
                    .ToListAsync();

                if (!criteriaList.Any())
                {
                    return ApiResponse<BidPackageDto>.Fail("Không thể bắt đầu giai đoạn Chấm điểm. Gói thầu chưa được thiết lập bộ tiêu chí đánh giá.");
                }

                var totalWeight = criteriaList.Sum(c => c.Weight);
                if (totalWeight != 100)
                {
                    return ApiResponse<BidPackageDto>.Fail(
                        $"Không thể bắt đầu giai đoạn Chấm điểm. Tổng trọng số bộ tiêu chí phải đạt đúng 100% (Hiện tại: {totalWeight}%).");
                }

                // Kiểm tra điều kiện Tổ chuyên gia (Ban giám khảo) theo quy định Luật Đấu thầu: Tối thiểu 3 thành viên và là số lẻ
                var evaluatorsCount = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .CountAsync(pe => pe.BidPackageId == id);

                if (evaluatorsCount < 3 || evaluatorsCount % 2 == 0)
                {
                    return ApiResponse<BidPackageDto>.Fail(
                        $"Không thể bắt đầu giai đoạn Chấm điểm. Tổ chuyên gia phải có tối thiểu 3 thành viên và số lượng thành viên phải là số lẻ (3, 5, 7,...) theo quy định của Luật Đấu thầu (Hiện tại: {evaluatorsCount} thành viên). Vui lòng phân công đủ giám khảo trước khi chuyển trạng thái.");
                }
            }

            package.Status = request.NewStatus;
            package.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<BidPackage>().Update(package);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<BidPackageDto>.Ok(MapToBidPackageDto(package), $"Chuyển trạng thái gói thầu sang '{request.NewStatus}' thành công.");
        }

        public async Task<ApiResponse<List<BidDocumentDto>>> UploadDocumentsAsync(int bidPackageId, List<IFormFile> files, int userId, bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(bidPackageId);
            if (package == null)
            {
                return ApiResponse<List<BidDocumentDto>>.Fail("Không tìm thấy gói thầu.");
            }

            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<List<BidDocumentDto>>.Fail("Bạn không có quyền upload tài liệu cho gói thầu này.");
            }

            // P2-2: Căn cứ Luật Đấu thầu: Chỉ được phép phát hành hoặc bổ sung tài liệu HSMT khi gói thầu đang trong thời hạn mở thầu (Open)
            if (package.Status != BidPackageStatus.Open)
            {
                return ApiResponse<List<BidDocumentDto>>.Fail(
                    $"Không thể tải lên tài liệu HSMT khi gói thầu đang ở trạng thái '{package.Status}'. Chỉ được phép thêm tài liệu khi gói thầu đang mở thầu (Open).");
            }

            if (files == null || files.Count == 0)
            {
                return ApiResponse<List<BidDocumentDto>>.Fail("Vui lòng chọn ít nhất một file để upload.");
            }

            var uploadedDtos = new List<BidDocumentDto>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Lưu file vào thư mục uploads/bid-packages/{bidPackageId}/
                var relativePath = await _fileStorageService.SaveFileAsync(file, $"bid-packages/{bidPackageId}");

                var doc = new BidDocument
                {
                    BidPackageId = bidPackageId,
                    FileName = file.FileName,
                    FilePath = relativePath,
                    UploadedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<BidDocument>().AddAsync(doc);
                await _unitOfWork.SaveChangesAsync();

                uploadedDtos.Add(new BidDocumentDto
                {
                    Id = doc.Id,
                    BidPackageId = doc.BidPackageId,
                    FileName = doc.FileName,
                    FilePath = doc.FilePath,
                    UploadedAt = doc.UploadedAt
                });
            }

            return ApiResponse<List<BidDocumentDto>>.Ok(uploadedDtos, $"Đã upload thành công {uploadedDtos.Count} tài liệu mời thầu.");
        }

        public async Task<ApiResponse<bool>> DeleteDocumentAsync(int bidPackageId, int documentId, int userId, bool isAdmin)
        {
            var doc = await _unitOfWork.Repository<BidDocument>().GetByIdAsync(documentId);
            if (doc == null || doc.BidPackageId != bidPackageId)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy tài liệu cần xóa.");
            }

            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(bidPackageId);
            if (package == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy gói thầu liên quan.");
            }

            if (!isAdmin && package.CreatedBy != userId)
            {
                return ApiResponse<bool>.Fail("Bạn không có quyền xóa tài liệu của gói thầu này.");
            }

            if (package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<bool>.Fail("Gói thầu đã ký hợp đồng, không thể xóa tài liệu mời thầu.");
            }

            // Xóa file vật lý
            _fileStorageService.DeleteFile(doc.FilePath);

            // Xóa record trong DB
            _unitOfWork.Repository<BidDocument>().Delete(doc);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Xóa tài liệu mời thầu thành công.");
        }

        public async Task<ApiResponse<List<PackageEvaluatorDto>>> GetPackageEvaluatorsAsync(int packageId)
        {
            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(packageId);
            if (package == null)
            {
                return ApiResponse<List<PackageEvaluatorDto>>.Fail("Không tìm thấy gói thầu.");
            }

            var evaluators = await _unitOfWork.Repository<BidPackageEvaluator>()
                .Query()
                .Include(pe => pe.Evaluator)
                .Where(pe => pe.BidPackageId == packageId)
                .OrderBy(pe => pe.AssignedAt)
                .ToListAsync();

            // Kiểm tra xem giám khảo nào đã chấm điểm hồ sơ trong gói thầu này
            var scoredEvaluatorIds = await _unitOfWork.Repository<EvaluationScore>()
                .Query()
                .Where(es => es.Criteria.BidPackageId == packageId)
                .Select(es => es.EvaluatorId)
                .Distinct()
                .ToListAsync();

            var dtos = evaluators.Select(pe => new PackageEvaluatorDto
            {
                EvaluatorId = pe.EvaluatorId,
                FullName = pe.Evaluator?.FullName ?? "N/A",
                Email = pe.Evaluator?.Email ?? "N/A",
                Phone = pe.Evaluator?.Phone,
                AssignedAt = pe.AssignedAt,
                AssignedBy = pe.AssignedBy,
                AssignedByName = pe.Assigner?.FullName,
                HasSubmittedScores = scoredEvaluatorIds.Contains(pe.EvaluatorId)
            }).ToList();

            return ApiResponse<List<PackageEvaluatorDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<PackageEvaluatorDto>> AssignEvaluatorAsync(int packageId, AssignEvaluatorRequest request, int currentUserId)
        {
            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(packageId);
            if (package == null)
            {
                return ApiResponse<PackageEvaluatorDto>.Fail("Không tìm thấy gói thầu.");
            }

            if (package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<PackageEvaluatorDto>.Fail("Gói thầu đã hoàn tất ký hợp đồng, không thể thay đổi Tổ chuyên gia.");
            }

            var evaluatorUser = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == request.EvaluatorId);

            if (evaluatorUser == null)
            {
                return ApiResponse<PackageEvaluatorDto>.Fail("Người dùng không tồn tại.");
            }

            var isEvaluator = evaluatorUser.UserRoles.Any(ur => ur.Role.Name == "Evaluator" || ur.Role.Name == "Admin");
            if (!isEvaluator)
            {
                return ApiResponse<PackageEvaluatorDto>.Fail("Người dùng được chỉ định phải có vai trò Giám khảo (Evaluator) hoặc Admin.");
            }

            var isAlreadyAssigned = await _unitOfWork.Repository<BidPackageEvaluator>()
                .ExistsAsync(pe => pe.BidPackageId == packageId && pe.EvaluatorId == request.EvaluatorId);

            if (isAlreadyAssigned)
            {
                return ApiResponse<PackageEvaluatorDto>.Fail("Giám khảo này đã được phân công vào Tổ chuyên gia của gói thầu.");
            }

            var packageEvaluator = new BidPackageEvaluator
            {
                BidPackageId = packageId,
                EvaluatorId = request.EvaluatorId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = currentUserId
            };

            await _unitOfWork.Repository<BidPackageEvaluator>().AddAsync(packageEvaluator);
            await _unitOfWork.SaveChangesAsync();

            var assignerUser = await _unitOfWork.Repository<User>().GetByIdAsync(currentUserId);

            var dto = new PackageEvaluatorDto
            {
                EvaluatorId = evaluatorUser.Id,
                FullName = evaluatorUser.FullName,
                Email = evaluatorUser.Email,
                Phone = evaluatorUser.Phone,
                AssignedAt = packageEvaluator.AssignedAt,
                AssignedBy = packageEvaluator.AssignedBy,
                AssignedByName = assignerUser?.FullName,
                HasSubmittedScores = false
            };

            return ApiResponse<PackageEvaluatorDto>.Ok(dto, "Phân công giám khảo vào Tổ chuyên gia thành công.");
        }

        public async Task<ApiResponse<bool>> RemoveEvaluatorAsync(int packageId, int evaluatorId, int currentUserId, bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(packageId);
            if (package == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy gói thầu.");
            }

            if (package.Status == BidPackageStatus.Contracted)
            {
                return ApiResponse<bool>.Fail("Gói thầu đã hoàn tất ký hợp đồng, không thể thay đổi Tổ chuyên gia.");
            }

            var assignment = await _unitOfWork.Repository<BidPackageEvaluator>()
                .Query()
                .FirstOrDefaultAsync(pe => pe.BidPackageId == packageId && pe.EvaluatorId == evaluatorId);

            if (assignment == null)
            {
                return ApiResponse<bool>.Fail("Giám khảo không nằm trong Tổ chuyên gia của gói thầu này.");
            }

            // Kiểm tra xem giám khảo này đã chấm điểm bất kỳ hồ sơ nào chưa
            var hasScored = await _unitOfWork.Repository<EvaluationScore>()
                .Query()
                .AnyAsync(es => es.Criteria.BidPackageId == packageId && es.EvaluatorId == evaluatorId);

            if (hasScored && package.Status == BidPackageStatus.Evaluating)
            {
                return ApiResponse<bool>.Fail("Giám khảo này đã thực hiện chấm điểm hồ sơ dự thầu cho gói thầu này, không thể xóa khỏi Tổ chuyên gia.");
            }

            _unitOfWork.Repository<BidPackageEvaluator>().Delete(assignment);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Xóa giám khảo khỏi Tổ chuyên gia thành công.");
        }

        private static BidPackageDto MapToBidPackageDto(BidPackage package)
        {
            return new BidPackageDto
            {
                Id = package.Id,
                Code = package.Code,
                Name = package.Name,
                Type = package.Type,
                Budget = package.Budget,
                Deadline = package.Deadline,
                Status = package.Status,
                Description = package.Description,
                CreatedBy = package.CreatedBy,
                CreatedByName = package.Creator?.FullName,
                CreatedAt = package.CreatedAt,
                UpdatedAt = package.UpdatedAt,
                BidDocuments = package.BidDocuments?.Select(d => new BidDocumentDto
                {
                    Id = d.Id,
                    BidPackageId = d.BidPackageId,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    UploadedAt = d.UploadedAt
                }).ToList() ?? new List<BidDocumentDto>(),
                SubmissionsCount = package.BidSubmissions?.Count ?? 0,
                IsAwarded = package.BidSubmissions != null && package.BidSubmissions.Any(s => s.Status == "Selected")
            };
        }
    }
}
