using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class BidDocument
    {
        [Key]
        public int Id { get; set; }

        public int BidPackageId { get; set; }

        [Required, MaxLength(300)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BidPackageId")]
        public virtual BidPackage BidPackage { get; set; } = null!;
    }
}
