namespace MaltalistApi.Models;

public class GetAllListingsResponse
{
    public int TotalNumber { get; set; }
    public List<ListingSummaryResponse> Listings { get; set; }
    public int Page { get; set; }
}