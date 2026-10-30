using Microsoft.EntityFrameworkCore;

namespace RejestrCzasu.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkType> WorkTypes => Set<WorkType>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(project =>
        {
            project.ToTable("project");
            project.Property(p => p.Code).HasMaxLength(Project.CodeMaxLength);
            project.Property(p => p.Name).HasMaxLength(Project.NameMaxLength);
            project.HasIndex(p => p.Code).IsUnique();
        });

        modelBuilder.Entity<WorkType>(workType =>
        {
            workType.ToTable("work_type");
            workType.Property(w => w.Name).HasMaxLength(WorkType.NameMaxLength);
            // Case-insensitive uniqueness: functional index on lower(name), created by SQL in the Initial migration.
        });

        modelBuilder.Entity<TimeEntry>(entry =>
        {
            entry.ToTable("time_entry");
            entry.Property(e => e.Description).HasColumnType("text");
            entry.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entry.HasOne(e => e.WorkType).WithMany().HasForeignKey(e => e.WorkTypeId).OnDelete(DeleteBehavior.Restrict);
            entry.HasIndex(e => e.Date);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeProjectCodes();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NormalizeProjectCodes();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void NormalizeProjectCodes()
    {
        foreach (var entry in ChangeTracker.Entries<Project>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified && entry.Entity.Code is { } code)
            {
                entry.Entity.Code = code.ToUpperInvariant();
            }
        }
    }
}
