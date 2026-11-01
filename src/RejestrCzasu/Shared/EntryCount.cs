namespace RejestrCzasu.Shared;

/// <summary>Polish texts with a number of Entries (Wpisy).</summary>
public static class EntryCount
{
    /// <summary>
    /// The number in the locative after „w”: „1 Wpisie”, otherwise „{n} Wpisach” (the plural locative is the same for
    /// every other number), e.g. „Projekt RDZ występuje w 3 Wpisach”.
    /// </summary>
    public static string InEntries(int count) => count == 1 ? "1 Wpisie" : $"{count} Wpisach";
}
