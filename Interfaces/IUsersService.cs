using MaltalistApi.Models;
using Google.Apis.Auth;
using System.Security.Claims;

namespace MaltalistApi.Services;

public interface IUsersService
{
    Task<User?> GetUserByIdAsync(string id);
    Task<User?> UpdateUserAsync(string id, User updatedUser);
    Task<bool> DeleteUserAsync(string id);
    Task<User> CreateUserAsync(User newUser);
    Task<string> UploadUserProfilePictureAsync(string userId, IFormFile file);
    Task<object> GetUserDataExportAsync(string id);
    Task<bool> DeactivateUserAsync(string id);
    Task<bool> ActivateUserAsync(string id);
    Task<string?> DownloadAndSaveGoogleProfilePictureAsync(string userId, string imageUrl);
    Task<User> LoginWithGoogleAsync(GoogleJsonWebSignature.Payload payload);
    Task<(bool isValid, GoogleJsonWebSignature.Payload? payload, string? errorMessage)> ValidateGoogleTokenAsync(string idToken);
    ClaimsPrincipal CreateClaimsPrincipal(User user);
    (ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) CreateAuthenticationData(User user);
}