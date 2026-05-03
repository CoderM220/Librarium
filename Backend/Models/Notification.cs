namespace Librarium.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Student? Student { get; set; }
    }
}