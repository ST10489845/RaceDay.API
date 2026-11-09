namespace RaceDay.MVC.ViewModels
{
    public class OrganiserDashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int TotalEnrolments { get; set; }
        public List<EventSummary> UpcomingEvents { get; set; } = new();
    }

    public class EventSummary
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string City { get; set; } = string.Empty;
        public int EnrolmentCount { get; set; }
    }
}