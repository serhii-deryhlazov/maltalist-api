using Microsoft.EntityFrameworkCore;
using MaltalistApi.Models;
using MaltalistApi.Helpers;

namespace MaltalistApi.Services;

public class ListingsService : IListingsService
{
    private readonly MaltalistDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public ListingsService(MaltalistDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<GetAllListingsResponse> GetMinimalListingsPaginatedAsync(GetAllListingsRequest request)
    {
        var query = _db.Listings.Where(l => l.Approved == true).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(l =>
                (l.Title != null && l.Title.Contains(request.Search))
                || (l.Description != null && l.Description.Contains(request.Search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(l => l.Category == request.Category);
        }

        // Sorting
        if (!string.IsNullOrEmpty(request.Sort))
        {
            switch (request.Sort)
            {
                case "price_asc":
                    query = query.OrderBy(l => l.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(l => l.Price);
                    break;
                case "date_asc":
                    query = query.OrderBy(l => l.CreatedAt);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(l => l.CreatedAt);
                    break;
                case "distance":
                    if (!string.IsNullOrEmpty(request.Location))
                    {
                        return await GetSortedByDistanceAsync(request);
                    }
                    break;
                default:
                    query = query.OrderByDescending(l => l.CreatedAt);
                    break;
            }
        }
        else
        {
            query = query.OrderByDescending(l => l.CreatedAt);
        }

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
                UpdatedAt = l.UpdatedAt,
                Location = l.Location,
                Picture = null
            })
            .ToListAsync();

        // Populate Picture with the first image URL
        foreach (var l in listings)
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

        return new GetAllListingsResponse
        {
            TotalNumber = totalNumber,
            Listings = listings,
            Page = request.Page
        };
    }

    public async Task<Listing?> GetListingByIdAsync(int id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            return null;

        // Populate Picture1 with the first image URL from filesystem
        var picDir = $"/images/listings/{listing.Id}";
        if (Directory.Exists(picDir))
        {
            var files = Directory.GetFiles(picDir)
                .Select(Path.GetFileName)
                .OrderBy(f => f)
                .ToList();
            if (files.Any())
            {
                listing.Picture1 = $"/assets/img/listings/{listing.Id}/{files.First()}";
            }
        }

        return listing;
    }

    public async Task<Listing?> CreateListingAsync(CreateListingRequest request)
    {
        var user = await _db.Users.FindAsync(request.UserId);
        if (user == null)
            return null;

        var newListing = new Listing
        {
            Title = InputSanitizer.SanitizeHtml(request.Title),
            Description = InputSanitizer.SanitizeHtml(request.Description),
            Price = request.Price,
            Category = InputSanitizer.SanitizeText(request.Category),
            UserId = request.UserId,
            Location = InputSanitizer.SanitizeText(request.Location),
            ShowPhone = request.ShowPhone,
            Complete = request.Complete,
            Lease = request.Lease,
            Approved = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Listings.Add(newListing);
        await _db.SaveChangesAsync();

        return newListing;
    }

    public async Task<Listing?> UpdateListingAsync(int id, UpdateListingRequest request)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            return null;

        listing.Title = InputSanitizer.SanitizeHtml(request.Title);
        listing.Description = InputSanitizer.SanitizeHtml(request.Description);
        listing.Price = request.Price;
        listing.Category = InputSanitizer.SanitizeText(request.Category);
        listing.Location = InputSanitizer.SanitizeText(request.Location);
        listing.ShowPhone = request.ShowPhone;
        listing.Complete = request.Complete;
        listing.Lease = request.Lease;
        listing.Approved = false;
        listing.UpdatedAt = DateTime.UtcNow;

        _db.Listings.Update(listing);
        await _db.SaveChangesAsync();

        return listing;
    }

    public async Task UpdateListingAsync(Listing listing)
    {
        _db.Listings.Update(listing);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteListingAsync(int id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            return false;

        // Delete pictures from file storage
        await _fileStorage.DeleteFilesAsync(id);

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

        var listings = await _db.Listings
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
                UpdatedAt = l.UpdatedAt,
                Location = l.Location,
                Picture = null
            })
            .ToListAsync();

        // Populate Picture with the first image URL
        foreach (var l in listings)
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

        return listings;
    }

    private async Task<GetAllListingsResponse> GetSortedByDistanceAsync(GetAllListingsRequest request)
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

        var allListings = await query.ToListAsync();
        var locationCoords = new Dictionary<string, (double lat, double lon)>
        {
            ["Valletta"] = (35.8989, 14.5146),
            ["Sliema"] = (35.9125, 14.5019),
            ["St. Julian's"] = (35.9183, 14.4894),
            ["Birkirkara"] = (35.8972, 14.4614),
            ["Mosta"] = (35.9092, 14.4256),
            ["Qormi"] = (35.8760, 14.4720),
            ["Zebbug"] = (35.8719, 14.4411),
            ["Attard"] = (35.8897, 14.4425),
            ["Balzan"] = (35.9003, 14.4538),
            ["Birzebbuga"] = (35.8258, 14.5269),
            ["Fgura"] = (35.8703, 14.5136),
            ["Floriana"] = (35.8958, 14.5084),
            ["Gzira"] = (35.9058, 14.4881),
            ["Hamrun"] = (35.8847, 14.4844),
            ["Marsaskala"] = (35.8625, 14.5675),
            ["Marsaxlokk"] = (35.8419, 14.5431),
            ["Mdina"] = (35.8856, 14.4042),
            ["Mellieha"] = (35.9564, 14.3622),
            ["Msida"] = (35.8972, 14.4822),
            ["Naxxar"] = (35.9136, 14.4436),
            ["Paola"] = (35.8731, 14.4989),
            ["Pembroke"] = (35.9306, 14.4767),
            ["Rabat"] = (35.8811, 14.3981),
            ["San Gwann"] = (35.9058, 14.4764),
            ["Santa Venera"] = (35.8908, 14.4747),
            ["Siggiewi"] = (35.8556, 14.4361),
            ["Swieqi"] = (35.9225, 14.4800),
            ["Tarxien"] = (35.8672, 14.5150),
            ["Vittoriosa"] = (35.8922, 14.5183),
            ["Xghajra"] = (35.8850, 14.5475),
            ["Zabbar"] = (35.8764, 14.5350),
            ["Zejtun"] = (35.8558, 14.5331),
            ["Zurrieq"] = (35.8311, 14.4742),
            ["Gozo - Victoria"] = (36.0444, 14.2397),
            ["Gozo - Xewkija"] = (36.0328, 14.2581),
            ["Gozo - Nadur"] = (36.0378, 14.2942),
            ["Gozo - Qala"] = (36.0367, 14.3094),
            ["Gozo - Ghajnsielem"] = (36.0250, 14.2850),
            ["Gozo - Xaghra"] = (36.0500, 14.2642),
            ["Gozo - Sannat"] = (36.0242, 14.2431),
            ["Gozo - Munxar"] = (36.0308, 14.2339),
            ["Gozo - Fontana"] = (36.0375, 14.2367),
            ["Gozo - Gharb"] = (36.0608, 14.2089),
            ["Gozo - San Lawrenz"] = (36.0556, 14.2039),
            ["Gozo - Zebbug"] = (36.0722, 14.2358)
        };

        if (locationCoords.TryGetValue(request.Location!, out var userCoord))
        {
            var sorted = allListings.Select(l =>
            {
                var coord = locationCoords.TryGetValue(l.Location!, out var c) ? c : userCoord;
                var distance = Haversine(userCoord.Item1, userCoord.Item2, coord.Item1, coord.Item2);
                return new { Listing = l, Distance = distance };
            }).OrderBy(x => x.Distance).Select(x => x.Listing).ToList();

            var total = sorted.Count;
            var paged = sorted.Skip((request.Page - 1) * request.Limit).Take(request.Limit).ToList();

            var result = paged.Select(l => new ListingSummaryResponse
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                Price = l.Price,
                Category = l.Category,
                UserId = l.UserId,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                Location = l.Location,
                Picture = null
            }).ToList();

            // Populate Picture with the first image URL
            foreach (var l in result)
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

            return new GetAllListingsResponse
            {
                Listings = result,
                TotalNumber = total,
                Page = request.Page
            };
        }
        else
        {
            // Fallback to default sorting
            var sorted = allListings.OrderByDescending(l => l.CreatedAt).ToList();
            var total = sorted.Count;
            var paged = sorted.Skip((request.Page - 1) * request.Limit).Take(request.Limit).ToList();

            var result = paged.Select(l => new ListingSummaryResponse
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                Price = l.Price,
                Category = l.Category,
                UserId = l.UserId,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                Location = l.Location,
                Picture = null
            }).ToList();

            // Populate Picture with the first image URL
            foreach (var l in result)
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

            return new GetAllListingsResponse
            {
                Listings = result,
                TotalNumber = total,
                Page = request.Page
            };
        }
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return 6371 * c; // km
    }
}