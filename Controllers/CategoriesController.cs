using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Categories;
using RaceDay.API.Entities;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages event categories (e.g., age groups, distances). Organisers manage categories for their own events; the public can view categories.
    /// </summary>
    [Route("api")]
    [ApiController]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all categories for a specific event.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <returns>A list of categories belonging to the event.</returns>
        /// <response code="200">Returns the list of categories.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("events/{eventId}/categories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists) return NotFound(new { Message = "Event not found." });

            var categories = await _context.Categories
                .Where(c => c.EventId == eventId)
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Retrieves a single category by its ID.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <returns>The category details.</returns>
        /// <response code="200">Returns the category.</response>
        /// <response code="404">Category not found.</response>
        [HttpGet("categories/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound(new { Message = "Category not found." });
            return Ok(category);
        }

        /// <summary>
        /// Creates a new category for a specific event.
        /// </summary>
        /// <remarks>
        /// Restricted to the Organiser who owns the event. Validates that AgeMin is not greater than AgeMax.
        /// </remarks>
        /// <param name="eventId">The ID of the event to add the category to.</param>
        /// <param name="request">The category details.</param>
        /// <returns>The newly created category.</returns>
        /// <response code="201">Category created successfully.</response>
        /// <response code="400">Invalid data (e.g., AgeMin greater than AgeMax).</response>
        /// <response code="401">Not logged in as an Organiser.</response>
        /// <response code="403">You do not own this event.</response>
        /// <response code="404">Event not found.</response>
        [HttpPost("events/{eventId}/categories")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryCreateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organiser == null) return Unauthorized();

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { Message = "Event not found." });

            if (ev.OrganiserId != organiser.OrganiserId)
                return StatusCode(403, new { Message = "You can only add categories to your own events." });

            if (request.AgeMin.HasValue && request.AgeMax.HasValue && request.AgeMin > request.AgeMax)
                return BadRequest(new { Message = "AgeMin cannot be greater than AgeMax." });

            var category = new Category
            {
                EventId = eventId,
                CategoryName = request.CategoryName,
                CategoryType = request.CategoryType,
                DistanceKm = request.DistanceKm,
                AgeMin = request.AgeMin,
                AgeMax = request.AgeMax,
                GenderRestriction = request.GenderRestriction,
                EntryFee = request.EntryFee,
                MaxParticipants = request.MaxParticipants
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategoryById), new { id = category.CategoryId }, category);
        }

        /// <summary>
        /// Updates an existing category.
        /// </summary>
        /// <remarks>
        /// Restricted to the Organiser who owns the parent event.
        /// </remarks>
        /// <param name="id">The ID of the category to update.</param>
        /// <param name="request">The updated category details.</param>
        /// <returns>A success message.</returns>
        /// <response code="200">Category updated successfully.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in as an Organiser.</response>
        /// <response code="403">You do not own the parent event.</response>
        /// <response code="404">Category not found.</response>
        [HttpPut("categories/{id}")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryCreateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organiser == null) return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound(new { Message = "Category not found." });

            if (category.Event!.OrganiserId != organiser.OrganiserId)
                return StatusCode(403, new { Message = "You can only modify categories for your own events." });

            category.CategoryName = request.CategoryName;
            category.CategoryType = request.CategoryType;
            category.DistanceKm = request.DistanceKm;
            category.AgeMin = request.AgeMin;
            category.AgeMax = request.AgeMax;
            category.GenderRestriction = request.GenderRestriction;
            category.EntryFee = request.EntryFee;
            category.MaxParticipants = request.MaxParticipants;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Category updated successfully." });
        }

        /// <summary>
        /// Deletes a category.
        /// </summary>
        /// <remarks>
        /// Restricted to the Organiser who owns the parent event.
        /// </remarks>
        /// <param name="id">The ID of the category to delete.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Category deleted.</response>
        /// <response code="401">Not logged in as an Organiser.</response>
        /// <response code="403">You do not own the parent event.</response>
        /// <response code="404">Category not found.</response>
        [HttpDelete("categories/{id}")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organiser == null) return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound(new { Message = "Category not found." });

            if (category.Event!.OrganiserId != organiser.OrganiserId)
                return StatusCode(403, new { Message = "You can only delete categories for your own events." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}