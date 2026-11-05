using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using RejestrCzasu.Accounts;
using RejestrCzasu.Features.Pomoc;
using RejestrCzasu.Features.Pomoc.Topics;

namespace RejestrCzasu.Tests.Shared;

/// <summary>
/// AccountLayout of the static /konto pages on the eod-ui copy classes (AD-11, AD-22): the brand top bar with the app name
/// and the Pomoc link of the page's topic, no EodShell or menu; the "no access" view as an eod-page.
/// </summary>
public sealed class AccountLayoutTests : BunitContext
{
    public AccountLayoutTests() => this.AddAppUi();

    [Fact]
    public void TopBar_AppName_AndHelpLinkOfThePageTopic_WithoutShell()
    {
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<AccountHelp>(0);
            b.AddComponentParameter(1, nameof(AccountHelp.Topic), "logowanie");
            b.CloseComponent();
        });

        var header = cut.Find("header.account-header");
        Assert.Equal("Rejestr czasu", header.QuerySelector(".eod-appname")!.TextContent.Trim());
        var help = header.QuerySelector("a.eod-onbrand")!;
        Assert.Equal("/pomoc/logowanie", help.GetAttribute("href"));
        Assert.Equal("Pomoc", help.TextContent.Trim());
        Assert.Empty(cut.FindAll(".eod-shell"));
        Assert.Empty(cut.FindAll(".eod-nav"));
        Assert.NotNull(cut.Find("main.account-content"));
    }

    [Fact]
    public void HelpLink_WithoutTopic_GoesToHelpIndex()
    {
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<AccountHelp>(0);
            b.CloseComponent();
        });

        Assert.Equal("/pomoc", cut.Find("header a.eod-onbrand").GetAttribute("href"));
    }

    [Fact]
    public void AnonymousHelpTopic_InAccountLayout_HasHelpLinkOfTheTopic()
    {
        var topic = new HelpTopic("logowanie", "Logowanie", typeof(StartHelp), HelpAccess.Anonymous);
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var cut = RenderLayout(b =>
        {
            b.OpenComponent<HelpTopicView>(0);
            b.AddComponentParameter(1, nameof(HelpTopicView.Topic), topic);
            b.AddComponentParameter(2, nameof(HelpTopicView.User), anonymous);
            b.CloseComponent();
        });

        Assert.Equal("/pomoc/logowanie", cut.Find("header a.eod-onbrand").GetAttribute("href"));
        Assert.Equal("Logowanie", cut.Find("main .eod-page h1").TextContent);
        Assert.Empty(cut.FindAll(".eod-shell"));
    }

    [Fact]
    public void AccessDeniedContent_IsAnEodPage_WithCard()
    {
        var cut = Render<AccessDeniedContent>();

        Assert.Equal("Brak dostępu", cut.Find("section.eod-page > h1.eod-h1").TextContent);
        var card = cut.Find("section.eod-page > .eod-card");
        Assert.Contains("Nie masz uprawnień do tej strony.", card.TextContent);
        Assert.Equal("/", card.QuerySelector("a")!.GetAttribute("href"));
    }

    private IRenderedComponent<AccountLayout> RenderLayout(RenderFragment body) =>
        Render<AccountLayout>(p => p.Add(l => l.Body, body));
}
