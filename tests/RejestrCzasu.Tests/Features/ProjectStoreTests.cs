using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Projekty;

namespace RejestrCzasu.Tests.Features;

/// <summary>Data of the Projekty screen (R1–R3, AD-4, AD-7, AD-16) on PostgreSQL.</summary>
public sealed class ProjectStoreTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private int _user;
    private int _otherUser;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync();
        _user = await postgres.CreateUserAsync("projekty-a");
        _otherUser = await postgres.CreateUserAsync("projekty-b");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Add_TrimsAndStoresCodeUpperCase_Active()
    {
        var store = StoreFor(_user);

        var change = await store.AddAsync("  rdz ", " Rozwój  ", Ct);

        Assert.Equal(new ProjectChange.Done("RDZ"), change);
        var row = Assert.Single(await store.ListAsync(Ct));
        Assert.Equal(("RDZ", "Rozwój", true, "Aktywny"), (row.Code, row.Name, row.IsActive, row.State));
    }

    [Fact]
    public async Task Add_DuplicateInOtherCase_SavesNothing()
    {
        var store = StoreFor(_user);
        await store.AddAsync("RDZ", "Rozwój", Ct);

        var change = await store.AddAsync("Rdz", "Inny", Ct);

        Assert.Equal(new ProjectChange.DuplicateCode("RDZ"), change);
        Assert.Equal(["Rozwój"], (await store.ListAsync(Ct)).Select(p => p.Name));
    }

    [Fact]
    public async Task TwoUsers_SameCode_BothSaved_ListsSeparated()
    {
        var store = StoreFor(_user);
        var otherStore = StoreFor(_otherUser);
        await store.AddAsync("RDZ", "Rozwój A", Ct);
        await store.AddAsync("XYZ", "Tylko A", Ct);

        var change = await otherStore.AddAsync("rdz", "Rozwój B", Ct);

        Assert.Equal(new ProjectChange.Done("RDZ"), change);
        Assert.Equal(["Rozwój A", "Tylko A"], (await store.ListAsync(Ct)).Select(p => p.Name));
        Assert.Equal(["Rozwój B"], (await otherStore.ListAsync(Ct)).Select(p => p.Name));
    }

    [Fact]
    public async Task List_IsSortedByCode_InPolish_WithInactive()
    {
        var store = StoreFor(_user);
        await store.AddAsync("MIELEC", "M", Ct);
        await store.AddAsync("łódź", "Ł", Ct);
        await store.AddAsync("LUBLIN", "L", Ct);
        var lublin = (await store.ListAsync(Ct)).Single(p => p.Code == "LUBLIN");
        await store.SetActiveAsync(lublin.Id, false, Ct);

        Assert.Equal(["LUBLIN", "ŁÓDŹ", "MIELEC"], (await store.ListAsync(Ct)).Select(p => p.Code));
    }

    [Fact]
    public async Task Update_ChangesCodeAndName_EntriesKeepTheProject()
    {
        var store = StoreFor(_user);
        await store.AddAsync("RDZ", "Rozwój", Ct);
        var id = (await store.ListAsync(Ct)).Single().Id;
        await AddEntriesAsync(_user, id, 2);

        var change = await store.UpdateAsync(id, " abc ", " Nowa nazwa ", Ct);

        Assert.Equal(new ProjectChange.Done("ABC"), change);
        var row = Assert.Single(await store.ListAsync(Ct));
        Assert.Equal((id, "ABC", "Nowa nazwa"), (row.Id, row.Code, row.Name));
        await using var db = await postgres.CreateUserContextAsync(_user);
        var entries = await db.TimeEntries.Include(e => e.Project).ToListAsync(Ct);
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.Equal((id, "ABC"), (e.ProjectId, e.Project.Code)));
    }

    [Fact]
    public async Task Update_ToExistingCodeInOtherCase_SavesNothing()
    {
        var store = StoreFor(_user);
        await store.AddAsync("RDZ", "Rozwój", Ct);
        await store.AddAsync("ABC", "Abc", Ct);
        var abc = (await store.ListAsync(Ct)).Single(p => p.Code == "ABC");

        var change = await store.UpdateAsync(abc.Id, "rdz", "Zmieniona", Ct);

        Assert.Equal(new ProjectChange.DuplicateCode("RDZ"), change);
        Assert.Equal(abc, (await store.ListAsync(Ct)).Single(p => p.Id == abc.Id));
    }

    [Fact]
    public async Task ChangesOfAnotherUsersProject_AreNotFound()
    {
        await StoreFor(_otherUser).AddAsync("OBC", "Cudzy", Ct);
        var foreignId = (await StoreFor(_otherUser).ListAsync(Ct)).Single().Id;
        var store = StoreFor(_user);

        Assert.IsType<ProjectChange.NotFound>(await store.UpdateAsync(foreignId, "MOJ", "Mój", Ct));
        Assert.IsType<ProjectChange.NotFound>(await store.SetActiveAsync(foreignId, false, Ct));
        Assert.IsType<ProjectChange.NotFound>(await store.DeleteAsync(foreignId, Ct));
        var foreign = Assert.Single(await StoreFor(_otherUser).ListAsync(Ct));
        Assert.Equal(("OBC", "Cudzy", true), (foreign.Code, foreign.Name, foreign.IsActive));
    }

    [Fact]
    public async Task Deactivate_ThenRestore_ChangesState()
    {
        var store = StoreFor(_user);
        await store.AddAsync("RDZ", "Rozwój", Ct);
        var id = (await store.ListAsync(Ct)).Single().Id;

        Assert.Equal(new ProjectChange.Done("RDZ"), await store.SetActiveAsync(id, false, Ct));
        Assert.Equal("Nieaktywny", (await store.ListAsync(Ct)).Single().State);

        Assert.Equal(new ProjectChange.Done("RDZ"), await store.SetActiveAsync(id, true, Ct));
        Assert.Equal("Aktywny", (await store.ListAsync(Ct)).Single().State);
    }

    [Fact]
    public async Task Delete_WithoutEntries_RemovesIt()
    {
        var store = StoreFor(_user);
        await store.AddAsync("RDZ", "Rozwój", Ct);
        var id = (await store.ListAsync(Ct)).Single().Id;

        Assert.Equal(new ProjectChange.Done("RDZ"), await store.DeleteAsync(id, Ct));
        Assert.Empty(await store.ListAsync(Ct));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Delete_WithEntries_DeletesNothing_AndCountsEntries(int entryCount)
    {
        var store = StoreFor(_user);
        await store.AddAsync("RDZ", "Rozwój", Ct);
        await store.AddAsync("INN", "Inny", Ct);
        var rows = await store.ListAsync(Ct);
        var id = rows.Single(p => p.Code == "RDZ").Id;
        await AddEntriesAsync(_user, id, entryCount);
        // Entries of another Project and of another User's Project with the same code do not count.
        await AddEntriesAsync(_user, rows.Single(p => p.Code == "INN").Id, 2);
        await StoreFor(_otherUser).AddAsync("RDZ", "Cudzy", Ct);
        await AddEntriesAsync(_otherUser, (await StoreFor(_otherUser).ListAsync(Ct)).Single().Id, 4);

        var change = await store.DeleteAsync(id, Ct);

        Assert.Equal(new ProjectChange.InUse("RDZ", entryCount), change);
        Assert.Contains(await store.ListAsync(Ct), p => p.Id == id);
    }

    private ProjectStore StoreFor(int userId) => new(postgres.CreateLoggedInUserDbContextFactory(userId));

    /// <summary>Adds <paramref name="count"/> Entries of the User with the Project and a new Work type.</summary>
    private async Task AddEntriesAsync(int userId, int projectId, int count)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        var workType = new WorkType { Name = $"Typ {Guid.NewGuid():N}" };
        db.WorkTypes.Add(workType);
        await db.SaveChangesAsync(Ct);
        for (var i = 0; i < count; i++)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                Date = new DateOnly(2026, 9, 1 + i),
                ProjectId = projectId,
                WorkTypeId = workType.Id,
                Description = $"Wpis {i}",
                Minutes = 60,
            });
        }
        await db.SaveChangesAsync(Ct);
    }
}
