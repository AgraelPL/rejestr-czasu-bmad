using RejestrCzasu.Data;
using RejestrCzasu.Shared;

namespace RejestrCzasu.Tests.Shared;

/// <summary>The Entry form validator (AD-8, R4): required fields, the Czas rule from Domain/Czas, Polish messages.</summary>
public sealed class EntryFormValidatorTests
{
    private readonly EntryFormValidator _validator = new();

    private static EntryInput Valid(string czas = "1:30") => new()
    {
        Date = new DateOnly(2026, 9, 3),
        Project = new ProjectOption(1, "RDZ", "Rozwój", true),
        WorkType = new WorkTypeOption(2, "Development", true),
        Description = "Spotkanie",
        Czas = czas,
    };

    [Theory]
    [InlineData("1:30")]
    [InlineData("0:15")]
    [InlineData("24:00")]
    [InlineData(" 8:45 ")]
    [InlineData("01:00")]
    [InlineData("1h 30m")]
    [InlineData("1H30M")]
    [InlineData("2h")]
    [InlineData("45m")]
    [InlineData("90m")]
    [InlineData("1,5h")]
    [InlineData("1.25h")]
    [InlineData("24h")]
    [InlineData("15m")]
    public void Valid_HasNoErrors(string czas)
    {
        Assert.True(_validator.Validate(Valid(czas)).IsValid);
    }

    [Theory]
    [InlineData("0:10")]
    [InlineData("24:15")]
    [InlineData("0:00")]
    [InlineData("1,5")]
    [InlineData("1.5")]
    [InlineData("90")]
    [InlineData("1:5")]
    [InlineData("1:60")]
    [InlineData("1h 75m")]
    [InlineData("10m")]
    [InlineData("24h 15m")]
    [InlineData("1,1h")]
    [InlineData("1d")]
    [InlineData("")]
    [InlineData("  ")]
    public void WrongCzas_HasPolishMessageAtCzas(string czas)
    {
        var error = Assert.Single(_validator.Validate(Valid(czas)).Errors);

        Assert.Equal(
            (nameof(EntryInput.Czas), "Podaj Czas, np. 1h 30m, 45m albo 1:30 — od 15m do 24h, w krokach co 15 minut."),
            (error.PropertyName, error.ErrorMessage));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyDescription_IsRequired(string description)
    {
        var input = Valid();
        input.Description = description;

        var error = Assert.Single(_validator.Validate(input).Errors);

        Assert.Equal((nameof(EntryInput.Description), "Podaj opis."), (error.PropertyName, error.ErrorMessage));
    }

    [Fact]
    public void MissingDateProjectAndWorkType_HaveMessagesAtFields()
    {
        var input = Valid();
        input.Date = null;
        input.Project = null;
        input.WorkType = null;

        var errors = _validator.Validate(input).Errors.Select(e => (e.PropertyName, e.ErrorMessage)).ToList();

        Assert.Equal(
            [
                (nameof(EntryInput.Date), "Podaj datę."),
                (nameof(EntryInput.Project), "Wybierz Projekt."),
                (nameof(EntryInput.WorkType), "Wybierz Typ pracy."),
            ],
            errors);
    }

    [Fact]
    public void ClearAfterSave_KeepsOnlyTheDate()
    {
        var input = Valid();

        input.ClearAfterSave();

        Assert.Equal(new DateOnly(2026, 9, 3), input.Date);
        Assert.Null(input.Project);
        Assert.Null(input.WorkType);
        Assert.Equal(("", ""), (input.Description, input.Czas));
    }
}
