using WebPush;
using Librarium.Models;
using Microsoft.Extensions.Configuration;

namespace Librarium.Services
{
    public class PushNotificationService
    {
        private readonly IConfiguration _config;
        private readonly LibrariumDbContext _db;

        public PushNotificationService(IConfiguration config, LibrariumDbContext db)
        {
            _config = config;
            _db = db;
        }

        public async Task SendToStudent(int studentId, string title, string body, string tag = "librarium")
        {
            var subscriptions = _db.PushSubscriptions
                .Where(s => s.StudentId == studentId)
                .ToList();

            var publicKey = _config["VapidKeys:PublicKey"]!;
            var privateKey = _config["VapidKeys:PrivateKey"]!;
            var subject = _config["VapidKeys:Subject"]!;

            var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body, tag });

            foreach (var sub in subscriptions)
            {
                try
                {
                    var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                    var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
                    var client = new WebPushClient();
                    await client.SendNotificationAsync(pushSub, payload, vapidDetails);
                }
                catch { }
            }
        }
    }
}