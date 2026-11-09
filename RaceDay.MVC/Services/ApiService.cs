using System.Text;
using System.Text.Json;

namespace RaceDay.MVC.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpClient GetClient()
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            // Retrieve the session cookie saved during login
            var sessionCookie = _httpContextAccessor.HttpContext?.Session.GetString("ApiSessionCookie");
            if (!string.IsNullOrEmpty(sessionCookie))
            {
                client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
            }
            return client;
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            return await GetClient().GetAsync(endpoint);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await GetClient().PostAsync(endpoint, content);
        }

        public async Task<HttpResponseMessage> PutAsync(string endpoint, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await GetClient().PutAsync(endpoint, content);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            return await GetClient().DeleteAsync(endpoint);
        }

        // Helper to read JSON responses
        public async Task<T?> ReadAsAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content)) return default;
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}