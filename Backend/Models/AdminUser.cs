using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarium.Models
{
    [Table("AdminUsers")]
    public class AdminUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = "";

        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; } = "";

        [StringLength(100)]
        public string DisplayName { get; set; } = "";

        [StringLength(200)]
        public string Email { get; set; } = "";
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
    }
}