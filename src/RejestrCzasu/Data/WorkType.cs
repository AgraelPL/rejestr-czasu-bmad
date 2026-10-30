namespace RejestrCzasu.Data;

public class WorkType
{
    public const int NameMaxLength = 200;

    public int Id { get; set; }

    /// <summary>Name, unique case-insensitively (functional index on <c>lower(name)</c>).</summary>
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
}
