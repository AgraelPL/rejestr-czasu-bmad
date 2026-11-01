using RejestrCzasu.Features.Projekty;

namespace RejestrCzasu.Tests.Features;

/// <summary>The Project form validator (AD-8): required after trimming, maximum lengths, Polish messages.</summary>
public sealed class ProjectFormValidatorTests
{
    private readonly ProjectFormValidator _validator = new();

    [Fact]
    public void Valid_HasNoErrors()
    {
        Assert.True(_validator.Validate(new ProjectForm { Code = "rdz", Name = "Rozwój" }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyCode_IsRequired(string code)
    {
        var result = _validator.Validate(new ProjectForm { Code = code, Name = "Rozwój" });

        var error = Assert.Single(result.Errors);
        Assert.Equal((nameof(ProjectForm.Code), "Podaj kod Projektu."), (error.PropertyName, error.ErrorMessage));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyName_IsRequired(string name)
    {
        var result = _validator.Validate(new ProjectForm { Code = "RDZ", Name = name });

        var error = Assert.Single(result.Errors);
        Assert.Equal((nameof(ProjectForm.Name), "Podaj nazwę Projektu."), (error.PropertyName, error.ErrorMessage));
    }

    [Fact]
    public void Code_LongerThan20_IsRejected_ButSpacesAroundAreNotCounted()
    {
        var tooLong = _validator.Validate(new ProjectForm { Code = new string('A', 21), Name = "Rozwój" });
        var trimmedFits = _validator.Validate(new ProjectForm { Code = $"  {new string('A', 20)}  ", Name = "Rozwój" });

        var error = Assert.Single(tooLong.Errors);
        Assert.Equal((nameof(ProjectForm.Code), "Kod może mieć najwyżej 20 znaków."), (error.PropertyName, error.ErrorMessage));
        Assert.True(trimmedFits.IsValid);
    }

    [Fact]
    public void Name_LongerThan200_IsRejected()
    {
        var tooLong = _validator.Validate(new ProjectForm { Code = "RDZ", Name = new string('n', 201) });
        var fits = _validator.Validate(new ProjectForm { Code = "RDZ", Name = new string('n', 200) });

        var error = Assert.Single(tooLong.Errors);
        Assert.Equal((nameof(ProjectForm.Name), "Nazwa może mieć najwyżej 200 znaków."), (error.PropertyName, error.ErrorMessage));
        Assert.True(fits.IsValid);
    }
}
