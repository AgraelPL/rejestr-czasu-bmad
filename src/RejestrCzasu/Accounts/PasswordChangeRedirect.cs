using Microsoft.AspNetCore.StaticAssets;

namespace RejestrCzasu.Accounts;

/// <summary>
/// Marks an endpoint that a logged-in User with <c>must_change_password</c> may still reach (AD-15): the password
/// change page, logout, <c>/health</c> and later the anonymous help topics. On a component as an attribute, on a
/// Minimal API endpoint through <see cref="PasswordChangeRedirect.AllowDuringPasswordChange{TBuilder}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowedDuringPasswordChangeAttribute : Attribute;

/// <summary>
/// Middleware after authentication (AD-15): a logged-in User with <c>must_change_password</c> is redirected from every
/// page and endpoint to <see cref="AccountEndpoints.ChangePasswordPath"/>, except endpoints recognized by their
/// metadata: <c>MapStaticAssets</c> resources, the Blazor framework endpoints (<c>/_framework</c>, <c>/_blazor</c>)
/// and endpoints marked with <see cref="AllowedDuringPasswordChangeAttribute"/>.
/// </summary>
public static class PasswordChangeRedirect
{
    public static IApplicationBuilder UsePasswordChangeRedirect(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true
                && AccountCookieValidator.StateOf(context) is { MustChangePassword: true }
                && !IsAllowed(context.GetEndpoint()))
            {
                context.Response.Redirect(AccountEndpoints.ChangePasswordPath);
                return;
            }
            await next(context);
        });

    /// <summary>Marks a Minimal API endpoint as reachable during a required password change.</summary>
    public static TBuilder AllowDuringPasswordChange<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AllowedDuringPasswordChangeAttribute());

    internal static bool IsAllowed(Endpoint? endpoint)
    {
        if (endpoint is null)
        {
            return false;
        }
        if (endpoint.Metadata.GetMetadata<AllowedDuringPasswordChangeAttribute>() is not null
            || endpoint.Metadata.GetMetadata<StaticAssetDescriptor>() is not null)
        {
            return true;
        }
        // Endpoints of the Blazor framework itself (circuit hub, its negotiate/disconnect/initializers, opaque redirect),
        // which cannot carry our marker: recognized by the route pattern of the matched endpoint, not the request path.
        var pattern = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
        return pattern is not null
            && (pattern.StartsWith("/_blazor", StringComparison.Ordinal)
                || pattern.StartsWith("/_framework/", StringComparison.Ordinal));
    }
}
