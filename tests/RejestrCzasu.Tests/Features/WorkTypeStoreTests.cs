using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;
using RejestrCzasu.Features.TypyPracy;

namespace RejestrCzasu.Tests.Features;

/// <summary>Data of the Typy pracy screen (R1–R3, AD-4, AD-7, AD-16) on PostgreSQL.</summary>
public sealed class WorkTypeStoreTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private int _user;
    private int _otherUser;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync();
        _user = await postgres.CreateUserAsync("typy-a");
        _otherUser = await postgres.CreateUserAsync("typy-b");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Add_TrimsAndKeepsLetterCase_Active()
    {
        var store = StoreFor(_user);

        var change = await store.AddAsync("  Development ", Ct);

        Assert.Equal(new WorkTypeChange.Done("Development"), change);
        var row = Assert.Single(await store.ListAsync(Ct));
        Assert.Equal(("Development", true, "Aktywny"), (row.Name, row.IsActive, row.State));
    }

    [Fact]
    public async Task Add_DuplicateInOtherCase_SavesNothing()
    {
        var store = StoreFor(_user);
        await store.AddAsync("Development", Ct);

        var change = await store.AddAsync("development", Ct);

        Assert.Equal(new WorkTypeChange.DuplicateName("development"), change);
        Assert.Equal(["Development"], (await store.ListAsync(Ct)).Select(w => w.Name));
    }

    [Fact]
    public async Task TwoUsers_SameName_BothSaved_ListsSeparated()
    {
        var store = StoreFor(_user);
        var otherStore = StoreFor(_otherUser);
        await store.AddAsync("Development", Ct);
        await store.AddAsync("Tylko A", Ct);

        var change = await otherStore.AddAsync("Development", Ct);

        Assert.Equal(new WorkTypeChange.Done("Development"), change);
        Assert.Equal(["Development", "Tylko A"], (await store.ListAsync(Ct)).Select(w => w.Name));
        Assert.Equal(["Development"], (await otherStore.ListAsync(Ct)).Select(w => w.Name));
    }

    [Fact]
    public async Task List_IsSortedByName_InPolish_WithInactive()
    {
        var store = StoreFor(_user);
        await store.AddAsync("Montaż", Ct);
        await store.AddAsync("łączenie", Ct);
        await store.AddAsync("Lutowanie", Ct);
        var lutowanie = (await store.ListAsync(Ct)).Single(w => w.Name == "Lutowanie");
        await store.SetActiveAsync(lutowanie.Id, false, Ct);

        Assert.Equal(["Lutowanie", "łączenie", "Montaż"], (await store.ListAsync(Ct)).Select(w => w.Name));
    }

    [Fact]
    public async Task Update_ChangesName_EntriesKeepTheWorkType()
    {
        var store = StoreFor(_user);
        await store.AddAsync("Development", Ct);
        var id = (await store.ListAsync(Ct)).Single().Id;
        await AddEntriesAsync(_user, id, 2);

        var change = await store.UpdateAsync(id, " Programowanie ", Ct);

        Assert.Equal(new WorkTypeChange.Done("Programowanie"), change);
        var row = Assert.Single(await store.ListAsync(Ct));
        Assert.Equal((id, "Programowanie"), (row.Id, row.Name));
        await using var db = await postgres.CreateUserContextAsync(_user);
        var entries = await db.TimeEntries.Include(e => e.WorkType).ToListAsync(Ct);
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.Equal((id, "Programowanie"), (e.WorkTypeId, e.WorkType.Name)));
    }

    [Fact]
    public async Task Update_ToExistingNameInOtherCase_SavesNothing()
    {
        var store = StoreFor(_user);
        await store.AddAsync("Development", Ct);
        await store.AddAsync("Spotkanie", Ct);
        var meeting = (await store.ListAsync(Ct)).Single(w => w.Name == "Spotkanie");

        var change = await store.UpdateAsync(meeting.Id, "DEVELOPMENT", Ct);

        Assert.Equal(new WorkTypeChange.DuplicateName("DEVELOPMENT"), change);
        Assert.Equal(meeting, (await store.ListAsync(Ct)).Single(w => w.Id == meeting.Id));
    }

    [Fact]
    public async Task Update_OnlyLetterCaseOfOwnName_IsSaved()
    {
        var store = StoreFor(_user);
        await store.AddAsync("development", Ct);
        var id = (await store.ListAsync(Ct)).Single().Id;

        Assert.Equal(new WorkTypeChange.Done("Development"), await store.UpdateAsync(id, "Development", Ct));
        Assert.Equal("Development", (await store.ListAsync(Ct)).Single().Name);
    }

    [Fact]
    public async Task ChangesOfAnotherUsersWorkType_AreNotFound()
    {
        await StoreFor(_otherUser).AddAsync("Cudzy", Ct);
        var foreignId = (await StoreFor(_otherUser).ListAsync(Ct)).Single().Id;
        var store = StoreFor(_user);

        Assert.IsType<WorkTypeChange.NotFound>(await store.UpdateAsync(foreignId, "Mój", Ct));
        Assert.IsType<WorkTypeChange.NotFound>(await store.SetActiveAsync(foreignId, false, Ct));
        Assert.IsType<WorkTypeChange.NotFound>(await store.DeleteAsync(foreignId, Ct));
        var foreign = Assert.Single(await StoreFor(_otherUser).ListAsync(Ct));
        Assert.Equal(("Cudzy", true), (foreign.Name, foreign.IsActive));
    }

    [Fact]
    public async Task Deactivate_ThenRestore_ChangesState()
    {
        var store = StoreFor(_user);
        await store.AddAsync("Development", Ct);
        var id = (await store.ListAsync(Ct)).Single().Id;

        Assert.Equal(new WorkTypeChange.Done("Development"), await store.SetActiveAsync(id, false, Ct));
        Assert.Equal("Nieaktywny", (await store.ListAsync(Ct)).Single().State);

        Assert.Equal(new WorkTypeChange.Done("Development"), await store.SetActiveAsync(id, true, Ct));
        Assert.Equal("Aktywny", (await store.ListAsync(Ct)).Single().State);
    }

    [Fact]
    public async Task Delete_WithoutEntries_RemovesIt()
    {
        var store = StoreFor(_user);
        await store.AddAsync("Development", Ct);
        var id = (await store.ListAsync(Ct)).Single().Id;

        Assert.Equal(new WorkTypeChange.Done("Development"), await store.DeleteAsync(id, Ct));
        Assert.Empty(await store.ListAsync(Ct));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Delete_WithEntries_DeletesNothing_AndCountsEntries(int entryCount)
    {
        var store = StoreFor(_user);
        await store.AddAsync("Development", Ct);
        await store.AddAsync("Inny", Ct);
        var rows = await store.ListAsync(Ct);
        var id = rows.Single(w => w.Name == "Development").Id;
        await AddEntriesAsync(_user, id, entryCount);
        // Entries of another Work type and of another User's Work type with the same name do not count.
        await AddEntriesAsync(_user, rows.Single(w => w.Name == "Inny").Id, 2);
        await StoreFor(_otherUser).AddAsync("Development", Ct);
        await AddEntriesAsync(_otherUser, (await StoreFor(_otherUser).ListAsync(Ct)).Single().Id, 4);

        var change = await store.DeleteAsync(id, Ct);

        Assert.Equal(new WorkTypeChange.InUse("Development", entryCount), change);
        Assert.Contains(await store.ListAsync(Ct), w => w.Id == id);
    }

    private WorkTypeStore StoreFor(int userId) => new(postgres.CreateLoggedInUserDbContextFactory(userId));

    /// <summary>Adds <paramref name="count"/> Entries of the User with the Work type and a new Project.</summary>
    private async Task AddEntriesAsync(int userId, int workTypeId, int count)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        var project = new Project { Code = $"P{Guid.NewGuid():N}"[..20], Name = "Projekt" };
        db.Projects.Add(project);
        await db.SaveChangesAsync(Ct);
        for (var i = 0; i < count; i++)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                Date = new DateOnly(2026, 9, 1 + i),
                ProjectId = project.Id,
                WorkTypeId = workTypeId,
                Description = $"Wpis {i}",
                Minutes = 60,
            });
        }
        await db.SaveChangesAsync(Ct);
    }
}
