using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Librarium.Models;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Services
{
    public class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly EmailService _email;

        public ReminderService(IServiceProvider services, EmailService email)
        {
            _services = services;
            _email = email;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await SendReminders();
                await CancelExpiredBookings();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task SendReminders()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LibrariumDbContext>();
            var targetDate = DateTime.Today.AddDays(3);
            var dueSoon = db.BorrowRecords
                .Where(r => r.Status == "active" && r.DueDate.Date == targetDate)
                .ToList();
            foreach (var record in dueSoon)
            {
                try
                {
                    await _email.SendDueReminderAsync(
                        record.StudentEmail,
                        record.StudentName,
                        record.BookTitle,
                        record.DueDate);
                }
                catch { }
            }
        }

        private async Task CancelExpiredBookings()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LibrariumDbContext>();
            var cutoff = DateTime.UtcNow.AddHours(-24);
            var expired = db.BookingRequests
                .Where(r => r.Status == "pending" && r.RequestedAt <= cutoff)
                .ToList();
            foreach (var booking in expired)
            {
                booking.Status = "expired";
                try
                {
                    await _email.SendBookingExpiredAsync(
                        booking.StudentEmail,
                        booking.StudentName,
                        booking.BookTitle);
                }
                catch { }
            }
            if (expired.Any())
                await db.SaveChangesAsync();
        }
    }
}