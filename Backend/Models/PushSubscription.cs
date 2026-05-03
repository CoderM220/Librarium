namespace Librarium.Models
{
    public class PushSubscription
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Endpoint { get; set; } = "";
        public string P256dh { get; set; } = "";
        public string Auth { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}