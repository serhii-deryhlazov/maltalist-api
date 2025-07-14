// UserProfileController.cs
using MaltalistApi.Models;
using MaltalistApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfileController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UserProfileController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserProfile(string id)
    {
        var user = await _usersService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<User>> UpdateUserProfile(string id, [FromBody] User updatedUser)
    {
        if (id != updatedUser.Id)
        {
            return BadRequest(new { Message = "User ID mismatch" });
        }

        var user = await _usersService.UpdateUserAsync(id, updatedUser);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUserProfile(string id)
    {
        var success = await _usersService.DeleteUserAsync(id);
        if (!success)
        {
            return NotFound(new { Message = "User not found" });
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUserProfile([FromBody] User newUser)
    {
        if (newUser == null
        || string.IsNullOrWhiteSpace(newUser.UserName)
        || string.IsNullOrWhiteSpace(newUser.Email)
        || string.IsNullOrWhiteSpace(newUser.Id))
        {
            return BadRequest(new { Message = "Invalid user data" });
        }

        var user = await _usersService.CreateUserAsync(newUser);
        return CreatedAtAction(nameof(GetUserProfile), new { id = user.Id }, user);
    }
}