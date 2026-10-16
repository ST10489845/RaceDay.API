using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Entities
{
    public class WeatherInfo
    {
        [Key]
        public int WeatherId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public DateTime ForecastDate { get; set; }

        public int? TemperatureHigh { get; set; }
        public int? TemperatureLow { get; set; }

        [MaxLength(50)]
        public string? Conditions { get; set; }

        public int? WindSpeed { get; set; }
        public int? Humidity { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("EventId")]
        public Event? Event { get; set; }
    }
}