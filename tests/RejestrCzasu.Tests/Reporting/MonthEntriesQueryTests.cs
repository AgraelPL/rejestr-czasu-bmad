using RejestrCzasu.Data;
using RejestrCzasu.Domain;
using RejestrCzasu.Reporting;

namespace RejestrCzasu.Tests.Reporting;

/// <summary>The Wpisy of a Miesiąc for <see cref="MonthReport"/> (AD-5, AD-16) on PostgreSQL.</summary>
public sealed class MonthEntriesQueryTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private int _user;
    private int _otherUser;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync();
        _user = await postgres.CreateUserAsync("miesiac-a");
        _otherUser = await postgres.CreateUserAsync("miesiac-b");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Load_OnlyDaysOfTheMonth_ProjectsCodeAndName_NeverAnotherUsers()
    {
        var mine = await SeedAsync(_user, "RDZ", "Development",
            (new DateOnly(2026, 8, 31), "Sierpień", 60),
            (new DateOnly(2026, 9, 1), "Pierwszy", 90),
            (new DateOnly(2026, 9, 30), "Ostatni", 120),
            (new DateOnly(2026, 10, 1), "Październik", 30));
        await SeedAsync(_otherUser, "CUD", "Cudza praca", (new DateOnly(2026, 9, 15), "Cudzy", 60));

        var entries = await Query(_user).LoadAsync(new YearMonth(2026, 9), Ct);

        Assert.Equal(
            [
                new MonthEntry(mine["Pierwszy"], new DateOnly(2026, 9, 1), mine["project"], "RDZ", mine["workType"], "Development", "Pierwszy", 90),
                new MonthEntry(mine["Ostatni"], new DateOnly(2026, 9, 30), mine["project"], "RDZ", mine["workType"], "Development", "Ostatni", 120),
            ],
            entries.OrderBy(e => e.Id));
    }

    [Fact]
    public async Task Load_EmptyMonth_Empty()
    {
        await SeedAsync(_user, "RDZ", "Development", (new DateOnly(2026, 8, 31), "Sierpień", 60));

        Assert.Empty(await Query(_user).LoadAsync(new YearMonth(2026, 9), Ct));
    }

    private MonthEntriesQuery Query(int userId) => new(postgres.CreateLoggedInUserDbContextFactory(userId));

    // One Projekt and one Typ pracy of the User, then the Wpisy; returns the ids by description plus "project", "workType".
    private async Task<Dictionary<string, int>> SeedAsync(
        int userId, string code, string workTypeName, params (DateOnly Date, string Description, int Minutes)[] entries)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        var project = new Project { Code = code, Name = code };
        var workType = new WorkType { Name = workTypeName };
        db.Projects.Add(project);
        db.WorkTypes.Add(workType);
        await db.SaveChangesAsync(Ct);
        var ids = new Dictionary<string, int> { ["project"] = project.Id, ["workType"] = workType.Id };
        foreach (var (date, description, minutes) in entries)
        {
            var entry = new TimeEntry
            {
                Date = date, ProjectId = project.Id, WorkTypeId = workType.Id, Description = description, Minutes = minutes,
            };
            db.TimeEntries.Add(entry);
            await db.SaveChangesAsync(Ct);
            ids[description] = entry.Id;
        }
        return ids;
    }
}
