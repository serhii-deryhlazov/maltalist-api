// UserProfileController.cs
using MaltalistApi.Models;
using MaltalistApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
    [Authorize]
    public async Task<ActionResult<User>> UpdateUserProfile(string id, [FromBody] User updatedUser)
    {
        // Verify ownership: Only allow users to modify their own profile
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
        {
            return Forbid();
        }

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
    [Authorize]
    public async Task<ActionResult> DeleteUserProfile(string id)
    {
        // Verify ownership: Only allow users to delete their own profile
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
        {
            return Forbid();
        }

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

    [HttpPost("{id}/picture")]
    [Authorize]
    public async Task<IActionResult> UploadProfilePicture(string id)
    {
        // Verify ownership: Only allow users to upload their own profile picture
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
        {
            return Forbid();
        }

        try
        {
            var user = await _usersService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            if (Request.Form.Files.Count == 0)
                return BadRequest(new { Message = "No file provided" });

            var file = Request.Form.Files[0];
            var result = await _usersService.UploadUserProfilePictureAsync(id, file);

            return Ok(new { PictureUrl = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{id}/export")]
    [Authorize]
    public async Task<ActionResult> ExportUserData(string id)
    {
        // Verify ownership: Only allow users to export their own data
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
        {
            return Forbid();
        }

        var user = await _usersService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var userData = await _usersService.GetUserDataExportAsync(id);
        return Ok(userData);
    }

    [HttpPost("{id}/deactivate")]
    [Authorize]
    public async Task<ActionResult> DeactivateAccount(string id)
    {
        // Verify ownership: Only allow users to deactivate their own account
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
        {
            return Forbid();
        }

        var user = await _usersService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var success = await _usersService.DeactivateUserAsync(id);
        return Ok(new { Message = "Account deactivated successfully" });
    }

    [HttpPost("{id}/activate")]
    [Authorize]
    public async Task<ActionResult> ActivateAccount(string id)
    {
        // Verify ownership: Only allow users to activate their own account
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
        {
            return Forbid();
        }

        var user = await _usersService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var success = await _usersService.ActivateUserAsync(id);
        return Ok(new { Message = "Account activated successfully" });
    }
}