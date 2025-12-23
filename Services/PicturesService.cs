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

        await _fileStorage.DeleteFilesAsync(listingId);

        var savedFiles = await _fileStorage.SaveFilesAsync(listingId, files);

        listing.UpdatedAt = DateTime.UtcNow;
        _db.Listings.Update(listing);
        await _db.SaveChangesAsync();

        return savedFiles;
    }
}