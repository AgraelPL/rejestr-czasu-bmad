using FluentValidation;
using RejestrCzasu.Data;

namespace RejestrCzasu.Features.UstawieniaKonta;

/// <summary>
/// The one validator of the Ustawienia konta form (AD-8), attached through Blazilla. Rules on the prefix after trimming
/// spaces: empty allowed, at most <see cref="AccountSettings.ExportPrefixMaxLength"/> characters, none of
/// <see cref="ForbiddenChars"/> (characters not allowed in a file name).
/// </summary>
public sealed class AccountSettingsFormValidator : AbstractValidator<AccountSettingsForm>
{
    /// <summary>Characters the prefix may not contain.</summary>
    public const string ForbiddenChars = "\\/:*?\"<>|";

    public AccountSettingsFormValidator()
    {
        RuleFor(f => f.Prefix)
            .Must(prefix => (prefix ?? "").Trim().Length <= AccountSettings.ExportPrefixMaxLength)
            .WithMessage($"Prefiks może mieć najwyżej {AccountSettings.ExportPrefixMaxLength} znaków.")
            .Must(prefix => (prefix ?? "").Trim().IndexOfAny(ForbiddenChars.ToCharArray()) < 0)
            .WithMessage("Prefiks nie może zawierać znaków \\ / : * ? \" < > |.");
    }
}
