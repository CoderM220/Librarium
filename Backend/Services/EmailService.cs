using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace Librarium.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string toEmail, string otp)
        {
            var fromEmail = _config["EmailSettings:FromEmail"] ?? throw new InvalidOperationException("FromEmail not configured.");
            var appPassword = _config["EmailSettings:AppPassword"] ?? throw new InvalidOperationException("AppPassword not configured.");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Your Librarium OTP Code";

            message.Body = new TextPart("plain")
            {
                Text = $"Your OTP code is: {otp}\n\nThis code expires in 10 minutes."
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(fromEmail, appPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendBookingStatusAsync(string toEmail, string studentName, string bookTitle, string status, string? adminNote)
        {
            var fromEmail = _config["EmailSettings:FromEmail"] ?? throw new InvalidOperationException("FromEmail not configured.");
            var appPassword = _config["EmailSettings:AppPassword"] ?? throw new InvalidOperationException("AppPassword not configured.");

            var isApproved = status == "approved";
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = isApproved
                ? $"✅ Booking Approved — {bookTitle}"
                : $"❌ Booking Rejected — {bookTitle}";

            message.Body = new TextPart("plain")
            {
                Text = $"Hi {studentName},\n\n" +
                       (isApproved
                           ? $"Your booking request for \"{bookTitle}\" has been approved! Please collect the book from the library within 2 days.\n\nYou have 14 days from collection to return it."
                           : $"Unfortunately your booking request for \"{bookTitle}\" has been rejected.") +
                       (string.IsNullOrEmpty(adminNote) ? "" : $"\n\nAdmin note: {adminNote}") +
                       "\n\n— Librarium"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(fromEmail, appPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendDueReminderAsync(string toEmail, string studentName, string bookTitle, DateTime dueDate)
        {
            var fromEmail = _config["EmailSettings:FromEmail"] ?? throw new InvalidOperationException("FromEmail not configured.");
            var appPassword = _config["EmailSettings:AppPassword"] ?? throw new InvalidOperationException("AppPassword not configured.");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"⏰ Reminder — '{bookTitle}' due in 3 days";
            message.Body = new TextPart("plain")
            {
                Text = $"Hi {studentName},\n\n" +
                       $"This is a reminder that \"{bookTitle}\" is due on {dueDate:MMM d, yyyy} — just 3 days away!\n\n" +
                       $"Please return it to the library on time to avoid any issues.\n\n" +
                       $"If you'd like to request a return, you can do so from your My Borrows page.\n\n" +
                       "— Librarium"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(fromEmail, appPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendAdminOtpAsync(string toEmail, string otp)
        {
            var fromEmail = _config["EmailSettings:AdminEmail"] ?? throw new InvalidOperationException("AdminEmail not configured.");
            var appPassword = _config["EmailSettings:AdminAppPassword"] ?? throw new InvalidOperationException("AdminAppPassword not configured.");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Librarium Admin — Password Reset OTP";

            message.Body = new TextPart("plain")
            {
                Text = $"Your admin password reset OTP is: {otp}\n\nThis code expires in 10 minutes.\n\n— Librarium"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(fromEmail, appPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        public async Task SendBookingExpiredAsync(string toEmail, string studentName, string bookTitle)
        {
            var fromEmail = _config["EmailSettings:FromEmail"] ?? throw new InvalidOperationException("FromEmail not configured.");
            var appPassword = _config["EmailSettings:AppPassword"] ?? throw new InvalidOperationException("AppPassword not configured.");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"⏳ Booking Expired — {bookTitle}";
            message.Body = new TextPart("plain")
            {
                Text = $"Hi {studentName},\n\n" +
                       $"Your booking request for \"{bookTitle}\" has expired because it was not approved within 24 hours.\n\n" +
                       $"If you're still interested in this book, please visit the library portal and submit a new booking request.\n\n" +
                       $"— Librarium"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(fromEmail, appPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        public async Task<bool> IsEmailRealAsync(string email)
        {
            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(8);

                var apiKey = _config["EmailValidation:ApiKey"] ?? throw new InvalidOperationException("Email validation API key not configured.");
                var url = $"https://emailreputation.abstractapi.com/v1/?api_key={apiKey}&email={Uri.EscapeDataString(email)}";
                var response = await http.GetStringAsync(url);

                var json = System.Text.Json.JsonDocument.Parse(response).RootElement;

                // Correct property paths based on actual API response
                var status = json.GetProperty("email_deliverability").GetProperty("status").GetString();
                var isSmtpValid = json.GetProperty("email_deliverability").GetProperty("is_smtp_valid").GetBoolean();
                var isFormatValid = json.GetProperty("email_deliverability").GetProperty("is_format_valid").GetBoolean();
                var isDisposable = json.GetProperty("email_quality").GetProperty("is_disposable").GetBoolean();

                return isFormatValid && isSmtpValid && status == "deliverable" && !isDisposable;
            }
            catch
            {
                return true;
            }
        }
    }
}