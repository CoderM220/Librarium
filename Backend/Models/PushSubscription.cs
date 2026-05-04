using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarium.Models
{
    [Table("PushSubscriptions")]
    public class PushSubscription
    {
        [Key]
        public int Id { get; set; }

        public int StudentId { get; set; }

        [Required]
        public string Endpoint { get; set; } = "";

        [Required]
        public string P256dh { get; set; } = "";

        [Required]
        public string Auth { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}