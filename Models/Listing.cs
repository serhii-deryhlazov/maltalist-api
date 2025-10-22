using System.ComponentModel.DataAnnotations.Schema;

namespace MaltalistApi.Models;

public class Listing
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public required string Category { get; set; }
    public required string Location { get; set; }
    public required string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    [NotMapped]
    public string? Picture1 { get; set; }
}