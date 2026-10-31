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
    [InlineData(typeof(DropIndexTestMigration), nameof(DropIndexOperation))]
    [InlineData(typeof(DropUniqueConstraintTestMigration), nameof(DropUniqueConstraintOperation))]
    [InlineData(typeof(DropForeignKeyTestMigration), nameof(DropForeignKeyOperation))]
    [InlineData(typeof(ForeignKeyOnExistingTableTestMigration), nameof(AddForeignKeyOperation))]
    public void DestructiveMigration_IsRejected_WithMigrationAndOperationName(Type migrationType, string operationName)
    {
        var violation = Assert.Single(FindViolations((Migration)Activator.CreateInstance(migrationType)!));

        Assert.Contains(migrationType.Name, violation);
        Assert.Contains(operationName, violation);
    }

    /// <summary>
    /// Lista dozwolonych operacji: wszystko, czego tu nie ma (usuwanie, zmiana nazwy, zmiana kolumny, <c>Sql</c>,
    /// nowe operacje EF), jest naruszeniem. Unikalność i klucze wolno dodać tylko do tabeli z tej samej migracji,
    /// bo poprzedni obraz jej nie zna.
    /// </summary>
    private static IEnumerable<string> FindViolations(Migration migration)
    {
        var migrationName = migration.GetType().Name;
        var newTables = migration.UpOperations.OfType<CreateTableOperation>()
            .Select(op => (op.Schema, op.Name))
            .ToHashSet();
        bool IsNew(string? schema, string table) => newTables.Contains((schema, table));

        foreach (var operation in migration.UpOperations)
        {
            var isAdditive = operation switch
            {
                CreateTableOperation or EnsureSchemaOperation or CreateSequenceOperation => true,
                AddColumnOperation op => op.IsNullable || op.DefaultValue is not null || op.DefaultValueSql is not null
                    || op.ComputedColumnSql is not null,
                CreateIndexOperation op => !op.IsUnique || IsNew(op.Schema, op.Table),
                AddUniqueConstraintOperation op => IsNew(op.Schema, op.Table),
                AddPrimaryKeyOperation op => IsNew(op.Schema, op.Table),
                AddForeignKeyOperation op => IsNew(op.Schema, op.Table),
                _ => false,
            };

            if (!isAdditive)
            {
                var table = operation is ITableMigrationOperation t ? $" na tabeli {t.Table}" : "";
                yield return $"Migracja {migrationName}: {operation.GetType().Name}{table} nie jest operacją dodającą — po Initial dozwolone są tylko migracje dodające (AD-21).";
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

            migrationBuilder.CreateTable(
                name: "tag",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false),
                    user_id = table.Column<int>(nullable: false),
                    name = table.Column<string>(nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_tag", x => x.id));
            migrationBuilder.AddUniqueConstraint(name: "ak_tag_user_id_id", table: "tag", columns: ["user_id", "id"]);
            migrationBuilder.CreateIndex(name: "ix_tag_user_id_name", table: "tag", columns: ["user_id", "name"], unique: true);
            migrationBuilder.AddForeignKey(
                name: "fk_tag_project_user_id", table: "tag", column: "user_id", principalTable: "project", principalColumn: "id");
        }
    }

    private sealed class DropIndexTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropIndex(name: "ix_project_code", table: "project");
    }

    private sealed class DropUniqueConstraintTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropUniqueConstraint(name: "ak_project_note", table: "project");
    }

    private sealed class DropForeignKeyTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropForeignKey(name: "fk_time_entry_project", table: "time_entry");
    }

    private sealed class ForeignKeyOnExistingTableTestMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddForeignKey(
                name: "fk_project_tag", table: "project", column: "note", principalTable: "tag", principalColumn: "name");
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
