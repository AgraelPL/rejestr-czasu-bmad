using Microsoft.AspNetCore.Identity;

namespace RejestrCzasu.Accounts;

/// <summary>Identity errors in Polish (AD-6); the static <c>/konto</c> forms show them as they are (AD-8).</summary>
public sealed class PolishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "Wystąpił nieznany błąd.");

    public override IdentityError ConcurrencyFailure() =>
        Error(nameof(ConcurrencyFailure), "Dane Konta zmieniły się w międzyczasie. Spróbuj ponownie.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "Nieprawidłowe hasło.");

    public override IdentityError InvalidToken() =>
        Error(nameof(InvalidToken), "Nieprawidłowy token.");

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        Error(nameof(RecoveryCodeRedemptionFailed), "Nie udało się użyć kodu odzyskiwania.");

    public override IdentityError LoginAlreadyAssociated() =>
        Error(nameof(LoginAlreadyAssociated), "To logowanie jest już przypisane do innego Konta.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName),
            $"Login „{userName}” jest nieprawidłowy: może zawierać tylko litery bez polskich znaków, cyfry i znaki - . _ @ +.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), $"Adres e-mail „{email}” jest nieprawidłowy.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"Login „{userName}” jest już zajęty.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), $"Adres e-mail „{email}” jest już zajęty.");

    public override IdentityError InvalidRoleName(string? role) =>
        Error(nameof(InvalidRoleName), $"Nazwa roli „{role}” jest nieprawidłowa.");

    public override IdentityError DuplicateRoleName(string role) =>
        Error(nameof(DuplicateRoleName), $"Rola „{role}” już istnieje.");

    public override IdentityError UserAlreadyHasPassword() =>
        Error(nameof(UserAlreadyHasPassword), "Konto ma już ustawione hasło.");

    public override IdentityError UserLockoutNotEnabled() =>
        Error(nameof(UserLockoutNotEnabled), "Wstrzymywanie logowania nie jest włączone dla tego Konta.");

    public override IdentityError UserAlreadyInRole(string role) =>
        Error(nameof(UserAlreadyInRole), $"Konto ma już rolę „{role}”.");

    public override IdentityError UserNotInRole(string role) =>
        Error(nameof(UserNotInRole), $"Konto nie ma roli „{role}”.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"Hasło musi mieć co najmniej {length} znaków.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"Hasło musi zawierać co najmniej {uniqueChars} różnych znaków.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Hasło musi zawierać co najmniej jeden znak, który nie jest literą ani cyfrą.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Hasło musi zawierać co najmniej jedną cyfrę.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Hasło musi zawierać co najmniej jedną małą literę.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Hasło musi zawierać co najmniej jedną wielką literę.");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
