using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarium.Models
{
    [Table("BookingRequests")]
    public class BookingRequest
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

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // "pending", "approved", "rejected"
        [StringLength(20)]
        public string Status { get; set; } = "pending";

        [StringLength(500)]
        public string? AdminNote { get; set; }

        [ForeignKey("BookId")]
        public virtual Book? Book { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }
}