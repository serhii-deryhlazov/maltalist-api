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

        // Simply save the new files to the directory (they will be appended)
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

        // Delete all existing files in the directory
        await _fileStorage.DeleteFilesAsync(listingId);

        // Save all new files from the request
        var savedFiles = await _fileStorage.SaveFilesAsync(listingId, files);

        listing.UpdatedAt = DateTime.UtcNow;
        _db.Listings.Update(listing);
        await _db.SaveChangesAsync();

        return savedFiles;
    }
}