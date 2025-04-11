using Google.Apis.Auth;
using MaltalistApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GoogleAuthController : ControllerBase
{
    private readonly MaltalistDbContext _db;

    public GoogleAuthController(MaltalistDbContext db)
    {
        _db = db;
    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; }
    }

    [HttpPost("login")]
    public async Task<ActionResult<User>> LoginWithGoogle([FromBody] GoogleLoginRequest request)
    {
        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { Message = "Invalid Google ID token" });
        }

        var userId = long.Parse(payload.Subject); // Google 'sub' is unique per user
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
