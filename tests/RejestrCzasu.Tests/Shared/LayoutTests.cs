using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Accounts;
using RejestrCzasu.Components.Pages;
using RejestrCzasu.Data;
using RejestrCzasu.Shared;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Shared;

/// <summary>
/// MainLayout on EodShell (AD-11, AD-17, AD-22, R19, R26, R31): top bar, Nowy wpis, menu by role, help "?" from PageHelp,
/// "nie znaleziono" on EodPage, the error boundary around the shell and its windows, the reconnect marker.
/// </summary>
public sealed class LayoutTests : BunitContext
{
    private readonly BunitAuthorizationContext _auth;
    private readonly CapturingLogger<AppErrorBoundary> _logger = new();
    private readonly FailureSwitch _failure = new();

    public LayoutTests()
    {
        this.AddAppUi();
        Services.AddSingleton<ILogger<AppErrorBoundary>>(_logger);
        Services.AddSingleton(_failure);
        _auth = AddAuthorization();
        _auth.SetAuthorized("zalogowany");
        _auth.SetRoles(AppRoles.User);
        // The application renders MainLayout in an Interactive Server circuit.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void Shell_TopBarWithAppName_MenuAndNewEntry()
    {
        var cut = RenderLayout(_ => { });

        Assert.Single(cut.FindAll(".eod-shell"));
        Assert.Equal("Rejestr czasu", cut.Find("a.eod-appname").TextContent.Trim());
        var newEntry = cut.Find(".eod-nav-top .app-new-entry");
        Assert.Equal("/wpisy/nowy", newEntry.GetAttribute("href"));
        Assert.Equal("primary", newEntry.GetAttribute("appearance"));
        Assert.Contains("Nowy wpis", newEntry.TextContent);
    }

    [Fact]
    public void Menu_User_HasAllSectionsWithoutAdministration()
    {
        var cut = RenderLayout(_ => { });

        var menu = cut.Find(".eod-nav-menu");
        foreach (var href in new[] { "/miesiac", "/wpisy", "/eksport", "/projekty", "/typy-pracy" })
        {
            Assert.NotNull(menu.QuerySelector($"[href='{href}']"));
        }
        Assert.Contains("Rejestr", menu.TextContent);
        Assert.Contains("Słowniki", menu.TextContent);
        Assert.Empty(cut.FindAll("[href='/uzytkownicy']"));
        Assert.DoesNotContain("Administracja", cut.Markup);
    }

    [Fact]
    public void Menu_Administrator_HasAdministration()
    {
        _auth.SetRoles(AppRoles.Administrator, AppRoles.User);

        var cut = RenderLayout(_ => { });

        Assert.NotNull(cut.Find(".eod-nav-menu").QuerySelector("[href='/uzytkownicy']"));
        Assert.Contains("Administracja", cut.Markup);
    }

    [Fact]
    public void TopBar_HasHelpThemeSettingsAndLogoutFormWithToken_OnBrand()
    {
        var cut = RenderLayout(_ => { });

        var header = TopBar(cut);
        var help = Assert.Single(header.QuerySelectorAll(".app-help"));
        Assert.Contains("eod-onbrand", help.ClassList);
        var theme = Assert.Single(header.QuerySelectorAll(".app-theme"));
        Assert.Contains("eod-onbrand", theme.ClassList);
        var settings = Assert.Single(header.QuerySelectorAll("[href='/ustawienia-konta']"));
        Assert.Contains("eod-onbrand", settings.ClassList);
        var form = header.QuerySelector("form.app-logout")!;
        Assert.Equal("post", form.GetAttribute("method"));
        Assert.Equal(AccountEndpoints.LogoutPath, form.GetAttribute("action"));
        Assert.NotNull(form.QuerySelector($"input[type='hidden'][name='{UiTestServices.AntiforgeryFieldName}']"));
        Assert.Contains("Wyloguj", form.TextContent);
        Assert.Contains("eod-onbrand", form.QuerySelector("fluent-button")!.ClassList);
    }

    [Fact]
    public async Task ThemeButton_SwitchesThroughAppearanceSettings()
    {
        var cut = RenderLayout(_ => { });

        await cut.Find(".app-theme").ClickAsync(new MouseEventArgs());

        // No theme chosen yet and the library reports light: the button switches to dark and remembers it.
        var saved = Assert.Single(JSInterop.Invocations, i => i.Identifier == "EodUI.saveTheme");
        Assert.Equal("dark", saved.Arguments[0]);
    }

    [Fact]
    public void Anonymous_SeesNoMenuAndNoLogout()
    {
        _auth.SetNotAuthorized();

        var cut = RenderLayout(_ => { });

        Assert.Empty(cut.FindAll("form.app-logout"));
        Assert.Empty(cut.FindAll("[href='/wpisy/nowy']"));
        Assert.Empty(cut.FindAll("[href='/projekty']"));
    }

    [Fact]
    public void HelpIcon_WithPageHelpTopic_LinksToTopic()
    {
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<PageHelp>(0);
            b.AddAttribute(1, nameof(PageHelp.Topic), "projekty");
            b.CloseComponent();
        });

