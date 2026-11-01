using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Pomoc;
using RejestrCzasu.Features.Pomoc.Topics;
using RejestrCzasu.Tests.Shared;

namespace RejestrCzasu.Tests.Features;

/// <summary>Help pages (AD-22, R26): index, topic, unknown topic, access per topic, layout choice.</summary>
public sealed class HelpTests : BunitContext
{
    private readonly BunitAuthorizationContext _auth;

    public HelpTests()
    {
        this.AddAppUi();
        _auth = AddAuthorization();
    }

    [Fact]
    public void Index_LoggedIn_ListsAvailableTopics_StartFirst()
    {
        _auth.SetAuthorized("zalogowany");

        var cut = Render<HelpPage>();

        Assert.Equal("Pomoc", cut.Find("h1").TextContent);
        var links = cut.FindAll(".help-index a");
        Assert.Equal("/pomoc/start", links[0].GetAttribute("href"));
        Assert.Equal(HelpTopics.AvailableTo(User("zalogowany")).Count, links.Count);
    }

    [Fact]
    public void Index_Anonymous_HasNoNonPublicTopics()
    {
        var cut = Render<HelpPage>();

        Assert.Empty(cut.FindAll("[href='/pomoc/start']"));
        Assert.NotEmpty(cut.FindAll($"[href='{AccountEndpoints.LoginPath}']"));
    }

    [Fact]
    public void Topic_Start_LoggedIn_RendersContent_WithBreadcrumbToIndex()
    {
        _auth.SetAuthorized("zalogowany");

        var cut = Render<HelpPage>(p => p.Add(h => h.Temat, "start"));

        Assert.Equal("Strona startowa i menu", cut.Find("h1").TextContent);
        Assert.NotEmpty(cut.FindComponents<StartHelp>());
        Assert.Equal("/pomoc", cut.Find(".page-breadcrumbs a").GetAttribute("href"));
    }

    [Fact]
    public void Topic_Unknown_IsNotFound()
    {
        _auth.SetAuthorized("zalogowany");
        var notFound = false;
        Services.GetRequiredService<NavigationManager>().OnNotFound += (_, _) => notFound = true;

        var cut = Render<HelpPage>(p => p.Add(h => h.Temat, "xyz"));

        Assert.True(notFound);
        Assert.Empty(cut.FindComponents<HelpTopicView>());
    }

    [Fact]
    public void Topic_NonPublic_Anonymous_GoesToLogin()
    {
        Render<HelpPage>(p => p.Add(h => h.Temat, "start"));

        var last = Services.GetRequiredService<BunitNavigationManager>().History.First();
        Assert.StartsWith(AccountEndpoints.LoginPath + "?ReturnUrl=", last.Uri);
        Assert.True(last.Options.ForceLoad);
    }

    [Fact]
    public void Topic_Administrator_UserWithoutRole_SeesNoAccess()
    {
        var topic = new HelpTopic("administracja", "Administracja", typeof(StartHelp), HelpAccess.Administrator);

        var cut = Render<HelpTopicView>(p => p
            .Add(v => v.Topic, topic)
            .Add(v => v.User, User("zalogowany", AppRoles.User)));

        Assert.Equal("Brak dostępu", cut.Find("h1").TextContent);
        Assert.NotEmpty(cut.FindComponents<AccessDeniedContent>());
        Assert.Empty(cut.FindComponents<StartHelp>());
    }

    [Fact]
    public void Topic_Administrator_Administrator_SeesContent()
    {
        var topic = new HelpTopic("administracja", "Administracja", typeof(StartHelp), HelpAccess.Administrator);

        var cut = Render<HelpTopicView>(p => p
            .Add(v => v.Topic, topic)
            .Add(v => v.User, User("admin", AppRoles.Administrator)));

        Assert.NotEmpty(cut.FindComponents<StartHelp>());
    }

    [Fact]
    public void AvailableTo_PutsStartFirst_AndSkipsTopicsWithoutAccess()
    {
        HelpTopic[] topics =
        [
            new("projekty", "Projekty", typeof(StartHelp), HelpAccess.LoggedIn),
            new("uzytkownicy", "Użytkownicy", typeof(StartHelp), HelpAccess.Administrator),
            new(HelpTopics.Start, "Start", typeof(StartHelp), HelpAccess.LoggedIn),
            new("logowanie", "Logowanie", typeof(StartHelp), HelpAccess.Anonymous),
        ];

        Assert.Equal(["start", "projekty", "logowanie"], HelpTopics.AvailableTo(User("u", AppRoles.User), topics).Select(t => t.Key));
        Assert.Equal(["logowanie"], HelpTopics.AvailableTo(new ClaimsPrincipal(new ClaimsIdentity()), topics).Select(t => t.Key));
    }

    [Fact]
    public void Topics_HaveUniqueKebabCaseKeys_AndStart()
    {
        Assert.Contains(HelpTopics.All, t => t.Key == HelpTopics.Start && t.Access == HelpAccess.LoggedIn);
        Assert.Equal(HelpTopics.All.Count, HelpTopics.All.Select(t => t.Key).Distinct().Count());
        Assert.All(HelpTopics.All, t => Assert.Matches("^[a-z]+(-[a-z]+)*$", t.Key));
    }

    [Theory]
    [InlineData("pomoc", false, true)]
    [InlineData("pomoc", true, false)]
    [InlineData("pomoc/start", false, false)]
    [InlineData("pomoc/start", true, false)]
    [InlineData("pomoc/xyz", true, false)]
    public void Layout_AnonymousIndexInAccountLayout_RestInMainLayout(string path, bool loggedIn, bool accountLayout)
    {
        var user = loggedIn ? User("u") : new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Equal(accountLayout, HelpLayout.UsesAccountLayout(HelpLayout.TopicKey(path), user));
    }

    [Fact]
    public void Layout_AnonymousIndex_AccountLayout_HasPolishReconnectMarker()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/pomoc");

        var cut = Render<HelpLayout>(p => p.Add(l => l.Body, b => b.AddContent(0, "Spis")));

        Assert.NotEmpty(cut.FindComponents<AccountLayout>());
        var modal = cut.Find("#components-reconnect-modal");
        Assert.Contains("Odśwież stronę", modal.TextContent);
    }

    [Theory]
    [InlineData("pomoc", null)]
    [InlineData("pomoc/", null)]
    [InlineData("pomoc/start", "start")]
    [InlineData("pomoc/start?x=1", "start")]
    public void TopicKey_FromPath(string path, string? key) => Assert.Equal(key, HelpLayout.TopicKey(path));

    private static ClaimsPrincipal User(string name, params string[] roles) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, name), .. roles.Select(r => new Claim(ClaimTypes.Role, r))],
            "test"));
}
