/*using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
namespace Librarium.Models
{
    public class LibrariumDbContext : DbContext
    {
        public LibrariumDbContext(DbContextOptions<LibrariumDbContext> options)
            : base(options)
        {
        }
        public DbSet<Book> Books { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<BorrowRecord> BorrowRecords { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<BookingRequest> BookingRequests { get; set; }
    }
}
*/
using Microsoft.EntityFrameworkCore;

namespace Librarium.Models
{
    public class LibrariumDbContext : DbContext
    {
        public LibrariumDbContext(DbContextOptions<LibrariumDbContext> options)
            : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<BorrowRecord> BorrowRecords { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<BookingRequest> BookingRequests { get; set; }
        public DbSet<Shelf> Shelves { get; set; }

        public DbSet<ReturnRequest> ReturnRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<BorrowRecord>()
    .HasOne(r => r.Book)
    .WithMany()
    .HasForeignKey(r => r.BookId)
    .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Shelf)
                .WithMany(s => s.Books)
                .HasForeignKey(b => b.ShelfId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Shelf>().HasData(
                new Shelf { Id = 1, ShelfCode = "A1", ShelfType = "almirah", GridCol = 3, GridRow = 1 },
                new Shelf { Id = 2, ShelfCode = "A2", ShelfType = "almirah", GridCol = 4, GridRow = 1 },
                new Shelf { Id = 3, ShelfCode = "A3", ShelfType = "almirah", GridCol = 5, GridRow = 1 },
                new Shelf { Id = 4, ShelfCode = "A4", ShelfType = "almirah", GridCol = 6, GridRow = 1 },
                new Shelf { Id = 5, ShelfCode = "A5", ShelfType = "almirah", GridCol = 7, GridRow = 1 },
                new Shelf { Id = 6, ShelfCode = "A6", ShelfType = "almirah", GridCol = 8, GridRow = 1 },
                new Shelf { Id = 7, ShelfCode = "A7", ShelfType = "almirah", GridCol = 9, GridRow = 1 },
                new Shelf { Id = 8, ShelfCode = "A8", ShelfType = "almirah", GridCol = 10, GridRow = 1 },
                new Shelf { Id = 9, ShelfCode = "A9", ShelfType = "almirah", GridCol = 3, GridRow = 2 },
                new Shelf { Id = 10, ShelfCode = "A10", ShelfType = "almirah", GridCol = 4, GridRow = 2 },
                new Shelf { Id = 11, ShelfCode = "A11", ShelfType = "almirah", GridCol = 5, GridRow = 2 },
                new Shelf { Id = 12, ShelfCode = "A12", ShelfType = "almirah", GridCol = 6, GridRow = 2 },
                new Shelf { Id = 13, ShelfCode = "A13", ShelfType = "almirah", GridCol = 7, GridRow = 2 },
                new Shelf { Id = 14, ShelfCode = "A14", ShelfType = "almirah", GridCol = 8, GridRow = 2 },
                new Shelf { Id = 15, ShelfCode = "A15", ShelfType = "almirah", GridCol = 9, GridRow = 2 },
                new Shelf { Id = 16, ShelfCode = "A16", ShelfType = "almirah", GridCol = 10, GridRow = 2 },
                new Shelf { Id = 17, ShelfCode = "A17", ShelfType = "almirah", GridCol = 3, GridRow = 4 },
                new Shelf { Id = 18, ShelfCode = "A18", ShelfType = "almirah", GridCol = 4, GridRow = 4 },
                new Shelf { Id = 19, ShelfCode = "A19", ShelfType = "almirah", GridCol = 5, GridRow = 4 },
                new Shelf { Id = 20, ShelfCode = "A20", ShelfType = "almirah", GridCol = 6, GridRow = 4 },
                new Shelf { Id = 21, ShelfCode = "A21", ShelfType = "almirah", GridCol = 7, GridRow = 4 },
                new Shelf { Id = 22, ShelfCode = "A22", ShelfType = "almirah", GridCol = 8, GridRow = 4 },
                new Shelf { Id = 23, ShelfCode = "A23", ShelfType = "almirah", GridCol = 9, GridRow = 4 },
                new Shelf { Id = 24, ShelfCode = "A24", ShelfType = "almirah", GridCol = 3, GridRow = 5 },
                new Shelf { Id = 25, ShelfCode = "A25", ShelfType = "almirah", GridCol = 4, GridRow = 5 },
                new Shelf { Id = 26, ShelfCode = "A26", ShelfType = "almirah", GridCol = 5, GridRow = 5 },
                new Shelf { Id = 27, ShelfCode = "A27", ShelfType = "almirah", GridCol = 6, GridRow = 5 },
                new Shelf { Id = 28, ShelfCode = "A28", ShelfType = "almirah", GridCol = 7, GridRow = 5 },
                new Shelf { Id = 29, ShelfCode = "A29", ShelfType = "almirah", GridCol = 8, GridRow = 5 },
                new Shelf { Id = 30, ShelfCode = "A30", ShelfType = "almirah", GridCol = 9, GridRow = 5 },
                new Shelf { Id = 31, ShelfCode = "A31", ShelfType = "almirah", GridCol = 3, GridRow = 10 },
                new Shelf { Id = 32, ShelfCode = "A32", ShelfType = "almirah", GridCol = 4, GridRow = 10 },
                new Shelf { Id = 33, ShelfCode = "A33", ShelfType = "almirah", GridCol = 5, GridRow = 10 },
                new Shelf { Id = 34, ShelfCode = "A34", ShelfType = "almirah", GridCol = 6, GridRow = 10 },
                new Shelf { Id = 35, ShelfCode = "A35", ShelfType = "almirah", GridCol = 7, GridRow = 10 },
                new Shelf { Id = 36, ShelfCode = "A36", ShelfType = "almirah", GridCol = 8, GridRow = 10 },
                new Shelf { Id = 37, ShelfCode = "A37", ShelfType = "almirah", GridCol = 1, GridRow = 4 },
                new Shelf { Id = 38, ShelfCode = "A38", ShelfType = "almirah", GridCol = 1, GridRow = 5 },
                new Shelf { Id = 39, ShelfCode = "A39", ShelfType = "almirah", GridCol = 1, GridRow = 6 },
                new Shelf { Id = 40, ShelfCode = "A40", ShelfType = "almirah", GridCol = 1, GridRow = 7 },
                new Shelf { Id = 41, ShelfCode = "A41", ShelfType = "almirah", GridCol = 1, GridRow = 8 },
                new Shelf { Id = 42, ShelfCode = "R42", ShelfType = "rack", GridCol = 3, GridRow = 7 },
                new Shelf { Id = 43, ShelfCode = "R43", ShelfType = "rack", GridCol = 4, GridRow = 7 },
                new Shelf { Id = 44, ShelfCode = "R44", ShelfType = "rack", GridCol = 5, GridRow = 7 },
                new Shelf { Id = 45, ShelfCode = "R45", ShelfType = "rack", GridCol = 6, GridRow = 7 },
                new Shelf { Id = 46, ShelfCode = "R46", ShelfType = "rack", GridCol = 7, GridRow = 7 },
                new Shelf { Id = 47, ShelfCode = "R47", ShelfType = "rack", GridCol = 3, GridRow = 8 },
                new Shelf { Id = 48, ShelfCode = "R48", ShelfType = "rack", GridCol = 4, GridRow = 8 },
                new Shelf { Id = 49, ShelfCode = "R49", ShelfType = "rack", GridCol = 5, GridRow = 8 },
                new Shelf { Id = 50, ShelfCode = "R50", ShelfType = "rack", GridCol = 6, GridRow = 8 },
                new Shelf { Id = 51, ShelfCode = "R51", ShelfType = "rack", GridCol = 7, GridRow = 8 }
            );
        }
    }
}