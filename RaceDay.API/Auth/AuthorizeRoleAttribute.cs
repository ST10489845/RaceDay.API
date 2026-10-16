using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.API.Auth
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userRole = context.HttpContext.Session.GetString("Role");

            // 1. Check if user is logged in
            if (string.IsNullOrEmpty(userRole))
            {
                context.Result = new UnauthorizedObjectResult(new { Message = "Unauthorized: Please log in." });
                return;
            }

            // 2. Check if the user's role is allowed
            if (!_roles.Contains(userRole))
            {
                context.Result = new ObjectResult(new { Message = $"Forbidden: Requires one of the following roles: {string.Join(", ", _roles)}" })
                {
                    StatusCode = 403 // Forbidden
                };
            }
        }
    }
}