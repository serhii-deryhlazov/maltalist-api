using Microsoft.EntityFrameworkCore;
using MaltalistApi.Models;

namespace MaltalistApi.Services;

public class ListingsService : IListingsService
{
    private readonly MaltalistDbContext _db;

    public ListingsService(MaltalistDbContext db)
    {
        _db = db;
    }

    public async Task<GetAllListingsResponse> GetMinimalListingsPaginatedAsync(GetAllListingsRequest request)
    {
        var query = _db.Listings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(l =>
                l.Title.Contains(request.Search)
                || l.Description.Contains(request.Search));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(l => l.Category == request.Category);
        }

        query = query.OrderByDescending(l => l.CreatedAt);

        var totalNumber = await query.CountAsync();

        var listings = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(l => new ListingSummaryResponse
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                Price = l.Price,
                Category = l.Category,
                UserId = l.UserId,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync();

        return new GetAllListingsResponse
        {
            TotalNumber = totalNumber,
            Listings = listings,
            Page = request.Page
        };
    }

    public async Task<Listing?> GetListingByIdAsync(int id)
    {
        return await _db.Listings.FindAsync(id);
    }

    public async Task<Listing?> CreateListingAsync(CreateListingRequest request)
    {
        var user = await _db.Users.FindAsync(request.UserId);
        if (user == null)
            return null;

        var newListing = new Listing
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Listings.Add(newListing);
        await _db.SaveChangesAsync();

        return newListing;
    }

    public async Task<Listing?> UpdateListingAsync(int id, CreateListingRequest request)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            return null;

        listing.Title = request.Title;
        listing.Description = request.Description;
        listing.Price = request.Price;
        listing.Category = request.Category;
        listing.UpdatedAt = DateTime.UtcNow;

        _db.Listings.Update(listing);
        await _db.SaveChangesAsync();

        return listing;
    }

    public async Task<bool> DeleteListingAsync(int id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            return false;

        _db.Listings.Remove(listing);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _db.Listings.Select(l => l.Category).Distinct().ToListAsync();
    }

    public async Task<IEnumerable<ListingSummaryResponse>?> GetUserListingsAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return null;

        return await _db.Listings
            .Where(l => l.UserId == userId)
            .Select(l => new ListingSummaryResponse
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                Price = l.Price,
                Category = l.Category,
                UserId = l.UserId,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync();
    }
}