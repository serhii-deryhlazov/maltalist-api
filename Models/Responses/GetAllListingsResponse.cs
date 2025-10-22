namespace MaltalistApi.Models;

public class GetAllListingsResponse
{
    public required List<ListingSummaryResponse> Listings { get; set; }
    public required int TotalNumber { get; set; }
    public required int Page { get; set; }
}