namespace RejestrCzasu.Options;

/// <summary>Database backup settings (section <c>Backup</c>); used by the startup backup step.</summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    /// <summary>Directory for backup files; outside the repository and cloud-synced folders.</summary>
    public string Directory { get; set; } = "";
}
