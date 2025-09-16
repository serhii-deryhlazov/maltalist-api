using System.ComponentModel.DataAnnotations;

namespace MaltalistApi.Models
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ListingId { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; }

        public Listing? Listing { get; set; }
    }
}
