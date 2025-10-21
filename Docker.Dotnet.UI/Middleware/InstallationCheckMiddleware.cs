using Docker.Dotnet.UI.Database.Models;
using Microsoft.AspNetCore.Identity;

namespace Docker.Dotnet.UI.Middleware;

public class InstallationCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InstallationCheckMiddleware> _logger;

    public InstallationCheckMiddleware(RequestDelegate next, ILogger<InstallationCheckMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        
        // Skip middleware for static files, API endpoints (including Install), and install page itself
        if (path.StartsWith("/api/") || 
            path.StartsWith("/_framework") ||
            path.StartsWith("/_content") ||
            path.StartsWith("/account/install") ||
            path.Contains("."))  // static files with extensions
        {
            await _next(context);
            return;
        }

        // Check if any users exist
        var hasUsers = userManager.Users.Any();
        
        if (!hasUsers)
        {
            _logger.LogInformation("No users found, redirecting to install page");
            context.Response.Redirect("/Account/Install");
            return;
        }

        await _next(context);
    }
}
