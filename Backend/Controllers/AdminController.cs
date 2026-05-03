using Librarium.Filters;
using Librarium.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Librarium.Controllers
{
    [AdminAuthorize]
    public class AdminController : Controller
    {
        private readonly LibrariumDbContext _db;
        private readonly Librarium.Services.EmailService _email;
        public AdminController(LibrariumDbContext db, Librarium.Services.EmailService email)
        {
            _db = db;
            _email = email;
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
                BookingRequestCount = _db.BookingRequests.Count(r => r.Status == "pending"),  // ADD THIS
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

        // ── BOOKS ──
        public IActionResult Books(string filter = "all", string search = "")
        {
            var q = _db.Books.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
            if (filter != "all")
                q = q.Where(b => b.Status == filter);
            ViewBag.Filter = filter;
            ViewBag.Search = search;
            return View(q.OrderBy(b => b.Title).ToList());
        }

        // ── ADD BOOK GET ──
        public IActionResult AddBook()
        {
            ViewBag.Shelves = _db.Shelves.ToList().OrderBy(s => {
                var parts = System.Text.RegularExpressions.Regex.Match(s.ShelfCode, @"([A-Za-z]+)(\d+)");
                return parts.Success ? parts.Groups[1].Value + parts.Groups[2].Value.PadLeft(5, '0') : s.ShelfCode;
            }).ToList();
            return View(new Book());
        }

        // ── ADD BOOK POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBook(Book book)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Shelves = _db.Shelves.OrderBy(s => s.ShelfCode).ToList();
                return View(book);
            }
            if (!string.IsNullOrEmpty(book.ISBN) && _db.Books.Any(b => b.ISBN == book.ISBN))
            {
                ModelState.AddModelError("ISBN", "A book with this ISBN already exists.");
                ViewBag.Shelves = _db.Shelves.OrderBy(s => s.ShelfCode).ToList();
                return View(book);
            }
            book.AvailableCopies = book.Copies;
            book.Status = "available";
            book.CreatedAt = DateTime.UtcNow;
            book.CoverUrl = Request.Form["CoverUrl"].ToString();
            _db.Books.Add(book);
            _db.SaveChanges();
            TempData["Success"] = $"'{book.Title}' added successfully!";
            return RedirectToAction("Books");
        }

        // ── EDIT BOOK GET ──
        public IActionResult EditBook(int id)
        {
            var book = _db.Books.Find(id);
            if (book == null) return NotFound();
            ViewBag.Shelves = _db.Shelves.ToList().OrderBy(s => {
                var parts = System.Text.RegularExpressions.Regex.Match(s.ShelfCode, @"([A-Za-z]+)(\d+)");
                return parts.Success ? parts.Groups[1].Value + parts.Groups[2].Value.PadLeft(5, '0') : s.ShelfCode;
            }).ToList();
            return View(book);
        }

        // ── EDIT BOOK POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditBook(Book book)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Shelves = _db.Shelves.ToList().OrderBy(s => {
                    var parts = System.Text.RegularExpressions.Regex.Match(s.ShelfCode, @"([A-Za-z]+)(\d+)");
                    return parts.Success ? parts.Groups[1].Value + parts.Groups[2].Value.PadLeft(5, '0') : s.ShelfCode;
                }).ToList();
                return View(book);
            }
            var existing = _db.Books.Find(book.Id);
            if (existing == null) return NotFound();
            int delta = book.Copies - existing.Copies;
            existing.Title = book.Title;
            existing.Author = book.Author;
            existing.Genre = book.Genre;
            existing.ISBN = book.ISBN;
            existing.Year = book.Year;
            existing.Copies = book.Copies;
            existing.AvailableCopies = Math.Max(0, existing.AvailableCopies + delta);
            existing.Emoji = book.Emoji;
            if (!string.IsNullOrEmpty(book.CoverUrl))
                existing.CoverUrl = book.CoverUrl;
            existing.Description = book.Description;
            existing.ShelfId = book.ShelfId;
            existing.Status = existing.AvailableCopies == 0 ? "borrowed" : "available";
            _db.SaveChanges();
            TempData["Success"] = "Book updated.";
            return RedirectToAction("Books");
        }

        // ── DELETE BOOK POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBook(int id)
        {
            var book = _db.Books.Find(id);
            if (book == null) return NotFound();

            // Block if any copy is currently borrowed
            if (_db.BorrowRecords.Any(r => r.BookId == id && r.Status == "active"))
                return Json(new { success = false, message = "Cannot delete: book is currently borrowed." });

            // Detach borrow history from this book (keep records, just null out the FK)
            var history = _db.BorrowRecords.Where(r => r.BookId == id).ToList();
            foreach (var r in history)
                r.BookId = null;

            _db.Books.Remove(book);
            _db.SaveChanges();
            return Json(new { success = true });
        }
        // ── REDUCE COPIES POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReduceCopies(int id, int removeCount)
        {
            try
            {
                var book = _db.Books.Find(id);
                if (book == null) return NotFound();

                if (removeCount > book.AvailableCopies)
                    return Json(new { success = false, message = $"Only {book.AvailableCopies} copy/copies are not currently borrowed. Cannot remove more than that." });

                book.Copies -= removeCount;
                book.AvailableCopies -= removeCount;

                if (book.Copies <= 0)
                {
                    var history = _db.BorrowRecords.Where(r => r.BookId == id).ToList();
                    foreach (var r in history) r.BookId = null;
                    var bookingRequests = _db.BookingRequests.Where(r => r.BookId == id).ToList();
                    foreach (var r in bookingRequests) r.BookId = null;
                    _db.Books.Remove(book);
                    _db.SaveChanges();
                    return Json(new { success = true, deleted = true });
                }

                book.Status = book.AvailableCopies == 0 ? "borrowed" : "available";
                _db.SaveChanges();
                return Json(new { success = true, deleted = false, newCopies = book.Copies, newAvailable = book.AvailableCopies });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message + " | " + ex.InnerException?.Message });
            }
        }
        // ── SHELVES PAGE ──
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
                b.CoverUrl,
                ShelfCode = b.Shelf != null ? b.Shelf.ShelfCode : (string?)null,
                ShelfType = b.Shelf != null ? b.Shelf.ShelfType : (string?)null
            });
        }

        // ── MEMBERS ──
        public IActionResult Members()
        {
            var students = _db.Students.ToList();
            var borrows = _db.BorrowRecords.Where(r => r.Status == "active" || r.Status == "overdue").ToList();
            var vm = students.Select(s => new MemberViewModel
            {
                Student = s,
                ActiveBorrows = borrows.Count(r => r.StudentId == s.Id),
                HasOverdue = borrows.Any(r => r.StudentId == s.Id && r.Status == "overdue")
            }).ToList();
            return View(vm);
        }

        // ── BORROWING LIST ──
        public IActionResult Borrowing()
        {
            UpdateOverdueStatuses();
            var active = _db.BorrowRecords
                .Where(r => r.Status == "active" || r.Status == "overdue")
                .OrderBy(r => r.DueDate).ToList();
            return View(active);
        }

        // ── ISSUE BOOK GET ──
        public IActionResult IssueBook()
        {
            ViewBag.Books = _db.Books.Where(b => b.AvailableCopies > 0).OrderBy(b => b.Title).ToList();
            ViewBag.Students = _db.Students.OrderBy(s => s.LastName).ToList();
            return View();
        }

        // ── ISSUE BOOK POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IssueBook(int bookId, int studentId, int dueDays = 14)
        {
            var book = _db.Books.Find(bookId);
            var student = _db.Students.Find(studentId);
            if (book == null || student == null) return NotFound();
            if (book.AvailableCopies <= 0)
            {
                TempData["Error"] = "No copies available.";
                return RedirectToAction("IssueBook");
            }
            var record = new BorrowRecord
            {
                BookId = bookId,
                StudentId = studentId,
                BookTitle = book.Title,
                StudentName = student.FullName,
                StudentEmail = student.Email,
                IssuedDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(dueDays),
                Status = "active"
            };
            book.AvailableCopies--;
            if (book.AvailableCopies == 0) book.Status = "borrowed";
            _db.BorrowRecords.Add(record);
            _db.SaveChanges();
            TempData["Success"] = $"'{book.Title}' issued to {student.FullName}.";
            return RedirectToAction("Borrowing");
        }

        // ── RETURN BOOK POST ──
        [HttpPost]
        public IActionResult ReturnBook(int recordId)
        {
            var record = _db.BorrowRecords.Find(recordId);
            if (record == null) return NotFound();
            var book = _db.Books.Find(record.BookId);
            record.Status = "returned";
            record.ReturnedDate = DateTime.Today;
            if (book != null)
            {
                book.AvailableCopies = Math.Min(book.Copies, book.AvailableCopies + 1);
                book.Status = book.AvailableCopies > 0 ? "available" : "borrowed";
            }
            _db.SaveChanges();
            return Json(new { success = true });
        }

        // ── OVERDUE ──
        public IActionResult Overdue()
        {
            UpdateOverdueStatuses();
            return View(_db.BorrowRecords.Where(r => r.Status == "overdue").OrderBy(r => r.DueDate).ToList());
        }

        // ── PROFILE ──
        public IActionResult Profile()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            return View(_db.AdminUsers.Find(adminId));
        }

        // ── CHANGE PASSWORD POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string username, string currentPassword, string newPassword, string confirmPassword)
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            var admin = _db.AdminUsers.Find(adminId);
            if (admin == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(username))
                admin.Username = username;
            if (!string.IsNullOrWhiteSpace(currentPassword) && !string.IsNullOrWhiteSpace(newPassword))
            {
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, admin.PasswordHash))
                { TempData["PwError"] = "Current password is incorrect."; return RedirectToAction("Profile"); }
                if (newPassword != confirmPassword)
                { TempData["PwError"] = "Passwords do not match."; return RedirectToAction("Profile"); }
                if (newPassword.Length < 8)
                { TempData["PwError"] = "Password must be at least 8 characters."; return RedirectToAction("Profile"); }
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }
            _db.SaveChanges();
            TempData["PwSuccess"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        public IActionResult BookingRequests()
        {
            var requests = _db.BookingRequests
                .OrderByDescending(r => r.RequestedAt)
                .ToList();
            var returnRequests = _db.ReturnRequests
                .OrderByDescending(r => r.RequestedAt)
                .ToList();
            ViewBag.ReturnRequests = returnRequests;
            return View(requests);
        }
        [HttpPost]
        public IActionResult ApproveReturn(int id)
        {
            var request = _db.ReturnRequests.Find(id);
            if (request == null) return NotFound();

            var record = _db.BorrowRecords.Find(request.BorrowRecordId);
            if (record != null)
            {
                record.Status = "returned";
                record.ReturnedDate = DateTime.Today;
                var book = _db.Books.Find(record.BookId);
                if (book != null)
                {
                    book.AvailableCopies = Math.Min(book.Copies, book.AvailableCopies + 1);
                    book.Status = book.AvailableCopies > 0 ? "available" : "borrowed";
                }
            }
            request.Status = "approved";
            _db.SaveChanges();
            return Json(new { success = true, message = "Return approved!" });
        }

        [HttpPost]
        public IActionResult RejectReturn(int id, string? adminNote)
        {
            var request = _db.ReturnRequests.Find(id);
            if (request == null) return NotFound();
            request.Status = "rejected";
            request.AdminNote = adminNote;
            _db.SaveChanges();
            return Json(new { success = true, message = "Return rejected." });
        }

        [HttpPost]
        public IActionResult ApproveBooking(int id, string? adminNote)
        {
            var request = _db.BookingRequests.Find(id);
            if (request == null) return NotFound();
            var book = _db.Books.Find(request.BookId);
            if (book == null || book.AvailableCopies <= 0)
                return Json(new { success = false, message = "No copies available." });
            // Check borrow limit
            var activeBorrowCount = _db.BorrowRecords.Count(r => r.StudentId == request.StudentId && (r.Status == "active" || r.Status == "overdue"));
            if (activeBorrowCount >= 3)
                return Json(new { success = false, message = "⚠️ Borrow limit reached! Users already have 3 active borrowing." });
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
            _ = Task.Run(() => _email.SendBookingStatusAsync(
            request.StudentEmail, request.StudentName, request.BookTitle, "approved", adminNote));
            return Json(new { success = true, message = "Booking approved!" });
        }

        [HttpPost]
        public IActionResult RejectBooking(int id, string? adminNote)
        {
            var request = _db.BookingRequests.Find(id);
            if (request == null) return NotFound();
            request.Status = "rejected";
            request.AdminNote = adminNote;
            _db.SaveChanges();
            _ = Task.Run(() => _email.SendBookingStatusAsync(
            request.StudentEmail, request.StudentName, request.BookTitle, "rejected", adminNote));
            return Json(new { success = true, message = "Booking rejected." });
        }
        // ── FORGOT PASSWORD GET ──
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        // ── FORGOT PASSWORD POST ──
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            var admin = _db.AdminUsers.FirstOrDefault(a => a.Email == email);
            if (admin == null)
            {
                ViewBag.Error = "No admin account found with that email.";
                return View();
            }
            var otp = new Random().Next(100000, 999999).ToString();
            admin.OtpCode = otp;
            admin.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            _db.SaveChanges();
            _ = Task.Run(() => _email.SendAdminOtpAsync(email, otp));
            HttpContext.Session.SetString("AdminResetEmail", email);
            return RedirectToAction("AdminResetVerifyOtp");
        }

        // ── VERIFY OTP GET ──
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AdminResetVerifyOtp()
        {
            if (HttpContext.Session.GetString("AdminResetEmail") == null)
                return RedirectToAction("ForgotPassword");
            var email = HttpContext.Session.GetString("AdminResetEmail")!;
            var atIndex = email.IndexOf('@');
            ViewBag.MaskedEmail = email[..2] + new string('*', atIndex - 2) + email[atIndex..];
            return View();
        }

        // ── VERIFY OTP POST ──
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult AdminResetVerifyOtp(string otp)
        {
            var email = HttpContext.Session.GetString("AdminResetEmail");
            if (email == null) return RedirectToAction("ForgotPassword");
            var admin = _db.AdminUsers.FirstOrDefault(a => a.Email == email);
            if (admin == null) return RedirectToAction("ForgotPassword");
            if (admin.OtpCode != otp || admin.OtpExpiry < DateTime.UtcNow)
            {
                var atIndex = email.IndexOf('@');
                ViewBag.MaskedEmail = email[..2] + new string('*', atIndex - 2) + email[atIndex..];
                ViewBag.Error = "Invalid or expired OTP. Please try again.";
                return View();
            }
            admin.OtpCode = null;
            admin.OtpExpiry = null;
            _db.SaveChanges();
            HttpContext.Session.SetString("AdminResetVerified", email);
            HttpContext.Session.Remove("AdminResetEmail");
            return RedirectToAction("AdminResetPassword");
        }

        // ── RESET PASSWORD GET ──
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AdminResetPassword()
        {
            if (HttpContext.Session.GetString("AdminResetVerified") == null)
                return RedirectToAction("ForgotPassword");
            return View();
        }

        // ── RESET PASSWORD POST ──
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult AdminResetPassword(string newPassword, string confirmPassword)
        {
            var email = HttpContext.Session.GetString("AdminResetVerified");
            if (email == null) return RedirectToAction("ForgotPassword");
            if (newPassword != confirmPassword)
            { ViewBag.Error = "Passwords do not match."; return View(); }
            if (newPassword.Length < 8)
            { ViewBag.Error = "Password must be at least 8 characters."; return View(); }
            var admin = _db.AdminUsers.FirstOrDefault(a => a.Email == email);
            if (admin == null) return RedirectToAction("ForgotPassword");
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.SaveChanges();
            HttpContext.Session.Remove("AdminResetVerified");
            TempData["LoginSuccess"] = "Password reset successfully! Please log in.";
            return RedirectToAction("AdminLogin", "Auth");
        }
        // ── IMPORT BOOKS CSV ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportBooks(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["CsvError"] = "Please select a CSV file.";
                return RedirectToAction("AddBook");
            }

            int added = 0, skipped = 0;

            using var reader = new System.IO.StreamReader(csvFile.OpenReadStream());
            var header = reader.ReadLine();
            if (header == null)
            {
                TempData["CsvError"] = "CSV file is empty.";
                return RedirectToAction("AddBook");
            }

            var cols = header.Split(',').Select(c => c.Trim().ToLower()).ToArray();
            int iTitle = Array.IndexOf(cols, "title");
            int iAuthor = Array.IndexOf(cols, "author");
            int iISBN = Array.IndexOf(cols, "isbn");
            int iYear = Array.IndexOf(cols, "year");
            int iCopies = Array.IndexOf(cols, "copies");
            int iShelf = Array.IndexOf(cols, "shelf");
            int iEmoji = Array.IndexOf(cols, "emoji");

            if (iTitle < 0 || iAuthor < 0 || iCopies < 0)
            {
                TempData["CsvError"] = "CSV must have Title, Author, and Copies columns.";
                return RedirectToAction("AddBook");
            }

            // Pre-load shelves
            var allShelves = _db.Shelves.ToList();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');

                string title = iTitle < parts.Length ? parts[iTitle].Trim() : "";
                string author = iAuthor < parts.Length ? parts[iAuthor].Trim() : "";
                string isbn = iISBN >= 0 && iISBN < parts.Length ? parts[iISBN].Trim() : "";

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(author))
                {
                    skipped++;
                    continue;
                }

                if (!string.IsNullOrEmpty(isbn) && _db.Books.Any(b => b.ISBN == isbn))
                {
                    skipped++;
                    continue;
                }

                int copies = 1;
                if (iCopies < parts.Length)
                    int.TryParse(parts[iCopies].Trim(), out copies);

                int year = 0;
                if (iYear >= 0 && iYear < parts.Length)
                    int.TryParse(parts[iYear].Trim(), out year);

                string emoji = "📖";
                if (iEmoji >= 0 && iEmoji < parts.Length && !string.IsNullOrEmpty(parts[iEmoji].Trim()))
                    emoji = parts[iEmoji].Trim();

                // Resolve shelf
                int? shelfId = null;
                if (iShelf >= 0 && iShelf < parts.Length)
                {
                    var shelfCode = parts[iShelf].Trim().ToUpper();
                    if (!string.IsNullOrEmpty(shelfCode))
                        shelfId = allShelves
                            .FirstOrDefault(s => s.ShelfCode.ToUpper() == shelfCode)?.Id;
                }

                // ✅ FIX: Generate Cover URL HERE (not async later)
                string? coverUrl = null;
                if (!string.IsNullOrEmpty(isbn))
                {
                    var cleanIsbn = isbn.Replace("-", "");
                    coverUrl = $"https://covers.openlibrary.org/b/isbn/{cleanIsbn}-L.jpg";
                }

                _db.Books.Add(new Book
                {
                    Title = title,
                    Author = author,
                    ISBN = isbn,
                    Year = year,
                    Copies = copies,
                    AvailableCopies = copies,
                    Emoji = emoji,
                    ShelfId = shelfId,
                    Status = "available",
                    CreatedAt = DateTime.UtcNow,
                    CoverUrl = coverUrl   // ✅ IMPORTANT LINE
                });

                added++;
            }

            if (added > 0)
            {
                _db.SaveChanges();
            }

            TempData["CsvSuccess"] =
                $"{added} book(s) imported successfully. " +
                (skipped > 0 ? $"{skipped} skipped (missing data or duplicate ISBN)." : "");

            return RedirectToAction("AddBook");
        }
        // ── HELPER ──
        private void UpdateOverdueStatuses()
        {
            var today = DateTime.Today;
            var toMark = _db.BorrowRecords.Where(r => r.Status == "active" && r.DueDate < today).ToList();
            foreach (var r in toMark) r.Status = "overdue";
            if (toMark.Any()) _db.SaveChanges();
        }
    }

    // ── ViewModels ──
    public class AdminDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveBorrows { get; set; }
        public int OverdueCount { get; set; }
        public int BookingRequestCount { get; set; }   
        public int ShelfCount { get; set; }             
        public List<Book> RecentBooks { get; set; } = new();
        public List<BorrowRecord> DueReturns { get; set; } = new();
        public List<GenreCount> GenreCounts { get; set; } = new();
    }

    public class GenreCount
    {
        public string Genre { get; set; } = "";
        public int Count { get; set; }
    }

    public class MemberViewModel
    {
        public Student Student { get; set; } = new();
        public int ActiveBorrows { get; set; }
        public bool HasOverdue { get; set; }
    }
}