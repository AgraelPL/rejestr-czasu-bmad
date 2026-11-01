using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Options;

namespace RejestrCzasu;

/// <summary>
/// <c>GET /health</c> (AD-20): anonymous, also during a required password change, checks the database through the
/// system-mode <see cref="AppDbContext"/> (AD-16) and reports the image tag. 200 when healthy, 503 when not, always as
/// <c>{"status":"...","tag":"..."}</c>. The deploy workflow (story 4.8) waits for it after <c>docker compose up</c>.
/// </summary>
public static class HealthEndpoint
{
    public const string Path = "/health";

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
        return services;
    }

    public static IEndpointRouteBuilder MapAppHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(Path, new HealthCheckOptions { ResponseWriter = WriteAsync })
            .AllowAnonymous()
            .AllowDuringPasswordChange();
        return endpoints;
    }

    /// <summary>The body of <c>/health</c>; status codes are the defaults (Unhealthy → 503).</summary>
    public sealed record HealthResponse(string Status, string Tag);

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var tag = context.RequestServices.GetRequiredService<IOptions<AppImageOptions>>().Value.Tag;
        // With a body and Content-Type, so UseStatusCodePagesWithReExecute leaves the 503 alone.
        return context.Response.WriteAsJsonAsync(new HealthResponse(report.Status.ToString(), tag));
    }
}
