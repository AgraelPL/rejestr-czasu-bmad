namespace RejestrCzasu.Accounts;

/// <summary>Redirect target after login: only a local path of this application, otherwise <c>/</c>.</summary>
public static class LocalUrl
{
    public static string OrHome(string? url) => IsLocal(url) ? url! : "/";

    /// <summary>
    /// A path starting with a single <c>/</c> (not <c>//</c> or <c>/\</c>, which browsers treat as another host)
    /// and without control characters; the same rule as <c>IUrlHelper.IsLocalUrl</c> minus <c>~/</c>.
    /// </summary>
    public static bool IsLocal(string? url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/')
        {
            return false;
        }
        if (url.Length > 1 && url[1] is '/' or '\\')
        {
            return false;
        }
        return !url.Any(char.IsControl);
    }
}
