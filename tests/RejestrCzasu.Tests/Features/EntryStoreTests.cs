using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Wpisy;

namespace RejestrCzasu.Tests.Features;

/// <summary>Adding Entries (R4, AD-4, AD-7, AD-16) on PostgreSQL.</summary>
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
