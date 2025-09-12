using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MaltalistApi.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public FileStorageService(IConfiguration config)
    {
        _basePath = config["FileStorage:BasePath"] ?? "./images";
    }

    public async Task<List<string>> SaveFilesAsync(int listingId, IFormFileCollection files)
    {
        var targetDir = Path.Combine(_basePath, listingId.ToString());
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

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

                idx++;
                if (idx > 10) break;
            }
        }
        return savedFiles;
    }

    public Task DeleteFilesAsync(int listingId)
    {
        var targetDir = Path.Combine(_basePath, listingId.ToString());
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, true);
        }
        return Task.CompletedTask;
    }
}
