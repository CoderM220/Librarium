using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarium.Models
{
    [Table("BorrowRecords")]
    public class BorrowRecord
    {
        [Key]
        public int Id { get; set; }

        public int? BookId { get; set; }
        public int StudentId { get; set; }

        [StringLength(300)]
        public string BookTitle { get; set; } = "";

        [StringLength(200)]
        public string StudentName { get; set; } = "";

        [StringLength(200)]
        public string StudentEmail { get; set; } = "";

        public DateTime IssuedDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedDate { get; set; }

        // TODO: Replace with enum (e.g. BorrowStatus.Active / Overdue / Returned)
        [StringLength(20)]
        public string Status { get; set; } = "active";

        [ForeignKey("BookId")]
        public virtual Book? Book { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }
}