using RejestrCzasu.Data;

namespace RejestrCzasu.Shared;

/// <summary>
/// Model of the Entry form (<see cref="EntryForm"/>, AD-10). The Project and the Work type are the chosen options of
/// <see cref="DictionaryOptions"/>; Czas is the text typed by the User, parsed only by <c>Domain/Czas</c> (AD-6).
/// </summary>
public sealed class EntryInput
{
    /// <summary><c>null</c> when the User has cleared the date field.</summary>
    public DateOnly? Date { get; set; }

    public ProjectOption? Project { get; set; }

    public WorkTypeOption? WorkType { get; set; }

    public string Description { get; set; } = "";

    /// <summary>Czas as typed, e.g. <c>1h 30m</c>, <c>45m</c> or <c>1:30</c>.</summary>
    public string Czas { get; set; } = "";

    /// <summary>After a saved Entry in the Sesja wpisywania (R5): the date stays, everything else is emptied.</summary>
    public void ClearAfterSave()
    {
        Project = null;
        WorkType = null;
        Description = "";
        Czas = "";
    }
}
