using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MaltalistApi.Helpers;

namespace MaltalistApi.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration config, ILogger<FileStorageService> logger)
    {
        _basePath = config["FileStorage:BasePath"] ?? "./images";
        _logger = logger;
    }

    public async Task<List<string>> SaveFilesAsync(int listingId, IFormFileCollection files)
    {
        var targetDir = Path.Combine(_basePath, "listings", listingId.ToString());
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        // Find the next available picture number when appending
        int nextIdx = 1;
        if (Directory.Exists(targetDir))
        {
            var existingFiles = Directory.GetFiles(targetDir);
            if (existingFiles.Length > 0)
            {
                // Get the highest picture number + 1
                var picNumbers = existingFiles
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Where(name => name.StartsWith("Picture"))
                    .Select(name => name.Replace("Picture", ""))
                    .Where(num => int.TryParse(num, out _))
                    .Select(int.Parse)
                    .ToList();
                
                if (picNumbers.Count > 0)
                {
                    nextIdx = picNumbers.Max() + 1;
                }
            }
        }

        var savedFiles = new List<string>();
        int idx = nextIdx;
        
        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                try
                {
                    // CRITICAL SECURITY: Validate and sanitize the image
                    // This prevents file type spoofing, malicious uploads, and ensures only valid images
                    var (sanitizedStream, extension) = await ImageValidator.SanitizeImageAsync(file);
                    
                    var fileName = $"Picture{idx}{extension}";
                    var filePath = Path.Combine(targetDir, fileName);

                    // Save the sanitized image
                    sanitizedStream.Position = 0;
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await sanitizedStream.CopyToAsync(fileStream);
                    }
                    
                    sanitizedStream.Dispose();
                    savedFiles.Add(fileName);

                    _logger.LogInformation("Saved and sanitized image {FileName} for listing {ListingId}", fileName, listingId);

                    idx++;
                    if (idx > 10) break; // Max 10 images per listing
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning("File validation failed for listing {ListingId}: {Error}", listingId, ex.Message);
                    throw; // Re-throw to inform the caller
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing file for listing {ListingId}", listingId);
                    throw new InvalidOperationException("Failed to process image file", ex);
                }
            }
        }
        
        return savedFiles;
    }

    public Task DeleteFilesAsync(int listingId)
    {
        var targetDir = Path.Combine(_basePath, "listings", listingId.ToString());
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, true);
        }
        return Task.CompletedTask;
    }
}
