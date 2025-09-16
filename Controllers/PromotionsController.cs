using Microsoft.AspNetCore.Mvc;
using MaltalistApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MaltalistApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionsController : ControllerBase
    {
        private readonly MaltalistDbContext _context;

        public PromotionsController(MaltalistDbContext context)
        {
            _context = context;
        }

        // GET: api/Promotions/promoted/{category}
        [HttpGet("promoted/{category}")]
        public async Task<IActionResult> GetPromotedListings(string category)
        {
            var now = DateTime.UtcNow;
            var promotedListings = await _context.Promotions
                .Where(p => p.Category == category && p.ExpirationDate > now)
                .Include(p => p.Listing)
                .Select(p => p.Listing)
                .ToListAsync();

            return Ok(promotedListings);
        }

        // POST: api/Promotions
        [HttpPost]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequest request)
        {
            // Assume Stripe payment is handled on frontend, here we just create the record
            // In a real app, verify payment intent

            var promotion = new Promotion
            {
                ListingId = request.ListingId,
                ExpirationDate = request.ExpirationDate,
                Category = request.Category
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPromotedListings), new { category = request.Category }, promotion);
        }
    }

    public class CreatePromotionRequest
    {
        public int ListingId { get; set; }
        public DateTime ExpirationDate { get; set; }
        public required string Category { get; set; }
    }
}
