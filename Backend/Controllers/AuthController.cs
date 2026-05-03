using Librarium.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Controllers
{
    public class AuthController : Controller
    {
        private readonly LibrariumDbContext _db;
        private readonly Librarium.Services.EmailService _email;
        public AuthController(LibrariumDbContext db, Librarium.Services.EmailService email)
        {
            _db = db;
            _email = email;
        }

        // GET /Auth/AdminLogin
        public IActionResult AdminLogin()
        {
            if (HttpContext.Session.GetInt32("AdminId") != null)
                return RedirectToAction("Index", "Admin");
            return View();
        }

        // POST /Auth/AdminLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogin(string username, string password)
        {
            var admin = _db.AdminUsers.FirstOrDefault(a => a.Username == username);
            if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                HttpContext.Session.SetInt32("AdminId", admin.Id);
                HttpContext.Session.SetString("AdminName", admin.DisplayName);
                HttpContext.Session.SetString("AdminUsername", admin.Username);
                return RedirectToAction("Index", "Admin");
            }
            ViewBag.Error = "Incorrect username or password.";
            return View();
        }

        // GET /Auth/StudentLogin
        public IActionResult StudentLogin()
        {
            if (HttpContext.Session.GetInt32("StudentId") != null)
                return RedirectToAction("Index", "Student");
            return View();
        }

        // POST /Auth/StudentLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StudentLogin(string email, string password)
        {
            var student = _db.Students.FirstOrDefault(s => s.Email == email);
            if (student != null && BCrypt.Net.BCrypt.Verify(password, student.PasswordHash) && student.IsVerified)
            {
                HttpContext.Session.SetInt32("StudentId", student.Id);
                HttpContext.Session.SetString("StudentName", student.FullName);
                HttpContext.Session.SetString("StudentEmail", student.Email);
                return RedirectToAction("Index", "Student");
            }
            ViewBag.Error = "Incorrect email or password.";
            return View();
        }

        // GET /Auth/Register
        public IActionResult Register() => View();

        // POST /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string firstName, string lastName, string email, string password)
        {
            if (_db.Students.Any(s => s.Email == email))
            {
                ViewBag.Error = "An account with this email already exists.";
                return View();
            }

            // ✅ Validate email is real
            var emailIsReal = await _email.IsEmailRealAsync(email);
            if (!emailIsReal)
            {
                ViewBag.Error = "Invalid or non-existent email address. Please use a real email.";
                ViewBag.ErrorField = "email";
                return View();
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var student = new Student
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                OtpCode = otp,
                OtpExpiry = DateTime.UtcNow.AddMinutes(10)
            };
                _db.Students.Add(student);
            _db.SaveChanges();
            _ = Task.Run(() => _email.SendOtpAsync(email, otp));
            HttpContext.Session.SetInt32("PendingStudentId", student.Id);
            return RedirectToAction("VerifyOtp");
        }

        // GET /Auth/VerifyOtp
        // GET /Auth/VerifyOtp
        public IActionResult VerifyOtp()
        {
            if (HttpContext.Session.GetInt32("PendingStudentId") == null)
                return RedirectToAction("Register");
            var studentId = HttpContext.Session.GetInt32("PendingStudentId");
            var student = _db.Students.Find(studentId);
            if (student == null) return RedirectToAction("Register");
            // Mask email: coderm220@gmail.com → co*****@gmail.com
            var email = student.Email;
            var atIndex = email.IndexOf('@');
            var masked = email[..2] + new string('*', atIndex - 2) + email[atIndex..];
            ViewBag.MaskedEmail = masked;
            ViewBag.OtpSentAt = student.OtpExpiry?.AddMinutes(-10);
            return View();
        }

        // POST /Auth/ResendOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResendOtp()
        {
            var studentId = HttpContext.Session.GetInt32("PendingStudentId");
            if (studentId == null) return RedirectToAction("Register");
            var student = _db.Students.Find(studentId);
            if (student == null) return RedirectToAction("Register");

            // Enforce 1 minute cooldown
            if (student.OtpExpiry.HasValue && student.OtpExpiry.Value.AddMinutes(-9) > DateTime.UtcNow)
            {
                TempData["ResendError"] = "Please wait 1 minute before requesting a new OTP.";
                return RedirectToAction("VerifyOtp");
            }

            var otp = new Random().Next(100000, 999999).ToString();
            student.OtpCode = otp;
            student.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            _db.SaveChanges();
            _ = Task.Run(() => _email.SendOtpAsync(student.Email, otp));
            TempData["ResendSuccess"] = "A new OTP has been sent to your email.";
            return RedirectToAction("VerifyOtp");
        }

        // POST /Auth/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(string otp)
        {
            var studentId = HttpContext.Session.GetInt32("PendingStudentId");
            if (studentId == null) return RedirectToAction("Register");

            var student = _db.Students.Find(studentId);
            if (student == null) return RedirectToAction("Register");

            if (student.OtpCode != otp || student.OtpExpiry < DateTime.UtcNow)
            {
                ViewBag.Error = "Invalid or expired OTP. Please try again.";
                return View();
            }

            student.OtpCode = null;
            student.OtpExpiry = null;
            student.IsVerified = true;
            _db.SaveChanges();

            HttpContext.Session.Remove("PendingStudentId");
            HttpContext.Session.SetInt32("StudentId", student.Id);
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("StudentEmail", student.Email);
            return RedirectToAction("Index", "Student");
        }
        // GET /Auth/ForgotPassword
        public IActionResult ForgotPassword() => View();

        // POST /Auth/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            var student = _db.Students.FirstOrDefault(s => s.Email == email);
            if (student == null || !student.IsVerified)
            {
                ViewBag.Error = "No verified account found with that email.";
                return View();
            }
            var otp = new Random().Next(100000, 999999).ToString();
            student.OtpCode = otp;
            student.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            _db.SaveChanges();
            _ = Task.Run(() => _email.SendOtpAsync(email, otp));
            HttpContext.Session.SetString("ResetEmail", email);
            return RedirectToAction("ResetVerifyOtp");
        }

        // GET /Auth/ResetVerifyOtp
        public IActionResult ResetVerifyOtp()
        {
            if (HttpContext.Session.GetString("ResetEmail") == null)
                return RedirectToAction("ForgotPassword");
            var email = HttpContext.Session.GetString("ResetEmail")!;
            var atIndex = email.IndexOf('@');
            var masked = email[..2] + new string('*', atIndex - 2) + email[atIndex..];
            ViewBag.MaskedEmail = masked;
            return View();
        }

        // POST /Auth/ResetVerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetVerifyOtp(string otp)
        {
            var email = HttpContext.Session.GetString("ResetEmail");
            if (email == null) return RedirectToAction("ForgotPassword");
            var student = _db.Students.FirstOrDefault(s => s.Email == email);
            if (student == null) return RedirectToAction("ForgotPassword");
            if (student.OtpCode != otp || student.OtpExpiry < DateTime.UtcNow)
            {
                ViewBag.Error = "Invalid or expired OTP. Please try again.";
                var atIndex = email.IndexOf('@');
                ViewBag.MaskedEmail = email[..2] + new string('*', atIndex - 2) + email[atIndex..];
                return View();
            }
            student.OtpCode = null;
            student.OtpExpiry = null;
            _db.SaveChanges();
            HttpContext.Session.SetString("ResetVerified", email);
            HttpContext.Session.Remove("ResetEmail");
            return RedirectToAction("ResetPassword");
        }

        // GET /Auth/ResetPassword
        public IActionResult ResetPassword()
        {
            if (HttpContext.Session.GetString("ResetVerified") == null)
                return RedirectToAction("ForgotPassword");
            return View();
        }

        // POST /Auth/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string newPassword, string confirmPassword)
        {
            var email = HttpContext.Session.GetString("ResetVerified");
            if (email == null) return RedirectToAction("ForgotPassword");
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }
            if (newPassword.Length < 8)
            {
                ViewBag.Error = "Password must be at least 8 characters.";
                return View();
            }
            var student = _db.Students.FirstOrDefault(s => s.Email == email);
            if (student == null) return RedirectToAction("ForgotPassword");
            student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.SaveChanges();
            HttpContext.Session.Remove("ResetVerified");
            TempData["LoginSuccess"] = "Password reset successfully! Please log in.";
            return RedirectToAction("StudentLogin");
        }
        // GET /Auth/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("AdminLogin");
        }

        // GET /Auth/StudentLogout
        public IActionResult StudentLogout()
        {
            HttpContext.Session.Clear();
            TempData["ShowLogin"] = true;
            return RedirectToAction("StudentLogin");
        }
    }
}