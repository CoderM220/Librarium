using Librarium.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Cryptography;

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

        // ─── Admin ────────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult AdminLogin()
        {
            if (HttpContext.Session.GetInt32("AdminId") != null)
                return RedirectToAction("Index", "Admin");

            return View();
        }

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

        // ─── Student Login ────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult StudentLogin()
        {
            if (HttpContext.Session.GetInt32("StudentId") != null)
                return RedirectToAction("Index", "Student");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StudentLogin(string email, string password)
        {
            var student = _db.Students.FirstOrDefault(s => s.Email == email);

            // FIX: Use a single error message for both "not found" and "wrong password"
            //      to avoid leaking whether an email is registered.
            if (student == null || !BCrypt.Net.BCrypt.Verify(password, student.PasswordHash))
            {
                ViewBag.Error = "Incorrect email or password.";
                return View();
            }

            if (!student.IsVerified)
            {
                // Resend a fresh OTP so the student isn't stuck
                var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                student.OtpCode = otp;
                student.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
                student.OtpAttempts = 0;
                _db.SaveChanges();

                // Fire-and-forget; failure is non-fatal here — student can request resend
                _ = _email.SendOtpAsync(student.Email, otp);

                return RedirectToAction("VerifyOtp", new { id = student.Id });
            }

            HttpContext.Session.SetInt32("StudentId", student.Id);
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("StudentEmail", student.Email);

            return RedirectToAction("Index", "Student");
        }

        // ─── Register ─────────────────────────────────────────────────────────────

        // FIX: GET must NOT accept or process the model — just show the empty form.
        //      Previously the GET overload was silently processing model data and
        //      conflicted with the POST overload's signature.
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_db.Students.Any(s => s.Email == model.Email))
            {
                ViewBag.Error = "An account with this email already exists.";
                ViewBag.ErrorField = "email";
                return View(model);
            }

            // FIX: Use cryptographically secure RNG (was new Random() in parts of the original)
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            var student = new Student
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                OtpCode = otp,
                OtpExpiry = DateTime.UtcNow.AddMinutes(10)
            };

            _db.Students.Add(student);
            _db.SaveChanges();

            try
            {
                await _email.SendOtpAsync(model.Email, otp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);

                _db.Students.Remove(student);
                _db.SaveChanges();

                ViewBag.Error = "Failed to send OTP. Please try again.";
                return View(model);
            }

            return RedirectToAction("VerifyOtp", new { id = student.Id });
        }

        // ─── OTP Verification ─────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult VerifyOtp(int id)
        {
            var student = _db.Students.Find(id);

            if (student == null)
                return RedirectToAction("Register");

            // FIX: Don't let an already-verified student re-visit this page
            if (student.IsVerified)
                return RedirectToAction("StudentLogin");

            var email = student.Email;
            var atIndex = email.IndexOf('@');
            var masked = email[..2] + new string('*', atIndex - 2) + email[atIndex..];

            ViewBag.MaskedEmail = masked;
            ViewBag.OtpSentAt = student.OtpExpiry?.AddMinutes(-10);
            ViewBag.StudentId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(int id, string otp)
        {
            var student = _db.Students.Find(id);
            if (student == null)
                return RedirectToAction("Register");

            ViewBag.StudentId = id;

            if (student.IsVerified)
                return RedirectToAction("StudentLogin");

            if (student.OtpAttempts >= 5)
            {
                ViewBag.Error = "Too many attempts. Try again later.";
                return View();
            }

            bool isOtpInvalid =
                string.IsNullOrWhiteSpace(student.OtpCode) ||
                student.OtpCode != otp ||
                student.OtpExpiry == null ||
                student.OtpExpiry < DateTime.UtcNow;

            if (isOtpInvalid)
            {
                student.OtpAttempts++;
                _db.SaveChanges();

                ViewBag.Error = "Invalid or expired OTP.";
                return View();
            }

            student.OtpAttempts = 0;
            student.OtpCode = null;
            student.OtpExpiry = null;
            student.IsVerified = true;
            _db.SaveChanges();

            HttpContext.Session.SetInt32("StudentId", student.Id);
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("StudentEmail", student.Email);

            return RedirectToAction("Index", "Student");
        }

        // ─── Forgot Password ─────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)   // FIX: made async
        {
            email = email.Trim();

            var student = _db.Students
                .FirstOrDefault(s => s.Email.ToLower() == email.ToLower());

            if (student == null)
            {
                ViewBag.Error = "Email not found.";
                return View();
            }

            // FIX 1: Use cryptographically secure RNG (was new Random())
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            student.OtpCode = otp;
            student.OtpExpiry = DateTime.UtcNow.AddMinutes(5);
            student.OtpAttempts = 0;    // reset attempts on fresh OTP
            _db.SaveChanges();

            // FIX 2: Actually send the OTP email (was missing in original)
            try
            {
                await _email.SendOtpAsync(student.Email, otp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
                ViewBag.Error = "Failed to send OTP. Please try again.";
                return View();
            }

            ViewBag.Success = "OTP sent successfully.";
            return View();
        }

        // ─── Logout ───────────────────────────────────────────────────────────────

        public IActionResult StudentLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("StudentLogin");
        }
    }
}
