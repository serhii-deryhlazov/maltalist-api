namespace MaltalistApi.Models;

public class UpdateListingRequest
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required decimal Price { get; set; }
    public required string Category { get; set; }
    public required string Location { get; set; }
    public bool ShowPhone { get; set; } = false;
    public bool Complete { get; set; } = false;
    public bool Lease { get; set; } = false;
}
