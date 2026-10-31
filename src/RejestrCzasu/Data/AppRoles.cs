using Microsoft.AspNetCore.Identity;

namespace RejestrCzasu.Data;

/// <summary>
/// The two roles (AD-15) as schema data: seeded by <see cref="AppDbContext"/> (<c>HasData</c>) with fixed
/// <c>Id</c>, <c>NormalizedName</c> and <c>ConcurrencyStamp</c>, so the migration is deterministic.
/// </summary>
public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string User = "User";

    internal static readonly IdentityRole<int>[] Seed =
    [
        new()
        {
            Id = 1,
            Name = Administrator,
            NormalizedName = "ADMINISTRATOR",
            ConcurrencyStamp = "5b0f2d0e-1f6a-4c3e-9a51-2c7d8f3e6a01",
        },
        new()
        {
            Id = 2,
            Name = User,
            NormalizedName = "USER",
            ConcurrencyStamp = "9c4e7a12-6b3d-4f80-8e25-7d1a0b5c3f02",
        },
    ];
}
