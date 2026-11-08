using RejestrCzasu.Features.UstawieniaKonta;

namespace RejestrCzasu.Tests.Features;

/// <summary>The Ustawienia konta validator (AD-8): length after trimming, forbidden file name characters, Polish messages.</summary>
public sealed class AccountSettingsFormValidatorTests
{
    private const string ForbiddenMessage = "Prefiks nie może zawierać znaków \\ / : * ? \" < > |.";
    private const string LengthMessage = "Prefiks może mieć najwyżej 100 znaków.";

    private readonly AccountSettingsFormValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABC")]
    [InlineData("Łódź-Zespół")]
    [InlineData("Zespół A.B 2026")]
    public void Valid_HasNoErrors(string prefix) =>
        Assert.True(_validator.Validate(new AccountSettingsForm { Prefix = prefix }).IsValid);

    [Theory]
    [InlineData("A\\B")]
    [InlineData("A/B")]
    [InlineData("A:B")]
    [InlineData("A*B")]
    [InlineData("A?B")]
    [InlineData("A\"B")]
    [InlineData("A<B")]
    [InlineData("A>B")]
    [InlineData("A|B")]
    public void ForbiddenChar_IsRejected(string prefix)
    {
        var result = _validator.Validate(new AccountSettingsForm { Prefix = prefix });

        var error = Assert.Single(result.Errors);
        Assert.Equal((nameof(AccountSettingsForm.Prefix), ForbiddenMessage), (error.PropertyName, error.ErrorMessage));
    }

    [Fact]
    public void ForbiddenChars_AreExactlyTheFileNameOnes() =>
        Assert.Equal("\\/:*?\"<>|", AccountSettingsFormValidator.ForbiddenChars);

    [Fact]
    public void Length_100_IsValid_101_IsRejected()
    {
        Assert.True(_validator.Validate(new AccountSettingsForm { Prefix = new string('a', 100) }).IsValid);

        var error = Assert.Single(_validator.Validate(new AccountSettingsForm { Prefix = new string('a', 101) }).Errors);
        Assert.Equal((nameof(AccountSettingsForm.Prefix), LengthMessage), (error.PropertyName, error.ErrorMessage));
    }

    [Fact]
    public void Length_100_WithSpacesAround_IsValid() =>
        Assert.True(_validator.Validate(new AccountSettingsForm { Prefix = "  " + new string('a', 100) + "  " }).IsValid);
}
