namespace RejestrCzasu.Data;

public class Project
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 200;

    public int Id { get; set; }

    /// <summary>Unique code, stored upper-case (normalized in <see cref="AppDbContext"/>).</summary>
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
}
