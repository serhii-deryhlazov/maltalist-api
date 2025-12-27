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

        [HttpGet("promoted/check/{listingId}")]
        public async Task<bool> TryGetPromotedListingById([FromRoute]int listingId)
        {
            var now = DateTime.UtcNow;
            var promotedListings = await _context.Promotions
                .Where(p => p.ListingId == listingId && p.ExpirationDate > now)
                .Include(p => p.Listing).AsNoTracking()
                .Select(p => p.Listing)
                .ToListAsync();

            if (promotedListings.Any()) 
            {
                return true;
            }

            return false;
        }

        [HttpGet("promoted/{category}")]
        public async Task<IActionResult> GetPromotedListings(string category)
        {
            var now = DateTime.UtcNow;
            var promotedListings = await _context.Promotions
                .Where(p => p.Category == category && p.ExpirationDate > now)
                .Include(p => p.Listing)
                .Select(p => p.Listing)
                .ToListAsync();

            foreach (var l in promotedListings)
            {
                if (l != null)
                {
                    var picDir = $"/images/listings/{l.Id}";
                    if (Directory.Exists(picDir))
                    {
                        var files = Directory.GetFiles(picDir)
                            .Select(Path.GetFileName)
                            .OrderBy(f => f)
                            .ToList();
                        if (files.Any())
                        {
                            l.Picture = $"/assets/img/listings/{l.Id}/{files.First()}";
                        }
                    }
                }
            }

            return Ok(promotedListings);
        }

        [HttpGet("pricing")]
        public IActionResult GetPricing()
        {
            var pricing = new
            {
                week = new
                {
                    priceId = _configuration["Stripe:PriceIds:Week"],
                    duration = "week",
                    label = "1 Week"
                },
                twoWeeks = new
                {
                    priceId = _configuration["Stripe:PriceIds:TwoWeeks"],
                    duration = "two-weeks",
                    label = "2 Weeks"
                },
                month = new
                {
                    priceId = _configuration["Stripe:PriceIds:Month"],
                    duration = "month",
                    label = "1 Month"
                }
            };

            return Ok(pricing);
        }

        [HttpPost("create-payment-intent")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            try
            {
                if (request == null || request.Amount <= 0)
                {
                    return BadRequest(new { Message = "Invalid payment amount" });
                }

                var listing = await _context.Listings.FindAsync(request.ListingId);
                if (listing == null)
                {
                    return NotFound(new { Message = "Listing not found" });
                }

                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || listing.UserId != currentUserId)
                {
                    return Forbid();
                }

                var clientSecret = await _paymentService.CreatePaymentIntentAsync(request.Amount);
                
                return Ok(new { clientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent for listing {ListingId}", request.ListingId);
                return StatusCode(500, new { Message = "Failed to create payment intent" });
            }
        }

        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.PriceId))
                {
                    return BadRequest(new { Message = "Invalid request. PriceId is required." });
                }

                var listing = await _context.Listings.FindAsync(request.ListingId);
                if (listing == null)
                {
                    return NotFound(new { Message = "Listing not found" });
                }

                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || listing.UserId != currentUserId)
                {
                    return Forbid();
                }

                // Use frontend URL from configuration or default
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost";
                var successUrl = $"{frontendUrl}/listing/{request.ListingId}?payment=success&session_id={{CHECKOUT_SESSION_ID}}&duration={request.Duration}";
                var cancelUrl = $"{frontendUrl}/listing/{request.ListingId}?payment=cancelled";

                var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                    request.ListingId, 
                    request.PriceId,
                    successUrl, 
                    cancelUrl
                );
                
                return Ok(new { checkoutUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session for listing {ListingId}", request.ListingId);
                return StatusCode(500, new { Message = "Failed to create checkout session" });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequest request)
        {
            if (request == null || request.ListingId <= 0)
            {
                return BadRequest(new { Message = "Invalid promotion request" });
            }

            var listing = await _context.Listings.FindAsync(request.ListingId);
            if (listing == null)
            {
                return NotFound(new { Message = "Listing not found" });
            }

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || listing.UserId != currentUserId)
            {
                return Forbid();
            }

            bool paymentVerified = false;

            // Support both checkout session ID and payment intent ID
            if (!string.IsNullOrWhiteSpace(request.SessionId))
            {
                paymentVerified = await _paymentService.VerifyCheckoutSessionAsync(request.SessionId);
            }
            else if (!string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                var promotionPrice = _configuration.GetValue<decimal>("Stripe:PromotionPrice", 9.99m);
                paymentVerified = await _paymentService.VerifyPaymentIntentAsync(
                    request.PaymentIntentId, 
                    promotionPrice
                );
            }
            else
            {
                return BadRequest(new { Message = "Payment verification required. Either SessionId or PaymentIntentId must be provided." });
            }
            
            if (!paymentVerified)
            {
                return BadRequest(new { Message = "Payment verification failed. Please ensure payment was successful." });
            }

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
        public string? SessionId { get; set; } // Stripe checkout session ID for verification (Stripe Checkout)
        public string? PaymentIntentId { get; set; } // Stripe payment intent ID for verification (Payment Intents API)
    }

    public class CreatePaymentIntentRequest
    {
        public int ListingId { get; set; }
        public decimal Amount { get; set; }
    }

    public class CreateCheckoutSessionRequest
    {
        public int ListingId { get; set; }
        public required string PriceId { get; set; } // Stripe Price ID from your dashboard
        public required string Duration { get; set; } // "week" or "month"
    }
}
