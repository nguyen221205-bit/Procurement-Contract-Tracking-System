namespace ProcurementSystem.Core.Interfaces
{
    /// <summary>
    /// Dịch vụ gửi email thông báo và cảnh báo hệ thống
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Gửi email đến địa chỉ người nhận
        /// </summary>
        /// <param name="toEmail">Địa chỉ email người nhận</param>
        /// <param name="subject">Tiêu đề email</param>
        /// <param name="body">Nội dung email</param>
        /// <param name="isHtml">Định dạng HTML hay văn bản thuần (mặc định: true)</param>
        /// <returns>True nếu gửi thành công hoặc giả lập thành công trong môi trường dev; False nếu lỗi</returns>
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    }
}
