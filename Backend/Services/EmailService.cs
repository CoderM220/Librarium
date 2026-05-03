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

        private SmtpClient GetClient()
        {
            return new SmtpClient(_config["SMTP_HOST"])
            {
                Port = int.Parse(_config["SMTP_PORT"]),
                Credentials = new NetworkCredential(
                    _config["SMTP_EMAIL"],
                    _config["SMTP_PASSWORD"]
                ),
                EnableSsl = true
            };
        }

        private MailMessage CreateMail(string to, string subject, string body)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(_config["SMTP_EMAIL"], "Librarium"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mail.To.Add(to);
            return mail;
        }

        // ================= OTP =================
        public async Task SendOtpAsync(string toEmail, string otp)
        {
            var client = GetClient();
            var mail = CreateMail(toEmail, "Your Librarium OTP Code", $"Your OTP is: {otp}");
            await client.SendMailAsync(mail);
        }

        public async Task SendAdminOtpAsync(string email, string otp)
        {
            await SendOtpAsync(email, otp);
        }

        // ================= REMINDER =================
        public async Task SendDueReminderAsync(string email, string name, string bookTitle, DateTime dueDate)
        {
            var client = GetClient();
            var body = $"Hi {name},\n\nYour book \"{bookTitle}\" is due on {dueDate:dd MMM yyyy}. Please return it on time.";
            var mail = CreateMail(email, "📚 Book Due Reminder", body);
            await client.SendMailAsync(mail);
        }

        // ================= BOOKING EXPIRED =================
        public async Task SendBookingExpiredAsync(string email, string name, string bookTitle)
        {
            var client = GetClient();
            var body = $"Hi {name},\n\nYour booking for \"{bookTitle}\" has expired.";
            var mail = CreateMail(email, "⏳ Booking Expired", body);
            await client.SendMailAsync(mail);
        }

        // ================= BOOKING STATUS =================
        public async Task SendBookingStatusAsync(string email, string name, string bookTitle, string status, string? message)
        {
            var client = GetClient();

            var safeMessage = string.IsNullOrEmpty(message) ? "" : message;

            var body = $"Hi {name},\n\nYour booking for \"{bookTitle}\" is {status}.\n\n{safeMessage}";

            var mail = CreateMail(email, "📖 Booking Status Update", body);

            await client.SendMailAsync(mail);
        }

        // ================= EMAIL VALIDATION =================
        public Task<bool> IsEmailRealAsync(string email)
        {
            return Task.FromResult(true);
        }
    }
}