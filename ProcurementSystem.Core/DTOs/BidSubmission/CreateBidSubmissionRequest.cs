using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.BidSubmission
{
    public class CreateBidSubmissionRequest
    {
        [Required(ErrorMessage = "Phải đính kèm ít nhất 1 file hồ sơ dự thầu.")]
        public List<IFormFile> Files { get; set; } = new();

        [Required(ErrorMessage = "Phải khai báo loại cho từng file đính kèm.")]
        public List<SubmissionFileType> FileTypes { get; set; } = new();
    }
}
