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

            listing.Approved = false;
            await _listingsService.UpdateListingAsync(listing);

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
            
            listing.Approved = false;
            await _listingsService.UpdateListingAsync(listing);
            
            return Ok(new { Message = "Picture deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpPut("{id}/reorder")]
    [Authorize]
    public async Task<IActionResult> ReorderListingPictures(int id, [FromBody] List<string> orderedFilenames)
    {
        try
        {
            // Validate listing exists
            var listing = await _listingsService.GetListingByIdAsync(id);
            if (listing == null)
                return NotFound("Listing not found");

            // Verify ownership
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || listing.UserId != currentUserId)
            {
                return Forbid();
            }

            if (orderedFilenames == null || orderedFilenames.Count == 0)
                return BadRequest("No filenames provided");

            var targetDir = Path.Combine("/images", "listings", id.ToString());
            if (!Directory.Exists(targetDir))
                return NotFound("No pictures found for this listing");

            // Validate all filenames exist and are safe
            var existingFiles = Directory.GetFiles(targetDir)
                .Select(f => Path.GetFileName(f))
                .ToHashSet();

            foreach (var filename in orderedFilenames)
            {
                if (string.IsNullOrWhiteSpace(filename) || filename.Contains("..") || 
                    filename.Contains("/") || filename.Contains("\\"))
                    return BadRequest($"Invalid filename: {filename}");
                
                if (!existingFiles.Contains(filename))
                    return BadRequest($"File not found: {filename}");
            }

            // Rename files with order prefix: 001_filename, 002_filename, etc.
            var tempDir = Path.Combine("/images", "listings", $"{id}_temp");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Move files to temp directory with new names
                for (int i = 0; i < orderedFilenames.Count; i++)
                {
                    var oldPath = Path.Combine(targetDir, orderedFilenames[i]);
                    var orderPrefix = (i + 1).ToString("D3");
                    
                    // Strip existing order prefix if present (format: 001_filename)
                    var cleanFilename = orderedFilenames[i];
                    if (cleanFilename.Length > 4 && cleanFilename[3] == '_' && 
                        char.IsDigit(cleanFilename[0]) && char.IsDigit(cleanFilename[1]) && char.IsDigit(cleanFilename[2]))
                    {
                        cleanFilename = cleanFilename.Substring(4);
                    }
                    
                    var newFilename = $"{orderPrefix}_{cleanFilename}";
                    var tempPath = Path.Combine(tempDir, newFilename);
                    
                    System.IO.File.Move(oldPath, tempPath);
                }

                // Move files back to original directory
                foreach (var file in Directory.GetFiles(tempDir))
                {
                    var filename = Path.GetFileName(file);
                    var destPath = Path.Combine(targetDir, filename);
                    System.IO.File.Move(file, destPath);
                }

                // Clean up temp directory
                Directory.Delete(tempDir);

                return Ok(new { Message = "Pictures reordered successfully" });
            }
            catch
            {
                // Rollback: restore original files if something went wrong
                if (Directory.Exists(tempDir))
                {
                    foreach (var file in Directory.GetFiles(tempDir))
                    {
                        var filename = Path.GetFileName(file);
                        // Remove order prefix
                        var originalName = filename.Substring(4); // Remove "001_"
                        var destPath = Path.Combine(targetDir, originalName);
                        if (!System.IO.File.Exists(destPath))
                        {
                            System.IO.File.Move(file, destPath);
                        }
                    }
                    Directory.Delete(tempDir, true);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}
