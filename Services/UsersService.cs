using MaltalistApi.Models;

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

        user.UserName = updatedUser.UserName;
        user.UserPicture = updatedUser.UserPicture;
        user.Email = updatedUser.Email;
        user.LastOnline = DateTime.UtcNow;
        user.PhoneNumber = updatedUser.PhoneNumber;

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
        newUser.CreatedAt = DateTime.UtcNow;
        newUser.LastOnline = DateTime.UtcNow;

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        return newUser;
    }
}