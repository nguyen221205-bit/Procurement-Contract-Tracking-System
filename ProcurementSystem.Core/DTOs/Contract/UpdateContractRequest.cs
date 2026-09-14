using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class UpdateContractRequest
    {
        [MaxLength(3000, ErrorMessage = "Điều khoản hợp đồng không được vượt quá 3000 ký tự.")]
        public string? Terms { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Giá trị hợp đồng phải lớn hơn 0.")]
        public decimal? Value { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
