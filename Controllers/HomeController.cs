using Microsoft.AspNetCore.Mvc;

namespace MaltalistApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        // Sample endpoint for home data
        [HttpGet("home-data")]
        public IActionResult GetHomeData()
        {
            var homeData = new
            {
                message = "Welcome to Maltalist",
                featuredListings = new[] { "Listing 1", "Listing 2", "Listing 3" }
            };

            return Ok(homeData);
        }
    }
}
