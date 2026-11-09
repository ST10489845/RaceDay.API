using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ApiService _apiService;

        public ParticipantController(ApiService apiService)
        {
            _apiService = apiService;
        }

        private bool IsParticipant() => HttpContext.Session.GetString("UserRole") == "Participant";

        // 1. BROWSE ALL EVENTS (Home)
        public async Task<IActionResult> Dashboard()
        {
            if (!IsParticipant()) return RedirectToAction("Login", "Auth");

            var response = await _apiService.GetAsync("api/events");
            var events = new List<EventListViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var json = await _apiService.ReadAsAsync<List<JsonElement>>(response);
                events = json.Select(e => new EventListViewModel
                {
                    EventId = e.GetProperty("eventId").GetInt32(),
                    EventName = e.GetProperty("eventName").GetString(),
                    EventDescription = e.TryGetProperty("eventDescription", out var d) ? d.GetString() : "",
                    EventDate = e.GetProperty("eventDate").GetDateTime(),
                    Venue = e.GetProperty("venue").GetString(),
                    City = e.GetProperty("city").GetString(),
                    OrganiserName = e.GetProperty("organiserName").GetString()
                }).ToList();
            }

            return View(events);
        }

        // 2. EVENT DETAILS (To see categories and enrol)
        [HttpGet]
        public async Task<IActionResult> EventDetails(int id)
        {
            if (!IsParticipant()) return RedirectToAction("Login", "Auth");

            var response = await _apiService.GetAsync($"api/events/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var ev = await _apiService.ReadAsAsync<JsonElement>(response);

            var model = new EventDetailsViewModel
            {
                EventId = ev.GetProperty("eventId").GetInt32(),
                EventName = ev.GetProperty("eventName").GetString(),
                EventDescription = ev.TryGetProperty("eventDescription", out var d) ? d.GetString() : "",
                EventDate = ev.GetProperty("eventDate").GetDateTime(),
                RegistrationDeadline = ev.GetProperty("registrationDeadline").GetDateTime(),
                Venue = ev.GetProperty("venue").GetString(),
                City = ev.GetProperty("city").GetString()
            };

            if (ev.TryGetProperty("categories", out var cats))
            {
                foreach (var c in cats.EnumerateArray())
                {
                    model.Categories.Add(new CategoryListViewModel
                    {
                        CategoryId = c.GetProperty("categoryId").GetInt32(),
                        CategoryName = c.GetProperty("categoryName").GetString(),
                        DistanceKm = c.GetProperty("distanceKm").GetDecimal(),
                        EntryFee = c.GetProperty("entryFee").GetDecimal()
                    });
                }
            }

            return View(model);
        }

        // 3. ENROL IN EVENT (POST)
        [HttpPost]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            if (!IsParticipant()) return RedirectToAction("Login", "Auth");

            var request = new { CategoryId = categoryId };
            var response = await _apiService.PostAsync($"api/events/{eventId}/enrolments", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "You have successfully enrolled!";
                return RedirectToAction("MyEnrolments");
            }

            // Read the EXACT error message from the API
            var errorJson = await response.Content.ReadAsStringAsync();
            try
            {
                var errorDoc = System.Text.Json.JsonDocument.Parse(errorJson);
                TempData["ErrorMessage"] = errorDoc.RootElement.GetProperty("message").GetString();
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to enrol. Please try again.";
            }

            return RedirectToAction("EventDetails", new { id = eventId });
        }

        // 4. MY ENROLMENTS
        public async Task<IActionResult> MyEnrolments()
        {
            if (!IsParticipant()) return RedirectToAction("Login", "Auth");

            var response = await _apiService.GetAsync("api/enrolments/my-enrolments");
            var enrolments = new List<MyEnrolmentViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var json = await _apiService.ReadAsAsync<List<JsonElement>>(response);
                enrolments = json.Select(e => new MyEnrolmentViewModel
                {
                    EnrolmentId = e.GetProperty("enrolmentId").GetInt32(),
                    EventName = e.GetProperty("eventName").GetString(),
                    EventDate = e.GetProperty("eventDate").GetDateTime(),
                    CategoryName = e.GetProperty("categoryName").GetString(),
                    DistanceKm = e.GetProperty("distanceKm").GetDecimal(),
                    Status = e.GetProperty("status").GetString(),
                    Paid = e.GetProperty("paid").GetBoolean()
                }).ToList();
            }

            return View(enrolments);
        }

        // 5. MY RESULTS
        public async Task<IActionResult> MyResults()
        {
            if (!IsParticipant()) return RedirectToAction("Login", "Auth");

            var response = await _apiService.GetAsync("api/results/my-results");
            var results = new List<ParticipantResultViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var json = await _apiService.ReadAsAsync<List<JsonElement>>(response);
                results = json.Select(r => new ParticipantResultViewModel
                {
                    ResultId = r.GetProperty("resultId").GetInt32(),
                    EventName = r.GetProperty("eventName").GetString(),
                    EventDate = r.GetProperty("eventDate").GetDateTime(),
                    CategoryName = r.GetProperty("categoryName").GetString(),
                    DistanceKm = r.GetProperty("distanceKm").GetDecimal(),
                    FinishTime = r.GetProperty("finishTime").GetString() != null ? TimeSpan.Parse(r.GetProperty("finishTime").GetString()!) : TimeSpan.Zero,
                    OverallRank = r.TryGetProperty("overallRank", out var or) && or.ValueKind != JsonValueKind.Null ? or.GetInt32() : null,
                    CategoryRank = r.TryGetProperty("categoryRank", out var cr) && cr.ValueKind != JsonValueKind.Null ? cr.GetInt32() : null
                }).ToList();
            }

            return View(results);
        }
    }
}