namespace ProcurementSystem.Core.DTOs.Contractor
{
    /// <summary>
    /// Thông tin hồ sơ năng lực và pháp lý của nhà thầu
    /// </summary>
    public class ContractorDto
    {
        /// <summary>Mã định danh duy nhất của nhà thầu</summary>
        public int Id { get; set; }

        /// <summary>Mã định danh tài khoản người dùng liên kết</summary>
        public int UserId { get; set; }

        /// <summary>Tên doanh nghiệp hoặc tổ chức nhà thầu</summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Mã số thuế doanh nghiệp</summary>
        public string? TaxCode { get; set; }

        /// <summary>Địa chỉ trụ sở chính của nhà thầu</summary>
        public string? Address { get; set; }

        /// <summary>Đường dẫn tệp giấy phép kinh doanh tải lên hệ thống</summary>
        public string? BusinessLicenseFile { get; set; }

        /// <summary>Điểm đánh giá uy tín năng lực nhà thầu (0.0 đến 5.0 sao)</summary>
        public decimal Rating { get; set; }

        /// <summary>Thời điểm nhà thầu đăng ký trên hệ thống (UTC)</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Họ và tên người đại diện pháp luật của nhà thầu</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Email liên hệ chính thức của người đại diện</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Số điện thoại liên hệ</summary>
        public string? Phone { get; set; }
    }
}
