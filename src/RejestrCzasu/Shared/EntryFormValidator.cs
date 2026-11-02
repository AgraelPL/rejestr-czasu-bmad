using FluentValidation;
using RejestrCzasu.Domain;

namespace RejestrCzasu.Shared;

/// <summary>
/// The one validator of the Entry form (AD-8), attached through Blazilla: required fields and the Czas rule taken from
/// <see cref="Czas"/>. The Suma dnia over 24 h is not checked (R4).
/// </summary>
public sealed class EntryFormValidator : AbstractValidator<EntryInput>
{
    public const string DateMessage = "Podaj datę.";
    public const string ProjectMessage = "Wybierz Projekt.";
    public const string WorkTypeMessage = "Wybierz Typ pracy.";
    public const string DescriptionMessage = "Podaj opis.";
    public const string CzasMessage = "Podaj Czas, np. 1h 30m, 45m albo 1:30 — od 15m do 24h, w krokach co 15 minut.";

    public EntryFormValidator()
    {
        RuleFor(e => e.Date).NotNull().WithMessage(DateMessage);
        RuleFor(e => e.Project).NotNull().WithMessage(ProjectMessage);
        RuleFor(e => e.WorkType).NotNull().WithMessage(WorkTypeMessage);
        RuleFor(e => e.Description)
            .Must(description => !string.IsNullOrWhiteSpace(description)).WithMessage(DescriptionMessage);
        RuleFor(e => e.Czas).Must(IsValidCzas).WithMessage(CzasMessage);
    }

    /// <summary>Czas of an Entry in any form accepted by <see cref="Czas.TryParse"/>: 15m–24h, a multiple of 15 minutes.</summary>
    public static bool IsValidCzas(string? text) => Czas.TryParse(text, out var minutes) && Czas.IsValidEntry(minutes);
}
