namespace RejestrCzasu.Data;

public class TimeEntry : IUserOwned
{
    /// <summary>
    /// The longest Opis, counted after trimming (EntryFormValidator, AD-8); the column stays <c>text</c>, no database limit.
    /// </summary>
    public const int DescriptionMaxLength = 500;

    public int Id { get; set; }

    /// <summary>Owner; assigned by <see cref="AppDbContext"/> in User mode (AD-16). Part of both dictionary foreign keys.</summary>
    public int UserId { get; set; }

    public DateOnly Date { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int WorkTypeId { get; set; }
    public WorkType WorkType { get; set; } = null!;

    public string Description { get; set; } = "";

    /// <summary>Duration in whole minutes.</summary>
    public int Minutes { get; set; }
}
