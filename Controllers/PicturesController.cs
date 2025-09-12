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
        var listing = await _listingsService.GetListingByIdAsync(id);
        if (listing == null)
            return NotFound();

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

    [HttpGet("{id}")]
    public IActionResult GetListingImageUrls(int id)
    {
        var targetDir = $"/images/{id}";
        if (!Directory.Exists(targetDir))
            return NotFound("No pictures found for this listing.");

        var files = Directory.GetFiles(targetDir)
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToList();

        var urls = files.Select(f => $"/assets/img/listings/{id}/{f}").ToList();

        return Ok(urls);
    }
}
