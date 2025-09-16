using Microsoft.EntityFrameworkCore;

namespace MaltalistApi.Models;

public class MaltalistDbContext : DbContext
{
    public MaltalistDbContext(DbContextOptions<MaltalistDbContext> options)
        : base(options)
    {
    }

    public DbSet<Listing> Listings { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
}
