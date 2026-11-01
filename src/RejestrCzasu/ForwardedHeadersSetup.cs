using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using RejestrCzasu.Options;

namespace RejestrCzasu;

/// <summary>
/// The application behind Caddy (AD-19): <c>UseForwardedHeaders</c> takes the client address and scheme from
/// <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c>, but only from a connection inside the trusted networks:
/// the default loopback plus the subnet from <see cref="ProxyOptions.KnownNetwork"/>. <c>KnownProxies</c> and
/// <c>ForwardLimit</c> keep their defaults, so headers from anywhere else are ignored.
/// </summary>
public static class ForwardedHeadersSetup
{
    public const string InvalidNetworkMessage =
        "Niepoprawna sieć proxy w ustawieniu `ForwardedHeaders:KnownNetwork` (na serwerze `ForwardedHeaders__KnownNetwork`): " +
        "„{0}”. Podaj podsieć w zapisie CIDR, np. `172.18.0.0/16`.";

    public static IServiceCollection AddAppForwardedHeaders(this IServiceCollection services)
    {
        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ProxyOptions>>((options, proxy) =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                var network = proxy.Value.KnownNetwork?.Trim();
                if (string.IsNullOrEmpty(network))
                {
                    return;
                }
                if (!System.Net.IPNetwork.TryParse(network, out var parsed))
                {
                    throw new ProxyConfigurationException(string.Format(CultureInfo.InvariantCulture, InvalidNetworkMessage, network));
                }
                options.KnownIPNetworks.Add(parsed);
            });
        return services;
    }

    /// <summary>
    /// Builds the options once, so a wrong <c>KnownNetwork</c> stops the start with
    /// <see cref="ProxyConfigurationException"/> before the database startup sequence and the server.
    /// </summary>
    public static void EnsureForwardedHeadersConfiguration(this IServiceProvider services) =>
        _ = services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
}

/// <summary>Wrong proxy configuration; <c>Program</c> prints the Polish message and exits with code 1.</summary>
public sealed class ProxyConfigurationException(string message) : Exception(message);
