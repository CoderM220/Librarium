using Librarium.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // ================= STUDENT LOGIN =================

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

            if (student != null &&
                BCrypt.Net.BCrypt.Verify(password, student.PasswordHash) &&
                student.IsVerified)
            {
                HttpContext.Session.SetInt32("StudentId", student.Id);
                HttpContext.Session.SetString("StudentName", student.FullName);
                HttpContext.Session.SetString("StudentEmail", student.Email);

                return RedirectToAction("Index", "Student");
            }

            ViewBag.Error = "Incorrect email or password.";
            return View();
        }

        // ================= REGISTER =================

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_db.Students.Any(s => s.Email == model.Email))
            {
                ViewBag.Error = "An account with this email already exists.";
                ViewBag.ErrorField = "email";
                return View();
            }
           
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

        // ================= VERIFY OTP =================

        public IActionResult VerifyOtp(int id)
        {
           

            var student = _db.Students.Find(id);

            if (student == null)
            {
                Console.WriteLine("STUDENT NOT FOUND");
                return RedirectToAction("Register");
            }

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

           
            if (student.OtpAttempts >= 5)
            {
                ViewBag.Error = "Too many attempts. Try again later.";
                return View();
            }

           
            if (string.IsNullOrWhiteSpace(student.OtpCode) ||
                student.OtpCode != otp ||
                student.OtpExpiry == null ||
                student.OtpExpiry < DateTime.UtcNow)
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

        public IActionResult StudentLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("StudentLogin");
        }
    }
}
