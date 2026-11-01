using FluentValidation;
using RejestrCzasu.Data;

namespace RejestrCzasu.Features.TypyPracy;

/// <summary>
/// The one validator of the Work type form (AD-8), attached through Blazilla: the name is required (after trimming
/// spaces) and has a maximum length. It never checks uniqueness of the name (AD-7): the database index does, through
/// <see cref="DbErrors"/>.
/// </summary>
public sealed class WorkTypeFormValidator : AbstractValidator<WorkTypeForm>
{
    public WorkTypeFormValidator()
    {
        RuleFor(f => f.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Podaj nazwę Typu pracy.")
            .Must(name => (name ?? "").Trim().Length <= WorkType.NameMaxLength)
            .WithMessage($"Nazwa może mieć najwyżej {WorkType.NameMaxLength} znaków.");
    }
}
