using MaltalistApi.Models;
using MaltalistApi.Helpers;

namespace MaltalistApi.Services;

public class UsersService : IUsersService
{
    private readonly MaltalistDbContext _db;

    public UsersService(MaltalistDbContext db)
    {
        _db = db;
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
}