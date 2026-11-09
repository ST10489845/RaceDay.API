using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models.Auth;
using RaceDay.MVC.Services;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiService _apiService;

        public AuthController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Call the Part 2 API
            var response = await _apiService.PostAsync("api/auth/login", model);

            if (response.IsSuccessStatusCode)
            {
                // 1. Extract the API Session Cookie
                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    var sessionCookie = cookies.FirstOrDefault(c => c.Contains(".RaceDay.Session"));
                    if (sessionCookie != null)
                    {
                        // Save the API cookie in the MVC Session
                        HttpContext.Session.SetString("ApiSessionCookie", sessionCookie.Split(';')[0]);
                    }
                }

                // 2. Read the response to get the User's Role
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("user", out var userElement) &&
                    userElement.TryGetProperty("role", out var roleElement))
                {
                    string role = roleElement.GetString() ?? "Participant";
                    HttpContext.Session.SetString("UserRole", role);

                    // 3. Redirect based on role
                    if (role == "Organiser")
                    {
                        return RedirectToAction("Dashboard", "Organiser");
                    }
                    else
                    {
                        // We will build the Participant dashboard in Phase 3
                        return RedirectToAction("Index", "Home");
                    }
                }

                // Fallback if JSON parsing fails
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var response = await _apiService.PostAsync("api/auth/register", model);

            if (response.IsSuccessStatusCode)
            {
                // Registration successful, redirect to login
                return RedirectToAction("Login");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Registration failed: {errorContent}");
            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}