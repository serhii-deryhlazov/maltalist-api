namespace MaltalistApi.Models;

public class CreateListingRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public string UserId { get; set; }
}