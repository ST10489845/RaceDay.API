using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Entities
{
    public class Result
    {
        [Key]
        public int ResultId { get; set; }

        [Required]
        public int EnrolmentId { get; set; }

        [Required]
        public int ParticipantId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public TimeSpan FinishTime { get; set; }

        public int? OverallRank { get; set; }
        public int? CategoryRank { get; set; }
        public bool IsVerified { get; set; } = false;
        public int? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }

        // Navigation Properties
        [ForeignKey("EnrolmentId")]
        public Enrolment? Enrolment { get; set; }

        [ForeignKey("ParticipantId")]
        public Participant? Participant { get; set; }

        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [ForeignKey("VerifiedBy")]
        public Organiser? Verifier { get; set; }
    }
}