using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class ResultViewModel
    {
        [Required]
        public int EnrolmentId { get; set; }

        [Required]
        public TimeSpan FinishTime { get; set; }

        public int? OverallRank { get; set; }
        public int? CategoryRank { get; set; }
    }
}