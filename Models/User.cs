namespace MaltalistApi.Models;

public class User
{
    public required string Id { get; set; }
    public required string UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public required string UserPicture { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastOnline { get; set; }
    public DateTime? ConsentTimestamp { get; set; }
    public bool IsActive { get; set; } = true;
    public bool UsingWA { get; set; } = false;
}