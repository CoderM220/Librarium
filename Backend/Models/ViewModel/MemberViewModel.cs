using Librarium.Models;

    namespace Librarium.Models
    {
        public class MemberViewModel
        {
            public Student Student { get; set; } = new();
            public int ActiveBorrows { get; set; }
            public bool HasOverdue { get; set; }
        }
    }
