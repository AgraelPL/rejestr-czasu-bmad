using System.Collections;
using System.Reflection;
using System.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Accounts;
using RejestrCzasu.Localization;

namespace RejestrCzasu.Tests;

/// <summary>Polish texts (AD-6): Fluent UI, Identity errors; and the local return URL rule of the login page.</summary>
public sealed class PolishTextsTests
{
    [Fact]
    public void FluentLocalizer_TranslatesEveryKeyOfTheInstalledFluentUi()
    {
        var assembly = typeof(IFluentLocalizer).Assembly;
        var resourceName = Assert.Single(assembly.GetManifestResourceNames(), n => n.EndsWith("LanguageResource.resources", StringComparison.Ordinal));
        using var reader = new ResourceReader(assembly.GetManifestResourceStream(resourceName)!);
        var keys = reader.Cast<DictionaryEntry>().Select(e => (string)e.Key).ToList();

        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.True(FluentLocalizer.Texts.ContainsKey(key), $"Brak tłumaczenia: {key}"));
    }

    [Fact]
    public void FluentLocalizer_FormatsArguments()
    {
        IFluentLocalizer localizer = new FluentLocalizer();

        Assert.Equal("Anuluj", localizer["MessageBox_ButtonCancel"]);
        Assert.Equal("Strona 2 z 5", localizer["Paginator_Status", 2, 5]);
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
