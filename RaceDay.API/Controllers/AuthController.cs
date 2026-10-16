using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Auth;
using RaceDay.API.Entities;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Handles user registration, login, logout, and session management.
    /// Authentication is session-based: after a successful login, a session cookie is set and used for all subsequent authenticated requests.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public AuthController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registers a new user account (Organiser or Participant).
        /// </summary>
        /// <remarks>
        /// The password is hashed using BCrypt before being stored. A role-specific profile (Organiser or Participant) is automatically created.
        /// </remarks>
        /// <param name="request">The registration details (email, password, full name, role).</param>
        /// <returns>A success message if registration is successful.</returns>
        /// <response code="200">User registered successfully.</response>
        /// <response code="400">Email already exists, or the request is invalid.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { Message = "A user with this email already exists." });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                FullName = request.FullName,
                Role = request.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (request.Role == "Organiser")
            {
                _context.Organisers.Add(new Organiser
                {
                    UserId = user.UserId,
                    OrganisationName = request.FullName + "'s Organisation",
                    ContactPhone = "0000000000",
                    IsVerified = false
                });
            }
            else if (request.Role == "Participant")
            {
                _context.Participants.Add(new Participant
                {
                    UserId = user.UserId,
                    DateOfBirth = DateTime.UtcNow,
                    Gender = "Other",
                    EmergencyContactName = "Not Provided",
                    EmergencyContactPhone = "0000000000"
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "User registered successfully." });
        }

        /// <summary>
        /// Authenticates a user and starts a session.
        /// </summary>
        /// <remarks>
        /// On success, the user's ID, role, name, and email are stored in the session. All subsequent requests must include the session cookie.
        /// </remarks>
        /// <param name="request">The login credentials (email and password).</param>
        /// <returns>User details and a session cookie.</returns>
        /// <response code="200">Login successful.</response>
        /// <response code="401">Invalid email/password, or the account is inactive.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { Message = "Invalid email or password." });

            if (!user.IsActive)
                return Unauthorized(new { Message = "This account has been deactivated." });

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Email", user.Email);

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Login successful",
                User = new UserResponseDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role
                }
            });
        }

        /// <summary>
        /// Retrieves the currently logged-in user's details from the session.
        /// </summary>
        /// <returns>The current user's ID, email, name, and role.</returns>
        /// <response code="200">Returns the current user.</response>
        /// <response code="401">No active session found.</response>
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized(new { Message = "Not logged in." });

            return Ok(new UserResponseDto
            {
                UserId = userId.Value,
                Role = HttpContext.Session.GetString("Role") ?? string.Empty,
                FullName = HttpContext.Session.GetString("FullName") ?? string.Empty,
                Email = HttpContext.Session.GetString("Email") ?? string.Empty
            });
        }

        /// <summary>
        /// Logs the user out by clearing their session.
        /// </summary>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Logout successful.</response>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { Message = "Logged out successfully." });
        }
    }
}