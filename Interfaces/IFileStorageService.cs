using Microsoft.AspNetCore.Http;

namespace MaltalistApi.Services;

public interface IFileStorageService
{
    Task<List<string>> SaveFilesAsync(int listingId, IFormFileCollection files);
    Task DeleteFilesAsync(int listingId);
}
