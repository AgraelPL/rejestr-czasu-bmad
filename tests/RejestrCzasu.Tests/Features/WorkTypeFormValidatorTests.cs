using RejestrCzasu.Features.TypyPracy;

namespace RejestrCzasu.Tests.Features;

/// <summary>The Work type form validator (AD-8): required after trimming, maximum length, Polish messages.</summary>
public sealed class WorkTypeFormValidatorTests
{
    private readonly WorkTypeFormValidator _validator = new();

    [Fact]
    public void Valid_HasNoErrors()
    {
        Assert.True(_validator.Validate(new WorkTypeForm { Name = "Development" }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_IsRequired(string name)
    {
        var result = _validator.Validate(new WorkTypeForm { Name = name });

        var error = Assert.Single(result.Errors);
        Assert.Equal((nameof(WorkTypeForm.Name), "Podaj nazwę Typu pracy."), (error.PropertyName, error.ErrorMessage));
    }

    [Fact]
    public void Name_LongerThan200_IsRejected_ButSpacesAroundAreNotCounted()
    {
        var tooLong = _validator.Validate(new WorkTypeForm { Name = new string('n', 201) });
        var fits = _validator.Validate(new WorkTypeForm { Name = new string('n', 200) });
        var trimmedFits = _validator.Validate(new WorkTypeForm { Name = $"  {new string('n', 200)}  " });

        var error = Assert.Single(tooLong.Errors);
        Assert.Equal((nameof(WorkTypeForm.Name), "Nazwa może mieć najwyżej 200 znaków."), (error.PropertyName, error.ErrorMessage));
        Assert.True(fits.IsValid);
        Assert.True(trimmedFits.IsValid);
    }
}
