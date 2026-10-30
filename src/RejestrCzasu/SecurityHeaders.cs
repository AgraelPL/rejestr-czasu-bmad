namespace RejestrCzasu;

public static class SecurityHeaders
{
    /// <summary>Only local resources (AD-12, NFR-1); no inline or external scripts.</summary>
    public const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self'; " +
        "img-src 'self' data:; font-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
}
