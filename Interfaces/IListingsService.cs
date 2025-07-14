using MaltalistApi.Models;

namespace MaltalistApi.Services;

public interface IListingsService
{
    Task<GetAllListingsResponse> GetMinimalListingsPaginatedAsync(GetAllListingsRequest request);
    Task<Listing?> GetListingByIdAsync(int id);
    Task<Listing?> CreateListingAsync(CreateListingRequest request);
    Task<Listing?> UpdateListingAsync(int id, CreateListingRequest request);
    Task<bool> DeleteListingAsync(int id);
    Task<List<string>> GetCategoriesAsync();
    Task<IEnumerable<ListingSummaryResponse>?> GetUserListingsAsync(string userId);
}