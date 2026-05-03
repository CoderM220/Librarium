using System.Net;
using System.Net.Mail;

namespace Librarium.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // 🔹 Core email sender (REUSABLE)
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var host = _config["SMTP_HOST"];
            var port = _config["SMTP_PORT"];
            var email = _config["SMTP_EMAIL"];
            var password = _config["SMTP_PASSWORD"];

            if (string.IsNullOrEmpty(host) ||
                string.IsNullOrEmpty(port) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password))
            {
                throw new Exception("SMTP configuration is missing in environment variables.");
            }

            var smtpClient = new SmtpClient(host)
            {
                Port = int.Parse(port),
                Credentials = new NetworkCredential(email, password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(email, "Librarium"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mail.To.Add(toEmail);

            await smtpClient.SendMailAsync(mail);
        }

        // 🔹 OTP for students
        public async Task SendOtpAsync(string toEmail, string otp)
        {
            var subject = "Your Librarium OTP Code";
            var body = $"Your OTP is: {otp}";

            await SendEmailAsync(toEmail, subject, body);
        }

        // 🔹 OTP for admin (reuse)
        public async Task SendAdminOtpAsync(string email, string otp)
        {
            await SendOtpAsync(email, otp);
        }

        // 🔹 Reminder for due books
        public async Task SendDueReminderAsync(string email, string bookTitle, DateTime dueDate)
        {
            var subject = "📚 Book Due Reminder";
            var body = $"Reminder: \"{bookTitle}\" is due on {dueDate:dd MMM yyyy}. Please return it on time.";

            await SendEmailAsync(email, subject, body);
        }

        // 🔹 Booking expired
        public async Task SendBookingExpiredAsync(string email, string bookTitle)
        {
            var subject = "⏳ Booking Expired";
            var body = $"Your booking for \"{bookTitle}\" has expired.";

            await SendEmailAsync(email, subject, body);
        }

        // 🔹 Booking status update
        public async Task SendBookingStatusAsync(string email, string bookTitle, string status)
        {
            var subject = "📖 Booking Status Update";
            var body = $"Your booking for \"{bookTitle}\" is now: {status}.";

            await SendEmailAsync(email, subject, body);
        }

        // 🔹 Email validation (disabled for now)
        public Task<bool> IsEmailRealAsync(string email)
        {
            return Task.FromResult(true);
        }
    }
}