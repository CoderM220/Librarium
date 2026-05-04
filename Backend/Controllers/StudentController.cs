using Microsoft.EntityFrameworkCore;
using Librarium.Models;
using Librarium.Filters;
using Microsoft.AspNetCore.Mvc;
namespace Librarium.Controllers
{
    [StudentAuthorize]
    public class StudentController : Controller
    {
        private readonly LibrariumDbContext _db;
        public StudentController(LibrariumDbContext db) { _db = db; }

        [HttpPost]
        public IActionResult SavePushSubscription([FromBody] PushSubscriptionRequest sub)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null) return Unauthorized();

            // Remove old subscriptions for this student
            var old = _db.PushSubscriptions.Where(s => s.StudentId == studentId).ToList();
            _db.PushSubscriptions.RemoveRange(old);

            _db.PushSubscriptions.Add(new PushSubscription
            {
                StudentId = studentId.Value,
                Endpoint = sub.Endpoint,
                P256dh = sub.P256dh,
                Auth = sub.Auth,
                CreatedAt = DateTime.UtcNow
            });
            _db.SaveChanges();
            return Json(new { success = true });
        }

        public IActionResult Index()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            var borrows = _db.BorrowRecords.Where(r => r.StudentId == studentId).OrderByDescending(r => r.IssuedDate).ToList();
            var vm = new StudentDashboardViewModel
            {
                Student = _db.Students.Find(studentId)!,
                ActiveBorrows = borrows.Count(r => r.Status == "active" || r.Status == "overdue"),
                ReturnedCount = borrows.Count(r => r.Status == "returned"),
                HasOverdue = borrows.Any(r => r.Status == "overdue"),
                RecentBorrows = borrows.Take(5).ToList()
            };
            return View(vm);
        }

        public IActionResult Books(string search = "", string genre = "")
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            var q = _db.Books.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
            if (!string.IsNullOrEmpty(genre))
                q = q.Where(b => b.Genre == genre);
            ViewBag.Search = search;
            ViewBag.Genre = genre;
            ViewBag.Genres = _db.Books.Select(b => b.Genre).Distinct().OrderBy(g => g).ToList();

            // Pass pending booking IDs so view can disable buttons
            ViewBag.PendingBookIds = _db.BookingRequests
               .Where(r => r.StudentId == studentId && r.Status == "pending")
               .Select(r => r.BookId)
               .Where(id => id != null)
               .Select(id => id!.Value)
               .ToList();
            return View(q.OrderBy(b => b.Title).ToList());
        }

        public IActionResult MyBorrows()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            return View(_db.BorrowRecords.Where(r => r.StudentId == studentId).OrderByDescending(r => r.IssuedDate).ToList());
        }

        [HttpPost]
        public IActionResult RequestBooking(int bookId)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            var book = _db.Books.Find(bookId);
            var student = _db.Students.Find(studentId);

            if (book == null)
                return Json(new { success = false, message = "Book not found." });

            // Check if student already has a pending request for this book
            if (_db.BookingRequests.Any(r => r.StudentId == studentId && r.BookId == bookId && r.Status == "pending"))
                return Json(new { success = false, message = "You already have a pending request for this book." });
            // Check if student already has it borrowed
            if (_db.BorrowRecords.Any(r => r.StudentId == studentId && r.BookId == bookId && r.Status == "active"))
                return Json(new { success = false, message = "You already have this book borrowed." });

            var request = new BookingRequest
            {
                BookId = bookId,
                StudentId = studentId!.Value,
                BookTitle = book.Title,
                StudentName = student!.FullName,
                StudentEmail = student.Email,
                RequestedAt = DateTime.UtcNow,
                Status = "pending"
            };

            _db.BookingRequests.Add(request);
            _db.SaveChanges();

            return Json(new { success = true, message = $"Request for '{book.Title}' submitted! Waiting for admin approval." });
        }

        [HttpPost]
        public IActionResult RequestReturn(int borrowRecordId)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            var record = _db.BorrowRecords.Find(borrowRecordId);
            if (record == null || record.StudentId != studentId)
                return Json(new { success = false, message = "Record not found." });
            if (_db.ReturnRequests.Any(r => r.BorrowRecordId == borrowRecordId && r.Status == "pending"))
                return Json(new { success = false, message = "You already have a pending return request." });
            try
            {
                var request = new ReturnRequest
                {
                    BorrowRecordId = borrowRecordId,
                    StudentId = studentId!.Value,
                    BookId = record.BookId ?? 0,
                    BookTitle = record.BookTitle,
                    StudentName = record.StudentName,
                    StudentEmail = record.StudentEmail,
                    RequestedAt = DateTime.UtcNow,
                    Status = "pending"
                };
                _db.ReturnRequests.Add(request);
                _db.SaveChanges();
                return Json(new { success = true, message = $"Return request for '{record.BookTitle}' submitted!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET - My Booking Requests
        public IActionResult MyBookings()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            var bookings = _db.BookingRequests
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.RequestedAt)
                .ToList();
            return View(bookings);
        }

        [HttpPost]
        public IActionResult RequestBorrow(int bookId)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            var book = _db.Books.Find(bookId);
            var student = _db.Students.Find(studentId);
            if (book == null || book.AvailableCopies <= 0)
                return Json(new { success = false, message = "Book not available." });
            if (_db.BorrowRecords.Any(r => r.StudentId == studentId && r.BookId == bookId && r.Status == "active"))
                return Json(new { success = false, message = "You already have this book borrowed." });
            var record = new BorrowRecord
            {
                BookId = bookId,
                StudentId = studentId!.Value,
                BookTitle = book.Title,
                StudentName = student!.FullName,
                StudentEmail = student.Email,
                IssuedDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(14),
                Status = "active"
            };
            book.AvailableCopies--;
            if (book.AvailableCopies == 0) book.Status = "borrowed";
            _db.BorrowRecords.Add(record);
            _db.SaveChanges();
            return Json(new { success = true, message = $"'{book.Title}' borrowed! Due {record.DueDate:MMM d, yyyy}" });
        }

        public IActionResult Profile()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            var totalBooks = _db.BorrowRecords.Count(r => r.StudentId == studentId);
            var myBorrows = _db.BorrowRecords.Count(r => r.StudentId == studentId && (r.Status == "active" || r.Status == "overdue"));
            var myBookings = _db.BookingRequests.Count(r => r.StudentId == studentId && r.Status == "pending");
            ViewBag.TotalBooks = totalBooks;
            ViewBag.MyBorrows = myBorrows;
            ViewBag.MyBookings = myBookings;
            return View(_db.Students.Find(studentId));
        }

        // ── SHELVES (view only) ──
        public IActionResult Shelves()
        {
            var shelves = _db.Shelves
                .Include(s => s.Books)
                .OrderBy(s => s.ShelfCode)
                .ToList();
            return View(shelves);
        }

        // ── BOOK SEARCH AUTOCOMPLETE ──
        public IActionResult BookSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            var results = _db.Books
                .Where(b => b.Title.Contains(q) || b.Author.Contains(q))
                .OrderBy(b => b.Title)
                .Take(8)
                .Select(b => new {
                    b.Id,
                    b.Title,
                    b.Author,
                    b.Genre,
                    b.ISBN,
                    b.Year,
                    b.Status,
                    b.AvailableCopies,
                    b.Copies,
                    b.Emoji,
                    ShelfCode = b.Shelf != null ? b.Shelf.ShelfCode : (string?)null,
                    ShelfType = b.Shelf != null ? b.Shelf.ShelfType : (string?)null
                })
                .ToList();

            return Json(results);
        }

        // ── BOOK DETAIL FOR MODAL ──
        public IActionResult BookDetail(int id)
        {
            var b = _db.Books
                .Include(x => x.Shelf)
                .FirstOrDefault(x => x.Id == id);
            if (b == null) return NotFound();

            return Json(new
            {
                b.Id,
                b.Title,
                b.Author,
                b.Genre,
                b.ISBN,
                b.Year,
                b.Status,
                b.Description,
                b.AvailableCopies,
                b.Copies,
                b.Emoji,
                ShelfCode = b.Shelf != null ? b.Shelf.ShelfCode : (string?)null,
                ShelfType = b.Shelf != null ? b.Shelf.ShelfType : (string?)null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string firstName, string lastName, string currentPassword, string newPassword, string confirmPassword)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            var student = _db.Students.Find(studentId);
            if (student == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(firstName)) student.FirstName = firstName;
            if (!string.IsNullOrWhiteSpace(lastName)) student.LastName = lastName;
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, student.PasswordHash))
            { TempData["PwError"] = "Current password is incorrect."; return RedirectToAction("Profile"); }
            if (newPassword != confirmPassword)
            { TempData["PwError"] = "Passwords do not match."; return RedirectToAction("Profile"); }
            if (newPassword.Length < 8)
            { TempData["PwError"] = "Password must be at least 8 characters."; return RedirectToAction("Profile"); }
            student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.SaveChanges();
            TempData["PwSuccess"] = "Password changed successfully!";
            return RedirectToAction("Profile");
        }

    }
    public class StudentDashboardViewModel
    {
        public Student Student { get; set; } = new();
        public int ActiveBorrows { get; set; }
        public int ReturnedCount { get; set; }
        public bool HasOverdue { get; set; }
        public List<BorrowRecord> RecentBorrows { get; set; } = new();
    }
}