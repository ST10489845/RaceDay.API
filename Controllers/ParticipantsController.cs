using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Manages Participant profiles and history. Only accessible to authenticated Participants.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [AuthorizeRole("Participant")]
    public class ParticipantsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ParticipantsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves the currently logged-in Participant's profile.
        /// </summary>
        /// <returns>The Participant's profile details.</returns>
        /// <response code="200">Returns the profile.</response>
        /// <response code="401">Not logged in as a Participant.</response>
        /// <response code="404">Participant profile not found.</response>
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var participant = await _context.Participants
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (participant == null) return NotFound(new { Message = "Participant profile not found." });

            return Ok(new
            {
                participant.ParticipantId,
                participant.DateOfBirth,
                participant.Gender,
                participant.EmergencyContactName,
                participant.EmergencyContactPhone,
                participant.MedicalConditions,
                participant.User!.Email,
                participant.User.FullName
            });
        }

        /// <summary>
        /// Updates the currently logged-in Participant's profile.
        /// </summary>
        /// <param name="request">The updated participant details.</param>
        /// <returns>A success message.</returns>
        /// <response code="200">Profile updated successfully.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in as a Participant.</response>
        /// <response code="404">Participant profile not found.</response>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] ParticipantUpdateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserId == userId);
            if (participant == null) return NotFound();

            participant.DateOfBirth = request.DateOfBirth;
            participant.Gender = request.Gender;
            participant.EmergencyContactName = request.EmergencyContactName;
            participant.EmergencyContactPhone = request.EmergencyContactPhone;
            participant.MedicalConditions = request.MedicalConditions;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Profile updated successfully." });
        }

        /// <summary>
        /// Retrieves the currently logged-in Participant's enrolment and results history.
        /// </summary>
        /// <returns>A list of enrolments, including any associated results.</returns>
        /// <response code="200">Returns the history.</response>
        /// <response code="401">Not logged in as a Participant.</response>
        /// <response code="404">Participant profile not found.</response>
        [HttpGet("me/history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserId == userId);
            if (participant == null) return NotFound();

            var history = await _context.Enrolments
                .Where(e => e.ParticipantId == participant.ParticipantId)
                .Select(e => new
                {
                    e.EnrolmentId,
                    EventName = e.Event!.EventName,
                    EventDate = e.Event.EventDate,
                    CategoryName = e.Category!.CategoryName,
                    e.Status,
                    Result = e.Result == null ? null : new
                    {
                        e.Result.FinishTime,
                        e.Result.OverallRank,
                        e.Result.CategoryRank
                    }
                })
                .ToListAsync();

            return Ok(history);
        }
    }

    /// <summary>
    /// Data transfer object for updating a Participant profile.
    /// </summary>
    public class ParticipantUpdateDto
    {
        /// <summary>The participant's date of birth.</summary>
        public DateTime DateOfBirth { get; set; }

        /// <summary>The participant's gender.</summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>The emergency contact's name.</summary>
        public string EmergencyContactName { get; set; } = string.Empty;

        /// <summary>The emergency contact's phone number.</summary>
        public string EmergencyContactPhone { get; set; } = string.Empty;

        /// <summary>Optional medical conditions to be aware of.</summary>
        public string? MedicalConditions { get; set; }
    }
}