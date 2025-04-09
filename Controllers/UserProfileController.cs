using Microsoft.AspNetCore.Mvc;
using MaltalistApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MaltalistApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfileController : ControllerBase
    {
        private readonly MaltalistDbContext _db;

        public UserProfileController(MaltalistDbContext db)
        {
            _db = db;
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserProfile(string username)
        {
            var listings = await _db.Listings
                .Where(l => l.UserName == username)
                .Select(l => new
                {
                    id = l.Id,
                    name = l.Title,
                    description = l.Description
                })
                .ToListAsync();

            var profile = new
            {
                userName = username,
                listings = listings
            };

            return Ok(profile);
        }
    }
}
