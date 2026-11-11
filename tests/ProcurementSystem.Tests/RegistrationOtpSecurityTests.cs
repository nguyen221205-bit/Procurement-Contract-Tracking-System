using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.DTOs.Notification;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Repositories;
using ProcurementSystem.Infrastructure.Services;
using Xunit;

namespace ProcurementSystem.Tests
{
    public class RegistrationOtpSecurityTests : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _db;
        private readonly IMemoryCache _memoryCache;
        private readonly CapturingFakeEmailService _emailService;
        private readonly OtpService _otpService;
        private readonly TestFakeTaxLookupService _taxLookupService;
        private readonly TestFakePdfSecurityService _pdfSecurityService;
        private readonly TestFakeFileStorageService _fileStorageService;
        private readonly TestFakeNotificationService _notificationService;
        private readonly ContractorAuthService _contractorAuthService;
        private readonly ProcuringEntityAuthService _procuringEntityAuthService;

        public RegistrationOtpSecurityTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new AppDbContext(options);
            _db.Database.EnsureCreated();

            // Seed Roles
            if (!_db.Roles.Any())
            {
                _db.Roles.AddRange(
                    new Role { Id = 1, Name = "Admin", Description = "Platform Administrator" },
                    new Role { Id = 2, Name = "Procurement", Description = "Procuring Entity Staff" },
                    new Role { Id = 3, Name = "Contractor", Description = "Bidding Contractor" },
                    new Role { Id = 4, Name = "Evaluator", Description = "Evaluation Committee Member" }
                );
                _db.SaveChanges();
            }

            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _emailService = new CapturingFakeEmailService();

