using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Shared;

namespace RejestrCzasu.Tests.Shared;

/// <summary>MainLayout (AD-11, AD-17, R19, R31): top bar, menu by role, help "?", error boundary, reconnect marker.</summary>
public sealed class LayoutTests : BunitContext
{
    private readonly BunitAuthorizationContext _auth;
    private readonly CapturingLogger<AppErrorBoundary> _logger = new();

    public LayoutTests()
    {
        this.AddAppUi();
        Services.AddSingleton<ILogger<AppErrorBoundary>>(_logger);
        _auth = AddAuthorization();
        _auth.SetAuthorized("zalogowany");
        _auth.SetRoles(AppRoles.User);
        // The application renders MainLayout in an Interactive Server circuit.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void Menu_User_HasAllSectionsWithoutAdministration()
    {
        var cut = RenderLayout(_ => { });

        foreach (var href in new[] { "/wpisy/nowy", "/miesiac", "/wpisy", "/eksport", "/projekty", "/typy-pracy" })
        {
            Assert.NotEmpty(cut.FindAll($"[href='{href}']"));
        }
        Assert.Contains("Rejestr", cut.Markup);
        Assert.Contains("Słowniki", cut.Markup);
        Assert.Empty(cut.FindAll("[href='/uzytkownicy']"));
        Assert.DoesNotContain("Administracja", cut.Markup);
    }

    [Fact]
    public void Menu_Administrator_HasAdministration()
    {
        _auth.SetRoles(AppRoles.Administrator, AppRoles.User);

        var cut = RenderLayout(_ => { });

        Assert.NotEmpty(cut.FindAll("[href='/uzytkownicy']"));
        Assert.Contains("Administracja", cut.Markup);
    }

    [Fact]
    public void TopBar_HasThemeSettingsAndLogoutFormWithToken()
    {
        var cut = RenderLayout(_ => { });

        Assert.Single(cut.FindAll(".app-theme"));
        Assert.Single(cut.FindAll(".app-header [href='/ustawienia-konta']"));
        var form = cut.Find("form.app-logout");
        Assert.Equal("post", form.GetAttribute("method"));
        Assert.Equal(AccountEndpoints.LogoutPath, form.GetAttribute("action"));
        Assert.NotNull(form.QuerySelector($"input[type='hidden'][name='{UiTestServices.AntiforgeryFieldName}']"));
        Assert.Contains("Wyloguj", form.TextContent);
    }

    [Fact]
    public void Anonymous_SeesNoMenuAndNoLogout()
    {
        _auth.SetNotAuthorized();

        var cut = RenderLayout(_ => { });

        Assert.Empty(cut.FindAll("form.app-logout"));
        Assert.Empty(cut.FindAll("[href='/wpisy/nowy']"));
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

        var help = Assert.Single(cut.FindAll(".app-header .app-help"));
        Assert.Equal("/pomoc/start", help.GetAttribute("href"));
        Assert.Equal("Ekran", cut.Find("h1").TextContent);
    }

    [Fact]
    public void HelpIcon_WithoutTopic_LinksToIndex()
    {
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<PageHeader>(0);
            b.AddAttribute(1, nameof(PageHeader.Title), "Ekran");
            b.CloseComponent();
        });

        Assert.Equal("/pomoc", Assert.Single(cut.FindAll(".app-header .app-help")).GetAttribute("href"));
    }

    [Fact]
    public void HelpIcon_WithoutPageHeader_LinksToIndex()
    {
        var cut = RenderLayout(_ => { });

        Assert.Equal("/pomoc", Assert.Single(cut.FindAll(".app-header .app-help")).GetAttribute("href"));
    }

    [Fact]
    public void ReconnectMarker_IsPolish_WithReloadLink()
    {
        var cut = RenderLayout(_ => { });

        var modal = cut.Find("#components-reconnect-modal");
        Assert.Contains("Utracono połączenie z serwerem.", modal.TextContent);
        var reload = modal.QuerySelector("a")!;
        Assert.Equal("Odśwież stronę", reload.TextContent);
        Assert.Equal("_top", reload.GetAttribute("target"));
        Assert.Empty(modal.QuerySelectorAll("button"));
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
    public void ErrorBoundary_PasswordChangeRequired_FullLoadOfChangePassword()
    {
        var cut = RenderLayout(Thrower(new PasswordChangeRequiredException()));

        cut.Find("button.thrower").Click();

        var last = Services.GetRequiredService<BunitNavigationManager>().History.First();
        Assert.Equal(AccountEndpoints.ChangePasswordPath, last.Uri);
        Assert.True(last.Options.ForceLoad);
    }

    [Fact]
    public void ErrorBoundary_OtherException_NotifiesLogsAndScreenWorks()
    {
        var cut = RenderLayout(Thrower(new InvalidOperationException("awaria")));

        cut.Find("button.thrower").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(AppErrorBoundary.GeneralMessage, cut.Find(".app-messages fluent-message-bar").TextContent);
            // The screen is rendered again and still works.
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

    /// <summary>A screen that throws on every initialization.</summary>
    private sealed class FailingOnRender : ComponentBase
    {
        protected override void OnInitialized() => throw new InvalidOperationException("zawsze");
    }
}
