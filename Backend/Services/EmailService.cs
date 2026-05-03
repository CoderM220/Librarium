using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Librarium.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _http = new HttpClient();
        }

        private async Task SendEmail(string to, string subject, string html)
        {
            Console.WriteLine("=== BREVO EMAIL FUNCTION CALLED ===");
            var apiKey = _config["BREVO_API_KEY"];
            var fromEmail = _config["FROM_EMAIL"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
            {
                Console.WriteLine("❌ Missing BREVO env variables");
                return;
            }

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            req.Headers.Add("api-key", apiKey);

            var payload = new
            {
                sender = new { email = fromEmail, name = "Librarium" },
                to = new[] { new { email = to } },
                subject = subject,
                htmlContent = html
            };

            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            Console.WriteLine($"BREVO STATUS: {res.StatusCode}");
            Console.WriteLine($"BREVO RESPONSE: {body}");
        }

        public Task SendOtpAsync(string email, string otp)
            => SendEmail(email, "Your OTP Code", $"<h2>Your OTP is: {otp}</h2>");

        public Task SendAdminOtpAsync(string email, string otp)
            => SendOtpAsync(email, otp);

        public Task SendDueReminderAsync(string email, string name, string bookTitle, DateTime dueDate)
            => SendEmail(email, "Book Due Reminder",
                $"Hi {name},<br>Your book <b>{bookTitle}</b> is due on {dueDate:dd MMM yyyy}.");

        public Task SendBookingExpiredAsync(string email, string name, string bookTitle)
            => SendEmail(email, "Booking Expired",
                $"Hi {name},<br>Your booking for <b>{bookTitle}</b> has expired.");

        public Task SendBookingStatusAsync(string email, string name, string bookTitle, string status, string? note)
            => SendEmail(email, "Booking Update",
                $"Hi {name},<br>Your booking for <b>{bookTitle}</b> is <b>{status}</b>.<br>{note}");
    }
}