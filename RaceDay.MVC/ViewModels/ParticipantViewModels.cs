namespace RaceDay.MVC.ViewModels
{
    public class EventListViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string OrganiserName { get; set; } = string.Empty;
    }

    public class EventDetailsViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public List<CategoryListViewModel> Categories { get; set; } = new();
    }

    public class CategoryListViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
    }

    public class MyEnrolmentViewModel
    {
        public int EnrolmentId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Paid { get; set; }
    }

    // Add these to the bottom of ParticipantViewModels.cs
    public class ParticipantResultViewModel
    {
        public int ResultId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public TimeSpan FinishTime { get; set; }
        public int? OverallRank { get; set; }
        public int? CategoryRank { get; set; }
    }

    public class EventEnrolmentsViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public List<EnrolmentDetail> Enrolments { get; set; } = new();
    }

    public class EnrolmentDetail
    {
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool HasResult { get; set; }
    }
}