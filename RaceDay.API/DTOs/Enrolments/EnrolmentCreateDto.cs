using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Enrolments
{
    public class EnrolmentCreateDto
    {
        [Required]
        public int CategoryId { get; set; }
    }
}