using CoreLayer.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;

namespace Ironer.Middleware
{
    public class RequirePhoneNumberAndAddressMiddleware
    {
        private readonly RequestDelegate _next;

        // Define allowed paths with their HTTP methods
        private static readonly Dictionary<string, string[]> AllowedPathsWithMethods = new()
        {
            ["/api/auth/send-verification-code"] = new[] { "POST" },
            ["/api/auth/verify-code"] = new[] { "POST" },
            ["/api/auth/google-login"] = new[] { "GET", "POST" },
            ["/api/auth/google-callback"] = new[] { "GET", "POST" },
            ["/api/auth/me"] = new[] { "GET" },
            ["/api/auth/refresh-token"] = new[] { "POST" },
            ["/api/auth/revoke-refresh"] = new[] { "POST" },
            ["/api/auth/logout"] = new[] { "POST" },
            ["/api/user/userPhone"] = new[] { "PUT" },
            ["/api/user/address"] = new[] { "GET", "PUT" }, // New endpoints for current user
        };

        // Convert paths to regex patterns for matching
        private static readonly Dictionary<Regex, string[]> AllowedRegexPatternsWithMethods;

        static RequirePhoneNumberAndAddressMiddleware()
        {
            AllowedRegexPatternsWithMethods = AllowedPathsWithMethods.ToDictionary(
                kvp =>
                {
                    var pattern = Regex.Escape(kvp.Key);
                    // Replace typed placeholders
                    pattern = Regex.Replace(pattern, @"\\\{[^}:]+:int\\\}", "[0-9]+"); // integers
                    pattern = Regex.Replace(pattern, @"\\\{[^}:]+:guid\\\}",
                        "[0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{12}"); // GUID
                    // Replace generic placeholders {something}
                    pattern = Regex.Replace(pattern, @"\\\{[^/]+\\\}", "[^/]+");
                    // Ensure full match
                    return new Regex("^" + pattern + "$", RegexOptions.IgnoreCase);
                },
                kvp => kvp.Value
            );
        }

        public RequirePhoneNumberAndAddressMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;

            // Check if the current path and method combination is allowed
            var isAllowed = AllowedRegexPatternsWithMethods.Any(kvp =>
                kvp.Key.IsMatch(path) && kvp.Value.Contains(method, StringComparer.OrdinalIgnoreCase));

            if (isAllowed)
            {
                await _next(context);
                return;
            }

            // Enforce phone number and address requirements for protected endpoints
            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                var missingRequirements = new List<string>();
                var missingActions = new List<string>();

                // Check phone number
                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    missingRequirements.Add("phone number");
                    missingActions.Add("update_phone");
                }

                // Check address - user must have an AddressId assigned
                if (user.AddressId == null)
                {
                    missingRequirements.Add("address");
                    missingActions.Add("update_address");
                }

                // If any requirements are missing, return 403
                if (missingRequirements.Any())
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = $"You must add a {string.Join(" and ", missingRequirements)} before using the API.",
                        requiredActions = missingActions
                    });
                    return;
                }
            }

            await _next(context);
        }
    }
}