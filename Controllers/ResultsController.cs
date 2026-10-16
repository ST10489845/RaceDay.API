using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Results;
using RaceDay.API.Entities;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages race results. Organisers capture finish times; Participants view their own results; the public can view verified event results.
    /// </summary>
    [Route("api")]
    [ApiController]
    [Produces("application/json")]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Captures a finish result for an enrolment.
        /// </summary>
        /// <remarks>
        /// Restricted to the Organiser who owns the event. Marks the result as verified and updates the enrolment status to Confirmed.
        /// </remarks>
        /// <param name="request">The result details (enrolment ID, finish time, ranks).</param>
        /// <returns>A confirmation with the new result ID.</returns>
        /// <response code="200">Result captured successfully.</response>
        /// <response code="400">A result already exists for this enrolment.</response>
        /// <response code="401">Not logged in as an Organiser.</response>
        /// <response code="403">You do not own this event.</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpPost("results")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CaptureResult([FromBody] ResultCreateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organiser == null) return Unauthorized();

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);

            if (enrolment == null)
                return NotFound(new { Message = "Enrolment not found." });

            if (enrolment.Event!.OrganiserId != organiser.OrganiserId)
                return StatusCode(403, new { Message = "You can only capture results for your own events." });

            var resultExists = await _context.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId);
            if (resultExists)
                return BadRequest(new { Message = "A result already exists for this enrolment." });

            var result = new Result
            {
                EnrolmentId = enrolment.EnrolmentId,
                ParticipantId = enrolment.ParticipantId,
                EventId = enrolment.EventId,
                CategoryId = enrolment.CategoryId,
                FinishTime = request.FinishTime,
                OverallRank = request.OverallRank,
                CategoryRank = request.CategoryRank,
                IsVerified = true,
                VerifiedBy = organiser.OrganiserId,
                VerifiedAt = DateTime.UtcNow
            };

            _context.Results.Add(result);
            enrolment.Status = "Confirmed";

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Result captured successfully.", ResultId = result.ResultId });
        }

        /// <summary>
        /// Retrieves all results for the currently logged-in participant.
        /// </summary>
        /// <returns>A list of the participant's results with event and category details.</returns>
        /// <response code="200">Returns the list of results.</response>
        /// <response code="401">Not logged in as a Participant.</response>
        [HttpGet("results/my-results")]
        [AuthorizeRole("Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserId == userId);
            if (participant == null) return Unauthorized();

            var results = await _context.Results
                .Where(r => r.ParticipantId == participant.ParticipantId)
                .Select(r => new
                {
                    r.ResultId,
                    EventName = r.Event!.EventName,
                    EventDate = r.Event.EventDate,
                    CategoryName = r.Category!.CategoryName,
                    DistanceKm = r.Category.DistanceKm,
                    r.FinishTime,
                    r.OverallRank,
                    r.CategoryRank,
                    r.IsVerified
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Retrieves all verified results for a specific event.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <returns>A list of verified results ordered by overall rank.</returns>
        /// <response code="200">Returns the list of results.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("events/{eventId}/results")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResultsForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists) return NotFound(new { Message = "Event not found." });

            var results = await _context.Results
                .Where(r => r.EventId == eventId && r.IsVerified)
                .OrderBy(r => r.OverallRank)
                .Select(r => new
                {
                    r.ResultId,
                    ParticipantName = r.Participant!.User!.FullName,
                    CategoryName = r.Category!.CategoryName,
                    r.FinishTime,
                    r.OverallRank,
                    r.CategoryRank
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Retrieves a single result by its ID.
        /// </summary>
        /// <param name="id">The result ID.</param>
        /// <returns>The result details.</returns>
        /// <response code="200">Returns the result.</response>
        /// <response code="404">Result not found.</response>
        [HttpGet("results/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResultById(int id)
        {
            var result = await _context.Results
                .Include(r => r.Participant).ThenInclude(p => p!.User)
                .Include(r => r.Category)
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null) return NotFound(new { Message = "Result not found." });

            return Ok(new
            {
                result.ResultId,
                ParticipantName = result.Participant!.User!.FullName,
                EventName = result.Event!.EventName,
                CategoryName = result.Category!.CategoryName,
                result.FinishTime,
                result.OverallRank,
                result.CategoryRank,
                result.IsVerified
            });
        }
    }
}