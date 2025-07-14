namespace MaltalistApi.Services;

public interface IPicturesService
{
    Task<List<string>> AddListingPicturesAsync(int listingId, IFormFileCollection files);
    Task<List<string>> UpdateListingPicturesAsync(int listingId, IFormFileCollection files);
}