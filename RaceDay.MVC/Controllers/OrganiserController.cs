using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly ApiService _apiService;

        public OrganiserController(ApiService apiService)
        {
            _apiService = apiService;
        }

        private bool IsOrganiser() => HttpContext.Session.GetString("UserRole") == "Organiser";

        // 1. DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            var response = await _apiService.GetAsync("api/events");
            if (!response.IsSuccessStatusCode) return View(new OrganiserDashboardViewModel());

            var events = await _apiService.ReadAsAsync<List<JsonElement>>(response);

            var dashboard = new OrganiserDashboardViewModel
            {
                TotalEvents = events.Count,
                UpcomingEvents = events.Select(e => new EventSummary
                {
                    EventId = e.GetProperty("eventId").GetInt32(),
                    EventName = e.GetProperty("eventName").GetString(),
                    EventDate = e.GetProperty("eventDate").GetDateTime(),
                    City = e.GetProperty("city").GetString()
                }).ToList()
            };

            return View(dashboard);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(EventViewModel model)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) return View(model);

            // 1. Upload Image first (if provided)
            if (model.BannerImage != null && model.BannerImage.Length > 0)
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = model.BannerImage.OpenReadStream();
                content.Add(new StreamContent(fileStream), "file", model.BannerImage.FileName);

                var uploadResponse = await _apiService.PostAsync("api/events/upload-image", content);
                if (uploadResponse.IsSuccessStatusCode)
                {
                    var json = await uploadResponse.Content.ReadAsStringAsync();
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    model.BannerImageUrl = doc.RootElement.GetProperty("url").GetString();
                }
            }

            // 2. Create Event (with the new image URL)
            var response = await _apiService.PostAsync("api/events", model);
            if (response.IsSuccessStatusCode) return RedirectToAction("Dashboard");

            ModelState.AddModelError("", "Failed to create event.");
            return View(model);
        }

        // 3. EDIT EVENT
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            var response = await _apiService.GetAsync($"api/events/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var ev = await _apiService.ReadAsAsync<JsonElement>(response);
            var model = new EventViewModel
            {
                EventId = ev.GetProperty("eventId").GetInt32(),
                EventName = ev.GetProperty("eventName").GetString(),
                EventDescription = ev.TryGetProperty("eventDescription", out var desc) ? desc.GetString() : null,
                EventDate = ev.GetProperty("eventDate").GetDateTime(),
                RegistrationDeadline = ev.GetProperty("registrationDeadline").GetDateTime(),
                Venue = ev.GetProperty("venue").GetString(),
                City = ev.GetProperty("city").GetString(),
                Province = ev.GetProperty("province").GetString()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditEvent(EventViewModel model)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) return View(model);

            var response = await _apiService.PutAsync($"api/events/{model.EventId}", model);
            if (response.IsSuccessStatusCode) return RedirectToAction("Dashboard");

            ModelState.AddModelError("", "Failed to update event.");
            return View(model);
        }

        // 4. DELETE EVENT
        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");
            await _apiService.DeleteAsync($"api/events/{id}");
            return RedirectToAction("Dashboard");
        }

        // 5. MANAGE CATEGORIES (GET)
        [HttpGet]
        public async Task<IActionResult> ManageCategories(int eventId)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            var response = await _apiService.GetAsync($"api/events/{eventId}/categories");
            var categories = new List<CategoryViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var json = await _apiService.ReadAsAsync<List<JsonElement>>(response);
                categories = json.Select(c => new CategoryViewModel
                {
                    CategoryId = c.GetProperty("categoryId").GetInt32(),
                    EventId = c.GetProperty("eventId").GetInt32(),
                    CategoryName = c.GetProperty("categoryName").GetString(),
                    CategoryType = c.GetProperty("categoryType").GetString(),
                    DistanceKm = c.GetProperty("distanceKm").GetDecimal(),
                    EntryFee = c.GetProperty("entryFee").GetDecimal(),
                    MaxParticipants = c.TryGetProperty("maxParticipants", out var mp) && mp.ValueKind != JsonValueKind.Null ? mp.GetInt32() : null
                }).ToList();
            }

            ViewBag.EventId = eventId;
            return View(categories);
        }

        // 6. CREATE CATEGORY (POST)
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CategoryViewModel model)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) return RedirectToAction("ManageCategories", new { eventId = model.EventId });

            var response = await _apiService.PostAsync($"api/events/{model.EventId}/categories", model);
            return RedirectToAction("ManageCategories", new { eventId = model.EventId });
        }

        // 7. VIEW ENROLMENTS (GET)
        [HttpGet]
        public async Task<IActionResult> ViewEnrolments(int eventId)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            var eventResponse = await _apiService.GetAsync($"api/events/{eventId}");
            var eventName = "Event Enrolments";
            if (eventResponse.IsSuccessStatusCode)
            {
                var ev = await _apiService.ReadAsAsync<JsonElement>(eventResponse);
                eventName = ev.GetProperty("eventName").GetString();
            }

            var model = new EventEnrolmentsViewModel { EventId = eventId, EventName = eventName };

            var response = await _apiService.GetAsync($"api/events/{eventId}/enrolments");
            if (response.IsSuccessStatusCode)
            {
                var json = await _apiService.ReadAsAsync<List<JsonElement>>(response);
                model.Enrolments = json.Select(e => new EnrolmentDetail
                {
                    EnrolmentId = e.GetProperty("enrolmentId").GetInt32(),
                    ParticipantName = e.GetProperty("participantName").GetString(),
                    CategoryName = e.GetProperty("categoryName").GetString(),
                    Status = e.GetProperty("status").GetString(),
                    HasResult = e.GetProperty("status").GetString() == "Confirmed" // Status changes to Confirmed when result is captured
                }).ToList();
            }

            return View(model);
        }

        // 8. CAPTURE RESULT (POST)
        [HttpPost]
        public async Task<IActionResult> CaptureResult(int eventId, int enrolmentId, string finishTime, int? overallRank, int? categoryRank)
        {
            if (!IsOrganiser()) return RedirectToAction("Login", "Auth");

            var request = new
            {
                EnrolmentId = enrolmentId,
                FinishTime = finishTime,
                OverallRank = overallRank,
                CategoryRank = categoryRank
            };

            var response = await _apiService.PostAsync("api/results", request);

            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "Result captured successfully!";
            else
                TempData["ErrorMessage"] = "Failed to capture result.";

            return RedirectToAction("ViewEnrolments", new { eventId = eventId });
        }
    }
}