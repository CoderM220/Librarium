using Librarium.Filters;
using Librarium.Models;
using Librarium.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Controllers
{
    [AdminAuthorize]
    public class AdminController : Controller
    {
        private readonly LibrariumDbContext _db;
        private readonly EmailService _email;
        private readonly PushNotificationService _push;

        public AdminController(LibrariumDbContext db, EmailService email, PushNotificationService push)
        {
            _db = db;
            _email = email;
            _push = push;
        }

        // ── DASHBOARD ──
        public IActionResult Index()
        {
            UpdateOverdueStatuses();

            var vm = new AdminDashboardViewModel
            {
                TotalBooks = _db.Books.Sum(b => (int?)b.Copies) ?? 0,
                TotalMembers = _db.Students.Count(),
                ActiveBorrows = _db.BorrowRecords.Count(r => r.Status == "active" || r.Status == "overdue"),
                OverdueCount = _db.BorrowRecords.Count(r => r.Status == "overdue"),
                BookingRequestCount = _db.BookingRequests.Count(r => r.Status == "pending"),
                ShelfCount = _db.Shelves.Count(),
                RecentBooks = _db.Books.OrderByDescending(b => b.CreatedAt).Take(6).ToList(),
                DueReturns = _db.BorrowRecords
                    .Where(r => r.Status == "active" || r.Status == "overdue")
                    .OrderBy(r => r.DueDate).Take(4).ToList(),
                GenreCounts = _db.Books
                    .GroupBy(b => b.Genre)
                    .Select(g => new GenreCount { Genre = g.Key, Count = g.Sum(b => b.Copies) })
                    .ToList()
            };

            return View(vm);
        }

        // ── APPROVE BOOKING ──
        [HttpPost]
        public async Task<IActionResult> ApproveBooking(int id, string? adminNote)
        {
            var request = _db.BookingRequests.Find(id);
            if (request == null) return NotFound();

            var book = _db.Books.Find(request.BookId);
            if (book == null || book.AvailableCopies <= 0)
                return Json(new { success = false, message = "No copies available." });

            var activeBorrowCount = _db.BorrowRecords.Count(r =>
                r.StudentId == request.StudentId &&
                (r.Status == "active" || r.Status == "overdue"));

            if (activeBorrowCount >= 3)
                return Json(new { success = false, message = "⚠️ Borrow limit reached!" });

            var record = new BorrowRecord
            {
                BookId = request.BookId,
                StudentId = request.StudentId,
                BookTitle = request.BookTitle,
                StudentName = request.StudentName,
                StudentEmail = request.StudentEmail,
                IssuedDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(14),
                Status = "active"
            };

            book.AvailableCopies--;
            if (book.AvailableCopies == 0) book.Status = "borrowed";

            request.Status = "approved";
            request.AdminNote = adminNote;

            _db.BorrowRecords.Add(record);
            _db.SaveChanges();

            // ✅ EMAIL
            try
            {
                await _email.SendBookingStatusAsync(
                    request.StudentEmail,
                    request.StudentName,
                    request.BookTitle,
                    "approved",
                    adminNote
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
            }

            // ✅ PUSH
            try
            {
                await _push.SendToStudent(
                    request.StudentId,
                    "✅ Booking Approved",
                    $"Your request for '{request.BookTitle}' has been approved!",
                    "booking-approved"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("PUSH ERROR: " + ex.Message);
            }

            _db.Notifications.Add(new Notification
            {
                StudentId = request.StudentId,
                Title = "Booking Approved",
                Message = $"Your request for '{request.BookTitle}' has been approved!",
                Type = "booking-approved",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            _db.SaveChanges();

            return Json(new { success = true, message = "Booking approved!" });
        }

        // ── REJECT BOOKING ──
        [HttpPost]
        public async Task<IActionResult> RejectBooking(int id, string? adminNote)
        {
            var request = _db.BookingRequests.Find(id);
            if (request == null) return NotFound();

            request.Status = "rejected";
            request.AdminNote = adminNote;

            _db.SaveChanges();

            // ✅ EMAIL
            try
            {
                await _email.SendBookingStatusAsync(
                    request.StudentEmail,
                    request.StudentName,
                    request.BookTitle,
                    "rejected",
                    adminNote
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
            }

            // ✅ PUSH
            try
            {
                await _push.SendToStudent(
                    request.StudentId,
                    "❌ Booking Rejected",
                    $"Your request for '{request.BookTitle}' was not approved.",
                    "booking-rejected"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("PUSH ERROR: " + ex.Message);
            }

            _db.Notifications.Add(new Notification
            {
                StudentId = request.StudentId,
                Title = "Booking Rejected",
                Message = $"Your request for '{request.BookTitle}' was not approved.",
                Type = "booking-rejected",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            _db.SaveChanges();

            return Json(new { success = true, message = "Booking rejected." });
        }

        // ── FORGOT PASSWORD ──
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var admin = _db.AdminUsers.FirstOrDefault(a => a.Email == email);
            if (admin == null)
            {
                ViewBag.Error = "No admin account found.";
                return View();
            }

            var otp = new Random().Next(100000, 999999).ToString();

            admin.OtpCode = otp;
            admin.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            _db.SaveChanges();

            try
            {
                await _email.SendAdminOtpAsync(email, otp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
                ViewBag.Error = "Failed to send OTP.";
                return View();
            }

            HttpContext.Session.SetString("AdminResetEmail", email);

            return RedirectToAction("AdminResetVerifyOtp");
        }

        // ── HELPER ──
        private void UpdateOverdueStatuses()
        {
            var today = DateTime.Today;

            var toMark = _db.BorrowRecords
                .Where(r => r.Status == "active" && r.DueDate < today)
                .ToList();

            foreach (var r in toMark)
                r.Status = "overdue";

            if (toMark.Any())
                _db.SaveChanges();
        }

        
    }
}
