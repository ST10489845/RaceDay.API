using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Events;
using RaceDay.API.Entities;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages RaceDay events. Organisers create and manage their own events; the public can browse active events.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all active events.
        /// </summary>
        /// <returns>A list of active events with organiser names.</returns>
        /// <response code="200">Returns the list of events.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Where(e => e.IsActive)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.EventDescription,
                    e.EventDate,
                    e.RegistrationDeadline,
                    e.Venue,
                    e.City,
                    e.Province,
                    OrganiserName = e.Organiser!.OrganisationName
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Retrieves a single event by its ID, including its categories.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <returns>The event details with its categories.</returns>
        /// <response code="200">Returns the event.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _context.Events
                .Where(e => e.EventId == id)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.EventDescription,
                    e.EventDate,
                    e.RegistrationDeadline,
                    e.Venue,
                    e.City,
                    e.Province,
                    e.IsActive,
                    OrganiserName = e.Organiser!.OrganisationName,
                    Categories = e.Categories.Select(c => new { c.CategoryId, c.CategoryName, c.DistanceKm, c.EntryFee })
                })
                .FirstOrDefaultAsync();

            if (ev == null) return NotFound(new { Message = "Event not found." });
            return Ok(ev);
        }

        /// <summary>
        /// Creates a new event. Restricted to Organisers.
        /// </summary>
        /// <remarks>The organiser is automatically determined from the current session.</remarks>
        /// <param name="request">The event details.</param>
        /// <returns>The newly created event.</returns>
        /// <response code="201">Event created successfully.</response>
        /// <response code="400">Invalid data (e.g., registration deadline after event date).</response>
        /// <response code="401">Not logged in as an Organiser.</response>
        [HttpPost]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organiser == null) return Unauthorized(new { Message = "Organiser profile not found." });

            if (request.RegistrationDeadline > request.EventDate)
                return BadRequest(new { Message = "Registration deadline cannot be after the event date." });

            var newEvent = new Event
            {
                OrganiserId = organiser.OrganiserId,
                EventName = request.EventName,
                EventDescription = request.EventDescription,
                EventDate = request.EventDate,
                RegistrationDeadline = request.RegistrationDeadline,
                Venue = request.Venue,
                City = request.City,
                Province = request.Province,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEventById), new { id = newEvent.EventId }, newEvent);
        }

        /// <summary>
        /// Updates an existing event. Restricted to the Organiser who owns it.
        /// </summary>
        /// <param name="id">The ID of the event to update.</param>
        /// <param name="request">The updated event details.</param>
        /// <returns>A success message.</returns>
        /// <response code="200">Event updated.</response>
        /// <response code="403">You do not own this event.</response>
        /// <response code="404">Event not found.</response>
        [HttpPut("{id}")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventCreateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organiser == null) return Unauthorized();

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null) return NotFound(new { Message = "Event not found." });

            if (existingEvent.OrganiserId != organiser.OrganiserId)
                return StatusCode(403, new { Message = "You can only modify events you created." });

            existingEvent.EventName = request.EventName;
            existingEvent.EventDescription = request.EventDescription;
            existingEvent.EventDate = request.EventDate;
            existingEvent.RegistrationDeadline = request.RegistrationDeadline;
            existingEvent.Venue = request.Venue;
            existingEvent.City = request.City;
            existingEvent.Province = request.Province;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Event updated successfully." });
        }

        /// <summary>
        /// Deletes an event. Restricted to the Organiser who owns it.
        /// </summary>
        /// <param name="id">The ID of the event to delete.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Event deleted.</response>
        /// <response code="403">You do not own this event.</response>
        /// <response code="404">Event not found.</response>
        [HttpDelete("{id}")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organiser == null) return Unauthorized();

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null) return NotFound(new { Message = "Event not found." });

            if (existingEvent.OrganiserId != organiser.OrganiserId)
                return StatusCode(403, new { Message = "You can only delete events you created." });

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Retrieves upcoming events (events whose date is in the future).
        /// </summary>
        /// <returns>A list of upcoming events sorted by date.</returns>
        /// <response code="200">Returns the list of upcoming events.</response>
        [HttpGet("upcoming")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingEvents()
        {
            var events = await _context.Events
                .Where(e => e.IsActive && e.EventDate > DateTime.UtcNow)
                .OrderBy(e => e.EventDate)
                .ToListAsync();
            return Ok(events);
        }
    }
}