namespace RejestrCzasu.Options;

/// <summary>
/// The first Administrator created at startup when the database has no Account with the Administrator role
/// (section <c>FirstAdmin</c>, AD-21). Locally in <c>appsettings.Local.json</c>, on the server
/// <c>FirstAdmin__UserName</c> and <c>FirstAdmin__Password</c> in <c>.env</c> (AD-13); never in <c>appsettings.json</c>.
/// </summary>
public sealed class FirstAdminOptions
{
    public const string SectionName = "FirstAdmin";

    public string? UserName { get; set; }
    public string? Password { get; set; }
}
