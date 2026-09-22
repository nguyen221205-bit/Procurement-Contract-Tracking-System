using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.BidSubmission
{
    /// <summary>
    /// Metadata file đính kèm trong hồ sơ dự thầu.
    /// </summary>
    public class SubmissionFileDto
    {
        /// <summary>Mã file đính kèm.</summary>
        public int Id { get; set; }
        /// <summary>Loại tài liệu dạng enum.</summary>
        public SubmissionFileType FileType { get; set; }
        /// <summary>Tên loại tài liệu.</summary>
        public string FileTypeName { get; set; } = string.Empty;
        /// <summary>Tên file gốc.</summary>
        public string FileName { get; set; } = string.Empty;
        /// <summary>Đường dẫn lưu file trên hệ thống.</summary>
        public string FilePath { get; set; } = string.Empty;
        /// <summary>Thời điểm tải file lên.</summary>
        public DateTime UploadedAt { get; set; }
    }
}
