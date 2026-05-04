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

            // ── SEED BOOKS ──
            if (!context.Books.Any())
            {
                var books = new Book[]
                {
                    new Book { Title="The Great Gatsby", Author="F. Scott Fitzgerald", Genre="Fiction", ISBN="978-0-7432-7356-5", Year=1925, Copies=3, AvailableCopies=2, Status="borrowed", Emoji="📗" },
                    new Book { Title="To Kill a Mockingbird", Author="Harper Lee", Genre="Fiction", ISBN="978-0-06-112008-4", Year=1960, Copies=4, AvailableCopies=4, Status="available", Emoji="📘" },
                    new Book { Title="A Brief History of Time", Author="Stephen Hawking", Genre="Science", ISBN="978-0-553-38016-3", Year=1988, Copies=2, AvailableCopies=1, Status="borrowed", Emoji="🔭" },
                    new Book { Title="Sapiens", Author="Yuval Noah Harari", Genre="History", ISBN="978-0-06-231609-7", Year=2011, Copies=5, AvailableCopies=5, Status="available", Emoji="🏛️" },
                    new Book { Title="1984", Author="George Orwell", Genre="Sci-Fi", ISBN="978-0-452-28423-4", Year=1949, Copies=3, AvailableCopies=0, Status="borrowed", Emoji="📕" },
                    new Book { Title="The Alchemist", Author="Paulo Coelho", Genre="Fiction", ISBN="978-0-06-231500-7", Year=1988, Copies=4, AvailableCopies=4, Status="available", Emoji="✨" }
                };
                context.Books.AddRange(books);
                context.SaveChanges();
            }
        }
    }
}
