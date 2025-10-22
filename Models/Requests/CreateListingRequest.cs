namespace MaltalistApi.Models;

public class CreateListingRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required decimal Price { get; set; }
    public required string Category { get; set; }
    public required string UserId { get; set; }
    public required string Location { get; set; }
}