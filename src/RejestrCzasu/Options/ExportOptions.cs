namespace RejestrCzasu.Options;

/// <summary>Monthly report export settings (section <c>Export</c>).</summary>
public sealed class ExportOptions
{
    public const string SectionName = "Export";

    /// <summary>File name prefix of the exported report.</summary>
    public string Prefix { get; set; } = "";
}
