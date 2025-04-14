namespace MaltalistApi.Models;

public class Listing
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }    
    public string Picture1 { get; set; }
    public string Picture2 { get; set; }
    public string Picture3 { get; set; }
    public string Picture4 { get; set; }
    public string Picture5 { get; set; }
    public string Picture6 { get; set; }
    public string Picture7 { get; set; }
    public string Picture8 { get; set; }
    public string Picture9 { get; set; }
    public string Picture10 { get; set; }
}