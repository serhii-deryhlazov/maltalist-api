using Microsoft.AspNetCore.Mvc;

namespace MaltalistApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ListingsController : ControllerBase
    {
        // Sample endpoint to get all listings
        [HttpGet]
        public IActionResult GetAllListings()
        {
            var listings = new[]
            {
                new { id = 1, name = "Listing 1", description = "Description 1" },
                new { id = 2, name = "Listing 2", description = "Description 2" },
                new { id = 3, name = "Listing 3", description = "Description 3" }
            };

            return Ok(listings);
        }

        // Sample endpoint for listing details by ID
        [HttpGet("{id}")]
        public IActionResult GetListingById(int id)
        {
            var listing = new { id, name = $"Listing {id}", description = $"Description {id}" };
            return Ok(listing);
        }
    }
}
