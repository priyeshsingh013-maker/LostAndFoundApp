using Microsoft.AspNetCore.Identity;
using LostAndFoundApp.Models;

namespace LostAndFoundApp.Middleware
{
    /// <summary>
    /// Middleware that intercepts requests from authenticated local users who have MustChangePassword=true.
    /// Redirects them to the Change Password page and blocks access to all other pages until password is changed.
    /// AD-synced users are exempt from this check.
    /// </summary>
    public class MustChangePasswordMiddleware
    {
        private readonly RequestDelegate _next;

        public MustChangePasswordMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Allow access to change-password, logout, and static files without redirect loop
                var path = context.Request.Path.Value?.ToLower() ?? "";
                var allowedPaths = new[]
                {
                    "/account/changepassword",
                    "/account/logout",
                    "/lib/",
                    "/css/",
                    "/js/",
                    "/favicon"
                };

                bool isAllowed = allowedPaths.Any(p => path.StartsWith(p));

                if (!isAllowed)
                {
                    var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var user = await userManager.GetUserAsync(context.User);

                    if (user != null && !user.IsAdUser && user.MustChangePassword)
                    {
                        context.Response.Redirect("/Account/ChangePassword");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    public static class MustChangePasswordMiddlewareExtensions
    {
        public static IApplicationBuilder UseMustChangePassword(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MustChangePasswordMiddleware>();
        }
    }
}
