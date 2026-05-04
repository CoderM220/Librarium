using Librarium.Models;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Services
{
    public class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public ReminderService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await SendReminders();
                await CancelExpiredBookings();
                await IssueOverdueFines();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task SendReminders()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LibrariumDbContext>();
            var _email = scope.ServiceProvider.GetRequiredService<EmailService>();
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

                db.Notifications.Add(new Notification
                {
                    StudentId = record.StudentId,
                    Title = "Due Date Reminder",
                    Message = $"'{record.BookTitle}' is due on {record.DueDate:MMM dd, yyyy}. Please return it on time.",
                    Type = "due-reminder",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        private async Task CancelExpiredBookings()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LibrariumDbContext>();
            var _email = scope.ServiceProvider.GetRequiredService<EmailService>();
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
        private async Task IssueOverdueFines()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LibrariumDbContext>();

            var overdue = db.BorrowRecords
                .Where(r => r.Status == "active" && r.DueDate < DateTime.Today)
                .ToList();

            foreach (var record in overdue)
            {
                var alreadyFined = db.Fines.Any(f =>
                    f.BorrowRecordId == record.Id && f.Status != "paid");

                if (alreadyFined) continue;

                db.Fines.Add(new Fine
                {
                    StudentId = record.StudentId,
                    StudentName = record.StudentName,
                    StudentEmail = record.StudentEmail,
                    BookTitle = record.BookTitle,
                    BorrowRecordId = record.Id,
                    Amount = 50,
                    Status = "unpaid",
                    IssuedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
