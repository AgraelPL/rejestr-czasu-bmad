using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Accounts;
using RejestrCzasu.Features.Pomoc;
using RejestrCzasu.Features.UstawieniaKonta;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Ustawienia konta screen (R7, R25, AD-8, AD-11) in bUnit with the real <see cref="AccountSettingsStore"/> on the
/// test database and a clock fixed in September 2026.
/// </summary>
public sealed class AccountSettingsPageTests : BunitContext, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _postgres;
    private readonly CapturingLogger<AppErrorBoundary> _errors = new();
    private int _user;

    public AccountSettingsPageTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        this.AddAppUi();
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AppErrorBoundary>>(_errors);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        Services.AddScoped(_ => new AccountSettingsStore(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        AddAuthorization().SetAuthorized("ekran-ustawienia");
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _postgres.ResetAsync();
        _user = await _postgres.CreateUserAsync("ekran-ustawienia");
    }

    private AccountSettingsStore Store => new(_postgres.CreateLoggedInUserDbContextFactory(_user));

    [Fact]
    public void NoRow_EmptyField_PreviewWithoutPrefix_PasswordLink_Help()
    {
        var cut = RenderScreen();

        Assert.Equal("Ustawienia konta", cut.Find(".eod-page h1").TextContent);
        Assert.Equal(["Zestawienie", "Hasło"], cut.FindAll(".eod-card h2").Select(h => h.TextContent.Trim()));
        Assert.Equal("Prefiks nazwy pliku Zestawienia", PrefixInput(cut).Instance.Label);
        Assert.Equal("", PrefixInput(cut).Instance.Value);
        Assert.Null(PrefixInput(cut).Instance.MaxLength);
        Assert.Equal("Nazwa pliku: Zestawienie-wrzesień_2026.xlsx", Preview(cut));
        var password = cut.Find($".eod-page [href='{AccountEndpoints.ChangePasswordPath}']");
        Assert.Equal("Zmień hasło", password.TextContent.Trim());
        Assert.Equal("/konto/zmiana-hasla", password.GetAttribute("href"));
        Assert.Contains(cut.FindComponents<PageHelp>(), h => h.Instance.Topic == HelpTopics.UstawieniaKonta);
        Assert.Equal("/pomoc/ustawienia-konta", cut.Find(".app-help").GetAttribute("href"));
    }

    [Fact]
    public async Task SavedPrefix_ShownInField_AndPreview()
    {
        await Store.SavePrefixAsync("ABC", Ct);

        var cut = RenderScreen();

        Assert.Equal("ABC", PrefixInput(cut).Instance.Value);
        Assert.Equal("Nazwa pliku: ABC_Zestawienie-wrzesień_2026.xlsx", Preview(cut));
    }

    [Fact]
    public async Task Typing_RefreshesPreview_WithTrimmedValue()
    {
        var cut = RenderScreen();

        await cut.InvokeAsync(() => PrefixInput(cut).Instance.ValueChanged.InvokeAsync("  Łódź-Zespół "));

        cut.WaitForAssertion(() => Assert.Equal("Nazwa pliku: Łódź-Zespół_Zestawienie-wrzesień_2026.xlsx", Preview(cut)));
        Assert.True(PrefixInput(cut).Instance.Immediate);
    }

    [Fact]
    public async Task Save_TrimmedPrefix_FieldShowsTrimmed_SuccessBar()
    {
        var cut = RenderScreen();

        await FillAndSubmitAsync(cut, "  ABC  ");

        WaitForMessage(cut, "success", "Zapisano Ustawienia konta.");
        Assert.Equal("ABC", await Store.GetPrefixAsync(Ct));
        Assert.Equal("ABC", PrefixInput(cut).Instance.Value);
    }

    [Fact]
    public async Task Clear_SavesEmpty_PreviewWithoutPrefix()
    {
        await Store.SavePrefixAsync("ABC", Ct);
        var cut = RenderScreen();

        await FillAndSubmitAsync(cut, "   ");

        WaitForMessage(cut, "success", "Zapisano Ustawienia konta.");
        Assert.Equal("", await Store.GetPrefixAsync(Ct));
        Assert.Equal("Nazwa pliku: Zestawienie-wrzesień_2026.xlsx", Preview(cut));
    }

    [Theory]
    [InlineData("A/B", "Prefiks nie może zawierać znaków \\ / : * ? \" < > |.")]
    [InlineData(null, "Prefiks może mieć najwyżej 100 znaków.")]
    public async Task Invalid_MessageUnderField_SavesNothing(string? prefix, string message)
    {
        await Store.SavePrefixAsync("STARY", Ct);
        var cut = RenderScreen();

        await FillAndSubmitAsync(cut, prefix ?? new string('a', 101));

        Assert.True(cut.Find("form.account-settings-form").HasAttribute("novalidate"));
        cut.WaitForAssertion(() => Assert.Contains(message, WebUtility.HtmlDecode(cut.Markup)));
        Assert.Empty(cut.FindAll("fluent-message-bar"));
        Assert.Equal("STARY", await Store.GetPrefixAsync(Ct));
    }

    /// <summary>The screen in MainLayout, as the router renders it: "?" of the top bar, message area.</summary>
    private IRenderedComponent<MainLayout> RenderScreen()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, b =>
        {
            b.OpenComponent<AccountSettingsPage>(0);
            b.CloseComponent();
        }));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("form.account-settings-form")), TimeSpan.FromSeconds(5));
        return cut;
    }

    private static async Task FillAndSubmitAsync(IRenderedComponent<MainLayout> cut, string prefix)
    {
        await cut.InvokeAsync(() => PrefixInput(cut).Instance.ValueChanged.InvokeAsync(prefix));
        await cut.Find("form.account-settings-form").SubmitAsync();
    }

    private static IRenderedComponent<FluentTextInput> PrefixInput(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponents<FluentTextInput>().Single(i => i.Instance.Class == "account-settings-prefix");

    private static string Preview(IRenderedComponent<MainLayout> cut) =>
        cut.Find(".account-settings-preview").TextContent.Trim();

    private void WaitForMessage(IRenderedComponent<MainLayout> cut, string intent, string text) =>
        cut.WaitForAssertion(() => Assert.Contains($"{intent}: {text}", Messages(cut)), TimeSpan.FromSeconds(5));

    private List<string> Messages(IRenderedComponent<MainLayout> cut) =>
    [
        .. cut.FindAll("fluent-message-bar").Select(b => $"{b.GetAttribute("intent")}: {b.TextContent.Trim()}"),
        .. _errors.Entries.Select(e => $"log: {e.Exception}"),
    ];
}
