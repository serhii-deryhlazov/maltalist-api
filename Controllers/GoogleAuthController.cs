using Google.Apis.Auth;
using MaltalistApi.Models;
using MaltalistApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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
        var (validationResult, payload) = await ValidateRequest(request);
        if (validationResult != null)
        {
            return validationResult;
        }

        var user = await _usersService.LoginWithGoogleAsync(payload!);
        
        var (claimsPrincipal, authProperties) = _usersService.CreateAuthenticationData(user);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            authProperties);

        return Ok(user);
    }

    private async Task<(ActionResult?, GoogleJsonWebSignature.Payload?)> ValidateRequest(GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return (BadRequest(new { Message = "ID token is required" }), null);
        }

        var (isValid, payload, errorMessage) = await _usersService.ValidateGoogleTokenAsync(request.IdToken);

        if (!isValid || payload == null)
        {
            return (Unauthorized(new { Message = errorMessage ?? "Authentication failed" }), null);
        }

        return (null, payload);
    }

}
