using Librarium.Models;

    namespace Librarium.Models
    {
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
    }

