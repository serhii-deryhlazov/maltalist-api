using MaltalistApi.Models;

namespace MaltalistApi.Services;

public interface IUsersService
{
    Task<User?> GetUserByIdAsync(string id);
    Task<User?> UpdateUserAsync(string id, User updatedUser);
    Task<bool> DeleteUserAsync(string id);
    Task<User> CreateUserAsync(User newUser);
    Task<string> UploadUserProfilePictureAsync(string userId, IFormFile file);
}