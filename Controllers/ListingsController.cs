using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaltalistApi.Models;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly MaltalistDbContext _db;

    public ListingsController(MaltalistDbContext db)
    {
        _db = db;
    }

    [HttpGet("minimal")]
    public async Task<ActionResult<IEnumerable<ListingSummaryResponse>>> GetListingsMinimal()
    {
        var listings = await _db.Listings.Select(l => new ListingSummaryResponse
        {
            Id = l.Id,
            Title = l.Title,
            Description = l.Description,
            Price = l.Price,
            Category = l.Category,
            UserId = l.UserId,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,
            Picture1 = l.Picture1
        }).ToListAsync();
        
        return Ok(listings);
    }

    [HttpGet]
    public async Task<ActionResult<GetAllListingsResponse>> GetAllListings([FromQuery] GetAllListingsRequest request)
    {
        var query = _db.Listings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(l => l.Title.Contains(request.Search) || l.Description.Contains(request.Search));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(l => l.Category == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            if (request.OrderBy == "desc")
            {
                query = query.OrderByDescending(l => EF.Property<object>(l, request.SortBy));
            }
            else
            {
                query = query.OrderBy(l => EF.Property<object>(l, request.SortBy));
            }
        }

        var totalNumber = await query.CountAsync();
        var listings = await query.Skip((request.Page - 1) * request.Limit).Take(request.Limit).ToListAsync();

        return Ok(new GetAllListingsResponse
        {
            TotalNumber = totalNumber,
            Listings = listings,
            Page = request.Page
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Listing>> GetListingById(int id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            return NotFound();

        return Ok(listing);
    }

    [HttpPost]
    public async Task<ActionResult<Listing>> CreateListing([FromBody] CreateListingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Invalid listing data");

        var user = await _db.Users.FindAsync(request.UserId);
        if (user == null)
            return BadRequest("Invalid UserId");

        var newListing = new Listing
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            UserId = request.UserId,
            Picture1 = request.Picture1,
            Picture2 = request.Picture2,
            Picture3 = request.Picture3,
            Picture4 = request.Picture4,
            Picture5 = request.Picture5,
            Picture6 = request.Picture6,
            Picture7 = request.Picture7,
            Picture8 = request.Picture8,
            Picture9 = request.Picture9,
            Picture10 = request.Picture10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Listings.Add(newListing);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetListingById), new { id = newListing.Id }, newListing);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Listing>> UpdateListing(int id, [FromBody] CreateListingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Invalid listing data");

        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            return NotFound();

        listing.Title = request.Title;
        listing.Description = request.Description;
        listing.Price = request.Price;
        listing.Category = request.Category;

        _db.Listings.Update(listing);
        await _db.SaveChangesAsync();

        return Ok(listing);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteListing(int id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            return NotFound();

        _db.Listings.Remove(listing);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _db.Listings.Select(l => l.Category).Distinct().ToListAsync();
        return Ok(categories);
    }

    [HttpGet("{id}/listings")]
    public async Task<ActionResult<IEnumerable<Listing>>> GetUserListings(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var listings = await _db.Listings.Where(l => l.UserId == id).ToListAsync();
        return Ok(listings);
    }
}
