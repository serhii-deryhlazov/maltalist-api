using Microsoft.EntityFrameworkCore;

namespace MaltalistApi.Models
{
    public class MaltalistDbContext : DbContext
    {
        public MaltalistDbContext(DbContextOptions<MaltalistDbContext> options)
            : base(options)
        {
        }

        public DbSet<Listing> Listings { get; set; }
    }

    public class Listing
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
    }
}
