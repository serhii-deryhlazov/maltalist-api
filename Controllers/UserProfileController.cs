using Microsoft.AspNetCore.Mvc;

namespace MaltalistApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfileController : ControllerBase
    {
        // Sample endpoint to get user profile data
        [HttpGet]
        public IActionResult GetUserProfile()
        {
            var profile = new
            {
                userName = "John Doe",
                listings = new[]
                {
                    new { id = 1, name = "Listing 1", description = "Description 1" },
                    new { id = 2, name = "Listing 2", description = "Description 2" }
                }
            };

            return Ok(profile);
        }
    }
}
