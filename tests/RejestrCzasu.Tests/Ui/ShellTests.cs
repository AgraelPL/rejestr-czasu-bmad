using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Ui;

/// <summary>Shell of the eod-ui copy: AD-23 items 7 (application name) and 8 (scale script from a file).</summary>
public sealed class ShellTests : BunitContext
{
    public ShellTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddEodPlatformUI();
        SetRendererInfo(new RendererInfo("Server", true));
    }

    [Fact]
    public void Shell_WithoutAppName_ShowsRejestrCzasu()
    {
        var cut = Render<EodShell>();

        Assert.Equal("Rejestr czasu", cut.Find("a.eod-appname").TextContent.Trim());
        Assert.DoesNotContain("MOJE EOD", cut.Markup);
        Assert.Empty(cut.FindAll(".eod-header-sep"));
    }

    [Theory]
    [InlineData("Projekty", "Projekty · Rejestr czasu")]
    [InlineData(null, "Rejestr czasu")]
    public void Page_BrowserTitle_WithRejestrCzasu(string? title, string expected)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<HeadOutlet>(0);
            builder.CloseComponent();
            builder.OpenComponent<EodPage>(1);
            builder.AddComponentParameter(2, nameof(EodPage.Title), title);
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => Assert.Equal(expected, cut.Find("title").TextContent));
    }

    [Fact]
    public void Head_HasNoInlineScript_AndLoadsScaleScriptBeforeEodJs()
    {
        var cut = Render<EodHead>();

        var scripts = cut.FindAll("script");
        Assert.All(scripts, s => Assert.False(string.IsNullOrEmpty(s.GetAttribute("src"))));
        Assert.All(scripts, s => Assert.True(string.IsNullOrWhiteSpace(s.TextContent)));
        var sources = scripts.Select(s => s.GetAttribute("src")).ToList();
        var scale = sources.IndexOf("ui/eod-scale.js");
        Assert.Equal(0, scale);
        Assert.True(scale < sources.IndexOf("ui/eod.js"));
        Assert.False(scripts[scale].HasAttribute("defer"));
        Assert.False(scripts[scale].HasAttribute("async"));
    }

    [Fact]
    public void ScaleScript_MatchesEodJs_KeyAndScales()
    {
        var root = FindWwwroot();
        var scale = File.ReadAllText(Path.Combine(root, "ui", "eod-scale.js"));
        var eod = File.ReadAllText(Path.Combine(root, "ui", "eod.js"));

        Assert.Contains("'eod.scale'", scale);
        Assert.Contains("const SCALE_KEY = 'eod.scale';", eod);
        Assert.Contains("['90', '100', '110', '125']", scale);
        Assert.Contains("const SCALES = ['90', '100', '110', '125'];", eod);
    }

    [Fact]
    public void EodJs_ExportsShowSelectedText_WithFrameLimit()
    {
        var eod = File.ReadAllText(Path.Combine(FindWwwroot(), "ui", "eod.js"));

        Assert.Contains("function showSelectedText(dropdown, text)", eod);
        Assert.Contains("attempt < 30", eod);
        Assert.Contains("requestAnimationFrame(() => show(attempt + 1))", eod);
        Assert.Contains("    showSelectedText,", eod);
    }

    private static string FindWwwroot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "RejestrCzasu", "wwwroot");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("src/RejestrCzasu/wwwroot");
    }
}
