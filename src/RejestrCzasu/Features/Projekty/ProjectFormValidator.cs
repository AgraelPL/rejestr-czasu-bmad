using FluentValidation;
using RejestrCzasu.Data;

namespace RejestrCzasu.Features.Projekty;

/// <summary>
/// The one validator of the Project form (AD-8), attached through Blazilla: required fields (after trimming spaces) and
/// maximum lengths. It never checks uniqueness of the code (AD-7): the database index does, through <see cref="DbErrors"/>.
/// </summary>
public sealed class ProjectFormValidator : AbstractValidator<ProjectForm>
{
    public ProjectFormValidator()
    {
        RuleFor(f => f.Code)
            .Must(code => !string.IsNullOrWhiteSpace(code)).WithMessage("Podaj kod Projektu.")
            .Must(code => (code ?? "").Trim().Length <= Project.CodeMaxLength)
            .WithMessage($"Kod może mieć najwyżej {Project.CodeMaxLength} znaków.");

        RuleFor(f => f.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Podaj nazwę Projektu.")
            .Must(name => (name ?? "").Trim().Length <= Project.NameMaxLength)
            .WithMessage($"Nazwa może mieć najwyżej {Project.NameMaxLength} znaków.");
    }
}
