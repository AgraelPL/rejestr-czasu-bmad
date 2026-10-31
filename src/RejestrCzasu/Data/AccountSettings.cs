namespace RejestrCzasu.Data;

/// <summary>Per-Account settings (table <c>account_settings</c>, one row per User, AD-7).</summary>
public class AccountSettings : IUserOwned
{
    public const int ExportPrefixMaxLength = 100;

    /// <summary>Primary key and foreign key to <c>users</c>.</summary>
    public int UserId { get; set; }

    /// <summary>File name prefix of the exported Zestawienie (R25).</summary>
    public string ExportPrefix { get; set; } = "";
}
