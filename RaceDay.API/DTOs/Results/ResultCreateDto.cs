using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Results
{
    public class ResultCreateDto
    {
        [Required]
        public int EnrolmentId { get; set; }

        [Required]
        public TimeSpan FinishTime { get; set; }

        public int? OverallRank { get; set; }
        public int? CategoryRank { get; set; }
    }
}