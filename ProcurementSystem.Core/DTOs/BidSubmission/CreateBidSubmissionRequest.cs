using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.BidSubmission
{
    /// <summary>
    /// Dữ liệu nhà thầu gửi khi nộp hồ sơ dự thầu.
    /// </summary>
    public class CreateBidSubmissionRequest
    {
        /// <summary>Danh sách file hồ sơ dự thầu.</summary>
        [Required(ErrorMessage = "Phải đính kèm ít nhất 1 file hồ sơ dự thầu.")]
        public List<IFormFile> Files { get; set; } = new();

        /// <summary>Danh sách loại file tương ứng với từng file đã tải lên.</summary>
        [Required(ErrorMessage = "Phải khai báo loại cho từng file đính kèm.")]
        public List<SubmissionFileType> FileTypes { get; set; } = new();
    }
}
