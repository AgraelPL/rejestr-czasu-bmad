using System.Reflection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace RejestrCzasu.Data;

/// <summary>
/// The application database (AD-7) with Identity and Data Protection keys. Two modes (AD-16):
/// <list type="bullet">
/// <item><b>User mode</b> (<see cref="CurrentUserId"/> set, only by <see cref="UserDbContextFactory"/>):
/// every <see cref="IUserOwned"/> entity is filtered by <c>user_id = CurrentUserId</c>; <c>SaveChanges</c>
/// assigns <c>user_id</c> to new records and rejects any change of <c>user_id</c>.</item>
/// <item><b>System mode</b> (no <see cref="CurrentUserId"/>): <see cref="IUserOwned"/> entities are invisible
/// and saving any of them throws.</item>
/// </list>
/// Filter and assignment work by the interface, not by a list of tables.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<int>, int>(options), IDataProtectionKeyContext
{
    public const int StatusMaxLength = 20;

    private static readonly MethodInfo ConfigureUserOwnedMethod =
        typeof(AppDbContext).GetMethod(nameof(ConfigureUserOwned), BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>The User this context is bound to; <c>null</c> in system mode.</summary>
    public int? CurrentUserId { get; private set; }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkType> WorkTypes => Set<WorkType>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<AccountSettings> AccountSettings => Set<AccountSettings>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>Identity schema without passkeys (AD-12).</summary>
    protected override Version SchemaVersion => IdentitySchemaVersions.Version2;

    /// <summary>Switches this context to User mode. Called once, only by <see cref="UserDbContextFactory"/>.</summary>
    internal void BindUser(int userId)
    {
        if (CurrentUserId is not null)
        {
            throw new InvalidOperationException("Kontekst bazy jest już związany z Userem.");
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        CurrentUserId = userId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity tables: int keys, snake_case names set explicitly (AD-7).
        modelBuilder.Entity<AppUser>(user =>
        {
            user.ToTable("users", t => t.HasCheckConstraint(
                "ck_users_status", "status IN ('pending', 'active', 'blocked')"));
            user.Property(u => u.Status)
                .HasMaxLength(StatusMaxLength)
                .HasConversion(
                    status => status.ToString().ToLowerInvariant(),
                    text => Enum.Parse<AccountStatus>(text, ignoreCase: true));
            user.Property(u => u.RegisteredAt).HasColumnType("timestamptz");
            user.Property(u => u.LastLoginAt).HasColumnType("timestamptz");
            user.HasIndex(u => u.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name");
            user.HasIndex(u => u.NormalizedEmail).HasDatabaseName("ix_users_normalized_email");
        });
        modelBuilder.Entity<IdentityRole<int>>(role =>
        {
            role.ToTable("roles");
            role.HasIndex(r => r.NormalizedName).HasDatabaseName("ix_roles_normalized_name");
        });
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("role_claims");

        modelBuilder.Entity<DataProtectionKey>().ToTable("data_protection_keys");

        modelBuilder.Entity<AccountSettings>(settings =>
        {
            settings.ToTable("account_settings");
            settings.HasKey(s => s.UserId);
            settings.Property(s => s.UserId).ValueGeneratedNever();
            settings.Property(s => s.ExportPrefix).HasMaxLength(Data.AccountSettings.ExportPrefixMaxLength);
        });

        modelBuilder.Entity<Project>(project =>
        {
            project.ToTable("project");
            project.Property(p => p.Code).HasMaxLength(Project.CodeMaxLength);
            project.Property(p => p.Name).HasMaxLength(Project.NameMaxLength);
            project.HasAlternateKey(p => new { p.UserId, p.Id });
            project.HasIndex(p => new { p.UserId, p.Code }).IsUnique();
        });

        modelBuilder.Entity<WorkType>(workType =>
        {
            workType.ToTable("work_type");
            workType.Property(w => w.Name).HasMaxLength(WorkType.NameMaxLength);
            workType.HasAlternateKey(w => new { w.UserId, w.Id });
            // Case-insensitive uniqueness per User: functional index on (user_id, lower(name)),
            // created by SQL in the Initial migration.
        });

        modelBuilder.Entity<TimeEntry>(entry =>
        {
            entry.ToTable("time_entry");
            entry.Property(e => e.Description).HasColumnType("text");
            // Composite foreign keys: an entry can only point to dictionaries of its own User (AD-7).
            entry.HasOne(e => e.Project).WithMany()
                .HasForeignKey(e => new { e.UserId, e.ProjectId })
                .HasPrincipalKey(p => new { p.UserId, p.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entry.HasOne(e => e.WorkType).WithMany()
                .HasForeignKey(e => new { e.UserId, e.WorkTypeId })
                .HasPrincipalKey(w => new { w.UserId, w.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entry.HasIndex(e => new { e.UserId, e.Date });
        });

        // Every IUserOwned entity: foreign key to users and the User filter (AD-16).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (typeof(IUserOwned).IsAssignableFrom(entityType.ClrType) && !entityType.IsOwned())
            {
                ConfigureUserOwnedMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
            }
        }

        // No ON DELETE CASCADE anywhere, also on the Identity tables (AD-7, AD-18).
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private void ConfigureUserOwned<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IUserOwned
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        // UPDATE/DELETE also match user_id, so a stub with another User's Id affects no row (concurrency failure).
        entity.Property(e => e.UserId).IsConcurrencyToken();
        // CurrentUserId is read from the context instance, so EF parameterizes it on every query.
        entity.HasQueryFilter(e => CurrentUserId != null && e.UserId == CurrentUserId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareChanges()
    {
        ChangeTracker.DetectChanges();
        ApplyUserOwnership();
        NormalizeProjectCodes();
    }

    private void ApplyUserOwnership()
    {
        foreach (var entry in ChangeTracker.Entries<IUserOwned>())
        {
            if (entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }
            if (CurrentUserId is not { } userId)
            {
                throw new InvalidOperationException(
                    $"Zapis {entry.Metadata.ClrType.Name} wymaga kontekstu Usera (UserDbContextFactory).");
            }

            var userIdProperty = entry.Property(e => e.UserId);
            if (entry.State == EntityState.Added)
            {
                // Unset is 0 or the temporary value EF puts into a foreign key to a store-generated key.
                var isSet = !userIdProperty.IsTemporary && userIdProperty.CurrentValue != 0;
                if (isSet && userIdProperty.CurrentValue != userId)
                {
                    throw ForeignUserException(entry);
                }
                userIdProperty.CurrentValue = userId;
                userIdProperty.IsTemporary = false;
            }
            else if (userIdProperty.OriginalValue != userId || userIdProperty.CurrentValue != userId)
            {
                // Modified or Deleted: the record must belong to this User and keep its user_id.
                throw ForeignUserException(entry);
            }
        }
    }

    private static InvalidOperationException ForeignUserException(EntityEntry<IUserOwned> entry) =>
        new($"Nie wolno zmieniać user_id ani zapisywać {entry.Metadata.ClrType.Name} innego Usera.");

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
