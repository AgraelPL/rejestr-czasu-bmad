using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Ui;

/// <summary>UiMessages windows of the eod-ui copy: AD-23 item 6, one test per I/O matrix row.</summary>
public sealed class UiMessagesTests : BunitContext
{
    private const string Html = "<p>Wpis <strong>ABC</strong> zostanie usunięty.</p>";

    public UiMessagesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<bool>("Microsoft.FluentUI.Blazor.Components.Dialog.ShouldHandleShortcut", _ => true).SetResult(true);
        Services.AddEodPlatformUI();
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData("y")]
    [InlineData("Y")]
    public async Task Confirm_ShortcutKeys_DoNotConfirm_WindowStaysOpen(string key)
    {
        var (cut, answer) = Confirm();

        await cut.Find("fluent-dialog").KeyDownAsync(new KeyboardEventArgs { Key = key });

        Assert.False(answer.IsCompleted);
        Assert.NotEmpty(cut.FindAll("fluent-dialog"));
    }

    [Fact]
    public void Confirm_FocusOnSecondaryButton_PrimaryFirst()
    {
        var (cut, _) = Confirm();

        var buttons = cut.Find("fluent-dialog").QuerySelectorAll("fluent-button");
        Assert.Equal(["Usuń", "Anuluj"], buttons.Select(b => b.TextContent.Trim()));
        Assert.Equal("primary", buttons[0].GetAttribute("appearance"));
        Assert.True(cut.Find(".eod-confirm-secondary").HasAttribute("autofocus"));
        Assert.False(cut.Find(".eod-confirm-primary").HasAttribute("autofocus"));
    }

    [Fact]
    public async Task Confirm_PrimaryButton_ReturnsTrue()
    {
        var (cut, answer) = Confirm();

        await cut.Find(".eod-confirm-primary").ClickAsync(new MouseEventArgs());

        Assert.True(await answer);
    }

    [Fact]
    public async Task Confirm_SecondaryButton_ReturnsFalse()
    {
        var (cut, answer) = Confirm();

        await cut.Find(".eod-confirm-secondary").ClickAsync(new MouseEventArgs());

        Assert.False(await answer);
    }

    [Fact]
    public async Task Confirm_Escape_ReturnsFalse()
    {
        var (cut, answer) = Confirm();
        var dialogId = cut.FindComponent<EodConfirmDialog>().Instance.Dialog.Id;

        // Escape closes the native dialog; the web component reports it as "beforetoggle" from open to closed.
        await cut.Find("fluent-dialog").TriggerEventAsync("ondialogbeforetoggle",
            new DialogToggleEventArgs { Id = dialogId, Type = "beforetoggle", OldState = "open", NewState = "closed" });

        Assert.False(await answer);
    }

    [Fact]
    public void Confirm_MessageRenderedAsHtml_WithKindIcon_AndEncodedTitle()
    {
        var (cut, _) = Confirm("Usunąć <b>Wpis</b>?", MessageBoxKind.Error);

        var dialog = cut.Find("fluent-dialog");
        Assert.Equal("ABC", dialog.QuerySelector(".fluent-message-box .message p strong")!.TextContent);
        var icon = dialog.QuerySelector(".fluent-message-box svg.icon")!;
        // Same icon markup as the library MessageBox of the same kind (Error: DismissCircle, Color.Error).
        Assert.Equal("width: 32px; fill: var(--error);", icon.GetAttribute("style"));
        Assert.Contains("Usunąć <b>Wpis</b>?", dialog.TextContent);
        Assert.Empty(dialog.QuerySelectorAll("[slot='title'] b"));
    }

    [Fact]
    public async Task MessageBox_Unchanged_OneButton_EnterCloses()
    {
        var cut = Render<FluentProviders>();
        var messages = Services.GetRequiredService<UiMessages>();
        var shown = cut.InvokeAsync(() => messages.ShowMessageBoxAsync(MessageBoxKind.Success, "Zapisano", Html));
        var dialog = cut.WaitForElement("fluent-dialog");

        var button = Assert.Single(dialog.QuerySelectorAll("fluent-button"));
        Assert.Equal("Zamknij", button.TextContent.Trim());
        Assert.Equal("ABC", dialog.QuerySelector(".fluent-message-box .message strong")!.TextContent);

        await cut.Find("fluent-dialog").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        await shown.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")));
    }

    private (IRenderedComponent<FluentProviders> Cut, Task<bool> Answer) Confirm(string title = "Usunąć Wpis?", MessageBoxKind kind = MessageBoxKind.Confirmation)
    {
        var cut = Render<FluentProviders>();
        var messages = Services.GetRequiredService<UiMessages>();
        var answer = cut.InvokeAsync(() => messages.ConfirmAsync(title, Html, "Usuń", kind: kind));
        cut.WaitForElement(".eod-confirm-secondary");
        return (cut, answer);
    }
}
