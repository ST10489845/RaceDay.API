using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Entities
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        public int OrganiserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventName { get; set; } = string.Empty;

        public string? EventDescription { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public DateTime RegistrationDeadline { get; set; }

        [Required]
        [MaxLength(200)]
        public string Venue { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Province { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrganiserId")]
        public Organiser? Organiser { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
        public ICollection<Result> Results { get; set; } = new List<Result>();
        public ICollection<WeatherInfo> WeatherInfos { get; set; } = new List<WeatherInfo>();
        public ICollection<RouteInfo> RouteInfos { get; set; } = new List<RouteInfo>();
    }
}