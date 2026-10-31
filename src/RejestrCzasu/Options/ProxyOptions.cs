namespace RejestrCzasu.Options;

/// <summary>
/// The reverse proxy in front of the application (section <c>ForwardedHeaders</c>, AD-19): the subnet (CIDR) whose
/// <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> headers are trusted. On the server the compose network of Caddy,
/// set as <c>ForwardedHeaders__KnownNetwork</c> (story 4.8); without it only loopback is trusted.
/// </summary>
public sealed class ProxyOptions
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>A subnet in CIDR notation, e.g. <c>172.18.0.0/16</c>; empty means only the default loopback networks.</summary>
    public string? KnownNetwork { get; set; }
}
