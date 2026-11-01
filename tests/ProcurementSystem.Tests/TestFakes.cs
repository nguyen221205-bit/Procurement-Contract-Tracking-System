using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Contractor;
using ProcurementSystem.Core.DTOs.Notification;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Tests
{
    public class FakeEmailService : IEmailService
    {
        public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

        public Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            SentEmails.Add((toEmail, subject, body));
            return Task.FromResult(true);
        }
    }

    public class FakeNotificationService : INotificationService
    {
        public Task<NotificationDto> CreateNotificationAsync(
            int userId, string title, string message, NotificationType type = NotificationType.Info, bool sendEmail = true)
            => Task.FromResult(new NotificationDto());

        public Task CreateBulkNotificationsAsync(
            IEnumerable<int> userIds, string title, string message, NotificationType type = NotificationType.Info, bool sendEmail = false)
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

    public class FakeContractorService : IContractorService
    {
        public Task<ApiResponse<PaginatedList<ContractorDto>>> GetContractorsAsync(ContractorFilterParams filter) =>
            Task.FromResult(ApiResponse<PaginatedList<ContractorDto>>.Ok(new PaginatedList<ContractorDto>(new List<ContractorDto>(), 0, 1, 10)));
        public Task<ApiResponse<ContractorDto>> GetContractorByIdAsync(int id) =>
            Task.FromResult(ApiResponse<ContractorDto>.Ok(new ContractorDto()));
        public Task<ApiResponse<ContractorDto>> GetCurrentContractorProfileAsync(int userId) =>
            Task.FromResult(ApiResponse<ContractorDto>.Ok(new ContractorDto()));
        public Task<ApiResponse<ContractorDto>> UpdateCurrentContractorProfileAsync(int userId, UpdateContractorProfileRequest request) =>
            Task.FromResult(ApiResponse<ContractorDto>.Ok(new ContractorDto()));
        public Task<ApiResponse<ContractorDto>> UpdateContractorRatingAsync(int contractorId, UpdateContractorRatingRequest request) =>
            Task.FromResult(ApiResponse<ContractorDto>.Ok(new ContractorDto()));
        public Task<ApiResponse<ContractorDto>> VerifyContractorAsync(int contractorId, VerifyContractorRequest request, int reviewerUserId) =>
            Task.FromResult(ApiResponse<ContractorDto>.Ok(new ContractorDto()));
        public Task<ApiResponse<ContractorDto>> RecalculateContractorRatingAsync(int contractorId) =>
            Task.FromResult(ApiResponse<ContractorDto>.Ok(new ContractorDto()));
        public Task<ApiResponse<ImportContractorResultDto>> ImportContractorsFromExcelAsync(IFormFile file, int? adminUserId = null) =>
            Task.FromResult(ApiResponse<ImportContractorResultDto>.Ok(new ImportContractorResultDto()));
        public Task<byte[]> GenerateImportTemplateExcelAsync() =>
            Task.FromResult(Array.Empty<byte>());
    }
}
