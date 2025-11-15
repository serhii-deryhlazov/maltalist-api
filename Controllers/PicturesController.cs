using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> AddListingPictures(int id)
    {
        try
        {
            var listing = await _listingsService.GetListingByIdAsync(id);
            if (listing == null)
                return NotFound();

            // Validate that files were provided
            if (Request.Form.Files.Count == 0)
                return BadRequest("No files provided");

            // Remove existing pictures before adding new ones
            var targetDir = $"/images/{id}";
            if (Directory.Exists(targetDir))
            {
                var existingFiles = Directory.GetFiles(targetDir);
                foreach (var file in existingFiles)
                {
                    System.IO.File.Delete(file);
                }
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

        var targetDir = $"/images/{id}";
        
        // Ensure the directory path doesn't contain traversal attempts
        var fullPath = Path.GetFullPath(targetDir);
        if (!fullPath.StartsWith("/images/"))
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
}
