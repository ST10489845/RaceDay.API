using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class EventViewModel
    {
        public int EventId { get; set; }

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

        // Add this property
        public IFormFile? BannerImage { get; set; }
        // And add this to hold the URL
        public string? BannerImageUrl { get; set; }
    }
}