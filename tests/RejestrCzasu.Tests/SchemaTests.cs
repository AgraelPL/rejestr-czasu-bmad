using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;

namespace RejestrCzasu.Tests;

public sealed class SchemaTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ProjectCode_IsStoredUpperCase()
    {
        await using (var db = postgres.CreateContext())
        {
            db.Projects.Add(new Project { Code = "rdz", Name = "Projekt testowy" });
            await db.SaveChangesAsync(Ct);
        }

        await using (var db = postgres.CreateContext())
        {
            Assert.Equal("RDZ", await db.Projects.Select(p => p.Code).SingleAsync(Ct));
        }
    }

    [Fact]
    public async Task ProjectCode_ChangedToLowerCase_IsStoredUpperCase_AlsoBySyncSaveChanges()
    {
        await using (var db = postgres.CreateContext())
        {
            var project = new Project { Code = "ABC", Name = "Projekt testowy" };
            db.Projects.Add(project);
            await db.SaveChangesAsync(Ct);

            project.Code = "xyz";
            db.SaveChanges();
        }

        await using (var db = postgres.CreateContext())
        {
            Assert.Equal("XYZ", await db.Projects.Select(p => p.Code).SingleAsync(Ct));
        }
    }

    [Fact]
    public async Task DuplicateProjectCode_DifferentCase_IsRejected()
    {
        await using (var db = postgres.CreateContext())
        {
            db.Projects.Add(new Project { Code = "RDZ", Name = "Pierwszy" });
            await db.SaveChangesAsync(Ct);
        }

        await using (var db = postgres.CreateContext())
        {
            db.Projects.Add(new Project { Code = "rdz", Name = "Drugi" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
        }
    }

    [Fact]
    public async Task DuplicateWorkTypeName_DifferentCase_IsRejected()
    {
        await using (var db = postgres.CreateContext())
        {
            db.WorkTypes.Add(new WorkType { Name = "Dev" });
            await db.SaveChangesAsync(Ct);
        }

        await using (var db = postgres.CreateContext())
        {
            db.WorkTypes.Add(new WorkType { Name = "dev" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
        }
    }

    [Fact]
    public async Task DeletingProjectOrWorkTypeUsedInEntry_IsRejected()
    {
        int projectId, workTypeId;
        await using (var db = postgres.CreateContext())
        {
            var project = new Project { Code = "RDZ", Name = "Projekt testowy" };
            var workType = new WorkType { Name = "Dev" };
            db.TimeEntries.Add(new TimeEntry
            {
                Date = new DateOnly(2026, 9, 1),
                Project = project,
                WorkType = workType,
                Description = "Opis testowy",
                Minutes = 60,
            });
            await db.SaveChangesAsync(Ct);
            projectId = project.Id;
            workTypeId = workType.Id;
        }

        await using (var db = postgres.CreateContext())
        {
            db.Projects.Remove(await db.Projects.SingleAsync(p => p.Id == projectId, Ct));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
        }

        await using (var db = postgres.CreateContext())
        {
            db.WorkTypes.Remove(await db.WorkTypes.SingleAsync(w => w.Id == workTypeId, Ct));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
        }

        await using (var db = postgres.CreateContext())
        {
            Assert.Equal(1, await db.Projects.CountAsync(Ct));
            Assert.Equal(1, await db.WorkTypes.CountAsync(Ct));
            Assert.Equal(1, await db.TimeEntries.CountAsync(Ct));
        }
    }

    [Fact]
    public async Task Schema_HasExpectedTables()
    {
        await using var db = postgres.CreateContext();
        var tables = await db.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync(Ct);

        Assert.Contains("project", tables);
        Assert.Contains("work_type", tables);
        Assert.Contains("time_entry", tables);
        Assert.Contains("__EFMigrationsHistory", tables);
    }
}
