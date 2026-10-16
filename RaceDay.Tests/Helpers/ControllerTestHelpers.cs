using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using System.Text;

namespace RaceDay.Tests.Helpers
{
    public static class ControllerTestHelpers
    {
        public static void SetSession(
            ControllerBase controller,
            int? userId,
            string? role = null,
            string? fullName = null,
            string? email = null)
        {
            var session = new FakeSession();

            if (userId.HasValue)
                session.SetInt32("UserId", userId.Value);
            if (!string.IsNullOrEmpty(role))
                session.SetString("Role", role);
            if (!string.IsNullOrEmpty(fullName))
                session.SetString("FullName", fullName);
            if (!string.IsNullOrEmpty(email))
                session.SetString("Email", email);

            var httpContext = new DefaultHttpContext
            {
                Session = session
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                ActionDescriptor = new ControllerActionDescriptor(),
                RouteData = new RouteData()
            };
        }
    }

    /// <summary>
    /// A minimal in-memory implementation of ISession for unit testing.
    /// Values are stored in a simple byte[] dictionary.
    /// </summary>
    public class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value)
        {
            return _store.TryGetValue(key, out value!);
        }
    }
}