using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Localization;
using RejestrCzasu.Shared;

namespace RejestrCzasu.Tests.Shared;

/// <summary>The services of the application UI in bUnit: Fluent UI with Polish texts, Notify, Confirm, antiforgery.</summary>
internal static class UiTestServices
{
    public static void AddAppUi(this BunitContext context)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddFluentUIComponents(options => options.Localizer = new FluentLocalizer());
        context.Services.AddScoped<Notify>();
        context.Services.AddScoped<Confirm>();
        context.Services.AddSingleton<AntiforgeryStateProvider>(new FakeAntiforgery());
    }

    public const string AntiforgeryFieldName = "__RequestVerificationToken";

    private sealed class FakeAntiforgery : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken GetAntiforgeryToken() => new("token-testowy", AntiforgeryFieldName);
    }
}

/// <summary>A logger that keeps its entries for assertions.</summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, exception, formatter(state, exception)));
}
