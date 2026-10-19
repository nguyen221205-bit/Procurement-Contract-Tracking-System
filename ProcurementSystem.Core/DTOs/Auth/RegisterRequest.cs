using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Auth
{
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        // Contractor-specific fields
        public string CompanyName { get; set; } = string.Empty;

        [RegularExpression(@"^\d{10}(-\d{3})?$", ErrorMessage = "Mã số thuế không đúng định dạng chuẩn (10 chữ số hoặc 13 chữ số dạng xxxxxxxxxx-xxx).")]
        public string TaxCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}
