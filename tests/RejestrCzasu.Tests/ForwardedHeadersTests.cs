using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RejestrCzasu.Accounts;
using RejestrCzasu.Options;

namespace RejestrCzasu.Tests;

/// <summary>
/// The application behind Caddy (AD-19): <c>UseForwardedHeaders</c> first in the pipeline, trusting only
/// <c>ForwardedHeaders:KnownNetwork</c> and loopback, so the login/registration limit (AD-15) counts the real client.
/// TestServer has no connection address, so an <see cref="IStartupFilter"/> (run before the pipeline of
/// <c>Program</c>) sets it and reports the scheme and client address the application ended up with.
/// </summary>
public sealed class ForwardedHeadersTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string TrustedNetwork = "10.10.0.0/16";
    private static readonly IPAddress TrustedProxy = IPAddress.Parse("10.10.0.5");
    private static readonly IPAddress UntrustedProxy = IPAddress.Parse("192.0.2.50");
    private const string ClientA = "203.0.113.1";
    private const string ClientB = "203.0.113.2";
    private const string SchemeHeader = "X-Test-Scheme";
    private const string ClientHeader = "X-Test-Client";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task TrustedProxy_LimitCountedPerForwardedClient()
    {
        await using var app = Behind(TrustedProxy);
        var fields = await LoginFieldsAsync(app);

        for (var attempt = 0; attempt < AccountRateLimit.PermitLimit; attempt++)
        {
            using var allowed = await PostLoginAsync(app, fields, ClientA);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            Assert.Equal(ClientA, Single(allowed, ClientHeader));
        }
        using (var limited = await PostLoginAsync(app, fields, ClientA))
        {
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }

        // Another client behind the same proxy has its own limit.
        using var other = await PostLoginAsync(app, fields, ClientB);
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
        Assert.Equal(ClientB, Single(other, ClientHeader));
    }

    [Fact]
    public async Task TrustedProxy_IPv4MappedConnectionAddress_IsTrusted()
    {
        // Kestrel on ASPNETCORE_HTTP_PORTS listens dual-mode, so an IPv4 proxy may arrive as ::ffff:a.b.c.d.
        await using var app = Behind(TrustedProxy.MapToIPv6());

        using var response = await GetLoginAsync(app, ClientA, "https");

        Assert.Equal(ClientA, Single(response, ClientHeader));
        Assert.Equal("https", Single(response, SchemeHeader));
    }

    [Fact]
    public async Task UntrustedProxy_HeadersIgnored_LimitCountedForConnectionAddress()
    {
        await using var app = Behind(UntrustedProxy);
        var fields = await LoginFieldsAsync(app);

        for (var attempt = 0; attempt < AccountRateLimit.PermitLimit; attempt++)
        {
            using var allowed = await PostLoginAsync(app, fields, $"203.0.113.{attempt + 10}");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            Assert.Equal(UntrustedProxy.ToString(), Single(allowed, ClientHeader));
        }

        using var limited = await PostLoginAsync(app, fields, ClientB);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    [Fact]
    public async Task ForwardedProtoHttps_FromTrustedNetwork_SetsRequestScheme()
    {
        await using var app = Behind(TrustedProxy);

        using var response = await GetLoginAsync(app, ClientA, "https");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https", Single(response, SchemeHeader));
    }

    [Fact]
    public async Task ForwardedProtoHttps_FromUntrustedNetwork_IsIgnored()
    {
        await using var app = Behind(UntrustedProxy);

        using var response = await GetLoginAsync(app, ClientA, "https");

        Assert.Equal("http", Single(response, SchemeHeader));
        Assert.Equal(UntrustedProxy.ToString(), Single(response, ClientHeader));
    }

    [Fact]
    public async Task WithoutKnownNetwork_OnlyLoopbackIsTrusted()
    {
        await using (var outside = Behind(TrustedProxy, knownNetwork: null))
        {
            using var ignored = await GetLoginAsync(outside, ClientA, "https");
            Assert.Equal("http", Single(ignored, SchemeHeader));
            Assert.Equal(TrustedProxy.ToString(), Single(ignored, ClientHeader));
        }

        await using var loopback = Behind(IPAddress.Loopback, knownNetwork: null);
        using var trusted = await GetLoginAsync(loopback, ClientA, "https");
        Assert.Equal("https", Single(trusted, SchemeHeader));
        Assert.Equal(ClientA, Single(trusted, ClientHeader));
    }

    [Theory]
    [InlineData("nie-cidr")]
    [InlineData("172.18.0.1")]
    [InlineData("172.18.0.0/33")]
    public void InvalidKnownNetwork_ThrowsPolishMessage(string network)
    {
        var services = ForwardedHeadersServices(network);

        var ex = Assert.Throws<ProxyConfigurationException>(services.EnsureForwardedHeadersConfiguration);
        Assert.Contains("Niepoprawna sieć proxy", ex.Message);
        Assert.Contains(network, ex.Message);
    }

    [Fact]
    public void KnownNetwork_AddedToKnownIPNetworks_KnownProxiesAndForwardLimitUnchanged()
    {
        var defaults = new ForwardedHeadersOptions();
        var options = ForwardedHeadersServices(" 172.18.0.0/16 ").GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
        Assert.Equal([.. defaults.KnownIPNetworks, System.Net.IPNetwork.Parse("172.18.0.0/16")], options.KnownIPNetworks);
        Assert.Equal(defaults.KnownProxies, options.KnownProxies);
        Assert.Equal(1, options.ForwardLimit);
    }

    private static ServiceProvider ForwardedHeadersServices(string network)
    {
        var services = new ServiceCollection();
        services.AddOptions<ProxyOptions>().Configure(o => o.KnownNetwork = network);
        services.AddAppForwardedHeaders();
        return services.BuildServiceProvider();
    }

    private AppClient Behind(IPAddress connectionAddress, string? knownNetwork = TrustedNetwork) =>
        new(
            postgres,
            knownNetwork is null ? null : new Dictionary<string, string?> { ["ForwardedHeaders:KnownNetwork"] = knownNetwork },
            builder => builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter>(new ConnectionAddressFilter(connectionAddress))));

    private static async Task<Dictionary<string, string>> LoginFieldsAsync(AppClient app)
    {
        var (_, fields) = await app.GetPageAsync("/konto/logowanie");
        fields["Input.UserName"] = "nikt";
        fields["Input.Password"] = "haslo1";
        return fields;
    }

    private static Task<HttpResponseMessage> PostLoginAsync(AppClient app, Dictionary<string, string> fields, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/konto/logowanie") { Content = new FormUrlEncodedContent(fields) };
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return app.Http.SendAsync(request, Ct);
    }

    private static Task<HttpResponseMessage> GetLoginAsync(AppClient app, string forwardedFor, string forwardedProto)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/konto/logowanie");
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        request.Headers.Add("X-Forwarded-Proto", forwardedProto);
        return app.Http.SendAsync(request, Ct);
    }

    private static string Single(HttpResponseMessage response, string header) =>
        Assert.Single(response.Headers.GetValues(header));

    /// <summary>Sets the connection address before the application's pipeline and reports what the application saw.</summary>
    private sealed class ConnectionAddressFilter(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = address;
                // UseForwardedHeaders changes the request in place, so at response time it shows the forwarded values.
                context.Response.OnStarting(() =>
                {
                    var client = context.Connection.RemoteIpAddress;
                    context.Response.Headers[SchemeHeader] = context.Request.Scheme;
                    context.Response.Headers[ClientHeader] =
                        (client is { IsIPv4MappedToIPv6: true } ? client.MapToIPv4() : client)?.ToString();
                    return Task.CompletedTask;
                });
                await nextMiddleware(context);
            });
            next(app);
        };
    }
}
