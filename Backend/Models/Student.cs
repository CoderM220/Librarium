using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarium.Models
{
    [Table("Students")]
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = "";

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; } = "";

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
        public bool IsVerified { get; set; } = false;
        public int OtpAttempts { get; set; } = 0;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

    }
}
