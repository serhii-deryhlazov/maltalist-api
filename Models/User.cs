namespace MaltalistApi.Models;

public class User
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public string UserPicture { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastOnline { get; set; }
}