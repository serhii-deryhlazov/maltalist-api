using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MaltalistApi.Services;
using System.IO;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/pictures")]
public class PicturesController : ControllerBase
{
    private readonly IPicturesService _picturesService;
    private readonly IListingsService _listingsService;

    public PicturesController(IPicturesService picturesService, IListingsService listingsService)
    {
        _picturesService = picturesService;
        _listingsService = listingsService;
    }

    [HttpPost("{id}")]
    [Authorize]
    public async Task<IActionResult> AddListingPictures(int id)
    {
        try
        {
            var listing = await _listingsService.GetListingByIdAsync(id);
            if (listing == null)
                return NotFound();

            // Verify ownership: Only allow the listing owner to add pictures
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || listing.UserId != currentUserId)
            {
                return Forbid();
            }

            // Validate that files were provided
            if (Request.Form.Files.Count == 0)
                return BadRequest("No files provided");

            // Create directory if it doesn't exist (but don't delete existing files)
            var targetDir = Path.Combine("/images", "listings", id.ToString());
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var result = await _picturesService.AddListingPicturesAsync(id, Request.Form.Files);

            return Ok(new { Saved = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public IActionResult GetListingImageUrls(int id)
    {
        // Validate id to prevent path traversal
        if (id <= 0)
            return BadRequest("Invalid listing ID");

        var targetDir = Path.Combine("/images", "listings", id.ToString());
        
        // Ensure the directory path doesn't contain traversal attempts
        var fullPath = Path.GetFullPath(targetDir);
        if (!fullPath.Contains("/images/listings/"))
            return BadRequest("Invalid path");
            
        if (!Directory.Exists(targetDir))
            return NotFound("No pictures found for this listing.");

        var files = Directory.GetFiles(targetDir)
            .Select(f => Path.GetFileName(f))
            .Where(f => !string.IsNullOrEmpty(f))
            .OrderBy(f => f)
            .ToList();

        var urls = files.Select(f => $"/assets/img/listings/{id}/{f}").ToList();

        return Ok(urls);
    }

    [HttpDelete("{id}/{filename}")]
    [Authorize]
    public async Task<IActionResult> DeleteListingPicture(int id, string filename)
    {
        try
        {
            // Validate listing exists
            var listing = await _listingsService.GetListingByIdAsync(id);
            if (listing == null)
                return NotFound("Listing not found");

            // Verify ownership: Only allow the listing owner to delete pictures
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || listing.UserId != currentUserId)
            {
                return Forbid();
            }

            // Sanitize filename to prevent path traversal
            if (string.IsNullOrWhiteSpace(filename) || filename.Contains("..") || filename.Contains("/") || filename.Contains("\\"))
                return BadRequest("Invalid filename");

            var targetDir = Path.Combine("/images", "listings", id.ToString());
            var filePath = Path.Combine(targetDir, filename);

            // Ensure the full path is within the listing's directory
            var fullPath = Path.GetFullPath(filePath);
            var fullDir = Path.GetFullPath(targetDir);
            if (!fullPath.StartsWith(fullDir))
                return BadRequest("Invalid path");

            if (!System.IO.File.Exists(filePath))
                return NotFound("Picture not found");

            // Delete the file
            System.IO.File.Delete(filePath);
            return Ok(new { Message = "Picture deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}
