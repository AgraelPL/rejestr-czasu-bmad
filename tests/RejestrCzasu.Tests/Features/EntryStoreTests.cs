using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Wpisy;

namespace RejestrCzasu.Tests.Features;

/// <summary>Adding, listing, finding, editing and deleting Entries (R4, R6, R8, AD-4, AD-7, AD-16) on PostgreSQL.</summary>
public sealed class EntryStoreTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private int _user;
    private int _otherUser;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync();
        _user = await postgres.CreateUserAsync("wpisy-a");
        _otherUser = await postgres.CreateUserAsync("wpisy-b");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Add_SavesEntryOfTheUser_InMinutes_WithTrimmedDescription()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);

        var change = await StoreFor(_user).AddAsync(new DateOnly(2026, 9, 3), projectId, workTypeId, "  Spotkanie ", "1:30", Ct);

        var done = Assert.IsType<EntryChange.Done>(change);
        Assert.Equal(90, done.Minutes);
        await using var db = await postgres.CreateUserContextAsync(_user);
        var entry = Assert.Single(await db.TimeEntries.ToListAsync(Ct));
        Assert.Equal(
            (done.Id, _user, new DateOnly(2026, 9, 3), projectId, workTypeId, "Spotkanie", 90),
            (entry.Id, entry.UserId, entry.Date, entry.ProjectId, entry.WorkTypeId, entry.Description, entry.Minutes));
    }

    [Fact]
    public async Task Add_TwoEntriesOf24Hours_SameDay_BothSaved()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var store = StoreFor(_user);

        Assert.IsType<EntryChange.Done>(await store.AddAsync(new DateOnly(2026, 9, 3), projectId, workTypeId, "A", "24:00", Ct));
        Assert.IsType<EntryChange.Done>(await store.AddAsync(new DateOnly(2026, 9, 3), projectId, workTypeId, "B", "24:00", Ct));

        await using var db = await postgres.CreateUserContextAsync(_user);
        Assert.Equal(2880, await db.TimeEntries.SumAsync(e => e.Minutes, Ct));
    }

    [Fact]
    public async Task Add_IsSeparatedPerUser()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);

        await StoreFor(_user).AddAsync(new DateOnly(2026, 9, 3), projectId, workTypeId, "Mój", "1:00", Ct);

        await using var other = await postgres.CreateUserContextAsync(_otherUser);
        Assert.Empty(await other.TimeEntries.ToListAsync(Ct));
        await using var own = await postgres.CreateUserContextAsync(_user);
        Assert.Single(await own.TimeEntries.ToListAsync(Ct));
    }

    [Fact]
    public async Task Add_WithAnotherUsersDictionaryItems_SavesNothing()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var (ownProjectId, ownWorkTypeId) = await SeedDictionariesAsync(_otherUser);
        var store = StoreFor(_otherUser);

        Assert.IsType<EntryChange.MissingDictionaryItem>(
            await store.AddAsync(new DateOnly(2026, 9, 3), projectId, ownWorkTypeId, "Cudzy Projekt", "1:00", Ct));
        Assert.IsType<EntryChange.MissingDictionaryItem>(
            await store.AddAsync(new DateOnly(2026, 9, 3), ownProjectId, workTypeId, "Cudzy Typ pracy", "1:00", Ct));

        await using var db = await postgres.CreateUserContextAsync(_otherUser);
        Assert.Empty(await db.TimeEntries.ToListAsync(Ct));
    }

    [Fact]
    public async Task Add_ProjectDeletedInTheMeantime_SavesNothing()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        await using (var db = await postgres.CreateUserContextAsync(_user))
        {
            db.Projects.Remove(await db.Projects.SingleAsync(p => p.Id == projectId, Ct));
            await db.SaveChangesAsync(Ct);
        }

        var change = await StoreFor(_user).AddAsync(new DateOnly(2026, 9, 3), projectId, workTypeId, "Opis", "1:00", Ct);

        Assert.IsType<EntryChange.MissingDictionaryItem>(change);
        await using var check = await postgres.CreateUserContextAsync(_user);
        Assert.Empty(await check.TimeEntries.ToListAsync(Ct));
    }

    [Theory]
    [InlineData("0:10")]
    [InlineData("1,5")]
    [InlineData("")]
    public async Task Add_InvalidCzas_Throws(string czas)
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            StoreFor(_user).AddAsync(new DateOnly(2026, 9, 3), projectId, workTypeId, "Opis", czas, Ct));
    }

    [Fact]
    public async Task List_DateRange_InclusiveBounds_NewestDateFirst_ThenHigherIdFirst()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var store = StoreFor(_user);
        await AddAsync(store, new DateOnly(2026, 8, 31), projectId, workTypeId, "Sierpień");
        await AddAsync(store, new DateOnly(2026, 9, 1), projectId, workTypeId, "Pierwszy");
        await AddAsync(store, new DateOnly(2026, 9, 30), projectId, workTypeId, "Ostatni A");
        await AddAsync(store, new DateOnly(2026, 9, 30), projectId, workTypeId, "Ostatni B");
        await AddAsync(store, new DateOnly(2026, 9, 15), projectId, workTypeId, "Środek");
        await AddAsync(store, new DateOnly(2026, 10, 1), projectId, workTypeId, "Październik");

        var rows = await store.ListAsync(new EntryFilter(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null, null), Ct);

        Assert.Equal(["Ostatni B", "Ostatni A", "Środek", "Pierwszy"], rows.Select(r => r.Description));
        var first = rows[0];
        Assert.Equal(("RDZ", "Development", projectId, workTypeId, 60), (first.ProjectCode, first.WorkTypeName, first.ProjectId, first.WorkTypeId, first.Minutes));
        Assert.Equal(("30.09.2026", "Środa", "1h"), (first.DateText, first.DayName, first.CzasText));
    }

    [Fact]
    public async Task List_EmptyBounds_AreNoBound()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var store = StoreFor(_user);
        await AddAsync(store, new DateOnly(2025, 1, 1), projectId, workTypeId, "Dawno");
        await AddAsync(store, new DateOnly(2026, 9, 3), projectId, workTypeId, "Teraz");
        await AddAsync(store, new DateOnly(2027, 1, 1), projectId, workTypeId, "Później");

        var untilTo = await store.ListAsync(new EntryFilter(null, new DateOnly(2026, 9, 3), null, null), Ct);
        var fromOnly = await store.ListAsync(new EntryFilter(new DateOnly(2026, 9, 3), null, null, null), Ct);
        var all = await store.ListAsync(new EntryFilter(null, null, null, null), Ct);

        Assert.Equal(["Teraz", "Dawno"], untilTo.Select(r => r.Description));
        Assert.Equal(["Później", "Teraz"], fromOnly.Select(r => r.Description));
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task List_ProjectAndWorkTypeFilters_IncludingInactiveProject()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        int oldId, meetingId;
        await using (var db = await postgres.CreateUserContextAsync(_user))
        {
            var inactive = new Project { Code = "OLD", Name = "Stary", IsActive = false };
            var meeting = new WorkType { Name = "Spotkanie" };
            db.Projects.Add(inactive);
            db.WorkTypes.Add(meeting);
            await db.SaveChangesAsync(Ct);
            (oldId, meetingId) = (inactive.Id, meeting.Id);
        }
        var store = StoreFor(_user);
        await AddAsync(store, new DateOnly(2026, 9, 1), projectId, workTypeId, "RDZ dev");
        await AddAsync(store, new DateOnly(2026, 9, 2), projectId, meetingId, "RDZ spotkanie");
        await AddAsync(store, new DateOnly(2026, 9, 3), oldId, workTypeId, "OLD dev");

        var rdz = await store.ListAsync(new EntryFilter(null, null, projectId, null), Ct);
        var old = await store.ListAsync(new EntryFilter(null, null, oldId, null), Ct);
        var meetings = await store.ListAsync(new EntryFilter(null, null, null, meetingId), Ct);
        var both = await store.ListAsync(new EntryFilter(null, null, projectId, workTypeId), Ct);

        Assert.Equal(["RDZ spotkanie", "RDZ dev"], rdz.Select(r => r.Description));
        Assert.Equal(("OLD dev", "OLD"), (Assert.Single(old).Description, old[0].ProjectCode));
        Assert.Equal("RDZ spotkanie", Assert.Single(meetings).Description);
        Assert.Equal("RDZ dev", Assert.Single(both).Description);
    }

    [Fact]
    public async Task List_NeverShowsAnotherUsersEntries()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var (otherProjectId, otherWorkTypeId) = await SeedDictionariesAsync(_otherUser);
        await AddAsync(StoreFor(_user), new DateOnly(2026, 9, 3), projectId, workTypeId, "Mój");
        await AddAsync(StoreFor(_otherUser), new DateOnly(2026, 9, 3), otherProjectId, otherWorkTypeId, "Cudzy");

        var all = await StoreFor(_user).ListAsync(new EntryFilter(null, null, null, null), Ct);
        var byOtherProject = await StoreFor(_user).ListAsync(new EntryFilter(null, null, otherProjectId, null), Ct);
        var byOtherWorkType = await StoreFor(_user).ListAsync(new EntryFilter(null, null, null, otherWorkTypeId), Ct);

        Assert.Equal("Mój", Assert.Single(all).Description);
        Assert.Empty(byOtherProject);
        Assert.Empty(byOtherWorkType);
    }

    [Fact]
    public async Task Find_ReturnsRowOfTheUser_NullForAnotherUserOrMissing()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var id = await AddAsync(StoreFor(_user), new DateOnly(2026, 8, 20), projectId, workTypeId, "Sierpień");

        var row = await StoreFor(_user).FindAsync(id, Ct);

        Assert.NotNull(row);
        Assert.Equal((id, new DateOnly(2026, 8, 20), "RDZ", "Development", "Sierpień", 60),
            (row.Id, row.Date, row.ProjectCode, row.WorkTypeName, row.Description, row.Minutes));
        Assert.Null(await StoreFor(_otherUser).FindAsync(id, Ct));
        Assert.Null(await StoreFor(_user).FindAsync(id + 1000, Ct));
    }

    [Fact]
    public async Task Update_ChangesEveryField_InAPastMonth()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var (otherProjectId, otherWorkTypeId) = await AddInactiveDictionariesAsync(_user);
        var store = StoreFor(_user);
        var id = await AddAsync(store, new DateOnly(2026, 8, 20), projectId, workTypeId, "Sierpień");

        var change = await store.UpdateAsync(id, new DateOnly(2026, 7, 31), otherProjectId, otherWorkTypeId, "  Lipiec ", "2h 15m", Ct);

        Assert.Equal(new EntryChange.Done(id, 135), change);
        await using var db = await postgres.CreateUserContextAsync(_user);
        var entry = Assert.Single(await db.TimeEntries.ToListAsync(Ct));
        Assert.Equal(
            (new DateOnly(2026, 7, 31), otherProjectId, otherWorkTypeId, "Lipiec", 135),
            (entry.Date, entry.ProjectId, entry.WorkTypeId, entry.Description, entry.Minutes));
    }

    [Fact]
    public async Task Update_AnotherUsersEntry_IsNotFound_AndUntouched()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var (otherProjectId, otherWorkTypeId) = await SeedDictionariesAsync(_otherUser);
        var id = await AddAsync(StoreFor(_user), new DateOnly(2026, 9, 3), projectId, workTypeId, "Mój");

        var change = await StoreFor(_otherUser).UpdateAsync(id, new DateOnly(2026, 9, 4), otherProjectId, otherWorkTypeId, "Cudzy", "2h", Ct);

        Assert.IsType<EntryChange.NotFound>(change);
        await using var db = await postgres.CreateUserContextAsync(_user);
        var entry = Assert.Single(await db.TimeEntries.ToListAsync(Ct));
        Assert.Equal((new DateOnly(2026, 9, 3), "Mój", 60), (entry.Date, entry.Description, entry.Minutes));
    }

    [Fact]
    public async Task Update_WithAnotherUsersDictionaryItem_IsMissingDictionaryItem_AndUntouched()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var (otherProjectId, otherWorkTypeId) = await SeedDictionariesAsync(_otherUser);
        var store = StoreFor(_user);
        var id = await AddAsync(store, new DateOnly(2026, 9, 3), projectId, workTypeId, "Mój");

        Assert.IsType<EntryChange.MissingDictionaryItem>(
            await store.UpdateAsync(id, new DateOnly(2026, 9, 3), otherProjectId, workTypeId, "Mój", "1h", Ct));
        Assert.IsType<EntryChange.MissingDictionaryItem>(
            await store.UpdateAsync(id, new DateOnly(2026, 9, 3), projectId, otherWorkTypeId, "Mój", "1h", Ct));

        await using var db = await postgres.CreateUserContextAsync(_user);
        var entry = Assert.Single(await db.TimeEntries.ToListAsync(Ct));
        Assert.Equal((projectId, workTypeId), (entry.ProjectId, entry.WorkTypeId));
    }

    [Fact]
    public async Task Update_MissingEntry_IsNotFound()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);

        var change = await StoreFor(_user).UpdateAsync(12345, new DateOnly(2026, 9, 3), projectId, workTypeId, "Opis", "1h", Ct);

        Assert.IsType<EntryChange.NotFound>(change);
    }

    [Theory]
    [InlineData("0:10")]
    [InlineData("")]
    public async Task Update_InvalidCzas_Throws(string czas)
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var id = await AddAsync(StoreFor(_user), new DateOnly(2026, 9, 3), projectId, workTypeId, "Opis");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            StoreFor(_user).UpdateAsync(id, new DateOnly(2026, 9, 3), projectId, workTypeId, "Opis", czas, Ct));
    }

    [Fact]
    public async Task Delete_RemovesEntry_ThenNotFound()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var store = StoreFor(_user);
        var id = await AddAsync(store, new DateOnly(2026, 8, 20), projectId, workTypeId, "Sierpień");
        var kept = await AddAsync(store, new DateOnly(2026, 9, 3), projectId, workTypeId, "Zostaje");

        Assert.Equal(new EntryChange.Done(id, 60), await store.DeleteAsync(id, Ct));
        Assert.IsType<EntryChange.NotFound>(await store.DeleteAsync(id, Ct));

        await using var db = await postgres.CreateUserContextAsync(_user);
        Assert.Equal(kept, Assert.Single(await db.TimeEntries.ToListAsync(Ct)).Id);
    }

    [Fact]
    public async Task Delete_AnotherUsersEntry_IsNotFound_AndUntouched()
    {
        var (projectId, workTypeId) = await SeedDictionariesAsync(_user);
        var id = await AddAsync(StoreFor(_user), new DateOnly(2026, 9, 3), projectId, workTypeId, "Mój");

        Assert.IsType<EntryChange.NotFound>(await StoreFor(_otherUser).DeleteAsync(id, Ct));

        await using var db = await postgres.CreateUserContextAsync(_user);
        Assert.Single(await db.TimeEntries.ToListAsync(Ct));
    }

    private static async Task<int> AddAsync(EntryStore store, DateOnly date, int projectId, int workTypeId, string description) =>
        Assert.IsType<EntryChange.Done>(await store.AddAsync(date, projectId, workTypeId, description, "1h", Ct)).Id;

    private async Task<(int ProjectId, int WorkTypeId)> AddInactiveDictionariesAsync(int userId)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        var project = new Project { Code = "OLD", Name = "Stary", IsActive = false };
        var workType = new WorkType { Name = "Archiwalny", IsActive = false };
        db.Projects.Add(project);
        db.WorkTypes.Add(workType);
        await db.SaveChangesAsync(Ct);
        return (project.Id, workType.Id);
    }

    private EntryStore StoreFor(int userId) => new(postgres.CreateLoggedInUserDbContextFactory(userId));

    private async Task<(int ProjectId, int WorkTypeId)> SeedDictionariesAsync(int userId)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        var project = new Project { Code = "RDZ", Name = "Rozwój" };
        var workType = new WorkType { Name = "Development" };
        db.Projects.Add(project);
        db.WorkTypes.Add(workType);
        await db.SaveChangesAsync(Ct);
        return (project.Id, workType.Id);
    }
}
