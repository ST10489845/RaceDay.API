using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Enrolments;
using RaceDay.API.Entities;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages event enrolments. Participants enrol in events and view their own enrolments; Organisers view enrolments for their events.
    /// </summary>
    [Route("api")]
    [ApiController]
    [Produces("application/json")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Enrols the currently logged-in participant into a specific event category.
        /// </summary>
        /// <remarks>
        /// Validates:
        /// - The event exists and is active.
        /// - The registration deadline has not passed.
        /// - The category belongs to the specified event.
        /// - The participant is not already enrolled in that category.
        /// - The category has not reached its maximum participant capacity.
        /// </remarks>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="request">The enrolment details (category ID).</param>
        /// <returns>A confirmation with the new enrolment ID.</returns>
        /// <response code="200">Enrolled successfully.</response>
        /// <response code="400">Registration deadline passed, category full, or already enrolled.</response>
        /// <response code="401">Not logged in as a Participant.</response>
        /// <response code="404">Event not found or inactive.</response>
        [HttpPost("events/{eventId}/enrolments")]
        [AuthorizeRole("Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EnrolInEvent(int eventId, [FromBody] EnrolmentCreateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserId == userId);
            if (participant == null) return Unauthorized(new { Message = "Participant profile not found." });

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null || !ev.IsActive)
                return NotFound(new { Message = "Event not found or is no longer active." });

            if (ev.RegistrationDeadline < DateTime.UtcNow)
                return BadRequest(new { Message = "Registration deadline has passed." });

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId);
            if (category == null)
                return BadRequest(new { Message = "Invalid category for this event." });

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.ParticipantId == participant.ParticipantId
                            && e.EventId == eventId
                            && e.CategoryId == request.CategoryId);
            if (alreadyEnrolled)
                return BadRequest(new { Message = "You are already enrolled in this category." });

            if (category.MaxParticipants.HasValue)
            {
                var currentCount = await _context.Enrolments
                    .CountAsync(e => e.CategoryId == request.CategoryId
                                  && e.Status != "Cancelled"
                                  && e.Status != "Withdrawn");
                if (currentCount >= category.MaxParticipants.Value)
                    return BadRequest(new { Message = "This category is full." });
            }

            var enrolment = new Enrolment
            {
                ParticipantId = participant.ParticipantId,
                CategoryId = request.CategoryId,
                EventId = eventId,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Pending",
                Paid = false
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Enrolled successfully.",
                EnrolmentId = enrolment.EnrolmentId,
                Status = enrolment.Status
            });
        }

        /// <summary>
        /// Retrieves all enrolments for the currently logged-in participant.
        /// </summary>
        /// <returns>A list of the participant's enrolments with event and category details.</returns>
        /// <response code="200">Returns the list of enrolments.</response>
        /// <response code="401">Not logged in as a Participant.</response>
        [HttpGet("enrolments/my-enrolments")]
        [AuthorizeRole("Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserId == userId);
            if (participant == null) return Unauthorized();

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantId == participant.ParticipantId)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.Status,
                    e.Paid,
                    e.BibNumber,
                    e.EnrolmentDate,
                    EventName = e.Event!.EventName,
                    EventDate = e.Event.EventDate,
                    CategoryName = e.Category!.CategoryName,
                    e.Category.DistanceKm
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        /// <summary>
        /// Retrieves all enrolments for a specific event.
        /// </summary>
        /// <remarks>
        /// Restricted to the Organiser who owns the event.
        /// </remarks>
        /// <param name="eventId">The ID of the event.</param>
        /// <returns>A list of enrolments for the event, with participant details.</returns>
        /// <response code="200">Returns the list of enrolments.</response>
        /// <response code="401">Not logged in as an Organiser.</response>
        /// <response code="403">You do not own this event.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("events/{eventId}/enrolments")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organiser == null) return Unauthorized();

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { Message = "Event not found." });

            if (ev.OrganiserId != organiser.OrganiserId)
                return StatusCode(403, new { Message = "You can only view enrolments for your own events." });

            var enrolments = await _context.Enrolments
                .Where(e => e.EventId == eventId)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.Status,
                    e.Paid,
                    e.BibNumber,
                    ParticipantName = e.Participant!.User!.FullName,
                    ParticipantEmail = e.Participant.User.Email,
                    CategoryName = e.Category!.CategoryName
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        /// <summary>
        /// Retrieves a single enrolment by its ID.
        /// </summary>
        /// <remarks>
        /// Only accessible by the owning Participant or the Organiser of the event.
        /// </remarks>
        /// <param name="id">The enrolment ID.</param>
        /// <returns>The enrolment details.</returns>
        /// <response code="200">Returns the enrolment.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Access denied.</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpGet("enrolments/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolmentById(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");
            if (userId == null) return Unauthorized();

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null) return NotFound(new { Message = "Enrolment not found." });

            if (role == "Participant")
            {
                if (enrolment.Participant!.UserId != userId)
                    return StatusCode(403, new { Message = "Access denied." });
            }
            else if (role == "Organiser")
            {
                var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
                if (enrolment.Event!.OrganiserId != organiser!.OrganiserId)
                    return StatusCode(403, new { Message = "Access denied." });
            }

            return Ok(enrolment);
        }

        /// <summary>
        /// Cancels the current participant's enrolment.
        /// </summary>
        /// <remarks>
        /// Cannot cancel an enrolment that is already Paid or Confirmed.
        /// </remarks>
        /// <param name="id">The ID of the enrolment to cancel.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Enrolment cancelled.</response>
        /// <response code="400">Cannot cancel a paid or confirmed enrolment.</response>
        /// <response code="401">Not logged in as a Participant.</response>
        /// <response code="403">You can only cancel your own enrolments.</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpDelete("enrolments/{id}")]
        [AuthorizeRole("Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserId == userId);
            if (participant == null) return Unauthorized();

            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null) return NotFound(new { Message = "Enrolment not found." });

            if (enrolment.ParticipantId != participant.ParticipantId)
                return StatusCode(403, new { Message = "You can only cancel your own enrolments." });

            if (enrolment.Status == "Paid" || enrolment.Status == "Confirmed")
                return BadRequest(new { Message = "You cannot cancel a paid or confirmed enrolment. Please contact the organiser." });

            enrolment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Enrolment cancelled." });
        }
    }
}