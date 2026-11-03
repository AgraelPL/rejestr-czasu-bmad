using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using RejestrCzasu.Accounts;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Shared;

/// <summary>
/// The one place that reacts to an unhandled exception of a screen or of its window (AD-11, AD-17, Consistency
/// Conventions "Błędy"), around the whole <c>EodShell</c> in <see cref="MainLayout"/> (the shell renders
/// <c>FluentProviders</c>, so windows are inside the boundary too): an inactive Account goes to the login page and a
/// required password change to <c>/konto/zmiana-hasla</c>, both with a full page load (the cookie validator then ends
/// the session); any other exception is logged, the shell and the screen are rendered again, and the error bar of
/// <see cref="UiMessages"/> with a general message is shown in the page's message section.
/// </summary>
/// <remarks>
/// During the error render the whole content, with the message bar provider of the page, is gone; the bar is therefore
/// shown only after <see cref="ErrorBoundaryBase.Recover"/>, once the screen has rendered its
/// <see cref="EodSections.Page"/> again (retried on the next renders while the section is still missing).
/// </remarks>
public sealed class AppErrorBoundary : ErrorBoundary, IDisposable
{
    public const string GeneralMessage =
        "Wystąpił nieoczekiwany błąd. Spróbuj ponownie, a jeśli błąd się powtarza, odśwież stronę.";

    /// <summary>
    /// Automatic recoveries on one page: a screen failing on every render would otherwise loop; after that the boundary
    /// shows the error with a reload link until the User goes to another page.
    /// </summary>
    internal const int MaxRecoveriesPerPage = 3;

    /// <summary>Renders after a recovery that try to show the error bar before giving up (the page has no section).</summary>
    internal const int MaxMessageAttempts = 3;

    private bool _recoverAfterRender;
    private int _messageAttempts;
    private bool _leaving;
    private int _recoveries;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private UiMessages Messages { get; set; } = default!;

    [Inject] private ILogger<AppErrorBoundary> Logger { get; set; } = default!;

    protected override void OnInitialized() => Navigation.LocationChanged += OnLocationChanged;

    protected override Task OnErrorAsync(Exception exception)
    {
        if (Find<AccountInactiveException>(exception) is { } inactive)
        {
            _leaving = true;
            Navigation.NavigateTo(inactive.LoginUrl, forceLoad: true);
            return Task.CompletedTask;
        }
        if (Find<PasswordChangeRequiredException>(exception) is not null)
        {
            _leaving = true;
            Navigation.NavigateTo(AccountEndpoints.ChangePasswordPath, forceLoad: true);
            return Task.CompletedTask;
        }

        Logger.LogError(exception, "Nieobsłużony wyjątek ekranu {Path}.", "/" + Navigation.ToBaseRelativePath(Navigation.Uri).Split('?')[0]);
        if (_recoveries < MaxRecoveriesPerPage)
        {
            _recoveries++;
            _recoverAfterRender = true;
            _messageAttempts = 0;
        }
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_recoverAfterRender)
        {
            _recoverAfterRender = false;
            // After the error render: the shell and the screen's components are created again from scratch; the
            // error bar follows once the page has its message section again.
            _messageAttempts = MaxMessageAttempts;
            Recover();
            return;
        }
        if (_messageAttempts > 0 && CurrentException is null)
        {
            _messageAttempts--;
            await ShowGeneralMessageAsync();
        }
    }

    private async Task ShowGeneralMessageAsync()
    {
        try
        {
            if (await Messages.ShowErrorAsync(GeneralMessage) is not null)
            {
                _messageAttempts = 0;
            }
            else if (_messageAttempts > 0)
            {
                // The page has not rendered its message section yet: try again after the next render.
                StateHasChanged();
            }
        }
        catch (Exception messageException)
        {
            // A failing message must not take down the circuit; the error is already logged.
            _messageAttempts = 0;
            Logger.LogWarning(messageException, "Nie udało się pokazać komunikatu o błędzie.");
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
        // Without the shell (the boundary encloses it): a page of the eod-ui copy's classes with the reload link.
        builder.OpenElement(1, "div");
        builder.AddAttribute(2, "class", "eod-page app-error");
        builder.AddAttribute(3, "role", "alert");
        builder.OpenElement(4, "p");
        builder.AddAttribute(5, "class", "eod-h2");
        builder.AddContent(6, "Ten ekran nie działa z powodu błędu.");
        builder.CloseElement();
        builder.OpenElement(7, "a");
        builder.AddAttribute(8, "href", Navigation.Uri);
        // A full page load, not the interactive router.
        builder.AddAttribute(9, "target", "_top");
        builder.AddContent(10, "Odśwież stronę");
        builder.CloseElement();
        builder.CloseElement();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _recoveries = 0;
        // A bar still pending for the failed page must not appear on the next one.
        _messageAttempts = 0;
        if (CurrentException is not null && !_leaving)
        {
            // Without the shell (error view) nothing has cleared the bars of the failed page: clear them first.
            _ = InvokeAsync(async () =>
            {
                await Messages.ClearAsync();
                Recover();
            });
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
