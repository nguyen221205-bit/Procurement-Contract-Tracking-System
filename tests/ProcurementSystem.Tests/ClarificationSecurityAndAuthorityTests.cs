using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.API.Controllers;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Clarification;
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
    public class ClarificationSecurityAndAuthorityTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _db;
        private readonly TestClarificationNotificationService _notificationService;
        private readonly TestClarificationFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ClarificationService _clarificationService;

        public ClarificationSecurityAndAuthorityTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new AppDbContext(options);
            _db.Database.EnsureCreated();

            _unitOfWork = new UnitOfWork(_db);
            _notificationService = new TestClarificationNotificationService();
            _fileStorageService = new TestClarificationFileStorageService();

            _clarificationService = new ClarificationService(
                _unitOfWork,
                _notificationService,
                _fileStorageService);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private async Task<(ProcuringEntity entity, BidPackage package, User creator, User colleague, User otherEntityUser, Contractor contractor, User contractorUser, User evaluator)> SeedBaselineDataAsync()
        {
            var creator = new User
            {
                Id = 101,
                Email = "creator@entity.gov.vn",
                FullName = "Chuyen Vien Tao Goi",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var colleague = new User
            {
                Id = 102,
                Email = "colleague@entity.gov.vn",
                FullName = "Lanh Dao Cung Don Vi",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var otherEntityUser = new User
            {
                Id = 103,
                Email = "other@otherentity.gov.vn",
                FullName = "Can Bo Don Vi Khac",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var contractorUser = new User
            {
                Id = 104,
                Email = "bidder@company.com",
                FullName = "Dai Dien Nha Thau",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var evaluator = new User
            {
                Id = 105,
                Email = "evaluator@expert.com",
                FullName = "Chuyen Gia Cham Thau",
                PasswordHash = "hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.AddRange(creator, colleague, otherEntityUser, contractorUser, evaluator);

            var entity1 = new ProcuringEntity
            {
                Id = 1,
                OrganizationName = "Ban Quan Ly Du An A",
                OrganizationType = "StateAgency",
                Address = "Ha Noi",
                UserId = creator.Id,
                CreatedAt = DateTime.UtcNow
            };

            var entity2 = new ProcuringEntity
            {
                Id = 2,
                OrganizationName = "Ban Quan Ly Du An B",
                OrganizationType = "StateAgency",
                Address = "Da Nang",
                UserId = otherEntityUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            _db.ProcuringEntities.AddRange(entity1, entity2);

            var memberCreator = new ProcuringEntityMember
            {
                Id = 1,
                ProcuringEntityId = entity1.Id,
                UserId = creator.Id,
                RoleInEntity = ProcuringEntityMemberRole.Officer,
                Status = MemberStatus.Active
            };

            var memberColleague = new ProcuringEntityMember
            {
                Id = 2,
                ProcuringEntityId = entity1.Id,
                UserId = colleague.Id,
                RoleInEntity = ProcuringEntityMemberRole.Manager,
                Status = MemberStatus.Active
            };

            var memberOther = new ProcuringEntityMember
            {
                Id = 3,
                ProcuringEntityId = entity2.Id,
                UserId = otherEntityUser.Id,
                RoleInEntity = ProcuringEntityMemberRole.Manager,
                Status = MemberStatus.Active
            };

            _db.ProcuringEntityMembers.AddRange(memberCreator, memberColleague, memberOther);

            var package = new BidPackage
            {
                Id = 2001,
                Code = "GOI-2001",
                Name = "Xay dung he thong thu nghiem",
                ProcuringEntityId = entity1.Id,
                CreatedBy = creator.Id,
                Status = BidPackageStatus.Evaluating,
                Deadline = DateTime.UtcNow.AddDays(5),
                Budget = 5000000000,
                CreatedAt = DateTime.UtcNow
            };

            _db.BidPackages.Add(package);

            var contractor = new Contractor
            {
                Id = 301,
                UserId = contractorUser.Id,
                CompanyName = "Cong ty Cong nghe ABC",
                TaxCode = "0101234567",
                CreatedAt = DateTime.UtcNow
            };

            _db.Contractors.Add(contractor);

            var submission = new BidSubmission
            {
                Id = 4001,
                BidPackageId = package.Id,
                ContractorId = contractor.Id,
                BidPrice = 4800000000,
                Status = BidSubmissionStatus.Submitted,
                SubmittedAt = DateTime.UtcNow.AddDays(-1)
            };

            _db.BidSubmissions.Add(submission);

            var packageEvaluator = new BidPackageEvaluator
            {
                BidPackageId = package.Id,
                EvaluatorId = evaluator.Id,
                AssignedAt = DateTime.UtcNow
            };

            _db.BidPackageEvaluators.Add(packageEvaluator);

            await _db.SaveChangesAsync();

            return (entity1, package, creator, colleague, otherEntityUser, contractor, contractorUser, evaluator);
        }

        [Fact]
        public async Task DownloadAttachment_ArbitraryFilePath_ThrowsKeyNotFoundException()
        {
            await SeedBaselineDataAsync();

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _clarificationService.GetAttachmentForDownloadAsync(
                    filePath: "contracts/1/confidential_contract.pdf",
                    clarificationId: null,
                    currentUserId: 104,
                    isAdmin: false,
                    isProcurement: false,
                    isEvaluator: false,
                    isContractor: true);
            });
        }

        [Fact]
        public async Task DownloadAttachment_SubmissionClarification_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            var data = await SeedBaselineDataAsync();

            var clarification = new BidClarification
            {
                Id = 5001,
                BidPackageId = data.package.Id,
                BidSubmissionId = 4001,
                Type = ClarificationType.SubmissionClarification,
                Title = "Yeu cau lam ro nang luc tai chinh",
                QuestionContent = "Vui long bo sung sao ke ngan hang 3 thang gan nhat",
                QuestionAttachmentUrl = "clarifications/5001_req.pdf",
                AskedByUserId = data.creator.Id,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };
            _db.BidClarifications.Add(clarification);
            await _db.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await _clarificationService.GetAttachmentForDownloadAsync(
                    filePath: "clarifications/5001_req.pdf",
                    clarificationId: clarification.Id,
                    currentUserId: data.otherEntityUser.Id,
                    isAdmin: false,
                    isProcurement: true,
                    isEvaluator: false,
                    isContractor: false);
            });
        }

        [Fact]
        public async Task DownloadAttachment_SubmissionClarification_OwnerContractor_Success()
        {
            var data = await SeedBaselineDataAsync();

            var clarification = new BidClarification
            {
                Id = 5002,
                BidPackageId = data.package.Id,
                BidSubmissionId = 4001,
                Type = ClarificationType.SubmissionClarification,
                Title = "Yeu cau giai trinh bien phap thi cong",
                QuestionContent = "Giai trinh chi tiet giai phap bao ve moi truong",
                QuestionAttachmentUrl = "clarifications/5002_req.pdf",
                AskedByUserId = data.creator.Id,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };
            _db.BidClarifications.Add(clarification);
            await _db.SaveChangesAsync();

            var result = await _clarificationService.GetAttachmentForDownloadAsync(
                filePath: "clarifications/5002_req.pdf",
                clarificationId: clarification.Id,
                currentUserId: data.contractorUser.Id,
                isAdmin: false,
                isProcurement: false,
                isEvaluator: false,
                isContractor: true);

            Assert.NotNull(result);
            Assert.Equal("5002_req.pdf", result.FileName);
            Assert.Equal("application/pdf", result.ContentType);
        }

        [Fact]
        public async Task DownloadAttachment_SubmissionClarification_ColleagueInSameEntity_Success()
        {
            var data = await SeedBaselineDataAsync();

            var clarification = new BidClarification
            {
                Id = 5003,
                BidPackageId = data.package.Id,
                BidSubmissionId = 4001,
                Type = ClarificationType.SubmissionClarification,
                Title = "Van ban giai trinh tien do",
                QuestionContent = "Noi dung yeu cau",
                AnswerContent = "Van ban giai trinh chi tiet",
                AnswerAttachmentUrl = "clarifications/5003_ans.pdf",
                AskedByUserId = data.creator.Id,
                AnsweredByUserId = data.contractorUser.Id,
                AskedAt = DateTime.UtcNow.AddDays(-2),
                AnsweredAt = DateTime.UtcNow.AddDays(-1),
                Status = ClarificationStatus.Answered
            };
            _db.BidClarifications.Add(clarification);
            await _db.SaveChangesAsync();

            var result = await _clarificationService.GetAttachmentForDownloadAsync(
                filePath: "clarifications/5003_ans.pdf",
                clarificationId: clarification.Id,
                currentUserId: data.colleague.Id,
                isAdmin: false,
                isProcurement: true,
                isEvaluator: false,
                isContractor: false);

            Assert.NotNull(result);
            Assert.Equal("5003_ans.pdf", result.FileName);
        }

        [Fact]
        public async Task DownloadAttachment_PackageClarification_PublicAuthenticatedUser_Success()
        {
            var data = await SeedBaselineDataAsync();

            var clarification = new BidClarification
            {
                Id = 5004,
                BidPackageId = data.package.Id,
                Type = ClarificationType.PackageClarification,
                Title = "Hoi ve tieu chuan ky thuat",
                QuestionContent = "Tieu chuan vat tu A co duoc thay the bang B khong?",
                QuestionAttachmentUrl = "clarifications/5004_question.docx",
                AskedByUserId = data.contractorUser.Id,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };
            _db.BidClarifications.Add(clarification);
            await _db.SaveChangesAsync();

            var result = await _clarificationService.GetAttachmentForDownloadAsync(
                filePath: "clarifications/5004_question.docx",
                clarificationId: clarification.Id,
                currentUserId: data.otherEntityUser.Id,
                isAdmin: false,
                isProcurement: true,
                isEvaluator: false,
                isContractor: false);

            Assert.NotNull(result);
            Assert.Equal("5004_question.docx", result.FileName);
        }

        [Fact]
        public async Task GetClarificationsAsync_ProcuringEntityColleague_CanViewPrivateSubmissionClarifications()
        {
            var data = await SeedBaselineDataAsync();

            var publicClar = new BidClarification
            {
                Id = 6001,
                BidPackageId = data.package.Id,
                Type = ClarificationType.PackageClarification,
                Title = "Cau hoi cong khai HSMT",
                QuestionContent = "Noi dung hoi",
                AskedByUserId = data.contractorUser.Id,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };

            var privateClar = new BidClarification
            {
                Id = 6002,
                BidPackageId = data.package.Id,
                BidSubmissionId = 4001,
                Type = ClarificationType.SubmissionClarification,
                Title = "Giai trinh HSDT rieng tu",
                QuestionContent = "Giai trinh don gia bat thuong",
                AskedByUserId = data.creator.Id,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };

            _db.BidClarifications.AddRange(publicClar, privateClar);
            await _db.SaveChangesAsync();

            var res = await _clarificationService.GetClarificationsAsync(
                data.package.Id,
                data.colleague.Id,
                isAdmin: false,
                isProcurement: true,
                isEvaluator: false,
                isContractor: false);

            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.Equal(2, res.Data.Count);
            Assert.Contains(res.Data, c => c.Id == 6002);
        }

        [Fact]
        public async Task GetClarificationsAsync_DifferentProcuringEntityStaff_CannotViewPrivateSubmissionClarifications()
        {
            var data = await SeedBaselineDataAsync();

            var publicClar = new BidClarification
            {
                Id = 6003,
                BidPackageId = data.package.Id,
                Type = ClarificationType.PackageClarification,
                Title = "Cau hoi cong khai",
                QuestionContent = "Noi dung hoi",
                AskedByUserId = data.contractorUser.Id,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };

            var privateClar = new BidClarification
            {
                Id = 6004,
                BidPackageId = data.package.Id,
                BidSubmissionId = 4001,
                Type = ClarificationType.SubmissionClarification,
                Title = "Giai trinh HSDT mat cua nha thau",
                QuestionContent = "Bao mat",
                AskedByUserId = data.creator.Id,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };

            _db.BidClarifications.AddRange(publicClar, privateClar);
            await _db.SaveChangesAsync();

            var res = await _clarificationService.GetClarificationsAsync(
                data.package.Id,
                data.otherEntityUser.Id,
                isAdmin: false,
                isProcurement: true,
                isEvaluator: false,
                isContractor: false);

            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.Single(res.Data);
            Assert.Equal(6003, res.Data[0].Id);
            Assert.DoesNotContain(res.Data, c => c.Id == 6004);
        }

        [Fact]
        public async Task AnswerClarificationAsync_ProcuringEntityColleague_CanAnswerPackageClarification()
        {
            var data = await SeedBaselineDataAsync();

            var packageClar = new BidClarification
            {
                Id = 7001,
                BidPackageId = data.package.Id,
                Type = ClarificationType.PackageClarification,
                Title = "Hoi ve thoi han bao lanh",
                QuestionContent = "Thoi han hieu luc la bao nhieu ngay?",
                AskedByUserId = data.contractorUser.Id,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };

            _db.BidClarifications.Add(packageClar);
            await _db.SaveChangesAsync();

            var request = new AnswerClarificationRequest
            {
                AnswerContent = "Thoi han hieu luc bao lanh du thau la 120 ngay ke tu thoi diem dong thau.",
                AnswerAttachmentUrl = "clarifications/7001_answer.pdf"
            };

            var res = await _clarificationService.AnswerClarificationAsync(
                clarificationId: packageClar.Id,
                request: request,
                currentUserId: data.colleague.Id,
                isAdmin: false,
                isProcurement: true,
                isContractor: false);

            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.Equal(ClarificationStatus.Answered, res.Data.Status);
            Assert.Equal(data.colleague.Id, res.Data.AnsweredByUserId);
        }

        [Fact]
        public async Task AnswerClarificationAsync_DifferentProcuringEntityStaff_Forbidden()
        {
            var data = await SeedBaselineDataAsync();

            var packageClar = new BidClarification
            {
                Id = 7002,
                BidPackageId = data.package.Id,
                Type = ClarificationType.PackageClarification,
                Title = "Hoi ve cong suat may moc",
                QuestionContent = "May moc thiet bi thi cong can tieu chuan nao?",
                AskedByUserId = data.contractorUser.Id,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };

            _db.BidClarifications.Add(packageClar);
            await _db.SaveChangesAsync();

            var request = new AnswerClarificationRequest
            {
                AnswerContent = "Tra loi trai phep tu don vi khac",
                AnswerAttachmentUrl = null
            };

            var res = await _clarificationService.AnswerClarificationAsync(
                clarificationId: packageClar.Id,
                request: request,
                currentUserId: data.otherEntityUser.Id,
                isAdmin: false,
                isProcurement: true,
                isContractor: false);

            Assert.False(res.Success);
            Assert.Equal(403, res.StatusCode);
        }

        [Fact]
        public async Task CreatePackageClarificationAsync_NotifiesActiveEntityStaff()
        {
            var data = await SeedBaselineDataAsync();

            data.package.Status = BidPackageStatus.Open;
            await _db.SaveChangesAsync();

            var request = new CreatePackageClarificationRequest
            {
                Title = "De nghi lam ro yeu cau ky thuat muc 3",
                QuestionContent = "Vui long huong dan chi tiet cach lap bang gia du thau",
                QuestionAttachmentUrl = null
            };

            var res = await _clarificationService.CreatePackageClarificationAsync(
                data.package.Id,
                request,
                data.contractorUser.Id);

            Assert.True(res.Success);
            Assert.NotEmpty(_notificationService.BulkNotifications);

            var lastBulk = _notificationService.BulkNotifications.Last();
            Assert.Contains(data.creator.Id, lastBulk.UserIds);
            Assert.Contains(data.colleague.Id, lastBulk.UserIds);
            Assert.DoesNotContain(data.otherEntityUser.Id, lastBulk.UserIds);
        }

        [Fact]
        public async Task ClarificationsController_DownloadAttachment_ReturnsNotFoundForArbitraryPath()
        {
            await SeedBaselineDataAsync();

            var controller = new ClarificationsController(_clarificationService, _fileStorageService);

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "104"),
                new Claim(ClaimTypes.Role, "Contractor")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            var result = await controller.DownloadAttachment(filePath: "backups/database_dump.bak");

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }
    }

    public class TestClarificationNotificationService : INotificationService
    {
        public List<(int UserId, string Title, string Message)> SingleNotifications { get; } = new();
        public List<(List<int> UserIds, string Title, string Message)> BulkNotifications { get; } = new();

        public Task<NotificationDto> CreateNotificationAsync(
            int userId, string title, string message, NotificationType type = NotificationType.Info, bool sendEmail = true)
        {
            SingleNotifications.Add((userId, title, message));
            return Task.FromResult(new NotificationDto { Id = SingleNotifications.Count, Title = title, Message = message });
        }

        public Task CreateBulkNotificationsAsync(
            IEnumerable<int> userIds, string title, string message, NotificationType type = NotificationType.Info, bool sendEmail = false)
        {
            BulkNotifications.Add((userIds.ToList(), title, message));
            return Task.CompletedTask;
        }

        public Task<ApiResponse<PaginatedList<NotificationDto>>> GetMyNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 20)
            => Task.FromResult(ApiResponse<PaginatedList<NotificationDto>>.Ok(null!));

        public Task<ApiResponse<int>> GetUnreadCountAsync(int userId)
            => Task.FromResult(ApiResponse<int>.Ok(0));

        public Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, int userId)
            => Task.FromResult(ApiResponse<bool>.Ok(true));

        public Task<ApiResponse<bool>> MarkAllAsReadAsync(int userId)
            => Task.FromResult(ApiResponse<bool>.Ok(true));
    }

    public class TestClarificationFileStorageService : IFileStorageService
    {
        public Task<string> SaveFileAsync(IFormFile file, string subFolder)
            => Task.FromResult($"{subFolder}/{file.FileName}");

        public void DeleteFile(string storedPath) { }

        public string GetPhysicalFilePath(string relativePath)
            => relativePath;

        public Task<FileDownloadResult> ResolveDownloadAsync(string storedPath, string originalFileName, string? contentType = null)
        {
            var ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            return Task.FromResult(FileDownloadResult.FromStream(ms, originalFileName, contentType ?? "application/octet-stream"));
        }

        public Task<string> GetPreSignedUrlAsync(string storedPath, int expirationMinutes = 15)
            => Task.FromResult(string.Empty);

        public bool IsCloudPath(string storedPath)
            => false;
    }
}
