using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MaltalistApi.Models;
using MaltalistApi.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MaltalistApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionsController : ControllerBase
    {
        private readonly MaltalistDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PromotionsController> _logger;

        public PromotionsController(
            MaltalistDbContext context, 
            IPaymentService paymentService,
            IConfiguration configuration,
            ILogger<PromotionsController> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _configuration = configuration;
            _logger = logger;
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

            // Populate Picture1 with the first image URL
            foreach (var l in promotedListings)
            {
                if (l != null)
                {
                    var picDir = $"/images/{l.Id}";
                    if (Directory.Exists(picDir))
                    {
                        var files = Directory.GetFiles(picDir)
                            .Select(Path.GetFileName)
                            .OrderBy(f => f)
                            .ToList();
                        if (files.Any())
                        {
                            l.Picture1 = $"/assets/img/listings/{l.Id}/{files.First()}";
                        }
                    }
                }
            }

            return Ok(promotedListings);
        }

        // POST: api/Promotions/create-payment-intent
        [HttpPost("create-payment-intent")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            try
            {
                // Validate input
                if (request == null || request.Amount <= 0)
                {
                    return BadRequest(new { Message = "Invalid payment amount" });
                }

                // Verify listing exists
                var listing = await _context.Listings.FindAsync(request.ListingId);
                if (listing == null)
                {
                    return NotFound(new { Message = "Listing not found" });
                }

                // Verify ownership
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || listing.UserId != currentUserId)
                {
                    return Forbid();
                }

                // Create payment intent
                var clientSecret = await _paymentService.CreatePaymentIntentAsync(request.Amount);
                
                return Ok(new { clientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent for listing {ListingId}", request.ListingId);
                return StatusCode(500, new { Message = "Failed to create payment intent" });
            }
        }

        // POST: api/Promotions
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequest request)
        {
            // Validate input
            if (request == null || request.ListingId <= 0)
            {
                return BadRequest(new { Message = "Invalid promotion request" });
            }

            // Verify listing exists
            var listing = await _context.Listings.FindAsync(request.ListingId);
            if (listing == null)
            {
                return NotFound(new { Message = "Listing not found" });
            }

            // Verify ownership: Only allow the listing owner to promote their listing
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || listing.UserId != currentUserId)
            {
                return Forbid();
            }

            // Verify payment intent ID is provided
            if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                return BadRequest(new { Message = "Payment verification required. PaymentIntentId must be provided." });
            }

            // Verify payment with payment service
            var promotionPrice = _configuration.GetValue<decimal>("Stripe:PromotionPrice", 9.99m);
            var paymentVerified = await _paymentService.VerifyPaymentIntentAsync(
                request.PaymentIntentId, 
                promotionPrice
            );
            
            if (!paymentVerified)
            {
                return BadRequest(new { Message = "Payment verification failed. Please ensure payment was successful." });
            }

            // Check if listing is already promoted and not expired
            var existingPromotion = await _context.Promotions
                .Where(p => p.ListingId == request.ListingId && p.ExpirationDate > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (existingPromotion != null)
            {
                return BadRequest(new { Message = "Listing is already promoted until " + existingPromotion.ExpirationDate });
            }

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
        public required string PaymentIntentId { get; set; } // Stripe payment intent ID for verification
    }

    public class CreatePaymentIntentRequest
    {
        public int ListingId { get; set; }
        public decimal Amount { get; set; }
    }
}
