using Google.Apis.Auth;
using MaltalistApi.Models;
using MaltalistApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class GoogleAuthController : ControllerBase
{
    private readonly ILogger<GoogleAuthController> _logger;
    private readonly IUsersService _usersService;

    public GoogleAuthController(ILogger<GoogleAuthController> logger, IUsersService usersService)
    {
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
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
            
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

        var user = await _usersService.LoginWithGoogleAsync(payload);
        
        // Create claims for authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, user.UserName ?? "")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Ok(user);
    }

}
