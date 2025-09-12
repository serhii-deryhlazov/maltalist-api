using Microsoft.AspNetCore.Mvc;
using MaltalistApi.Models;
using MaltalistApi.Services;
using System.Text;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly IListingsService _listingsService;
    private readonly MaltalistDbContext _db;

    public ListingsController(IListingsService listingsService, MaltalistDbContext db)
    {
        _listingsService = listingsService;
        _db = db;
    }

    [HttpGet("minimal")]
    public async Task<ActionResult<GetAllListingsResponse>> GetMinimalListingsPaginated([FromQuery] GetAllListingsRequest request)
    {
        try
        {
            var result = await _listingsService.GetMinimalListingsPaginatedAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while processing your request.", Details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Listing>> GetListingById(int id)
    {
        var listing = await _listingsService.GetListingByIdAsync(id);
        if (listing == null)
            return NotFound();

        return Ok(listing);
    }

    [HttpPost]
    public async Task<ActionResult<Listing>> CreateListing([FromBody] CreateListingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Invalid listing data");

        var listing = await _listingsService.CreateListingAsync(request);
        if (listing == null)
            return BadRequest("Invalid UserId");

        return CreatedAtAction(nameof(GetListingById), new { id = listing.Id }, listing);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Listing>> UpdateListing(int id, [FromBody] CreateListingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Invalid listing data");

        var listing = await _listingsService.UpdateListingAsync(id, request);
        if (listing == null)
            return NotFound();

        return Ok(listing);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteListing(int id)
    {
        var success = await _listingsService.DeleteListingAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _listingsService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{id}/listings")]
    public async Task<ActionResult<IEnumerable<ListingSummaryResponse>>> GetUserListings(string id)
    {
        var listings = await _listingsService.GetUserListingsAsync(id);
        if (listings == null)
            return NotFound(new { Message = "User not found" });

        return Ok(listings);
    }

    [HttpPost("seed/{count}")]
    public async Task<IActionResult> SeedListings(int count)
    {
        if (count <= 0 || count > 1000) return BadRequest("Count must be between 1 and 1000");

        var random = new Random();
        var locations = new[]
        {
            "Valletta", "Sliema", "St. Julian's", "Birkirkara", "Mosta", "Qormi", "Zebbug", "Attard", "Balzan", "Birzebbuga",
            "Fgura", "Floriana", "Gzira", "Hamrun", "Marsaskala", "Marsaxlokk", "Mdina", "Mellieha", "Msida", "Naxxar",
            "Paola", "Pembroke", "Rabat", "San Gwann", "Santa Venera", "Siggiewi", "Swieqi", "Tarxien", "Vittoriosa",
            "Xghajra", "Zabbar", "Zejtun", "Zurrieq", "Gozo - Victoria", "Gozo - Xewkija", "Gozo - Nadur", "Gozo - Qala",
            "Gozo - Ghajnsielem", "Gozo - Xaghra", "Gozo - Sannat", "Gozo - Munxar", "Gozo - Fontana", "Gozo - Gharb",
            "Gozo - San Lawrenz", "Gozo - Zebbug"
        };
        var categories = new[] { "Electronics", "Furniture", "Clothing", "Vehicles", "Real Estate", "Sports&Hobby", "Books", "Other" };
        var titles = new[]
        {
            "Brand New Laptop", "Used Bicycle", "Vintage Watch", "Gaming Console", "Smartphone", "Bookshelf", "Dining Table",
            "Winter Jacket", "Mountain Bike", "Apartment for Rent", "Collectible Coins", "Musical Instrument", "Office Chair",
            "Garden Tools", "Antique Vase", "Fitness Equipment", "Car Tires", "Jewelry Set", "Board Games", "Camping Gear"
        };
        var descriptions = new[]
        {
            "Short description.",
            "This is a medium length description with some details about the item.",
            "This is a longer description that provides more information about the product, its condition, features, and any other relevant details that a buyer might want to know before making a purchase. It includes multiple sentences to give a comprehensive overview."
        };

        int usersNeeded = (int)Math.Ceiling((double)count / 10);
        for (int i = 0; i < usersNeeded; i++)
        {
            string userId = GenerateRandom18DigitString(random);
            var user = new User
            {
                Id = userId,
                UserName = "User" + userId.Substring(0, 5),
                Email = $"user{userId}@example.com",
                PhoneNumber = random.Next(2) == 0 ? $"+356{random.Next(10000000, 99999999)}" : null,
                UserPicture = "",
                CreatedAt = DateTime.UtcNow,
                LastOnline = DateTime.UtcNow
            };
            _db.Users.Add(user);

            int listingsForUser = Math.Min(10, count - i * 10);
            for (int j = 0; j < listingsForUser; j++)
            {
                var request = new CreateListingRequest
                {
                    Title = titles[random.Next(titles.Length)],
                    Description = descriptions[random.Next(descriptions.Length)],
                    Price = random.Next(1, 10001),
                    Category = categories[random.Next(categories.Length)],
                    Location = locations[random.Next(locations.Length)],
                    UserId = userId
                };
                await _listingsService.CreateListingAsync(request);
            }
        }

        await _db.SaveChangesAsync();

        return Ok($"Seeded {count} listings across {usersNeeded} users.");
    }

    private string GenerateRandom18DigitString(Random random)
    {
        var sb = new StringBuilder(18);
        for (int i = 0; i < 18; i++)
        {
            sb.Append(random.Next(0, 10));
        }
        return sb.ToString();
    }
}