            // Cung cấp cấu hình Smtp hợp lệ để OtpService hoạt động bình thường
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Smtp:Host"] = "smtp.test.com",
                    ["Smtp:Username"] = "noreply@procurement.test",
                    ["Smtp:Password"] = "TestPasswordSecret123"
                })
                .Build();

            _otpService = new OtpService(_memoryCache, NullLogger<OtpService>.Instance, config, _emailService);

            _taxLookupService = new TestFakeTaxLookupService();
            _pdfSecurityService = new TestFakePdfSecurityService();
            _fileStorageService = new TestFakeFileStorageService();
            _notificationService = new TestFakeNotificationService();

            _contractorAuthService = new ContractorAuthService(
                new UnitOfWork(_db),
                _taxLookupService,
                _pdfSecurityService,
                _fileStorageService,
                _otpService,
                _notificationService
            );

            _procuringEntityAuthService = new ProcuringEntityAuthService(
                _db,
                _fileStorageService,
                _otpService,
                _pdfSecurityService,
                _notificationService
            );
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
            _memoryCache.Dispose();
        }

        private static IFormFile CreateDummyFile(string fileName = "test.pdf")
        {
            var content = "DUMMY PDF CONTENT"u8.ToArray();
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
        }

        private void SeedOtpInCache(string email, string otpCode, string purpose)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var targetPurpose = string.IsNullOrWhiteSpace(purpose) ? "Register" : purpose.Trim();
            var otpKey = $"OTP_{targetPurpose}_{normalizedEmail}";

            // Giả lập cấu trúc cache entry giống OtpService
            var entryType = typeof(OtpService).GetNestedType("OtpCacheEntry", System.Reflection.BindingFlags.NonPublic);
            var entry = Activator.CreateInstance(entryType!)!;
            entryType!.GetProperty("Code")!.SetValue(entry, otpCode);
            entryType.GetProperty("Attempts")!.SetValue(entry, 0);
            entryType.GetProperty("CreatedAt")!.SetValue(entry, DateTime.UtcNow);

            _memoryCache.Set(otpKey, entry, TimeSpan.FromMinutes(5));
        }

        private static string ExtractOtpFromEmailBody(string emailBody)
        {
            var match = Regex.Match(emailBody, @"border:\s*2px\s+dashed\s+#38bdf8;'>\s*(\d{6})\s*</div>", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            var fallbackMatch = Regex.Match(emailBody, @"\b(\d{6})\b");
            if (fallbackMatch.Success)
            {
                return fallbackMatch.Groups[1].Value.Trim();
            }

            throw new InvalidOperationException("Không thể tìm thấy mã OTP 6 số trong nội dung email gửi đi.");
        }

        #region 1. Contractor Registration OTP Tests

        [Fact]
        public async Task RegisterContractor_WhenOtpMissingOrNull_ShouldFailImmediately_AndNotCreateEntities()
        {
            var request = new RegisterContractorRequest
            {
                FullName = "Nguyễn Văn A",
                Email = "contractor_no_otp@test.com",
                Password = "Password123!",
                CompanyName = "Công ty CP Test",
                TaxCode = "0101248141",
                OtpCode = null!, // Missing OTP
                BusinessLicenseFile = CreateDummyFile()
            };

            var result = await _contractorAuthService.RegisterContractorAsync(request);

            Assert.False(result.Success);
            Assert.Contains("OTP", result.Message);
            Assert.Equal(0, await _db.Users.CountAsync(u => u.Email == "contractor_no_otp@test.com"));
            Assert.Equal(0, await _db.Contractors.CountAsync(c => c.TaxCode == "0101248141"));
        }

        [Fact]
        public async Task RegisterContractor_WhenOtpWhitespace_ShouldFailImmediately()
        {
            var request = new RegisterContractorRequest
            {
                FullName = "Nguyễn Văn A",
                Email = "contractor_blank_otp@test.com",
                Password = "Password123!",
                CompanyName = "Công ty CP Test",
                TaxCode = "0101248142",
                OtpCode = "   ", // Blank
                BusinessLicenseFile = CreateDummyFile()
            };

            var result = await _contractorAuthService.RegisterContractorAsync(request);

            Assert.False(result.Success);
            Assert.Contains("OTP", result.Message);
            Assert.Equal(0, await _db.Users.CountAsync(u => u.Email == "contractor_blank_otp@test.com"));
        }

        [Fact]
        public async Task RegisterContractor_WhenOtpIncorrect_ShouldFail_AndNotCreateEntities()
        {
            const string email = "contractor_wrong_otp@test.com";
            SeedOtpInCache(email, "654321", "RegisterContractor");

            var request = new RegisterContractorRequest
            {
                FullName = "Nguyễn Văn A",
                Email = email,
                Password = "Password123!",
                CompanyName = "Công ty CP Test",
                TaxCode = "0101248143",
                OtpCode = "000000", // Sai mã
                BusinessLicenseFile = CreateDummyFile()
            };

            var result = await _contractorAuthService.RegisterContractorAsync(request);

            Assert.False(result.Success);
            Assert.Contains("không chính xác", result.Message);
            Assert.Equal(0, await _db.Users.CountAsync(u => u.Email == email));
        }

        [Fact]
        public async Task RegisterContractor_WhenOtpPurposeMismatch_ShouldFail()
        {
            const string email = "contractor_wrong_purpose@test.com";
            // Mã được cấp cho mục đích RegisterProcuringEntity
            SeedOtpInCache(email, "123456", "RegisterProcuringEntity");

            var request = new RegisterContractorRequest
            {
                FullName = "Nguyễn Văn A",
                Email = email,
                Password = "Password123!",
                CompanyName = "Công ty CP Test",
                TaxCode = "0101248144",
                OtpCode = "123456",
                BusinessLicenseFile = CreateDummyFile()
            };

            var result = await _contractorAuthService.RegisterContractorAsync(request);

            Assert.False(result.Success);
            Assert.Equal(0, await _db.Users.CountAsync(u => u.Email == email));
        }

        [Fact]
        public async Task RegisterContractor_WhenOtpValid_ShouldSucceed_AndConsumeOtp()
        {
            const string email = "contractor_success@test.com";
            const string taxCode = "0101248145";
            SeedOtpInCache(email, "123456", "RegisterContractor");

            var request = new RegisterContractorRequest
            {
                FullName = "Nguyễn Văn A",
                Email = email,
                Password = "Password123!",
                CompanyName = "Công ty CP Thành Công",
                TaxCode = taxCode,
                OtpCode = "123456",
                BusinessLicenseFile = CreateDummyFile()
            };

            var result = await _contractorAuthService.RegisterContractorAsync(request);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            // Kiểm tra User & Contractor được tạo đúng
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
            Assert.NotNull(user);
            Assert.Equal("Nguyễn Văn A", user.FullName);

            var contractor = await _db.Contractors.SingleOrDefaultAsync(c => c.TaxCode == taxCode);
            Assert.NotNull(contractor);
            Assert.Equal("Pending", contractor.VerificationStatus);

            // Xác minh OTP đã bị hủy sau khi dùng (chống Replay Attack)
            var canReuse = _otpService.VerifyOtp(email, "123456", "RegisterContractor", out _);
            Assert.False(canReuse);
        }

        #endregion

        #region 2. ProcuringEntity Registration OTP Tests

        [Fact]
        public async Task RegisterProcuringEntity_WhenOtpMissingOrNull_ShouldFailImmediately_AndNotCreateEntities()
        {
            var request = new RegisterProcuringEntityRequest
            {
                FullName = "Trần Thị B",
                Email = "entity_no_otp@gov.vn",
                Password = "Password123!",
                OrganizationName = "Ban QLDA Hạ Tầng",
                OrganizationType = "Ban Quản lý Dự án",
                TaxCode = "0301248151",
                RepresentativeName = "Trần Văn C",
                RepresentativeTitle = "Giám đốc Ban",
                OtpCode = null!, // Missing
                EstablishmentDecisionFile = CreateDummyFile()
            };

            var result = await _procuringEntityAuthService.RegisterProcuringEntityAsync(request);

            Assert.False(result.Success);
            Assert.Contains("OTP", result.Message);
            Assert.Equal(0, await _db.Users.CountAsync(u => u.Email == "entity_no_otp@gov.vn"));
            Assert.Equal(0, await _db.ProcuringEntities.CountAsync(pe => pe.TaxCode == "0301248151"));
        }

        [Fact]
        public async Task RegisterProcuringEntity_WhenOtpIncorrect_ShouldFail_AndNotCreateEntities()
        {
            const string email = "entity_wrong_otp@gov.vn";
            SeedOtpInCache(email, "888888", "RegisterProcuringEntity");

            var request = new RegisterProcuringEntityRequest
            {
                FullName = "Trần Thị B",
                Email = email,
                Password = "Password123!",
                OrganizationName = "Ban QLDA Hạ Tầng",
                OrganizationType = "Ban Quản lý Dự án",
                TaxCode = "0301248152",
                RepresentativeName = "Trần Văn C",
                RepresentativeTitle = "Giám đốc Ban",
                OtpCode = "111111", // Sai mã
                EstablishmentDecisionFile = CreateDummyFile()
            };

            var result = await _procuringEntityAuthService.RegisterProcuringEntityAsync(request);

            Assert.False(result.Success);
            Assert.Contains("không chính xác", result.Message);
            Assert.Equal(0, await _db.Users.CountAsync(u => u.Email == email));
        }

        [Fact]
        public async Task RegisterProcuringEntity_WhenOtpValid_ShouldSucceed_AndCreateManagerMember()
        {
            const string email = "entity_success@gov.vn";
            const string taxCode = "0301248153";
            SeedOtpInCache(email, "999999", "RegisterProcuringEntity");

            var request = new RegisterProcuringEntityRequest
            {
                FullName = "Trần Thị B",
                Email = email,
                Password = "Password123!",
                OrganizationName = "Ban QLDA Mua Sắm",
                OrganizationType = "Ban Quản lý Dự án",
                TaxCode = taxCode,
                RepresentativeName = "Trần Văn C",
                RepresentativeTitle = "Giám đốc Ban",
                OtpCode = "999999",
                EstablishmentDecisionFile = CreateDummyFile()
            };

            var result = await _procuringEntityAuthService.RegisterProcuringEntityAsync(request);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            // Kiểm tra User & Entity được tạo
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
            Assert.NotNull(user);

            var entity = await _db.ProcuringEntities.SingleOrDefaultAsync(pe => pe.TaxCode == taxCode);
            Assert.NotNull(entity);
            Assert.Equal("Pending", entity.VerificationStatus);

            // Kiểm tra tự động gắn quyền Manager cho người đăng ký
            var member = await _db.ProcuringEntityMembers.SingleOrDefaultAsync(m => m.UserId == user.Id && m.ProcuringEntityId == entity.Id);
            Assert.NotNull(member);
            Assert.Equal(ProcuringEntityMemberRole.Manager, member.RoleInEntity);
            Assert.Equal(MemberStatus.Active, member.Status);

            // OTP phải bị tiêu thụ sau khi đăng ký
            var canReuse = _otpService.VerifyOtp(email, "999999", "RegisterProcuringEntity", out _);
            Assert.False(canReuse);
        }

        #endregion

        #region 3. R-B04 Real Boundary OTP Verification & Security Tests

        [Fact]
        public async Task SendOtpAsync_WithRealCacheAndEmail_GeneratesSecureCode_AndDoesNotExposeDevOtpCode()
        {
            // R-B04: Gọi OtpService.SendOtpAsync thật với IMemoryCache thật và CapturingFakeEmailService
            const string email = "real_otp_contractor@test.com";
            var sendRequest = new SendOtpRequest
            {
                Email = email,
                Purpose = "RegisterContractor"
            };

            var sendResult = await _otpService.SendOtpAsync(sendRequest);

            // 1. Phản hồi API gửi OTP phải thành công
            Assert.True(sendResult.Success);
            Assert.NotNull(sendResult.Data);
            Assert.Equal(email, sendResult.Data.Email);
            Assert.Equal(60, sendResult.Data.CooldownSeconds);
            Assert.Equal(5, sendResult.Data.ExpiryMinutes);

            // 2. BẢO MẬT: DevOtpCode tuyệt đối không được trả về trong API response (phải là null)
            Assert.Null(sendResult.Data.DevOtpCode);

            // 3. Email phải được gửi đến hòm thư người dùng
            var sentEmail = _emailService.SentEmails.FirstOrDefault(e => e.To == email);
            Assert.NotNull(sentEmail.To);
            Assert.Contains("Hệ thống Đấu thầu", sentEmail.Subject, StringComparison.OrdinalIgnoreCase);

            // 4. Trích xuất mã OTP 6 số từ nội dung email
            var otpCode = ExtractOtpFromEmailBody(sentEmail.Body);
            Assert.Equal(6, otpCode.Length);
            Assert.True(int.TryParse(otpCode, out _));

            // 5. Verify với mã trích xuất từ email -> Thành công
            var verifySuccess = _otpService.VerifyOtp(email, otpCode, "RegisterContractor", out string errorMessage);
            Assert.True(verifySuccess);
            Assert.Empty(errorMessage);

            // 6. CHỐNG REPLAY ATTACK: Dùng lại mã vừa verify thành công -> Phải bị từ chối vì đã tiêu thụ
            var replayResult = _otpService.VerifyOtp(email, otpCode, "RegisterContractor", out string replayError);
            Assert.False(replayResult);
            Assert.Contains("hết hạn hoặc không tồn tại", replayError);
        }

        [Fact]
        public async Task VerifyOtp_WhenFailed5Times_ShouldInvalidateCode_AndBlockSubsequentAttempts()
        {
            // R-B04: Kiểm chứng ngưỡng brute-force 5 lần thử sai
            const string email = "bruteforce_victim@test.com";
            var sendResult = await _otpService.SendOtpAsync(new SendOtpRequest
            {
                Email = email,
                Purpose = "RegisterContractor"
            });
            Assert.True(sendResult.Success);

            var sentEmail = _emailService.SentEmails.First(e => e.To == email);
            var correctCode = ExtractOtpFromEmailBody(sentEmail.Body);

            // Thử sai 4 lần đầu
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                var isOk = _otpService.VerifyOtp(email, "000000", "RegisterContractor", out string err);
                Assert.False(isOk);
                Assert.Contains($"Bạn còn {5 - attempt} lần thử", err);
            }

            // Lần thứ 5 sai -> Mã bị hủy ngay lập tức
            var attempt5 = _otpService.VerifyOtp(email, "000000", "RegisterContractor", out string err5);
            Assert.False(attempt5);
            Assert.Contains("hủy do nhập sai quá 5 lần", err5);

            // Lần thứ 6: Dù nhập ĐÚNG mã ban đầu cũng phải thất bại vì mã đã bị xóa khỏi cache
            var attempt6 = _otpService.VerifyOtp(email, correctCode, "RegisterContractor", out string err6);
            Assert.False(attempt6);
            Assert.Contains("hết hạn hoặc không tồn tại", err6);
        }

        [Fact]
        public async Task VerifyOtp_WhenPurposeMismatch_ShouldFailVerification()
        {
            // R-B04: Phân tách mục đích xác thực
            const string email = "purpose_isolation@test.com";
            var sendResult = await _otpService.SendOtpAsync(new SendOtpRequest
            {
                Email = email,
                Purpose = "RegisterContractor"
            });
            Assert.True(sendResult.Success);

            var sentEmail = _emailService.SentEmails.First(e => e.To == email);
            var otpCode = ExtractOtpFromEmailBody(sentEmail.Body);

            // Xác thực với purpose khác -> Thất bại
            var mismatchResult1 = _otpService.VerifyOtp(email, otpCode, "RegisterProcuringEntity", out string err1);
            Assert.False(mismatchResult1);
            Assert.Contains("hết hạn hoặc không tồn tại", err1);

            var mismatchResult2 = _otpService.VerifyOtp(email, otpCode, "ResetPassword", out string err2);
            Assert.False(mismatchResult2);
            Assert.Contains("hết hạn hoặc không tồn tại", err2);

            // Xác thực đúng purpose ban đầu -> Thành công
            var matchResult = _otpService.VerifyOtp(email, otpCode, "RegisterContractor", out string matchErr);
            Assert.True(matchResult);
            Assert.Empty(matchErr);
        }

        [Fact]
        public async Task EndToEnd_ContractorRegistration_WithRealOtpDelivery()
        {
            // R-B04: Kiểm thử tích hợp toàn diện luồng Nhà thầu đăng ký với OTP thật gửi qua email
            const string email = "contractor_e2e_real@test.com";
            const string taxCode = "0101248191";

            // 1. Gửi OTP thật
            var sendResult = await _otpService.SendOtpAsync(new SendOtpRequest
            {
                Email = email,
                Purpose = "RegisterContractor"
            });
            Assert.True(sendResult.Success);

            // 2. Trích xuất mã OTP từ email nhận được
            var sentEmail = _emailService.SentEmails.First(e => e.To == email);
            var realOtp = ExtractOtpFromEmailBody(sentEmail.Body);

            // 3. Đăng ký Nhà thầu với mã OTP thật
            var registerRequest = new RegisterContractorRequest
            {
                FullName = "Nguyễn Văn Chân Thật",
                Email = email,
                Password = "Password123!",
                CompanyName = "Công ty CP Thật 100%",
                TaxCode = taxCode,
                OtpCode = realOtp,
                BusinessLicenseFile = CreateDummyFile("giay_phep_that.pdf")
            };

            var registerResult = await _contractorAuthService.RegisterContractorAsync(registerRequest);

            // 4. Kiểm tra đăng ký thành công
            Assert.True(registerResult.Success);
            Assert.NotNull(registerResult.Data);
            Assert.Equal(email, registerResult.Data.Email);

            var createdUser = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
            Assert.NotNull(createdUser);
            var createdContractor = await _db.Contractors.SingleOrDefaultAsync(c => c.TaxCode == taxCode);
            Assert.NotNull(createdContractor);

            // 5. Thử đăng ký lại với cùng mã OTP -> Bắt buộc thất bại
            var replayRegisterResult = await _contractorAuthService.RegisterContractorAsync(registerRequest);
            Assert.False(replayRegisterResult.Success);
            Assert.Contains("hết hạn hoặc không tồn tại", replayRegisterResult.Message);
        }

        [Fact]
        public async Task EndToEnd_ProcuringEntityRegistration_WithRealOtpDelivery()
        {
            // R-B04: Kiểm thử tích hợp toàn diện luồng Bên mời thầu đăng ký với OTP thật gửi qua email
            const string email = "entity_e2e_real@gov.vn";
            const string taxCode = "0301248192";

            // 1. Gửi OTP thật
            var sendResult = await _otpService.SendOtpAsync(new SendOtpRequest
            {
                Email = email,
                Purpose = "RegisterProcuringEntity"
            });
            Assert.True(sendResult.Success);

            // 2. Trích xuất mã OTP từ email
            var sentEmail = _emailService.SentEmails.First(e => e.To == email);
            var realOtp = ExtractOtpFromEmailBody(sentEmail.Body);

            // 3. Đăng ký Bên mời thầu với mã OTP thật
            var registerRequest = new RegisterProcuringEntityRequest
            {
                FullName = "Phạm Thị Thật",
                Email = email,
                Password = "Password123!",
                OrganizationName = "Ban Quản lý Dự án E2E",
                OrganizationType = "Ban Quản lý Dự án",
                TaxCode = taxCode,
                RepresentativeName = "Lê Văn Thật",
                RepresentativeTitle = "Trưởng ban",
                OtpCode = realOtp,
                EstablishmentDecisionFile = CreateDummyFile("quyet_dinh_that.pdf")
            };

            var registerResult = await _procuringEntityAuthService.RegisterProcuringEntityAsync(registerRequest);

            // 4. Kiểm tra đăng ký thành công
            Assert.True(registerResult.Success);
            Assert.NotNull(registerResult.Data);

            var createdUser = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
            Assert.NotNull(createdUser);
            var createdEntity = await _db.ProcuringEntities.SingleOrDefaultAsync(pe => pe.TaxCode == taxCode);
            Assert.NotNull(createdEntity);

            // 5. Thử tái sử dụng mã OTP -> Bắt buộc thất bại
            var replayRegisterResult = await _procuringEntityAuthService.RegisterProcuringEntityAsync(registerRequest);
            Assert.False(replayRegisterResult.Success);
            Assert.Contains("hết hạn hoặc không tồn tại", replayRegisterResult.Message);
        }

        #endregion

        #region Private Test Fakes

        public class CapturingFakeEmailService : IEmailService
        {
            public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

            public Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
            {
                SentEmails.Add((toEmail, subject, body));
                return Task.FromResult(true);
            }
        }

        private class TestFakeTaxLookupService : ITaxLookupService
        {
            public Task<TaxBusinessData?> VerifyTaxCodeAsync(string taxCode)
            {
                return Task.FromResult<TaxBusinessData?>(new TaxBusinessData
                {
                    Name = "DOANH NGHIEP KIEM THU HOP LE",
                    Address = "123 Pho Hue, Ha Noi"
                });
            }
        }

        private class TestFakePdfSecurityService : IPdfSecurityService
        {
            public PdfSignatureResult InspectPdfSignature(Stream pdfStream)
            {
                return new PdfSignatureResult
                {
                    IsSigned = true,
                    IsIntegrityValid = true,
                    SignerName = "Chu Ky So Doanh Nghiep",
                    SignDate = DateTime.UtcNow,
                    SummaryMessage = "Chữ ký số hợp lệ"
                };
            }
        }

        private class TestFakeFileStorageService : IFileStorageService
        {
            public Task<string> SaveFileAsync(IFormFile file, string subFolder)
                => Task.FromResult($"/uploads/{subFolder}/{Guid.NewGuid()}.pdf");

            public void DeleteFile(string storedPath)
            {
            }

            public string GetPhysicalFilePath(string relativePath)
                => Path.Combine(Directory.GetCurrentDirectory(), relativePath.TrimStart('/', '\\'));

            public Task<FileDownloadResult> ResolveDownloadAsync(string storedPath, string originalFileName, string? contentType = null)
                => Task.FromResult(FileDownloadResult.FromPhysicalPath(GetPhysicalFilePath(storedPath), originalFileName, contentType ?? "application/octet-stream"));

            public Task<string> GetPreSignedUrlAsync(string storedPath, int expirationMinutes = 15)
                => Task.FromResult("https://storage.example.com/presigned");

            public bool IsCloudPath(string storedPath) => false;
        }

        private class TestFakeNotificationService : INotificationService
        {
            public Task<NotificationDto> CreateNotificationAsync(int userId, string title, string message, NotificationType type = NotificationType.Info, bool sendEmail = true)
                => Task.FromResult(new NotificationDto());

            public Task CreateBulkNotificationsAsync(IEnumerable<int> userIds, string title, string message, NotificationType type = NotificationType.Info, bool sendEmail = false)
                => Task.CompletedTask;

            public Task<ApiResponse<PaginatedList<NotificationDto>>> GetMyNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 20)
                => Task.FromResult(ApiResponse<PaginatedList<NotificationDto>>.Ok(null!));

            public Task<ApiResponse<int>> GetUnreadCountAsync(int userId)
                => Task.FromResult(ApiResponse<int>.Ok(0));

            public Task<ApiResponse<bool>> MarkAllAsReadAsync(int userId)
                => Task.FromResult(ApiResponse<bool>.Ok(true));

            public Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, int userId)
                => Task.FromResult(ApiResponse<bool>.Ok(true));
        }

        #endregion
    }
}
