using Librarium.Filters;
using Librarium.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Controllers
{
    [StudentAuthorize]
    public class NotificationsController : Controller
    {
        private readonly LibrariumDbContext _db;

        public NotificationsController(LibrariumDbContext db)
        {
            _db = db;
        }

        // ── NOTIFICATIONS PAGE ──
        public IActionResult Index()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null) return RedirectToAction("StudentLogin", "Auth");

            var notifications = _db.Notifications
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(notifications);
        }

        // ── MARK ALL AS READ ──
        [HttpPost]
        public IActionResult MarkAllRead()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null) return Unauthorized();

            var unread = _db.Notifications
                .Where(n => n.StudentId == studentId && !n.IsRead)
                .ToList();

            foreach (var n in unread)
                n.IsRead = true;

            _db.SaveChanges();
            return Json(new { success = true });
        }

        // ── UNREAD COUNT (for red dot) ──
        public IActionResult UnreadCount()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null) return Json(new { count = 0 });

            var count = _db.Notifications
                .Count(n => n.StudentId == studentId && !n.IsRead);

            return Json(new { count });
        }
    }
}