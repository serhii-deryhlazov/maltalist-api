using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MaltalistApi.Services;
using MaltalistApi.Models;

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

    private async Task<(IActionResult? Error, Listing? Listing)> ValidateListingOwnershipAsync(int id)
    {
        var listing = await _listingsService.GetListingByIdAsync(id);
        if (listing == null)
            return (NotFound("Listing not found"), null);

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || listing.UserId != currentUserId)
        {
            return (Forbid(), null);
        }

        return (null, listing);
    }

    private IActionResult? ValidateFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename) || filename.Contains("..") || 
            filename.Contains("/") || filename.Contains("\\"))
            return BadRequest("Invalid filename");
        
        return null;
    }

    private string GetListingImagesPath(int listingId)
    {
        return Path.Combine("/images", "listings", listingId.ToString());
    }

    private IActionResult? ValidatePath(string fullPath, string expectedPrefix)
    {
        if (!fullPath.StartsWith(expectedPrefix))
            return BadRequest("Invalid path");
        
        return null;
    }

    [HttpPost("{id}")]
    [Authorize]
    public async Task<IActionResult> AddListingPictures(int id)
    {
        try
        {
            var (error, listing) = await ValidateListingOwnershipAsync(id);
            if (error != null)
                return error;

            if (Request.Form.Files.Count == 0)
                return BadRequest("No files provided");

            var targetDir = GetListingImagesPath(id);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var result = await _picturesService.AddListingPicturesAsync(id, Request.Form.Files);

            listing!.Approved = false;
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
        if (id <= 0)
            return BadRequest("Invalid listing ID");

        var targetDir = GetListingImagesPath(id);
        
        var fullPath = Path.GetFullPath(targetDir);
        var pathError = ValidatePath(fullPath, "/images/listings/");
        if (pathError != null)
            return pathError;
            
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
            var (error, listing) = await ValidateListingOwnershipAsync(id);
            if (error != null)
                return error;

            var filenameError = ValidateFilename(filename);
            if (filenameError != null)
                return filenameError;

            var targetDir = GetListingImagesPath(id);
            var filePath = Path.Combine(targetDir, filename);

            var fullPath = Path.GetFullPath(filePath);
            var fullDir = Path.GetFullPath(targetDir);
            var pathError = ValidatePath(fullPath, fullDir);
            if (pathError != null)
                return pathError;

            if (!System.IO.File.Exists(filePath))
                return NotFound("Picture not found");

            System.IO.File.Delete(filePath);
            
            listing!.Approved = false;
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
            var (error, listing) = await ValidateListingOwnershipAsync(id);
            if (error != null)
                return error;

            if (orderedFilenames == null || orderedFilenames.Count == 0)
                return BadRequest("No filenames provided");

            var targetDir = GetListingImagesPath(id);
            if (!Directory.Exists(targetDir))
                return NotFound("No pictures found for this listing");

            var existingFiles = Directory.GetFiles(targetDir)
                .Select(f => Path.GetFileName(f))
                .ToHashSet();

            foreach (var filename in orderedFilenames)
            {
                var filenameError = ValidateFilename(filename);
                if (filenameError != null)
                    return BadRequest($"Invalid filename: {filename}");
                
                if (!existingFiles.Contains(filename))
                    return BadRequest($"File not found: {filename}");
            }

            var tempDir = Path.Combine("/images", "listings", $"{id}_temp");
            Directory.CreateDirectory(tempDir);

            try
            {
                for (int i = 0; i < orderedFilenames.Count; i++)
                {
                    var oldPath = Path.Combine(targetDir, orderedFilenames[i]);
                    var orderPrefix = (i + 1).ToString("D3");
                    
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

                foreach (var file in Directory.GetFiles(tempDir))
                {
                    var filename = Path.GetFileName(file);
                    var destPath = Path.Combine(targetDir, filename);
                    System.IO.File.Move(file, destPath);
                }

                Directory.Delete(tempDir);

                return Ok(new { Message = "Pictures reordered successfully" });
            }
            catch
            {
                if (Directory.Exists(tempDir))
                {
                    foreach (var file in Directory.GetFiles(tempDir))
                    {
                        var filename = Path.GetFileName(file);
                        var originalName = filename.Substring(4);
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
