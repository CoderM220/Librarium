using Librarium.Models;
using Microsoft.AspNetCore.Mvc;
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

            if (student == null || !BCrypt.Net.BCrypt.Verify(password, student.PasswordHash))
            {
                ViewBag.Error = "Incorrect email or password.";
                return View();
            }

            HttpContext.Session.SetInt32("StudentId", student.Id);
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("StudentEmail", student.Email);

            return RedirectToAction("Index", "Student");
        }

        // ─── Register ─────────────────────────────────────────────────────────────

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

            // Check against already-verified students only
            if (_db.Students.Any(s => s.Email == model.Email))
            {
                ViewBag.Error = "An account with this email already exists.";
                ViewBag.ErrorField = "email";
                return View(model);
            }

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // Store registration data in session — do NOT write to DB yet
            var pending = new PendingRegistration
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                // Hash now so plain-text password never sits in session
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                OtpCode = otp,
                OtpExpiry = DateTime.UtcNow.AddMinutes(10),
                OtpAttempts = 0
            };

            HttpContext.Session.SetString("PendingRegistration", JsonConvert.SerializeObject(pending));

            try
            {
                await _email.SendOtpAsync(model.Email, otp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
                HttpContext.Session.Remove("PendingRegistration");
                ViewBag.Error = "Failed to send OTP. Please try again.";
                return View(model);
            }

            return RedirectToAction("VerifyOtp");
        }

        // ─── OTP Verification ─────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var json = HttpContext.Session.GetString("PendingRegistration");
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("Register");

            var pending = JsonConvert.DeserializeObject<PendingRegistration>(json)!;

            SetMaskedEmailViewBag(pending.Email, pending.OtpExpiry);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(string otp)
        {
            var json = HttpContext.Session.GetString("PendingRegistration");
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("Register");

            var pending = JsonConvert.DeserializeObject<PendingRegistration>(json)!;

            // Too many attempts — wipe session, force re-register
            if (pending.OtpAttempts >= 5)
            {
                HttpContext.Session.Remove("PendingRegistration");
                TempData["Error"] = "Too many failed attempts. Please register again.";
                return RedirectToAction("Register");
            }

            bool isOtpInvalid =
                string.IsNullOrWhiteSpace(pending.OtpCode) ||
                pending.OtpCode != otp ||
                pending.OtpExpiry < DateTime.UtcNow;

            if (isOtpInvalid)
            {
                pending.OtpAttempts++;
                HttpContext.Session.SetString("PendingRegistration", JsonConvert.SerializeObject(pending));

                ViewBag.Error = "Invalid or expired OTP.";
                SetMaskedEmailViewBag(pending.Email, pending.OtpExpiry);
                return View();
            }

            // ✅ OTP verified — NOW create the student in the database
            // Race-condition guard: check email uniqueness one more time
            if (_db.Students.Any(s => s.Email == pending.Email))
            {
                HttpContext.Session.Remove("PendingRegistration");
                TempData["Error"] = "An account with this email already exists.";
                return RedirectToAction("Register");
            }

            var student = new Student
            {
                FirstName = pending.FirstName,
                LastName = pending.LastName,
                Email = pending.Email,
                PasswordHash = pending.PasswordHash,
                IsVerified = true   // verified at creation, no OTP fields needed on the model
            };

            _db.Students.Add(student);
            _db.SaveChanges();

            HttpContext.Session.Remove("PendingRegistration");

            // Log the student in immediately after registration
            HttpContext.Session.SetInt32("StudentId", student.Id);
            HttpContext.Session.SetString("StudentName", student.FullName);
            HttpContext.Session.SetString("StudentEmail", student.Email);

            return RedirectToAction("Index", "Student");
        }

        // ─── Resend OTP ───────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp()
        {
            var json = HttpContext.Session.GetString("PendingRegistration");
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("Register");

            var pending = JsonConvert.DeserializeObject<PendingRegistration>(json)!;

            // Fresh OTP + reset attempts
            pending.OtpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            pending.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            pending.OtpAttempts = 0;

            HttpContext.Session.SetString("PendingRegistration", JsonConvert.SerializeObject(pending));

            try
            {
                await _email.SendOtpAsync(pending.Email, pending.OtpCode);
                TempData["Success"] = "A new OTP has been sent.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
                TempData["Error"] = "Failed to resend OTP. Please try again.";
            }

            return RedirectToAction("VerifyOtp");
        }

        // ─── Forgot Password ─────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            email = email.Trim();

            var student = _db.Students
                .FirstOrDefault(s => s.Email.ToLower() == email.ToLower());

            if (student == null)
            {
                ViewBag.Error = "Email not found.";
                return View();
            }

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            student.OtpCode = otp;
            student.OtpExpiry = DateTime.UtcNow.AddMinutes(5);
            student.OtpAttempts = 0;
            _db.SaveChanges();

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

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void SetMaskedEmailViewBag(string email, DateTime otpExpiry)
        {
            var atIndex = email.IndexOf('@');
            ViewBag.MaskedEmail = email[..2] + new string('*', atIndex - 2) + email[atIndex..];
            ViewBag.OtpSentAt = otpExpiry.AddMinutes(-10);
        }
    }

    // Temporary model stored in session during registration — never written to DB
    // until OTP is confirmed. Move to its own file if preferred.
    public class PendingRegistration
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string OtpCode { get; set; } = "";
        public DateTime OtpExpiry { get; set; }
        public int OtpAttempts { get; set; }
    }
}
