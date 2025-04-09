using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace MaltalistApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreateListingController : ControllerBase
    {
        private static List<dynamic> Listings = new List<dynamic>();  // In-memory store

        // Sample endpoint for creating a listing
        [HttpPost]
        public IActionResult CreateListing([FromBody] CreateListingRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Description))
                return BadRequest("Invalid listing data");

            // Creating new listing with a generated ID
            var newListing = new
            {
                id = Listings.Count + 1,  // Simple ID generation logic
                request.Name,
                request.Description
            };

            Listings.Add(newListing);  // Add to in-memory list

            return CreatedAtAction(nameof(GetListingById), new { id = newListing.id }, newListing);
        }

        // Sample endpoint to get listing by ID (for response validation)
        [HttpGet("{id}")]
        public IActionResult GetListingById(int id)
        {
            var listing = Listings.FirstOrDefault(l => l.id == id);
            if (listing == null)
                return NotFound();

            return Ok(listing);
        }
    }

    public class CreateListingRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
