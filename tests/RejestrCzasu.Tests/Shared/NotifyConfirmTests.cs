using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Shared;

namespace RejestrCzasu.Tests.Shared;

/// <summary>Notify and Confirm (AD-11): message bars in one area, cleared on navigation; confirmation without shortcut.</summary>
public sealed class NotifyConfirmTests : BunitContext
{
    public NotifyConfirmTests() => this.AddAppUi();

    [Fact]
    public async Task Notify_Error_ShowsMessageBar_WithPlainText()
    {
        var cut = Render<FluentMessageBarProvider>(p => p.Add(m => m.Section, Notify.Section));
        var notify = Services.GetRequiredService<Notify>();

        await cut.InvokeAsync(() => notify.Error("Nie zapisano <b>Wpisu</b>."));

        var bar = cut.WaitForElement("fluent-message-bar");
        Assert.Equal("error", bar.GetAttribute("intent"));
        Assert.Contains("Nie zapisano <b>Wpisu</b>.", bar.TextContent);
        Assert.Empty(bar.QuerySelectorAll("b"));
    }

    [Theory]
    [InlineData("success")]
    [InlineData("info")]
    [InlineData("warning")]
    public async Task Notify_OtherIntents_ShowMessageBar(string intent)
    {
        var cut = Render<FluentMessageBarProvider>(p => p.Add(m => m.Section, Notify.Section));
        var notify = Services.GetRequiredService<Notify>();

        await cut.InvokeAsync(() => intent switch
        {
            "success" => notify.Success("ok"),
            "info" => notify.Info("ok"),
            _ => notify.Warning("ok"),
        });

        Assert.Equal(intent, cut.WaitForElement("fluent-message-bar").GetAttribute("intent"));
    }

    [Fact]
    public async Task Notify_Navigation_ClearsMessages()
    {
        var cut = Render<FluentMessageBarProvider>(p => p.Add(m => m.Section, Notify.Section));
        var notify = Services.GetRequiredService<Notify>();
        await cut.InvokeAsync(() => notify.Error("Błąd"));
        cut.WaitForElement("fluent-message-bar");

        await cut.InvokeAsync(() => Services.GetRequiredService<NavigationManager>().NavigateTo("/projekty"));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-message-bar")));
    }

    [Fact]
    public async Task Notify_MessageOfDestinationPageInit_SurvivesNavigation()
    {
        // As in Routes.razor: Notify exists before the router subscribes to LocationChanged.
        var notify = Services.GetRequiredService<Notify>();
        var cut = Render<FakeRouter>();
        await cut.InvokeAsync(() => notify.Error("Komunikat strony źródłowej"));
        cut.WaitForElement("fluent-message-bar");

        await cut.InvokeAsync(() => Services.GetRequiredService<NavigationManager>().NavigateTo("/cel"));

        cut.WaitForAssertion(() =>
        {
            var bar = Assert.Single(cut.FindAll("fluent-message-bar"));
            Assert.Contains(DestinationPage.Message, bar.TextContent);
        });
    }

    /// <summary>Like the Router: on LocationChanged it renders the destination page synchronously.</summary>
    private sealed class FakeRouter : ComponentBase, IDisposable
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private bool _destination;

        protected override void OnInitialized() => Navigation.LocationChanged += OnLocationChanged;

        private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            _destination = true;
            StateHasChanged();
        }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<FluentMessageBarProvider>(0);
            builder.AddComponentParameter(1, nameof(FluentMessageBarProvider.Section), Notify.Section);
            builder.CloseComponent();
            if (_destination)
            {
                builder.OpenComponent<DestinationPage>(2);
                builder.CloseComponent();
            }
        }

        public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;
    }

    /// <summary>A screen that shows a message during its synchronous initialization.</summary>
    private sealed class DestinationPage : ComponentBase
    {
        public const string Message = "Komunikat strony docelowej";

        [Inject] private Notify Notify { get; set; } = default!;

        protected override void OnInitialized() => _ = Notify.Warning(Message);
    }

    [Fact]
    public void Confirm_Window_HasOwnButtons_CancelFocused_NoLibraryFooter()
    {
        var (cut, _) = Ask();

        var dialog = cut.Find("fluent-dialog");
        Assert.Contains("Usunąć Wpis?", dialog.TextContent);
        Assert.Contains("Tej operacji nie można cofnąć.", dialog.TextContent);
        var buttons = dialog.QuerySelectorAll("fluent-button");
        Assert.Equal(["Usuń", "Anuluj"], buttons.Select(b => b.TextContent.Trim()));
        Assert.True(cut.Find(".confirm-cancel").HasAttribute("autofocus"));
        Assert.False(cut.Find(".confirm-primary").HasAttribute("autofocus"));
    }

    [Fact]
    public async Task Confirm_Cancel_ReturnsFalse()
    {
        var (cut, answer) = Ask();

        await cut.Find(".confirm-cancel").ClickAsync(new MouseEventArgs());

        Assert.False(await answer);
    }

    [Fact]
    public async Task Confirm_Primary_ReturnsTrue()
    {
        var (cut, answer) = Ask();

        await cut.Find(".confirm-primary").ClickAsync(new MouseEventArgs());

        Assert.True(await answer);
    }

    [Fact]
    public async Task Confirm_Escape_ReturnsFalse()
    {
        var (cut, answer) = Ask();
        var dialogId = cut.FindComponent<ConfirmDialog>().Instance.Dialog.Id;

        // Escape closes the native dialog; the web component reports it as "beforetoggle" from open to closed.
        await cut.Find("fluent-dialog").TriggerEventAsync("ondialogbeforetoggle",
            new DialogToggleEventArgs { Id = dialogId, Type = "beforetoggle", OldState = "open", NewState = "closed" });

        Assert.False(await answer);
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData("y")]
    [InlineData("Y")]
    public async Task Confirm_ShortcutKeys_DoNotConfirm(string key)
    {
        JSInterop.Setup<bool>("Microsoft.FluentUI.Blazor.Components.Dialog.ShouldHandleShortcut", _ => true).SetResult(true);
        var (cut, answer) = Ask();

        await cut.Find("fluent-dialog").KeyDownAsync(new KeyboardEventArgs { Key = key });

        Assert.False(answer.IsCompleted);
        Assert.NotEmpty(cut.FindAll("fluent-dialog"));
    }

    private (IRenderedComponent<FluentProviders> Cut, Task<bool> Answer) Ask()
    {
        var cut = Render<FluentProviders>();
        var confirm = Services.GetRequiredService<Confirm>();
        var answer = cut.InvokeAsync(() => confirm.AskAsync("Usunąć Wpis?", "Tej operacji nie można cofnąć.", "Usuń"));
        cut.WaitForElement(".confirm-cancel");
        return (cut, answer);
    }
}
