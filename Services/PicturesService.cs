using MaltalistApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MaltalistApi.Services;

public class PicturesService : IPicturesService
{
    private readonly MaltalistDbContext _db;

    public PicturesService(MaltalistDbContext db)
    {
        _db = db;
    }

    public async Task<List<string>> AddListingPicturesAsync(int listingId, IFormFileCollection files)
    {
        var listing = await _db.Listings.FindAsync(listingId);
        if (listing == null)
            throw new InvalidOperationException("Listing not found");

        var targetDir = $"/var/www/maltalist/ui/assets/img/listings/{listingId}";
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        var savedFiles = new List<string>();
        int idx = 1;
        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var ext = Path.GetExtension(file.FileName);
                var fileName = $"{idx}{ext}";
                var filePath = Path.Combine(targetDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                savedFiles.Add(fileName);

                idx++;
                if (idx > 10) break;
            }
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

        var targetDir = $"/var/www/maltalist/ui/assets/img/listings/{listingId}";
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        for (int i = 1; i <= 10; i++)
        {
            var prop = typeof(Listing).GetProperty($"Picture{i}");
            if (prop != null)
                prop.SetValue(listing, null);
        }

        try
        {
            var existingFiles = Directory.GetFiles(targetDir, "Picture*");
            foreach (var existingFile in existingFiles)
            {
                File.Delete(existingFile);
            }
        }
        catch (Exception)
        {
            // Log the exception but continue
        }

        var savedFiles = new List<string>();
        int idx = 1;
        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var ext = Path.GetExtension(file.FileName);
                var fileName = $"Picture{idx}{ext}";
                var filePath = Path.Combine(targetDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                savedFiles.Add(fileName);

                var prop = typeof(Listing).GetProperty($"Picture{idx}");
                if (prop != null)
                    prop.SetValue(listing, $"/assets/img/listings/{listingId}/{fileName}");

                idx++;
                if (idx > 10) break;
            }
        }

        listing.UpdatedAt = DateTime.UtcNow;
        _db.Listings.Update(listing);
        await _db.SaveChangesAsync();

        return savedFiles;
    }
}