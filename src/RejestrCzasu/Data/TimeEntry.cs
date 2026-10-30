namespace RejestrCzasu.Data;

public class TimeEntry
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int WorkTypeId { get; set; }
    public WorkType WorkType { get; set; } = null!;

    public string Description { get; set; } = "";

    /// <summary>Duration in whole minutes.</summary>
    public int Minutes { get; set; }
}
