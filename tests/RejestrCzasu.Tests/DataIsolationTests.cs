using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;

namespace RejestrCzasu.Tests;

/// <summary>Data separation between Users through the two modes of <see cref="AppDbContext"/> (AD-16).</summary>
public sealed class DataIsolationTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private int _userA;
    private int _userB;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync();
        _userA = await postgres.CreateUserAsync("test-a");
        _userB = await postgres.CreateUserAsync("test-b");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task EachUser_SeesOnlyOwnData()
    {
        await SeedAsync(_userA, "AAA", "Analiza", "Opis A");
        await SeedAsync(_userB, "BBB", "Budowa", "Opis B");

        await AssertSeesOnlyAsync(_userA, "AAA", "Analiza", "Opis A");
        await AssertSeesOnlyAsync(_userB, "BBB", "Budowa", "Opis B");
    }

    [Fact]
    public async Task NewRecords_GetUserIdOfTheContext()
    {
        await SeedAsync(_userA, "AAA", "Analiza", "Opis A");

        await using var system = postgres.CreateContext();
        var owners = await system.Database
            .SqlQueryRaw<int>(
                "SELECT user_id AS \"Value\" FROM project UNION ALL SELECT user_id FROM work_type " +
                "UNION ALL SELECT user_id FROM time_entry UNION ALL SELECT user_id FROM account_settings")
            .ToListAsync(Ct);
        Assert.Equal(4, owners.Count);
        Assert.All(owners, owner => Assert.Equal(_userA, owner));
    }

    [Fact]
    public async Task SameProjectCode_ForTwoUsers_IsSaved()
    {
        foreach (var userId in new[] { _userA, _userB })
        {
            await using var db = await postgres.CreateUserContextAsync(userId);
            db.Projects.Add(new Project { Code = "RDZ", Name = "Projekt testowy" });
            db.WorkTypes.Add(new WorkType { Name = "Dev" });
            await db.SaveChangesAsync(Ct);
        }

        foreach (var userId in new[] { _userA, _userB })
        {
            await using var db = await postgres.CreateUserContextAsync(userId);
            Assert.Equal("RDZ", await db.Projects.Select(p => p.Code).SingleAsync(Ct));
            Assert.Equal("Dev", await db.WorkTypes.Select(w => w.Name).SingleAsync(Ct));
        }
    }

    [Fact]
    public async Task EntryWithDictionaryOfAnotherUser_IsRejectedByDatabase()
    {
        var (projectB, workTypeB) = await SeedAsync(_userB, "BBB", "Budowa", "Opis B");
        var (projectA, workTypeA) = await SeedAsync(_userA, "AAA", "Analiza", "Opis A");

        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            db.TimeEntries.Add(NewEntry(projectB, workTypeA));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
        }

        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            db.TimeEntries.Add(NewEntry(projectA, workTypeB));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
        }

        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            Assert.Equal(1, await db.TimeEntries.CountAsync(Ct));
        }
    }

    [Fact]
    public async Task SystemMode_SeesNoUserOwnedData()
    {
        await SeedAsync(_userA, "AAA", "Analiza", "Opis A");

        await using var db = postgres.CreateContext();
        Assert.Null(db.CurrentUserId);
        Assert.Empty(await db.Projects.ToListAsync(Ct));
        Assert.Empty(await db.WorkTypes.ToListAsync(Ct));
        Assert.Empty(await db.TimeEntries.ToListAsync(Ct));
        Assert.Empty(await db.AccountSettings.ToListAsync(Ct));
        // Identity data is not user-owned and stays visible.
        Assert.Equal(2, await db.Users.CountAsync(Ct));
    }

    [Fact]
    public async Task SystemMode_SavingUserOwnedEntity_ThrowsBeforeSave()
    {
        await using (var db = postgres.CreateContext())
        {
            db.Projects.Add(new Project { UserId = _userA, Code = "AAA", Name = "Projekt testowy" });
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
            Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        }

        await AssertProjectCountAsync(_userA, 0);
    }

    [Fact]
    public async Task UserMode_ChangingUserIdOfExistingProject_ThrowsBeforeSave()
    {
        var (projectA, _) = await SeedAsync(_userA, "AAA", "Analiza", "Opis A");

        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            var project = await db.Projects.SingleAsync(p => p.Id == projectA, Ct);
            project.UserId = _userB;
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        }

        await AssertProjectCountAsync(_userA, 1);
        await AssertProjectCountAsync(_userB, 0);
    }

    [Fact]
    public async Task UserMode_ChangingUserIdOfExistingEntry_ThrowsBeforeSave()
    {
        await SeedAsync(_userA, "AAA", "Analiza", "Opis A");

        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            var entry = await db.TimeEntries.SingleAsync(Ct);
            entry.UserId = _userB;
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        }

        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            Assert.Equal(1, await db.TimeEntries.CountAsync(Ct));
        }
    }

    [Fact]
    public async Task UserMode_AddingRecordForAnotherUser_ThrowsBeforeSave()
    {
        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            db.Projects.Add(new Project { UserId = _userB, Code = "BBB", Name = "Projekt testowy" });
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        }

        await AssertProjectCountAsync(_userB, 0);
    }

    [Fact]
    public async Task UserMode_DeletingRecordOfAnotherUser_ThrowsBeforeSave()
    {
        var (projectB, _) = await SeedDictionariesAsync(_userB, "BBB", "Budowa");

        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            // A stub attached by id, bypassing the filter.
            db.Projects.Remove(new Project { Id = projectB, UserId = _userB });
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        }

        await AssertProjectCountAsync(_userB, 1);
    }

    [Fact]
    public async Task UserContext_CannotBeBoundTwice_OrToInvalidUser()
    {
        var factory = new UserDbContextFactory(postgres.ContextFactory);
        await using var db = await factory.CreateDbContextAsync(_userA, Ct);
        Assert.Equal(_userA, db.CurrentUserId);

        Assert.Throws<InvalidOperationException>(() => db.BindUser(_userB));
        Assert.Equal(_userA, db.CurrentUserId);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => factory.CreateDbContextAsync(0, Ct));
    }

    [Fact]
    public async Task UserMode_RemovingStubWithOwnUserIdAndIdOfAnotherUser_DoesNotDelete()
    {
        var (projectB, _) = await SeedDictionariesAsync(_userB, "BBB", "Budowa");

        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            db.Projects.Remove(new Project { Id = projectB, UserId = _userA });
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync(Ct));
        }

        await using (var db = await postgres.CreateUserContextAsync(_userB))
        {
            var project = await db.Projects.SingleAsync(Ct);
            Assert.Equal(projectB, project.Id);
            Assert.Equal("BBB", project.Code);
        }
    }

    [Fact]
    public async Task UserMode_UpdatingStubWithOwnUserIdAndIdOfAnotherUser_DoesNotModify()
    {
        await SeedAsync(_userB, "BBB", "Budowa", "Opis B");
        var (projectA, workTypeA) = await SeedDictionariesAsync(_userA, "AAA", "Analiza");
        int entryB;
        await using (var db = await postgres.CreateUserContextAsync(_userB))
        {
            entryB = await db.TimeEntries.Select(e => e.Id).SingleAsync(Ct);
        }

        await using (var db = await postgres.CreateUserContextAsync(_userA))
        {
            var stub = NewEntry(projectA, workTypeA);
            stub.Id = entryB;
            stub.UserId = _userA;
            stub.Description = "Zmienione";
            db.TimeEntries.Update(stub);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync(Ct));
        }

        await using (var db = await postgres.CreateUserContextAsync(_userB))
        {
            var entry = await db.TimeEntries.SingleAsync(Ct);
            Assert.Equal("Opis B", entry.Description);
            Assert.Equal(90, entry.Minutes);
            Assert.Equal(_userB, entry.UserId);
        }
    }

    /// <summary>AD-16: every entity with a <c>UserId</c> (outside Identity and Data Protection) is <see cref="IUserOwned"/>.</summary>
    [Fact]
    public void EveryEntityWithUserId_ImplementsIUserOwned()
    {
        Type[] excluded =
        [
            typeof(AppUser), typeof(IdentityRole<int>), typeof(IdentityUserRole<int>), typeof(IdentityUserClaim<int>),
            typeof(IdentityUserLogin<int>), typeof(IdentityUserToken<int>), typeof(IdentityRoleClaim<int>),
            typeof(DataProtectionKey),
        ];
        using var db = postgres.CreateContext();
        var entityTypes = db.Model.GetEntityTypes().ToList();

        var withUserId = entityTypes
            .Where(e => !excluded.Contains(e.ClrType))
            .Where(e => e.GetProperties().Any(p => p.GetColumnName() == "user_id"))
            .Select(e => e.ClrType)
            .ToList();

        Assert.Equal(
            [typeof(AccountSettings), typeof(Project), typeof(TimeEntry), typeof(WorkType)],
            withUserId.OrderBy(t => t.Name, StringComparer.Ordinal));
        Assert.All(withUserId, type => Assert.True(typeof(IUserOwned).IsAssignableFrom(type), type.Name));
        Assert.All(withUserId, type => Assert.NotEmpty(db.Model.FindEntityType(type)!.GetDeclaredQueryFilters()));
    }

    private async Task<(int ProjectId, int WorkTypeId)> SeedAsync(int userId, string code, string workTypeName, string description)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        var project = new Project { Code = code, Name = "Projekt " + code };
        var workType = new WorkType { Name = workTypeName };
        db.TimeEntries.Add(new TimeEntry
        {
            Date = new DateOnly(2026, 9, 1),
            Project = project,
            WorkType = workType,
            Description = description,
            Minutes = 90,
        });
        db.AccountSettings.Add(new AccountSettings { ExportPrefix = "Prefiks_" + code });
        await db.SaveChangesAsync(Ct);
        return (project.Id, workType.Id);
    }

    private async Task<(int ProjectId, int WorkTypeId)> SeedDictionariesAsync(int userId, string code, string workTypeName)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        var project = new Project { Code = code, Name = "Projekt " + code };
        var workType = new WorkType { Name = workTypeName };
        db.AddRange(project, workType);
        await db.SaveChangesAsync(Ct);
        return (project.Id, workType.Id);
    }

    private async Task AssertSeesOnlyAsync(int userId, string code, string workTypeName, string description)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        Assert.Equal(code, await db.Projects.Select(p => p.Code).SingleAsync(Ct));
        Assert.Equal(workTypeName, await db.WorkTypes.Select(w => w.Name).SingleAsync(Ct));
        Assert.Equal(description, await db.TimeEntries.Select(e => e.Description).SingleAsync(Ct));
        Assert.Equal("Prefiks_" + code, await db.AccountSettings.Select(s => s.ExportPrefix).SingleAsync(Ct));
        Assert.All(await db.TimeEntries.Include(e => e.Project).Include(e => e.WorkType).ToListAsync(Ct), entry =>
        {
            Assert.Equal(userId, entry.UserId);
            Assert.Equal(userId, entry.Project.UserId);
            Assert.Equal(userId, entry.WorkType.UserId);
        });
    }

    private async Task AssertProjectCountAsync(int userId, int expected)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        Assert.Equal(expected, await db.Projects.CountAsync(Ct));
    }

    private static TimeEntry NewEntry(int projectId, int workTypeId) => new()
    {
        Date = new DateOnly(2026, 9, 2),
        ProjectId = projectId,
        WorkTypeId = workTypeId,
        Description = "Opis testowy",
        Minutes = 30,
    };
}
