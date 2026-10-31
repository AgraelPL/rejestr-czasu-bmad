namespace RejestrCzasu.Data;

public class WorkType : IUserOwned
{
    public const int NameMaxLength = 200;

    public int Id { get; set; }

    /// <summary>Owner; assigned by <see cref="AppDbContext"/> in User mode (AD-16).</summary>
    public int UserId { get; set; }

    /// <summary>Name, unique per User case-insensitively (functional index on <c>(user_id, lower(name))</c>).</summary>
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
}
