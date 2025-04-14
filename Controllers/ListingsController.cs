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

    [HttpGet]
    public async Task<ActionResult<GetAllListingsResponse>> GetAllListings(GetAllListingsRequest request)
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
        if (request == null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Invalid listing data");

        var newListing = new Listing
        {
            Title = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category
        };

        _db.Listings.Add(newListing);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetListingById), new { id = newListing.Id }, newListing);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Listing>> UpdateListing(int id, [FromBody] CreateListingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Invalid listing data");

        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            return NotFound();

        listing.Title = request.Name;
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
