using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using RejestrCzasu.Accounts;

namespace RejestrCzasu.Shared;

/// <summary>
/// The one place that reacts to an unhandled exception of a screen (AD-17, Consistency Conventions "Błędy"), around
/// <c>@Body</c> in <see cref="MainLayout"/>: an inactive Account goes to the login page and a required password change
/// to <c>/konto/zmiana-hasla</c>, both with a full page load (the cookie validator then ends the session); any other
/// exception is logged, shown in <see cref="Notify"/> with a general message, and the screen is rendered again.
/// </summary>
public sealed class AppErrorBoundary : ErrorBoundary, IDisposable
{
    public const string GeneralMessage =
        "Wystąpił nieoczekiwany błąd. Spróbuj ponownie, a jeśli błąd się powtarza, odśwież stronę.";

    /// <summary>
    /// Automatic recoveries on one page: a screen failing on every render would otherwise loop; after that the boundary
    /// shows the error with a reload link until the User goes to another page.
    /// </summary>
    internal const int MaxRecoveriesPerPage = 3;

    private bool _recoverAfterRender;
    private bool _leaving;
    private int _recoveries;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private Notify Notify { get; set; } = default!;

    [Inject] private ILogger<AppErrorBoundary> Logger { get; set; } = default!;

    protected override void OnInitialized() => Navigation.LocationChanged += OnLocationChanged;

    protected override async Task OnErrorAsync(Exception exception)
    {
        if (Find<AccountInactiveException>(exception) is { } inactive)
        {
            _leaving = true;
            Navigation.NavigateTo(inactive.LoginUrl, forceLoad: true);
            return;
        }
        if (Find<PasswordChangeRequiredException>(exception) is not null)
        {
            _leaving = true;
            Navigation.NavigateTo(AccountEndpoints.ChangePasswordPath, forceLoad: true);
            return;
        }

        Logger.LogError(exception, "Nieobsłużony wyjątek ekranu {Path}.", "/" + Navigation.ToBaseRelativePath(Navigation.Uri).Split('?')[0]);
        if (_recoveries < MaxRecoveriesPerPage)
        {
            _recoveries++;
            _recoverAfterRender = true;
        }
        try
        {
            await Notify.Error(GeneralMessage);
        }
        catch (Exception notifyException)
        {
            // A failing message must not take down the circuit; the error is already logged.
            Logger.LogWarning(notifyException, "Nie udało się pokazać komunikatu o błędzie.");
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (_recoverAfterRender)
        {
            _recoverAfterRender = false;
            // After the error render: the screen's components are created again from scratch.
            Recover();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CurrentException is null)
        {
            builder.AddContent(0, ChildContent);
            return;
        }
        if (_leaving || _recoverAfterRender)
        {
            return;
        }
        builder.OpenElement(1, "div");
        builder.AddAttribute(2, "class", "app-error");
        builder.AddAttribute(3, "role", "alert");
        builder.OpenElement(4, "p");
        builder.AddContent(5, "Ten ekran nie działa z powodu błędu.");
        builder.CloseElement();
        builder.OpenElement(6, "a");
        builder.AddAttribute(7, "href", Navigation.Uri);
        // A full page load, not the interactive router.
        builder.AddAttribute(8, "target", "_top");
        builder.AddContent(9, "Odśwież stronę");
        builder.CloseElement();
        builder.CloseElement();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _recoveries = 0;
        if (CurrentException is not null && !_leaving)
        {
            _ = InvokeAsync(Recover);
        }
    }

    private static T? Find<T>(Exception exception) where T : Exception
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            // InnerException of an AggregateException is its first inner exception.
            if (current is T found)
            {
                return found;
            }
        }
        return null;
    }

    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;
}
