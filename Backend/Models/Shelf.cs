using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarium.Models
{
    [Table("Shelves")]
    public class Shelf
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string ShelfCode { get; set; } = "";

        [StringLength(10)]
        public string ShelfType { get; set; } = "almirah";

        public int GridCol { get; set; }
        public int GridRow { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}