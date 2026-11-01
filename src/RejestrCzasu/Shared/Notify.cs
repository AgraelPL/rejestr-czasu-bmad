using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.FluentUI.AspNetCore.Components;

namespace RejestrCzasu.Shared;

/// <summary>
/// Screen messages (AD-11): a <c>FluentMessageBar</c> in the one message area of <see cref="MainLayout"/>, under the
/// top bar. No toasts. Every message disappears when the User goes to another page.
/// </summary>
/// <remarks>
/// Created by <c>Components/Routes.razor</c> before the Router attaches, so the clearing handler runs before the Router
/// renders the new page and a message shown by the new page's initialization survives.
/// </remarks>
public sealed class Notify : IDisposable
{
    /// <summary>The section of the <c>FluentMessageBarProvider</c> rendered by <see cref="MainLayout"/>.</summary>
    public const string Section = "app-messages";

    private readonly INotificationService notifications;
    private readonly NavigationManager navigation;

    public Notify(INotificationService notifications, NavigationManager navigation)
    {
        this.notifications = notifications;
        this.navigation = navigation;
        navigation.LocationChanged += OnLocationChanged;
    }

    public Task Success(string message) => ShowAsync(MessageBarIntent.Success, message);

    public Task Info(string message) => ShowAsync(MessageBarIntent.Info, message);

    public Task Warning(string message) => ShowAsync(MessageBarIntent.Warning, message);

    public Task Error(string message) => ShowAsync(MessageBarIntent.Error, message);

    private async Task ShowAsync(MessageBarIntent intent, string message)
    {
        await notifications.ShowMessageBarAsync(options =>
        {
            options.Section = Section;
            options.Intent = intent;
            // The library renders the message as (sanitized) HTML; a message is plain text.
            options.Message = WebUtility.HtmlEncode(message);
            options.AllowDismiss = true;
        });
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        _ = notifications.CloseAllMessageBarsAsync();

    public void Dispose() => navigation.LocationChanged -= OnLocationChanged;
}
