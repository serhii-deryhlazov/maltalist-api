using MaltalistApi.Models;
using MaltalistApi.Helpers;
using Google.Apis.Auth;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace MaltalistApi.Services;

public class UsersService : IUsersService
{
    private readonly MaltalistDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersService(MaltalistDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _db.Users.FindAsync(id);
    }

    public async Task<User?> UpdateUserAsync(string id, User updatedUser)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return null;

        user.UserName = InputSanitizer.SanitizeHtml(updatedUser.UserName);
        user.UserPicture = InputSanitizer.SanitizeUrl(updatedUser.UserPicture) ?? user.UserPicture;
        user.Email = InputSanitizer.SanitizeEmail(updatedUser.Email) ?? user.Email;
        user.LastOnline = DateTime.UtcNow;
        user.PhoneNumber = InputSanitizer.SanitizeText(updatedUser.PhoneNumber);

        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        return user;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return false;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<User> CreateUserAsync(User newUser)
    {
        newUser.UserName = InputSanitizer.SanitizeHtml(newUser.UserName);
        var sanitizedPicture = InputSanitizer.SanitizeUrl(newUser.UserPicture);
        if (sanitizedPicture != null)
            newUser.UserPicture = sanitizedPicture;
        newUser.Email = InputSanitizer.SanitizeEmail(newUser.Email) ?? newUser.Email;
        newUser.PhoneNumber = InputSanitizer.SanitizeText(newUser.PhoneNumber);
        newUser.CreatedAt = DateTime.UtcNow;
        newUser.LastOnline = DateTime.UtcNow;

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        return newUser;
    }

    public async Task<string> UploadUserProfilePictureAsync(string userId, IFormFile file)
    {
        // CRITICAL SECURITY: Validate the image file comprehensively
        // This prevents file type spoofing, malicious uploads, and ensures only valid images
        await ImageValidator.ValidateImageFileAsync(file);

        // Create user directory
        var userDir = Path.Combine("/images/users", userId);
        if (!Directory.Exists(userDir))
            Directory.CreateDirectory(userDir);

        // Delete old profile picture if exists
        if (Directory.Exists(userDir))
        {
            var existingFiles = Directory.GetFiles(userDir);
            foreach (var existingFile in existingFiles)
            {
                File.Delete(existingFile);
            }
        }

        // Always save as PNG (re-encoding removes any malicious content)
        var fileName = "profile.png";
        var filePath = Path.Combine(userDir, fileName);

        using (var stream = file.OpenReadStream())
        using (var image = await Image.LoadAsync(stream))
        using (var outputStream = new FileStream(filePath, FileMode.Create))
        {
            await image.SaveAsPngAsync(outputStream);
        }

        // Update user's picture URL in database
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.UserPicture = $"/assets/img/users/{userId}/{fileName}";
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        return $"/assets/img/users/{userId}/{fileName}";
    }

    public async Task<object> GetUserDataExportAsync(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return new { };

        var userListings = _db.Listings
            .Where(l => l.UserId == id)
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.Description,
                l.Price,
                l.Category,
                l.Location,
                l.ShowPhone,
                l.Complete,
                l.Lease,
                l.Approved,
                l.CreatedAt,
                l.UpdatedAt
            })
            .ToList();

        return new
        {
            User = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.UserPicture,
                user.CreatedAt,
                user.LastOnline,
                user.ConsentTimestamp
            },
            Listings = userListings,
            ExportDate = DateTime.UtcNow
        };
    }

    public async Task<bool> DeactivateUserAsync(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return false;

        user.IsActive = false;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ActivateUserAsync(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return false;

        user.IsActive = true;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<string?> DownloadAndSaveGoogleProfilePictureAsync(string userId, string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return null;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode) return null;

            var userDir = Path.Combine("/images/users", userId);
            if (!Directory.Exists(userDir))
                Directory.CreateDirectory(userDir);

            // Delete old profile picture if exists
            var existingFiles = Directory.GetFiles(userDir);
            foreach (var existingFile in existingFiles)
            {
                File.Delete(existingFile);
            }

            // Always save as PNG
            var fileName = "profile.png";
            var filePath = Path.Combine(userDir, fileName);

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var image = await Image.LoadAsync(stream))
            using (var outputStream = new FileStream(filePath, FileMode.Create))
            {
                await image.SaveAsPngAsync(outputStream);
            }

            return $"/assets/img/users/{userId}/{fileName}";
        }
        catch
        {
            // If download fails, return null so we can fallback to original URL or placeholder
            return null;
        }
    }

    public async Task<User> LoginWithGoogleAsync(GoogleJsonWebSignature.Payload payload)
    {
        var userId = payload.Subject;
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
        {
            user = new User
            {
                Id = userId,
                Email = payload.Email,
                UserName = $"{payload.GivenName} {payload.FamilyName}",
                UserPicture = payload.Picture,
                CreatedAt = DateTime.UtcNow,
                LastOnline = DateTime.UtcNow
            };

            // Try to download and save Google profile picture locally
            if (!string.IsNullOrEmpty(payload.Picture))
            {
                var localPictureUrl = await DownloadAndSaveGoogleProfilePictureAsync(userId, payload.Picture);
                if (localPictureUrl != null)
                {
                    user.UserPicture = localPictureUrl;
                }
            }

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            user.LastOnline = DateTime.UtcNow;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        return user;
    }
}