        var help = Assert.Single(TopBar(cut).QuerySelectorAll(".app-help"));
        Assert.Equal("/pomoc/projekty", help.GetAttribute("href"));
    }

    [Fact]
    public void HelpIcon_WithPageHeaderTopic_LinksToTopic()
    {
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<PageHeader>(0);
            b.AddAttribute(1, nameof(PageHeader.Title), "Ekran");
            b.AddAttribute(2, nameof(PageHeader.Topic), "start");
            b.CloseComponent();
        });

        var help = Assert.Single(TopBar(cut).QuerySelectorAll(".app-help"));
        Assert.Equal("/pomoc/start", help.GetAttribute("href"));
        Assert.Equal("Ekran", cut.Find("h1").TextContent);
    }

    [Fact]
    public void HelpIcon_WithoutTopic_LinksToIndex()
    {
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<PageHelp>(0);
            b.CloseComponent();
        });

        Assert.Equal("/pomoc", Assert.Single(TopBar(cut).QuerySelectorAll(".app-help")).GetAttribute("href"));
    }

    [Fact]
    public void HelpIcon_WithoutPageHelp_LinksToIndex()
    {
        var cut = RenderLayout(_ => { });

        Assert.Equal("/pomoc", Assert.Single(TopBar(cut).QuerySelectorAll(".app-help")).GetAttribute("href"));
    }

    [Fact]
    public void NotFound_IsEodPageInTheShell()
    {
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<NotFound>(0);
            b.CloseComponent();
        });

        var page = cut.Find(".eod-shell .eod-page");
        Assert.Equal("Nie znaleziono", page.QuerySelector("h1.eod-h1")!.TextContent);
        Assert.NotNull(page.QuerySelector(".eod-card .eod-empty"));
        Assert.Equal("/", page.QuerySelector(".not-found-home")!.GetAttribute("href"));
    }

    [Fact]
    public void ReconnectMarker_IsPolish_WithReloadLink_AndPatternClasses()
    {
        var cut = RenderLayout(_ => { });

        var modal = cut.Find("#components-reconnect-modal");
        Assert.Contains("Utracono połączenie z serwerem.", modal.TextContent);
        Assert.NotNull(modal.QuerySelector(".eod-card .eod-h2"));
        Assert.NotNull(modal.QuerySelector(".eod-caption"));
        var reload = modal.QuerySelector("a")!;
        Assert.Equal("Odśwież stronę", reload.TextContent);
        Assert.Equal("_top", reload.GetAttribute("target"));
        Assert.Empty(modal.QuerySelectorAll("button, fluent-button, script"));
    }

    [Fact]
    public void ErrorBoundary_AccountBlocked_FullLoadOfLoginWithReason()
    {
        var cut = RenderLayout(Thrower(new AccountInactiveException(AccountStatus.Blocked)));

        cut.Find("button.thrower").Click();

        var navigation = Services.GetRequiredService<BunitNavigationManager>();
        var last = navigation.History.First();
        Assert.Equal("/konto/logowanie?powod=blocked", last.Uri);
        Assert.True(last.Options.ForceLoad);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public void ErrorBoundary_AccountInactiveInWindow_FullLoadOfLogin()
    {
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<WindowOpener>(0);
            b.CloseComponent();
        });

        cut.Find("button.open-window").Click();
        cut.WaitForElement("button.window-thrower").Click();

        var last = Services.GetRequiredService<BunitNavigationManager>().History.First();
        Assert.Equal("/konto/logowanie?powod=blocked", last.Uri);
        Assert.True(last.Options.ForceLoad);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public void ErrorBoundary_PasswordChangeRequired_FullLoadOfChangePassword()
    {
        var cut = RenderLayout(Thrower(new PasswordChangeRequiredException()));

        cut.Find("button.thrower").Click();

        var last = Services.GetRequiredService<BunitNavigationManager>().History.First();
        Assert.Equal(AccountEndpoints.ChangePasswordPath, last.Uri);
        Assert.True(last.Options.ForceLoad);
    }

    [Fact]
    public void ErrorBoundary_OtherException_ErrorBarInThePage_LogsAndScreenWorks()
    {
        var cut = RenderLayout(OnEodPage(Thrower(new InvalidOperationException("awaria"))));

        cut.Find("button.thrower").Click();

        cut.WaitForAssertion(() =>
        {
            var bar = cut.Find(".eod-page .eod-messages fluent-message-bar");
            Assert.Equal("error", bar.GetAttribute("intent"));
            Assert.Contains(AppErrorBoundary.GeneralMessage, bar.TextContent);
            // The shell and the screen are rendered again and still work.
            Assert.Single(cut.FindAll(".eod-shell"));
            Assert.Single(cut.FindAll("button.thrower"));
        });
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("awaria", entry.Exception?.Message);
        Assert.Empty(Services.GetRequiredService<BunitNavigationManager>().History);
    }

    [Fact]
    public void ErrorBoundary_ScreenFailingOnEveryRender_StopsRecovering_AndShowsReloadLink()
    {
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<FailingOnRender>(0);
            b.CloseComponent();
        });

        cut.WaitForAssertion(() => Assert.Contains("Odśwież stronę", cut.Find(".app-error").TextContent));
        Assert.Equal(AppErrorBoundary.MaxRecoveriesPerPage + 1, _logger.Entries.Count);
    }

    [Fact]
    public void ErrorBoundary_AfterGivingUp_NavigationToAHealthyPage_ShowsNoErrorBar()
    {
        var failure = _failure;
        var cut = RenderLayout(OnEodPage(b =>
        {
            b.OpenComponent<FailingWhileSwitched>(0);
            b.CloseComponent();
        }));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".app-error")));

        failure.Fail = false;
        Services.GetRequiredService<NavigationManager>().NavigateTo("/projekty");

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".healthy")));
        cut.Render();
        Assert.Empty(cut.FindAll("fluent-message-bar"));
    }

    private static AngleSharp.Dom.IElement TopBar(IRenderedComponent<MainLayout> cut) =>
        cut.Find("a.eod-appname").ParentElement!;

    private static RenderFragment OnEodPage(RenderFragment content) => b =>
    {
        b.OpenComponent<EodPage>(0);
        b.AddAttribute(1, nameof(EodPage.Title), "Ekran");
        b.AddAttribute(2, nameof(EodPage.ChildContent), content);
        b.CloseComponent();
    };

    private IRenderedComponent<MainLayout> RenderLayout(RenderFragment body) =>
        Render<MainLayout>(p => p.Add(l => l.Body, body));

    private static RenderFragment Thrower(Exception exception) => b =>
    {
        b.OpenComponent<ThrowingScreen>(0);
        b.AddAttribute(1, nameof(ThrowingScreen.Exception), exception);
        b.CloseComponent();
    };

    /// <summary>A screen whose button throws the given exception (from an event handler).</summary>
    private sealed class ThrowingScreen : ComponentBase
    {
        [Parameter] public Exception Exception { get; set; } = default!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "thrower");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => throw Exception));
            builder.AddContent(3, "Akcja");
            builder.CloseElement();
        }
    }

    /// <summary>A screen whose button opens a window with a button throwing an inactive Account (e.g. a save in EntryDialog).</summary>
    private sealed class WindowOpener : ComponentBase
    {
        [Inject] private IDialogService Dialogs { get; set; } = default!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "open-window");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => _ = Dialogs.ShowDialogAsync<ThrowingWindow>(new DialogOptions())));
            builder.AddContent(3, "Okno");
            builder.CloseElement();
        }
    }

    /// <summary>Content of a window whose button throws <see cref="AccountInactiveException"/>.</summary>
    private sealed class ThrowingWindow : ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "window-thrower");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => throw new AccountInactiveException(AccountStatus.Blocked)));
            builder.AddContent(3, "Zapisz");
            builder.CloseElement();
        }
    }

    private sealed class FailureSwitch
    {
        public bool Fail { get; set; } = true;
    }

    /// <summary>A screen that throws on initialization while the switch is on.</summary>
    private sealed class FailingWhileSwitched : ComponentBase
    {
        [Inject] private FailureSwitch Failure { get; set; } = default!;

        protected override void OnInitialized()
        {
            if (Failure.Fail)
            {
                throw new InvalidOperationException("na razie");
            }
        }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddAttribute(1, "class", "healthy");
            builder.CloseElement();
        }
    }

    /// <summary>A screen that throws on every initialization.</summary>
    private sealed class FailingOnRender : ComponentBase
    {
        protected override void OnInitialized() => throw new InvalidOperationException("zawsze");
    }
}
