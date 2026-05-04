using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarium.Models
{
    [Table("Fines")]
    public class Fine
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [StringLength(200)]
        public string StudentName { get; set; } = "";

        [Required]
        [StringLength(254)]
        public string StudentEmail { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string BookTitle { get; set; } = "";

        public int BorrowRecordId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "unpaid";

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }
    }
}
