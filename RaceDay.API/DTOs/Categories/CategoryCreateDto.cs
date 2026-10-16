using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Categories
{
    public class CategoryCreateDto
    {
        [Required, MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        [Required, RegularExpression("^(Running|Walking|Cycling)$")]
        public string CategoryType { get; set; } = string.Empty;

        [Required, Range(0.01, 999.99)]
        public decimal DistanceKm { get; set; }

        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }

        [RegularExpression("^(Male|Female)$")]
        public string? GenderRestriction { get; set; }

        [Required, Range(0, 100000)]
        public decimal EntryFee { get; set; }

        public int? MaxParticipants { get; set; }
    }
}