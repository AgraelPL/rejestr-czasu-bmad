using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Components.Pages;

namespace RejestrCzasu.Tests;

public sealed class HomePageTests : BunitContext
{
    public HomePageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
    }

    [Fact]
    public void HomePage_RendersTitle_WithFluentComponent()
    {
        var page = Render<Home>();

        Assert.Equal("Rejestr czasu pracy", page.Find("h1").TextContent);
        Assert.NotEmpty(page.FindComponents<FluentLabel>());
    }

    [Fact]
    public void FluentServices_IncludeDialogService()
    {
        Assert.NotNull(Services.GetService<IDialogService>());
    }
}
