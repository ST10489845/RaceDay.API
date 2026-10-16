using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Entities
{
    public class Enrolment
    {
        [Key]
        public int EnrolmentId { get; set; }

        [Required]
        public int ParticipantId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int EventId { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Paid, Cancelled, Withdrawn

        public bool Paid { get; set; } = false;
        public int? BibNumber { get; set; }
        public TimeSpan? StartTime { get; set; }

        // Navigation Properties
        [ForeignKey("ParticipantId")]
        public Participant? Participant { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        public Result? Result { get; set; }
    }
}