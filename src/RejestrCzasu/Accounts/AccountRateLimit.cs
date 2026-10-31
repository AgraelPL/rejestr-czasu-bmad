using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RejestrCzasu.Accounts;

/// <summary>
/// Limit of login and registration attempts (AD-15): 10 POST requests per minute per IP address, shared by both
/// pages (<c>[EnableRateLimiting(AccountRateLimit.PolicyName)]</c>). Other methods are not limited.
/// </summary>
public static class AccountRateLimit
{
    public const string PolicyName = "konto";
    public const int PermitLimit = 10;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public const string RejectedMessage = "Zbyt wiele prób w krótkim czasie. Odczekaj minutę i spróbuj ponownie.";

    public static IServiceCollection AddAccountRateLimit(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(PolicyName, context => HttpMethods.IsPost(context.Request.Method)
                ? RateLimitPartition.GetFixedWindowLimiter(
                    // The client address; behind the proxy it comes from UseForwardedHeaders (AD-19), placed before the limiter.
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = PermitLimit,
                        Window = Window,
                        QueueLimit = 0,
                    })
                : RateLimitPartition.GetNoLimiter("not-post"));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await context.HttpContext.Response.WriteAsync(RejectedMessage, cancellationToken);
            };
        });
}
