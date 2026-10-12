using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Contractor;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class ContractorService : IContractorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public ContractorService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<PaginatedList<ContractorDto>>> GetContractorsAsync(ContractorFilterParams filter)
        {
            var query = _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .AsNoTracking();

            // 1. Tìm kiếm theo Tên công ty hoặc Mã số thuế
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(c => c.CompanyName.ToLower().Contains(term)
                    || (c.TaxCode != null && c.TaxCode.ToLower().Contains(term)));
            }

            // 2. Lọc theo điểm đánh giá Rating
            if (filter.MinRating.HasValue)
            {
                query = query.Where(c => c.Rating >= filter.MinRating.Value);
            }

            if (filter.MaxRating.HasValue)
            {
                query = query.Where(c => c.Rating <= filter.MaxRating.Value);
            }

            // 3. Lọc theo trạng thái thẩm định
            if (!string.IsNullOrWhiteSpace(filter.VerificationStatus))
            {
                var vStatus = filter.VerificationStatus.Trim().ToLower();
                query = query.Where(c => c.VerificationStatus.ToLower() == vStatus);
            }

            // 4. Sắp xếp
            query = (filter.SortBy?.ToLower()) switch
            {
                "name" or "companyname" => filter.SortDescending
                    ? query.OrderByDescending(c => c.CompanyName)
                    : query.OrderBy(c => c.CompanyName),
                "createdat" or "date" => filter.SortDescending
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt),
                _ => filter.SortDescending
                    ? query.OrderByDescending(c => c.Rating)
                    : query.OrderBy(c => c.Rating)
            };

            // 5. Ánh xạ sang DTO và phân trang
            var dtoQuery = query.Select(c => new ContractorDto
            {
                Id = c.Id,
                UserId = c.UserId,
                CompanyName = c.CompanyName,
                TaxCode = c.TaxCode,
                Address = c.Address,
                BusinessLicenseFile = c.BusinessLicenseFile,
                Rating = c.Rating,
                VerificationStatus = c.VerificationStatus,
                CreatedAt = c.CreatedAt,
                FullName = c.User.FullName,
                Email = c.User.Email,
                Phone = c.User.Phone
            });

            var paginatedResult = await PaginatedList<ContractorDto>.CreateAsync(
                dtoQuery, filter.PageIndex, filter.PageSize);

            return ApiResponse<PaginatedList<ContractorDto>>.Ok(paginatedResult);
        }

        public async Task<ApiResponse<ContractorDto>> GetContractorByIdAsync(int id)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy thông tin nhà thầu.");
            }

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor));
        }

        public async Task<ApiResponse<ContractorDto>> GetCurrentContractorProfileAsync(int userId)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy hồ sơ nhà thầu liên kết với tài khoản này.");
            }

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor));
        }

        public async Task<ApiResponse<ContractorDto>> UpdateCurrentContractorProfileAsync(int userId, UpdateContractorProfileRequest request)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy hồ sơ nhà thầu để cập nhật.");
            }

            contractor.CompanyName = request.CompanyName.Trim();
            contractor.Address = request.Address?.Trim();

            // Nếu người dùng tải lên file GPKD mới để thay thế
            if (request.BusinessLicenseFile != null && request.BusinessLicenseFile.Length > 0)
            {
                try
                {
                    var oldFilePath = contractor.BusinessLicenseFile;
                    var newFilePath = await _fileStorageService.SaveFileAsync(request.BusinessLicenseFile, "contractors");
                    contractor.BusinessLicenseFile = newFilePath;

                    // Xóa file cũ nếu lưu file mới thành công
                    if (!string.IsNullOrEmpty(oldFilePath))
                    {
                        _fileStorageService.DeleteFile(oldFilePath);
                    }
                }
                catch (Exception ex)
                {
                    return ApiResponse<ContractorDto>.Fail($"Lỗi khi lưu trữ file Giấy phép kinh doanh: {ex.Message}");
                }
            }

            _unitOfWork.Repository<Contractor>().Update(contractor);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor), "Cập nhật hồ sơ nhà thầu thành công.");
        }

        public async Task<ApiResponse<ContractorDto>> UpdateContractorRatingAsync(int contractorId, UpdateContractorRatingRequest request)
        {
            if (request.Rating < 0 || request.Rating > 5)
            {
                return ApiResponse<ContractorDto>.Fail("Điểm đánh giá phải nằm trong khoảng từ 0.0 đến 5.0 sao.");
            }

            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == contractorId);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy nhà thầu.");
            }

            contractor.Rating = Math.Round(request.Rating, 2);

            _unitOfWork.Repository<Contractor>().Update(contractor);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor), "Cập nhật điểm đánh giá uy tín nhà thầu thành công.");
        }

        public async Task<ApiResponse<ContractorDto>> VerifyContractorAsync(int contractorId, VerifyContractorRequest request, int reviewerUserId)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == contractorId);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy thông tin nhà thầu.");
            }

            var oldStatus = contractor.VerificationStatus;
            var newStatus = request.IsApproved ? "Approved" : "Rejected";
            contractor.VerificationStatus = newStatus;

            _unitOfWork.Repository<Contractor>().Update(contractor);

            // Ghi nhận AuditLog
            var auditLog = new AuditLog
            {
                UserId = reviewerUserId,
                Action = "VERIFY_CONTRACTOR",
                EntityType = "Contractor",
                EntityId = contractor.Id,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { VerificationStatus = oldStatus }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { VerificationStatus = newStatus, request.Notes }),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

            // Gửi thông báo đến tài khoản nhà thầu
            var notif = new Notification
            {
                UserId = contractor.UserId,
                Title = request.IsApproved ? "Hồ sơ năng lực nhà thầu đã được phê duyệt" : "Hồ sơ năng lực nhà thầu bị từ chối",
                Message = request.IsApproved
                    ? $"Hồ sơ năng lực của doanh nghiệp {contractor.CompanyName} đã được thẩm định phê duyệt thành công. Bạn đã có thể nộp hồ sơ tham gia các gói thầu."
                    : $"Hồ sơ năng lực của doanh nghiệp {contractor.CompanyName} đã bị từ chối phê duyệt. Lý do: {request.Notes ?? "Chưa đáp ứng đủ tiêu chuẩn hồ sơ năng lực theo quy định."}",
                Type = request.IsApproved ? Core.Enums.NotificationType.Info : Core.Enums.NotificationType.Warning,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _unitOfWork.Repository<Notification>().AddAsync(notif);

            await _unitOfWork.SaveChangesAsync();

            var message = request.IsApproved 
                ? "Phê duyệt hồ sơ năng lực nhà thầu thành công." 
                : "Đã từ chối phê duyệt hồ sơ năng lực nhà thầu.";

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor), message);
        }

        public async Task<ApiResponse<ContractorDto>> RecalculateContractorRatingAsync(int contractorId)
        {
            var contractor = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == contractorId);

            if (contractor == null)
            {
                return ApiResponse<ContractorDto>.Fail("Không tìm thấy nhà thầu.");
            }

            var contracts = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.Acceptances)
                .Where(c => c.ContractorId == contractorId)
                .ToListAsync();

            decimal baseScore = 5.0m;
            int terminatedCount = contracts.Count(c => c.Status == ContractStatus.Terminated);
            int rejectedAcceptanceCount = contracts.SelectMany(c => c.Acceptances).Count(a => a.Status == AcceptanceStatus.Rejected);

            decimal calculatedScore = baseScore - (terminatedCount * 1.5m) - (rejectedAcceptanceCount * 0.2m);
            calculatedScore = Math.Clamp(calculatedScore, 1.0m, 5.0m);

            contractor.Rating = Math.Round(calculatedScore, 2);
            _unitOfWork.Repository<Contractor>().Update(contractor);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ContractorDto>.Ok(MapToDto(contractor), "Đã tự động tính toán lại điểm uy tín nhà thầu thành công.");
        }

        private static ContractorDto MapToDto(Contractor contractor)
        {
            return new ContractorDto
            {
                Id = contractor.Id,
                UserId = contractor.UserId,
                CompanyName = contractor.CompanyName,
                TaxCode = contractor.TaxCode,
                Address = contractor.Address,
                BusinessLicenseFile = contractor.BusinessLicenseFile,
                Rating = contractor.Rating,
                VerificationStatus = contractor.VerificationStatus,
                CreatedAt = contractor.CreatedAt,
                FullName = contractor.User?.FullName ?? string.Empty,
                Email = contractor.User?.Email ?? string.Empty,
                Phone = contractor.User?.Phone
            };
        }

        public async Task<ApiResponse<ImportContractorResultDto>> ImportContractorsFromExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return ApiResponse<ImportContractorResultDto>.Fail("Vui lòng tải lên tệp Excel để nhập dữ liệu.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
            {
                return ApiResponse<ImportContractorResultDto>.Fail("Chỉ chấp nhận tệp Excel định dạng .xlsx.");
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                return ApiResponse<ImportContractorResultDto>.Fail("Dung lượng tệp không được vượt quá 10MB.");
            }

            var result = new ImportContractorResultDto();
            var errors = new List<string>();

            // Lấy danh sách MST và Email hiện có trong hệ thống để đối soát trùng lặp
            var existingTaxCodes = (await _unitOfWork.Repository<Contractor>()
                .Query()
                .Select(c => c.TaxCode)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToListAsync())
                .Select(t => t!.Trim().ToLowerInvariant())
                .ToHashSet();

            var existingEmails = (await _unitOfWork.Repository<User>()
                .Query()
                .Select(u => u.Email)
                .ToListAsync())
                .Select(e => e.Trim().ToLowerInvariant())
                .ToHashSet();

            var contractorRole = (await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == "Contractor"))
                .FirstOrDefault();

            if (contractorRole == null)
            {
                return ApiResponse<ImportContractorResultDto>.Fail("Vai trò 'Contractor' chưa được định cấu hình trong hệ thống.");
            }

            var seenTaxCodesInFile = new HashSet<string>();
            var seenEmailsInFile = new HashSet<string>();

            var validContractorsToCreate = new List<(string companyName, string taxCode, string? address, string fullName, string email, string? phone)>();

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    return ApiResponse<ImportContractorResultDto>.Fail("Tệp Excel không chứa trang tính (worksheet) hợp lệ nào.");
                }

                var lastRowUsed = worksheet.LastRowUsed();
                if (lastRowUsed == null || lastRowUsed.RowNumber() < 2)
                {
                    return ApiResponse<ImportContractorResultDto>.Fail("Tệp Excel không chứa bất kỳ dòng dữ liệu nào sau tiêu đề.");
                }

                int totalDataRows = 0;
                int failureCount = 0;

                // Duyệt từng dòng dữ liệu từ dòng 2
                for (int row = 2; row <= lastRowUsed.RowNumber(); row++)
                {
                    var companyName = worksheet.Cell(row, 1).GetString().Trim();
                    var taxCode = worksheet.Cell(row, 2).GetString().Trim();
                    var address = worksheet.Cell(row, 3).GetString().Trim();
                    var fullName = worksheet.Cell(row, 4).GetString().Trim();
                    var email = worksheet.Cell(row, 5).GetString().Trim();
                    var phone = worksheet.Cell(row, 6).GetString().Trim();

                    // Bỏ qua dòng trống hoàn toàn
                    if (string.IsNullOrEmpty(companyName) && string.IsNullOrEmpty(taxCode) && string.IsNullOrEmpty(email))
                    {
                        continue;
                    }

                    totalDataRows++;
                    var rowErrors = new List<string>();

                    if (string.IsNullOrWhiteSpace(companyName))
                    {
                        rowErrors.Add("Thiếu Tên doanh nghiệp");
                    }

                    if (string.IsNullOrWhiteSpace(taxCode))
                    {
                        rowErrors.Add("Thiếu Mã số thuế");
                    }

                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        fullName = !string.IsNullOrWhiteSpace(companyName) ? $"Đại diện {companyName}" : "Đại diện nhà thầu";
                    }

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        rowErrors.Add("Thiếu Email liên hệ/đăng nhập");
                    }
                    else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        rowErrors.Add($"Email '{email}' không đúng định dạng chuẩn");
                    }

                    // Kiểm tra trùng MST
                    if (!string.IsNullOrWhiteSpace(taxCode))
                    {
                        var normTaxCode = taxCode.ToLowerInvariant();
                        if (existingTaxCodes.Contains(normTaxCode))
                        {
                            rowErrors.Add($"Mã số thuế '{taxCode}' đã tồn tại trong hệ thống");
                        }
                        else if (seenTaxCodesInFile.Contains(normTaxCode))
                        {
                            rowErrors.Add($"Mã số thuế '{taxCode}' bị trùng lặp với dòng trước trong cùng tệp");
                        }
                    }

                    // Kiểm tra trùng Email
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        var normEmail = email.ToLowerInvariant();
                        if (existingEmails.Contains(normEmail))
                        {
                            rowErrors.Add($"Email '{email}' đã tồn tại trong hệ thống");
                        }
                        else if (seenEmailsInFile.Contains(normEmail))
                        {
                            rowErrors.Add($"Email '{email}' bị trùng lặp với dòng trước trong cùng tệp");
                        }
                    }

                    if (rowErrors.Count > 0)
                    {
                        failureCount++;
                        errors.Add($"Dòng {row}: {string.Join("; ", rowErrors)}.");
                    }
                    else
                    {
                        seenTaxCodesInFile.Add(taxCode.ToLowerInvariant());
                        seenEmailsInFile.Add(email.ToLowerInvariant());
                        validContractorsToCreate.Add((companyName, taxCode, string.IsNullOrEmpty(address) ? null : address, fullName, email, string.IsNullOrEmpty(phone) ? null : phone));
                    }
                }

                result.TotalRows = totalDataRows;
                result.FailureCount = failureCount;
                result.Errors = errors;

                // Nếu có bản ghi hợp lệ, lưu vào DB qua Transaction
                if (validContractorsToCreate.Count > 0)
                {
                    await _unitOfWork.BeginTransactionAsync();
                    try
                    {
                        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Contractor@123");

                        foreach (var item in validContractorsToCreate)
                        {
                            var user = new User
                            {
                                FullName = item.fullName,
                                Email = item.email.ToLowerInvariant(),
                                PasswordHash = defaultPasswordHash,
                                Phone = item.phone,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };

                            await _unitOfWork.Repository<User>().AddAsync(user);
                            await _unitOfWork.SaveChangesAsync();

                            var userRole = new UserRole
                            {
                                UserId = user.Id,
                                RoleId = contractorRole.Id
                            };
                            await _unitOfWork.Repository<UserRole>().AddAsync(userRole);

                            var contractor = new Contractor
                            {
                                UserId = user.Id,
                                CompanyName = item.companyName,
                                TaxCode = item.taxCode,
                                Address = item.address,
                                Rating = 5.0m,
                                VerificationStatus = "Approved",
                                CreatedAt = DateTime.UtcNow
                            };
                            await _unitOfWork.Repository<Contractor>().AddAsync(contractor);
                        }

                        await _unitOfWork.SaveChangesAsync();
                        await _unitOfWork.CommitTransactionAsync();

                        result.SuccessCount = validContractorsToCreate.Count;
                    }
                    catch (Exception)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw;
                    }
                }

                return ApiResponse<ImportContractorResultDto>.Ok(result,
                    $"Nhập danh sách nhà thầu hoàn tất: Thành công {result.SuccessCount}/{result.TotalRows} dòng, Thất bại {result.FailureCount} dòng.");
            }
            catch (Exception ex)
            {
                return ApiResponse<ImportContractorResultDto>.Fail($"Lỗi trong quá trình xử lý tệp Excel: {ex.Message}");
            }
        }

        public async Task<byte[]> GenerateImportTemplateExcelAsync()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Mau_Nhap_Nha_Thau");

            var headers = new[]
            {
                "Tên doanh nghiệp (*)",
                "Mã số thuế (*)",
                "Địa chỉ trụ sở",
                "Người đại diện (*)",
                "Email đăng nhập (*)",
                "Số điện thoại"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Dòng ví dụ minh họa
            worksheet.Cell(2, 1).Value = "Công ty TNHH Giải pháp Công nghệ Sao Mai";
            worksheet.Cell(2, 2).Value = "0109988776";
            worksheet.Cell(2, 3).Value = "Số 15 Lê Duẩn, Quận Hoàn Kiếm, Hà Nội";
            worksheet.Cell(2, 4).Value = "Nguyễn Văn An";
            worksheet.Cell(2, 5).Value = "saomai.tech@example.com";
            worksheet.Cell(2, 6).Value = "0987654321";

            for (int col = 1; col <= 6; col++)
            {
                worksheet.Cell(2, col).Style.Font.Italic = true;
                worksheet.Cell(2, col).Style.Font.FontColor = XLColor.DarkGray;
            }

            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return await Task.FromResult(memoryStream.ToArray());
        }
    }
}
