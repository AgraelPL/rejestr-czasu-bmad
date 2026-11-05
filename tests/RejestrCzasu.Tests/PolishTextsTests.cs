using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Accounts;
using Microsoft.Extensions.DependencyInjection;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests;

/// <summary>Polish texts (AD-6): Fluent UI (PolishLocalizer of the eod-ui copy), Identity errors; and the local return URL rule of the login page.</summary>
public sealed class PolishTextsTests
{
    [Fact]
    public void AddEodPlatformUI_RegistersPolishLocalizer_OfTheCopy()
    {
        using var services = new ServiceCollection().AddEodPlatformUI().BuildServiceProvider();

        var configuration = services.GetRequiredService<LibraryConfiguration>();
        Assert.IsType<PolishLocalizer>(configuration.Localizer);
        Assert.Equal("Anuluj", configuration.Localizer!["MessageBox_ButtonCancel"]);
    }

    [Fact]
    public void PolishIdentityErrorDescriber_OverridesEveryError()
    {
        var notOverridden = typeof(IdentityErrorDescriber)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.IsVirtual && m.ReturnType == typeof(IdentityError))
            .Where(m => typeof(PolishIdentityErrorDescriber).GetMethod(m.Name, m.GetParameters().Select(p => p.ParameterType).ToArray())!
                .DeclaringType != typeof(PolishIdentityErrorDescriber))
            .Select(m => m.Name)
            .ToList();

        Assert.Empty(notOverridden);
        Assert.Equal("Hasło musi mieć co najmniej 6 znaków.", new PolishIdentityErrorDescriber().PasswordTooShort(6).Description);
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("/", "/")]
    [InlineData("/wpisy?x=1", "/wpisy?x=1")]
    [InlineData("wpisy", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("https://evil.example/", "/")]
    [InlineData("/a\nb", "/")]
    public void LocalUrl_AcceptsOnlyLocalPaths(string? url, string expected)
    {
        Assert.Equal(expected, LocalUrl.OrHome(url));
    }
}
