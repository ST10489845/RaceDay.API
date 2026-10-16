using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be either 'Organiser' or 'Participant'.")]
        public string Role { get; set; } = string.Empty;
    }
}