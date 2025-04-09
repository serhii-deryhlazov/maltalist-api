using Microsoft.AspNetCore.Mvc;
using MaltalistApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MaltalistApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ListingsController : ControllerBase
    {
        private readonly MaltalistDbContext _db;

        public ListingsController(MaltalistDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Listing>>> GetAllListings()
        {
            var listings = await _db.Listings.ToListAsync();
            return Ok(listings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Listing>> GetListingById(int id)
        {
            var listing = await _db.Listings.FindAsync(id);
            if (listing == null)
                return NotFound();

            return Ok(listing);
        }

        [HttpPost]
        public async Task<ActionResult<Listing>> CreateListing([FromBody] CreateListingRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
                return BadRequest("Invalid listing data");

            var newListing = new Listing
            {
                Title = request.Name,
                Description = request.Description,
                Price = request.Price,
                Category = request.Category
            };

            _db.Listings.Add(newListing);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetListingById), new { id = newListing.Id }, newListing);
        }
    }

    public class CreateListingRequest
    {
        public string Name { get; set; }  // maps to Title in DB
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
    }
}
