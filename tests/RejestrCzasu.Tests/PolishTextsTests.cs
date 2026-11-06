using System.Collections;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Accounts;
using Microsoft.Extensions.DependencyInjection;
using RejestrCzasu.Tests.Shared;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests;

/// <summary>Polish texts (AD-6): Fluent UI (PolishLocalizer of the eod-ui copy, exactly the keys and placeholders of the installed library), Identity errors; and the local return URL rule of the login page.</summary>
public sealed partial class PolishTextsTests : BunitContext
{
    /// <summary>The keys and default (English) texts of <c>LanguageResource.resources</c> of the installed Fluent UI.</summary>
    private static IReadOnlyDictionary<string, string> FluentResource()
    {
        var assembly = typeof(IFluentLocalizer).Assembly;
        var resourceName = Assert.Single(assembly.GetManifestResourceNames(), n => n.EndsWith("LanguageResource.resources", StringComparison.Ordinal));
        using var reader = new ResourceReader(assembly.GetManifestResourceStream(resourceName)!);
        return reader.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!, StringComparer.Ordinal);
    }

    [GeneratedRegex(@"\{(\d+)")]
    private static partial Regex Placeholder();

    private static SortedSet<int> Placeholders(string text) =>
        new(Placeholder().Matches(text).Select(m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void PolishLocalizer_HasExactlyTheKeysOfTheInstalledFluentUi()
    {
        var keys = FluentResource().Keys.ToHashSet(StringComparer.Ordinal);

        var missing = keys.Where(k => !PolishLocalizer.Texts.ContainsKey(k)).Order(StringComparer.Ordinal).ToList();
        var extra = PolishLocalizer.Texts.Keys.Where(k => !keys.Contains(k)).Order(StringComparer.Ordinal).ToList();

        Assert.NotEmpty(keys);
        Assert.True(missing.Count == 0, $"Brak polskiego tekstu: {string.Join(", ", missing)}");
        Assert.True(extra.Count == 0, $"Klucze spoza zasobu Fluent UI: {string.Join(", ", extra)}");
    }

    [Fact]
    public void PolishLocalizer_KeepsThePlaceholdersOfTheLibraryTexts()
    {
        var different = FluentResource()
            .Where(e => PolishLocalizer.Texts.TryGetValue(e.Key, out var text) && !Placeholders(text).SetEquals(Placeholders(e.Value)))
            .Select(e => $"{e.Key} (biblioteka: {e.Value}, polski: {PolishLocalizer.Texts[e.Key]})")
            .ToList();

        Assert.True(different.Count == 0, $"Inne miejsca {{n}}: {string.Join("; ", different)}");
    }

    [Fact]
    public void PolishLocalizer_TextsUseOnlyThePlainHyphen()
    {
        Assert.All(PolishLocalizer.Texts, e => Assert.False(e.Value.Contains('—') || e.Value.Contains('–'), $"Myślnik w tekście: {e.Key}"));
    }

    [Fact]
    public void PolishLocalizer_FormatsArguments_AndFallsBackToTheLibraryDefault()
    {
        IFluentLocalizer localizer = new PolishLocalizer();

        Assert.Equal("Anuluj", localizer["MessageBox_ButtonCancel"]);
        Assert.Equal("Strona 2 z 5", localizer["Paginator_Status", 2, 5]);
        Assert.Equal("Wczytywanie 1/3 - plik.xlsx", localizer["FluentInputFile_LoadingInProgress", 1, 3, "plik.xlsx"]);

        // A key outside the dictionary goes to the library default unchanged; in Fluent UI 5.0.1 GetDefault throws for a key
        // outside LanguageResource too, so the fallback gives the same outcome as the library.
        Assert.Equal(Outcome(() => IFluentLocalizer.GetDefault("Brak_Takiego_Klucza")), Outcome(() => localizer["Brak_Takiego_Klucza"]));

        static string Outcome(Func<string> text)
        {
            try
            {
                return text();
            }
            catch (ArgumentException e)
            {
                return $"{e.GetType().Name}: {e.Message}";
            }
        }
    }

    [Fact]
    public void RequiredTextInput_EmptyAfterValidation_ShowsPolishMessage()
    {
        this.AddAppUi();
        var model = new RequiredModel();
        var editContext = new EditContext(model);

        var form = Render<EditForm>(p => p
            .Add(f => f.EditContext, editContext)
            .Add(f => f.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<FluentTextInput>(0);
                builder.AddAttribute(1, nameof(FluentTextInput.Value), model.Code);
                builder.AddAttribute(2, nameof(FluentTextInput.ValueExpression), (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Code));
                builder.AddAttribute(3, nameof(FluentTextInput.Label), "Kod");
                builder.AddAttribute(4, nameof(FluentTextInput.Required), true);
                builder.CloseComponent();
            })));

        // Leaving the empty field validates it, as when the User clears Kod in the Projekt window.
        form.Find("fluent-text-input").FocusOut();

        form.WaitForAssertion(() => Assert.Contains("To pole jest wymagane.", form.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("This field is required", form.Markup, StringComparison.Ordinal);
    }

    private sealed class RequiredModel
    {
        public string? Code { get; set; }
    }

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
