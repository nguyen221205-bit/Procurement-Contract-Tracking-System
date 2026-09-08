using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class SubmissionFile
    {
        [Key]
        public int Id { get; set; }

        public int BidSubmissionId { get; set; }

        public SubmissionFileType FileType { get; set; }

        [Required, MaxLength(300)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BidSubmissionId")]
        public virtual BidSubmission BidSubmission { get; set; } = null!;
    }
}
