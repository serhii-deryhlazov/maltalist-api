namespace MaltalistApi.Models;

public class GetAllListingsRequest
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Sort { get; set; }
    public string? Order { get; set; }
    public string? Location { get; set; }
}