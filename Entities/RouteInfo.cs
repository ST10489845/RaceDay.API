using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Entities
{
    public class RouteInfo
    {
        [Key]
        public int RouteId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RouteName { get; set; } = string.Empty;

        public string? RouteDescription { get; set; }

        [Required]
        [MaxLength(200)]
        public string StartPoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string EndPoint { get; set; } = string.Empty;

        public int? ElevationGain { get; set; }
        public int? ElevationLoss { get; set; }

        public string? RouteGpx { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("EventId")]
        public Event? Event { get; set; }
    }
}