using Microsoft.AspNetCore.Mvc;
using MaltalistApi.Models;
using MaltalistApi.Services;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly IListingsService _listingsService;
    private readonly IPicturesService _picturesService;

    public ListingsController(IListingsService listingsService, IPicturesService picturesService)
    {
        _listingsService = listingsService;
        _picturesService = picturesService;
    }

    [HttpGet("minimal")]
    public async Task<ActionResult<GetAllListingsResponse>> GetMinimalListingsPaginated([FromQuery] GetAllListingsRequest request)
    {
        try
        {
            var result = await _listingsService.GetMinimalListingsPaginatedAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while processing your request.", Details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Listing>> GetListingById(int id)
    {
        var listing = await _listingsService.GetListingByIdAsync(id);
        if (listing == null)
            return NotFound();

        return Ok(listing);
    }

    [HttpPost("{id}/pictures")]
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

    [HttpPut("{id}/pictures")]
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

    [HttpPost]
    public async Task<ActionResult<Listing>> CreateListing([FromBody] CreateListingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Invalid listing data");

        var listing = await _listingsService.CreateListingAsync(request);
        if (listing == null)
            return BadRequest("Invalid UserId");

        return CreatedAtAction(nameof(GetListingById), new { id = listing.Id }, listing);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Listing>> UpdateListing(int id, [FromBody] CreateListingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Invalid listing data");

        var listing = await _listingsService.UpdateListingAsync(id, request);
        if (listing == null)
            return NotFound();

        return Ok(listing);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteListing(int id)
    {
        var success = await _listingsService.DeleteListingAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _listingsService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{id}/listings")]
    public async Task<ActionResult<IEnumerable<ListingSummaryResponse>>> GetUserListings(string id)
    {
        var listings = await _listingsService.GetUserListingsAsync(id);
        if (listings == null)
            return NotFound(new { Message = "User not found" });

        return Ok(listings);
    }
}