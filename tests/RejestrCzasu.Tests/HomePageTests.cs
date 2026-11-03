using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Components.Pages;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;

namespace RejestrCzasu.Tests;

public sealed class HomePageTests : BunitContext
{
    public HomePageTests() => this.AddAppUi();

    [Fact]
    public void HomePage_IsEodPage_WithStartTopic()
    {
        var page = Render<Home>();

        Assert.Equal("Rejestr czasu pracy", page.Find(".eod-page h1.eod-h1").TextContent);
        Assert.Equal("start", page.FindComponent<PageHelp>().Instance.Topic);
        Assert.NotNull(page.Find(".eod-page .eod-card"));
        Assert.Empty(page.FindComponents<PageHeader>());
    }

    [Fact]
    public void FluentServices_IncludeDialogService()
    {
        Assert.NotNull(Services.GetService<IDialogService>());
    }
}
