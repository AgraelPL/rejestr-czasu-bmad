using RejestrCzasu.Reporting;

namespace RejestrCzasu.Tests.Reporting;

/// <summary>
/// Fictional data in the shape of September 2026 (export-layout.md „Kontrola: wrzesień 2026”): week sums
/// 30 / 16 / 18 / 25 / 14 = 103 h, 31 Wpisy giving 28 Wiersze zestawienia. Merges: 02.09 two Wpisy AAA/Development
/// (Id 9 and 4); 15.09 three Wpisy BBB/Analiza (Id 14, 16, 17) with BBB/Testy Id 15 between them. AAA/Development on
/// other days is never merged with them. Descriptions „Zadanie N”, N = Id.
/// </summary>
internal static class SeptemberShape
{
    public static readonly string[] Projects = ["AAA", "BBB", "CCC"];

    public static readonly string[] WorkTypes = ["Analiza", "Development", "Serwis", "Środowisko", "Testy"];

    /// <summary>(Id, day of September, Projekt code, Typ pracy name, minutes), ascending by Id.</summary>
    public static readonly (int Id, int Day, string Project, string WorkType, int Minutes)[] Raw =
    [
        // Week 1: 01–04.09 → 8 / 8 / 8 / 6 h = 30 h
        (1, 1, "AAA", "Development", 240), (2, 1, "BBB", "Testy", 120), (3, 1, "CCC", "Serwis", 120),
        (4, 2, "AAA", "Development", 120), (5, 2, "CCC", "Analiza", 240), (9, 2, "AAA", "Development", 120),
        (6, 3, "BBB", "Development", 480),
        (7, 4, "AAA", "Środowisko", 180), (8, 4, "AAA", "Serwis", 180),
        // Week 2: 07–08.09 → 8 / 8 h = 16 h
        (10, 7, "CCC", "Testy", 480),
        (11, 8, "AAA", "Analiza", 240), (12, 8, "BBB", "Analiza", 240),
        // Week 3: 14–16.09 → 6 / 6 / 6 h = 18 h
        (13, 14, "AAA", "Development", 360),
        (14, 15, "BBB", "Analiza", 60), (15, 15, "BBB", "Testy", 60), (16, 15, "BBB", "Analiza", 120),
        (17, 15, "BBB", "Analiza", 60), (18, 15, "AAA", "Serwis", 60),
        (19, 16, "CCC", "Development", 360),
        // Week 4: 21–23.09 → 8 / 8 / 8 h and Saturday 26.09 1 h = 25 h
        (20, 21, "AAA", "Development", 480),
        (21, 22, "BBB", "Testy", 240), (22, 22, "CCC", "Testy", 240),
        (23, 23, "AAA", "Analiza", 480),
        (24, 26, "CCC", "Serwis", 60),
        // Week 5: 28–29.09 → 6 / 8 h = 14 h
        (25, 28, "AAA", "Development", 180), (26, 28, "BBB", "Środowisko", 180),
        (27, 29, "AAA", "Testy", 120), (28, 29, "BBB", "Serwis", 120), (29, 29, "CCC", "Analiza", 120),
        (30, 29, "CCC", "Development", 60), (31, 29, "CCC", "Środowisko", 60),
    ];

    /// <summary>The Wpisy as <see cref="MonthEntriesQuery"/> gives them; ProjectId and WorkTypeId are 1-based list positions.</summary>
    public static IReadOnlyList<MonthEntry> Entries() =>
        [.. Raw.OrderBy(r => r.Id).Select(r => new MonthEntry(
            r.Id, new DateOnly(2026, 9, r.Day),
            Array.IndexOf(Projects, r.Project) + 1, r.Project,
            Array.IndexOf(WorkTypes, r.WorkType) + 1, r.WorkType,
            Description(r.Id), r.Minutes))];

    /// <summary>The Wpisy in a fixed, mixed order.</summary>
    public static IReadOnlyList<MonthEntry> Shuffled()
    {
        var entries = Entries().ToArray();
        new Random(20260901).Shuffle(entries);
        return entries;
    }

    public static string Description(int id) => $"Zadanie {id}";
}
