using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;

namespace RejestrCzasu.Tests;

/// <summary>Recognition of constraint violations (AD-7) on real PostgreSQL errors of the Initial schema.</summary>
public sealed class DbErrorsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private int _user;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync();
        _user = await postgres.CreateUserAsync("bledy-bazy");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DuplicateProjectCode_IsUniqueViolationOfProjectCodeIndexOnly()
    {
        await using (var db = await postgres.CreateUserContextAsync(_user))
        {
            db.Projects.Add(new Project { Code = "RDZ", Name = "Rozwój" });
            await db.SaveChangesAsync(Ct);
        }
        await using var second = await postgres.CreateUserContextAsync(_user);
        second.Projects.Add(new Project { Code = "rdz", Name = "Drugi" });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync(Ct));

        Assert.True(DbErrors.IsUniqueViolation(ex, DbErrors.ProjectCodeIndex));
        Assert.False(DbErrors.IsUniqueViolation(ex, DbErrors.WorkTypeNameIndex));
        Assert.False(DbErrors.IsForeignKeyViolation(ex));
    }

    [Fact]
    public async Task DuplicateWorkTypeName_IsUniqueViolationOfWorkTypeNameIndexOnly()
    {
        await using (var db = await postgres.CreateUserContextAsync(_user))
        {
            db.WorkTypes.Add(new WorkType { Name = "Development" });
            await db.SaveChangesAsync(Ct);
        }
        await using var second = await postgres.CreateUserContextAsync(_user);
        second.WorkTypes.Add(new WorkType { Name = "DEVELOPMENT" });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync(Ct));

        Assert.True(DbErrors.IsUniqueViolation(ex, DbErrors.WorkTypeNameIndex));
        Assert.False(DbErrors.IsUniqueViolation(ex, DbErrors.ProjectCodeIndex));
    }

    [Fact]
    public async Task DeletingProjectUsedByEntry_IsForeignKeyViolation()
    {
        await using var db = await postgres.CreateUserContextAsync(_user);
        var project = new Project { Code = "RDZ", Name = "Rozwój" };
        db.TimeEntries.Add(new TimeEntry
        {
            Date = new DateOnly(2026, 9, 1),
            Project = project,
            WorkType = new WorkType { Name = "Development" },
            Description = "Wpis",
            Minutes = 60,
        });
        await db.SaveChangesAsync(Ct);
        db.ChangeTracker.Clear();
        db.Projects.Remove(new Project { Id = project.Id, UserId = _user });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));

        // ON DELETE RESTRICT: PostgreSQL reports 23001 (restrict_violation), not 23503.
        Assert.True(DbErrors.IsForeignKeyViolation(ex));
        Assert.False(DbErrors.IsUniqueViolation(ex, DbErrors.ProjectCodeIndex));
    }

    [Fact]
    public async Task EntryPointingToAnotherUsersProject_IsForeignKeyViolation()
    {
        int foreignProjectId;
        var other = await postgres.CreateUserAsync("bledy-bazy-inny");
        await using (var otherDb = await postgres.CreateUserContextAsync(other))
        {
            var foreign = new Project { Code = "OBC", Name = "Cudzy" };
            otherDb.Projects.Add(foreign);
            await otherDb.SaveChangesAsync(Ct);
            foreignProjectId = foreign.Id;
        }
        await using var db = await postgres.CreateUserContextAsync(_user);
        var workType = new WorkType { Name = "Development" };
        db.WorkTypes.Add(workType);
        await db.SaveChangesAsync(Ct);
        db.TimeEntries.Add(new TimeEntry
        {
            Date = new DateOnly(2026, 9, 1), ProjectId = foreignProjectId, WorkTypeId = workType.Id, Description = "Wpis", Minutes = 60,
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));

        // The composite key (user_id, project_id) has no row of this User: 23503 (foreign_key_violation).
        Assert.True(DbErrors.IsForeignKeyViolation(ex));
    }

    [Fact]
    public void OtherExceptions_AreNotViolations()
    {
        var ex = new InvalidOperationException("x", new TimeoutException());

        Assert.False(DbErrors.IsUniqueViolation(ex, DbErrors.ProjectCodeIndex));
        Assert.False(DbErrors.IsForeignKeyViolation(ex));
    }
}
