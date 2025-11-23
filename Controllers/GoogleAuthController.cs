using Google.Apis.Auth;
using MaltalistApi.Models;
using MaltalistApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class GoogleAuthController : ControllerBase
{
    private readonly MaltalistDbContext _db;
    private readonly ILogger<GoogleAuthController> _logger;
    private readonly IUsersService _usersService;

    public GoogleAuthController(MaltalistDbContext db, ILogger<GoogleAuthController> logger, IUsersService usersService)
    {
        _db = db;
        _logger = logger;
        _usersService = usersService;
    }

    public class GoogleLoginRequest
    {
        public required string IdToken { get; set; }
    }

    [HttpPost("login")]
    public async Task<ActionResult<User>> LoginWithGoogle([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest(new { Message = "ID token is required" });
        }

        GoogleJsonWebSignature.Payload payload;

        try
        {
            // Validate token with specific settings
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                // Add audience validation if you have a specific client ID
                // Audience = new[] { "your-client-id.apps.googleusercontent.com" }
            };
            
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            
            // Verify token expiration
            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(payload.ExpirationTimeSeconds ?? 0);
            if (expirationTime < DateTimeOffset.UtcNow)
            {
                return Unauthorized(new { Message = "Token has expired" });
            }

            // Verify issued time (token should not be too old)
            var issuedTime = DateTimeOffset.FromUnixTimeSeconds(payload.IssuedAtTimeSeconds ?? 0);
            if (DateTimeOffset.UtcNow - issuedTime > TimeSpan.FromMinutes(10))
            {
                return Unauthorized(new { Message = "Token is too old" });
            }
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid JWT token received");
            return Unauthorized(new { Message = "Invalid Google ID token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return Unauthorized(new { Message = "Authentication failed" });
        }

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
                var localPictureUrl = await _usersService.DownloadAndSaveGoogleProfilePictureAsync(userId, payload.Picture);
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

        return Ok(user);
    }

}
