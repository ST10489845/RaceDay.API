using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages Organiser profiles. Only accessible to authenticated Organisers.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [AuthorizeRole("Organiser")]
    public class OrganisersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public OrganisersController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves the currently logged-in Organiser's profile.
        /// </summary>
        /// <returns>The Organiser's profile details.</returns>
        /// <response code="200">Returns the profile.</response>
        /// <response code="401">Not logged in as an Organiser.</response>
        /// <response code="404">Organiser profile not found.</response>
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organiser == null) return NotFound(new { Message = "Organiser profile not found." });

            return Ok(new
            {
                organiser.OrganiserId,
                organiser.OrganisationName,
                organiser.ContactPhone,
                organiser.IsVerified,
                organiser.User!.Email,
                organiser.User.FullName
            });
        }

        /// <summary>
        /// Updates the currently logged-in Organiser's profile.
        /// </summary>
        /// <param name="request">The updated organisation name and contact phone.</param>
        /// <returns>A success message.</returns>
        /// <response code="200">Profile updated successfully.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in as an Organiser.</response>
        /// <response code="404">Organiser profile not found.</response>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] OrganiserUpdateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organiser == null) return NotFound();

            organiser.OrganisationName = request.OrganisationName;
            organiser.ContactPhone = request.ContactPhone;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Profile updated successfully." });
        }

        /// <summary>
        /// Retrieves all active events created by a specific Organiser.
        /// </summary>
        /// <param name="id">The Organiser's ID.</param>
        /// <returns>A list of the Organiser's active events.</returns>
        /// <response code="200">Returns the list of events.</response>
        /// <response code="401">Not logged in.</response>
        [HttpGet("{id}/events")]
        [AuthorizeRole("Organiser", "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetOrganiserEvents(int id)
        {
            var events = await _context.Events
                .Where(e => e.OrganiserId == id && e.IsActive)
                .ToListAsync();
            return Ok(events);
        }
    }

    /// <summary>
    /// Data transfer object for updating an Organiser profile.
    /// </summary>
    public class OrganiserUpdateDto
    {
        /// <summary>The organisation name.</summary>
        public string OrganisationName { get; set; } = string.Empty;

        /// <summary>The contact phone number.</summary>
        public string ContactPhone { get; set; } = string.Empty;
    }
}