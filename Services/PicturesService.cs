using MaltalistApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MaltalistApi.Services;

public class PicturesService : IPicturesService
{
    private readonly MaltalistDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public PicturesService(MaltalistDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<List<string>> AddListingPicturesAsync(int listingId, IFormFileCollection files)
    {
        var listing = await _db.Listings.FindAsync(listingId);
        if (listing == null)
            throw new InvalidOperationException("Listing not found");

        var savedFiles = await _fileStorage.SaveFilesAsync(listingId, files);

        int idx = 1;
        foreach (var fileName in savedFiles)
        {
            var prop = typeof(Listing).GetProperty($"Picture{idx}");
            if (prop != null)
                prop.SetValue(listing, $"/assets/img/listings/{listingId}/{fileName}");
            idx++;
        }

        listing.UpdatedAt = DateTime.UtcNow;
        _db.Listings.Update(listing);
        await _db.SaveChangesAsync();

        return savedFiles;
    }

    public async Task<List<string>> UpdateListingPicturesAsync(int listingId, IFormFileCollection files)
    {
        var listing = await _db.Listings.FindAsync(listingId);
        if (listing == null)
            throw new InvalidOperationException("Listing not found");

        for (int i = 1; i <= 10; i++)
        {
            var prop = typeof(Listing).GetProperty($"Picture{i}");
            if (prop != null)
                prop.SetValue(listing, null);
        }

        await _fileStorage.DeleteFilesAsync(listingId);

        var savedFiles = await _fileStorage.SaveFilesAsync(listingId, files);

        int idx = 1;
        foreach (var fileName in savedFiles)
        {
            var prop = typeof(Listing).GetProperty($"Picture{idx}");
            if (prop != null)
                prop.SetValue(listing, $"/assets/img/listings/{listingId}/{fileName}");
            idx++;
        }

        listing.UpdatedAt = DateTime.UtcNow;
        _db.Listings.Update(listing);
        await _db.SaveChangesAsync();

        return savedFiles;
    }
}