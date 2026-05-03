using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarium.Models
{
    [Table("Books")]
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string Author { get; set; } = "";

        [StringLength(50)]
        public string Genre { get; set; } = "";

        [StringLength(20)]
        public string ISBN { get; set; } = "";

        public int Year { get; set; }

        public int Copies { get; set; }

        public int AvailableCopies { get; set; }

        // "available" or "borrowed"
        [StringLength(20)]
        public string Status { get; set; } = "available";

        [StringLength(10)]
        public string Emoji { get; set; } = "📖";

        [StringLength(500)]
        public string? CoverUrl { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? ShelfId { get; set; }

        [ForeignKey("ShelfId")]
        public Shelf? Shelf { get; set; }
    }
}