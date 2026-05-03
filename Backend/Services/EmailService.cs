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

        public async Task SendOtpAsync(string toEmail, string otp)
        {
            var smtpClient = new SmtpClient(_config["SMTP_HOST"])
            {
                Port = int.Parse(_config["SMTP_PORT"]),
                Credentials = new NetworkCredential(
                    _config["SMTP_EMAIL"],
                    _config["SMTP_PASSWORD"]
                ),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_config["SMTP_EMAIL"], "Librarium"),
                Subject = "Your Librarium OTP Code",
                Body = $"Your OTP is: {otp}",
                IsBodyHtml = false
            };

            mail.To.Add(toEmail);

            await smtpClient.SendMailAsync(mail);
        }

        // Disable validation for now
        public Task<bool> IsEmailRealAsync(string email)
        {
            return Task.FromResult(true);
        }
    }
}