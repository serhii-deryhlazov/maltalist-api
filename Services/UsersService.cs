using MaltalistApi.Models;
using MaltalistApi.Helpers;

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
        const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        // Validate file size
        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException($"File exceeds maximum size of 5MB");
        }

        // Validate file extension
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException($"File type {ext} is not allowed. Only image files are permitted.");
        }

        // Validate MIME type
        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException($"Invalid file type. Only image files are permitted.");
        }

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

        // Save new profile picture
        var fileName = $"profile{ext}";
        var filePath = Path.Combine(userDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
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

            var contentType = response.Content.Headers.ContentType?.MediaType;
            var ext = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg" // Default fallback
            };

            var userDir = Path.Combine("/images/users", userId);
            if (!Directory.Exists(userDir))
                Directory.CreateDirectory(userDir);

            // Delete old profile picture if exists
            var existingFiles = Directory.GetFiles(userDir);
            foreach (var existingFile in existingFiles)
            {
                File.Delete(existingFile);
            }

            var fileName = $"profile{ext}";
            var filePath = Path.Combine(userDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await response.Content.CopyToAsync(stream);
            }

            return $"/assets/img/users/{userId}/{fileName}";
        }
        catch
        {
            // If download fails, return null so we can fallback to original URL or placeholder
            return null;
        }
    }
}