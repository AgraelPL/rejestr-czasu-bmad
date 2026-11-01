using System.Security.Claims;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Pomoc.Topics;

namespace RejestrCzasu.Features.Pomoc;

/// <summary>Who may read a help topic (AD-22).</summary>
public enum HelpAccess
{
    /// <summary>Everybody, also without logging in; rendered in <c>AccountLayout</c>.</summary>
    Anonymous,

    /// <summary>A logged-in User; rendered in <c>MainLayout</c>.</summary>
    LoggedIn,

    /// <summary>A logged-in User with the role Administrator; rendered in <c>MainLayout</c>.</summary>
    Administrator,
}

/// <summary>What the help page shows for a topic and a User.</summary>
public enum HelpDecision
{
    Allowed,

    /// <summary>An anonymous visitor and a non-public topic: the login page.</summary>
    LoginRequired,

    /// <summary>A logged-in User without the required role: the same view as <c>/konto/brak-dostepu</c>.</summary>
    Denied,
}

/// <summary>One help topic: the key is the route segment of its screen in kebab-case (AD-22).</summary>
public sealed record HelpTopic(string Key, string Title, Type Content, HelpAccess Access);

/// <summary>
/// The only list of help topics (AD-22): the route <c>/pomoc/{temat}</c> and the index <c>/pomoc</c> are generated
/// from it. A story changing a screen updates its topic here in the same change.
/// </summary>
public static class HelpTopics
{
    public const string Start = "start";

    public static IReadOnlyList<HelpTopic> All { get; } =
    [
        new(Start, "Strona startowa i menu", typeof(StartHelp), HelpAccess.LoggedIn),
    ];

    /// <summary>The topic with the key (case-insensitive), or <c>null</c>.</summary>
    public static HelpTopic? Find(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : All.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));

    public static HelpDecision Decide(HelpTopic topic, ClaimsPrincipal user)
    {
        if (topic.Access == HelpAccess.Anonymous)
        {
            return HelpDecision.Allowed;
        }
        if (user.Identity?.IsAuthenticated != true)
        {
            return HelpDecision.LoginRequired;
        }
        return topic.Access == HelpAccess.Administrator && !user.IsInRole(AppRoles.Administrator)
            ? HelpDecision.Denied
            : HelpDecision.Allowed;
    }

    /// <summary>The topics the User may read, <see cref="Start"/> first, the rest in list order.</summary>
    public static IReadOnlyList<HelpTopic> AvailableTo(ClaimsPrincipal user, IEnumerable<HelpTopic>? topics = null) =>
        (topics ?? All)
            .Where(t => Decide(t, user) == HelpDecision.Allowed)
            .OrderBy(t => t.Key == Start ? 0 : 1)
            .ToList();
}
