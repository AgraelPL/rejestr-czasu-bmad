using Microsoft.AspNetCore.Identity;

namespace RejestrCzasu.Data;

/// <summary>An Account (table <c>users</c>, AD-7).</summary>
public class AppUser : IdentityUser<int>
{
    /// <summary>The only source of the Account state (AD-15): <c>pending</c>, <c>active</c> or <c>blocked</c>.</summary>
    public AccountStatus Status { get; set; } = AccountStatus.Pending;

    /// <summary>Registration time, UTC (<c>timestamptz</c>).</summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>Last successful login, UTC (<c>timestamptz</c>); <c>null</c> before the first login.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Set when an Administrator sets the password; forces <c>/konto/zmiana-hasla</c>.</summary>
    public bool MustChangePassword { get; set; }
}
