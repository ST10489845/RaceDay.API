using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Entities
{
    public class Organiser
    {
        [Key]
        public int OrganiserId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string OrganisationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        public bool IsVerified { get; set; } = false;

        // Navigation Properties
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<Result> VerifiedResults { get; set; } = new List<Result>();
    }
}