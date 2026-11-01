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
    public void HomePage_RendersTitle_ThroughPageHeader_WithStartTopic()
    {
        var page = Render<Home>();

        Assert.Equal("Rejestr czasu pracy", page.Find("h1").TextContent);
        var header = page.FindComponent<PageHeader>();
        Assert.Equal("start", header.Instance.Topic);
        Assert.NotEmpty(page.FindComponents<FluentLabel>());
    }

    [Fact]
    public void FluentServices_IncludeDialogService()
    {
        Assert.NotNull(Services.GetService<IDialogService>());
    }
}
