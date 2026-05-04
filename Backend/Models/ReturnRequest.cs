using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarium.Models
{
    [Table("ReturnRequests")]
    public class ReturnRequest
    {
        [Key]
        public int Id { get; set; }

        public int BorrowRecordId { get; set; }
        public int StudentId { get; set; }
        public int BookId { get; set; }

        [StringLength(300)]
        public string BookTitle { get; set; } = "";

        [StringLength(200)]
        public string StudentName { get; set; } = "";

        [StringLength(254)]
        public string StudentEmail { get; set; } = "";

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // TODO: Replace with enum (e.g. ReturnStatus.Pending / Approved / Rejected)
        [StringLength(20)]
        public string Status { get; set; } = "pending";

        [StringLength(500)]
        public string? AdminNote { get; set; }
    }
}