using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MaltalistApi.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

    public FileStorageService(IConfiguration config)
    {
        _basePath = config["FileStorage:BasePath"] ?? "./images";
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
                // Validate file size
                if (file.Length > MaxFileSize)
                {
                    throw new InvalidOperationException($"File {file.FileName} exceeds maximum size of 5MB");
                }

                // Validate file extension
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                {
                    throw new InvalidOperationException($"File type {ext} is not allowed. Only image files are permitted.");
                }

                // Validate MIME type
                if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                {
                    throw new InvalidOperationException($"Invalid file type. Only image files are permitted.");
                }

                var fileName = $"Picture{idx}{ext}";
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
