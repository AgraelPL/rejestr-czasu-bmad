using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using RejestrCzasu.Data;

namespace RejestrCzasu.Tests;

/// <summary>
/// AD-21: po migracji <c>Initial</c> migracje tylko dodają, żeby poprzedni obraz działał na nowszym schemacie (rollback).
/// </summary>
public sealed class MigrationRulesTests
{
    private const string InitialMigrationName = "Initial";

    [Fact]
    public void AppMigrations_AfterInitial_AreAdditiveOnly()
    {
        var migrationTypes = typeof(AppDbContext).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .ToList();

        Assert.Single(migrationTypes, t => t.Name == InitialMigrationName);

        var violations = migrationTypes
            .Where(t => t.Name != InitialMigrationName)
            .SelectMany(t => FindViolations((Migration)Activator.CreateInstance(t)!))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void AdditiveMigration_IsAllowed()
    {
        Assert.Empty(FindViolations(new AdditiveTestMigration()));
    }

    [Theory]
    [InlineData(typeof(DropColumnTestMigration), nameof(DropColumnOperation))]
    [InlineData(typeof(DropTableTestMigration), nameof(DropTableOperation))]
    [InlineData(typeof(RenameColumnTestMigration), nameof(RenameColumnOperation))]
    [InlineData(typeof(RenameTableTestMigration), nameof(RenameTableOperation))]
    [InlineData(typeof(AlterColumnTestMigration), nameof(AlterColumnOperation))]
    [InlineData(typeof(UniqueIndexTestMigration), nameof(CreateIndexOperation))]
    [InlineData(typeof(SqlTestMigration), nameof(SqlOperation))]
    [InlineData(typeof(UniqueConstraintTestMigration), nameof(AddUniqueConstraintOperation))]
    [InlineData(typeof(PrimaryKeyTestMigration), nameof(AddPrimaryKeyOperation))]
    [InlineData(typeof(RequiredColumnWithoutDefaultTestMigration), nameof(AddColumnOperation))]
    public void DestructiveMigration_IsRejected_WithMigrationAndOperationName(Type migrationType, string operationName)
    {
        var violation = Assert.Single(FindViolations((Migration)Activator.CreateInstance(migrationType)!));

        Assert.Contains(migrationType.Name, violation);
        Assert.Contains(operationName, violation);
    }

    private static IEnumerable<string> FindViolations(Migration migration)
    {
        var migrationName = migration.GetType().Name;
        foreach (var operation in migration.UpOperations)
        {
            var reason = operation switch
            {
                DropTableOperation op => $"usuwa tabelę {op.Name}",
                DropColumnOperation op => $"usuwa kolumnę {op.Table}.{op.Name}",
                RenameTableOperation op => $"zmienia nazwę tabeli {op.Name} na {op.NewName}",
                RenameColumnOperation op => $"zmienia nazwę kolumny {op.Table}.{op.Name} na {op.NewName}",
                AlterColumnOperation op => $"zmienia kolumnę {op.Table}.{op.Name}",
                CreateIndexOperation { IsUnique: true } op => $"tworzy unikalny indeks {op.Name} na {op.Table}",
                AddUniqueConstraintOperation op => $"dodaje ograniczenie unikalne {op.Name} na {op.Table}",
                AddPrimaryKeyOperation op => $"dodaje klucz główny {op.Name} na {op.Table}",
                AddColumnOperation { IsNullable: false, DefaultValue: null, DefaultValueSql: null, ComputedColumnSql: null } op =>
                    $"dodaje kolumnę {op.Table}.{op.Name} NOT NULL bez wartości domyślnej",
                SqlOperation => "wykonuje migrationBuilder.Sql(...)",
                _ => null,
            };

            if (reason is not null)
            {
                yield return $"Migracja {migrationName}: {operation.GetType().Name} {reason} — po Initial dozwolone są tylko migracje dodające (AD-21).";
            }
        }
    }

    // Migracje testowe poza assembly aplikacji: bez [DbContext] i [Migration], więc EF ich nie widzi.

    private sealed class AdditiveTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "note", table: "project", nullable: true);
            migrationBuilder.AddColumn<int>(name: "priority", table: "project", nullable: false, defaultValue: 0);
            migrationBuilder.CreateIndex(name: "ix_project_note", table: "project", column: "note");
        }
    }

    private sealed class DropColumnTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(name: "note", table: "project");
    }

    private sealed class DropTableTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(name: "project");
    }

    private sealed class RenameColumnTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.RenameColumn(name: "note", table: "project", newName: "remark");
    }

    private sealed class RenameTableTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.RenameTable(name: "project", newName: "projects");
    }

    private sealed class AlterColumnTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AlterColumn<string>(name: "note", table: "project", maxLength: 50, nullable: true);
    }

    private sealed class UniqueIndexTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.CreateIndex(name: "ix_project_note", table: "project", column: "note", unique: true);
    }

    private sealed class UniqueConstraintTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddUniqueConstraint(name: "ak_project_note", table: "project", column: "note");
    }

    private sealed class PrimaryKeyTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddPrimaryKey(name: "pk_project", table: "project", column: "id");
    }

    private sealed class RequiredColumnWithoutDefaultTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<int>(name: "priority", table: "project", nullable: false);
    }

    private sealed class SqlTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql("DELETE FROM project;");
    }
}
