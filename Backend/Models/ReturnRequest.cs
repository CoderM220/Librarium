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
        public string BookTitle { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string StudentEmail { get; set; } = "";
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "pending";
        public string? AdminNote { get; set; }
    }
}