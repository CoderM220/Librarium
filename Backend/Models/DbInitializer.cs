using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Librarium.Models
{
    public static class DbInitializer
    {
        public static void Initialize(LibrariumDbContext context)
        {
            try
            {
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB INIT ERROR: " + ex.Message);
            }
            // ── SEED ADMIN ──
            if (!context.AdminUsers.Any())
            {
                context.AdminUsers.Add(new AdminUser
                {
                    Username = "librarium_admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("YourStrongPassword@2026"),
                    DisplayName = "Admin",
                    Email = "adminlibrarium@gmail.com"
                });
                context.SaveChanges();
            }

        }
    }
}
