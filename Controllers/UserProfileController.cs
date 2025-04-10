using MaltalistApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfileController : ControllerBase
{
    private readonly MaltalistDbContext _db;

    public UserProfileController(MaltalistDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserProfile(long id)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<User>> UpdateUserProfile(long id, [FromBody] User updatedUser)
    {
        if (id != updatedUser.Id)
        {
            return BadRequest(new { Message = "User ID mismatch" });
        }

        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        user.UserName = updatedUser.UserName;
        user.UserPicture = updatedUser.UserPicture;
        user.Email = updatedUser.Email;
        user.LastOnline = DateTime.UtcNow;

        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUserProfile(long id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUserProfile([FromBody] User newUser)
    {
        if (newUser == null || string.IsNullOrWhiteSpace(newUser.UserName) || string.IsNullOrWhiteSpace(newUser.Email))
        {
            return BadRequest(new { Message = "Invalid user data" });
        }

        newUser.CreatedAt = DateTime.UtcNow;
        newUser.LastOnline = DateTime.UtcNow;

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserProfile), new { id = newUser.Id }, newUser);
    }    
}