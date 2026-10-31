namespace RejestrCzasu.Data;

public class Project : IUserOwned
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 200;

    public int Id { get; set; }

    /// <summary>Owner; assigned by <see cref="AppDbContext"/> in User mode (AD-16).</summary>
    public int UserId { get; set; }

    /// <summary>Code unique per User, stored upper-case (normalized in <see cref="AppDbContext"/>).</summary>
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
}
