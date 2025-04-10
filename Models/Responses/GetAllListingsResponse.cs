namespace MaltalistApi.Models;

public class GetAllListingsResponse
{
    public int TotalNumber { get; set; }
    public List<Listing> Listings { get; set; }
    public int Page { get; set; }
}