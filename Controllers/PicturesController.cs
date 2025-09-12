using Microsoft.AspNetCore.Mvc;
using MaltalistApi.Services;

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

        var files = Request.Form.Files;
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        var result = await _picturesService.AddListingPicturesAsync(id, files);
        return Ok(new { Saved = result });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateListingPictures(int id)
    {
        var listing = await _listingsService.GetListingByIdAsync(id);
        if (listing == null)
            return NotFound();

        var files = Request.Form.Files;
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        var result = await _picturesService.UpdateListingPicturesAsync(id, files);
        return Ok(new { Updated = result });
    }

    [HttpGet("{id}")]
    public IActionResult GetListingImageUrls(int id)
    {
        var targetDir = $"/images/{id}";
        if (!Directory.Exists(targetDir))
            return NotFound("No pictures found for this listing.");

        var files = Directory.GetFiles(targetDir)
            .Select(Path.GetFileName)
            .ToList();

        var urls = files.Select(f => $"/assets/img/listings/{id}/{f}").Reverse().ToList();

        return Ok(urls);
    }
}
