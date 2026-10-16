using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Events
{
    public class EventCreateDto
    {
        [Required, MaxLength(100)]
        public string EventName { get; set; } = string.Empty;

        public string? EventDescription { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public DateTime RegistrationDeadline { get; set; }

        [Required, MaxLength(200)]
        public string Venue { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Province { get; set; } = string.Empty;
    }
}