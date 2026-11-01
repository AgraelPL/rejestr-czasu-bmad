using Npgsql;

namespace RejestrCzasu.Data;

/// <summary>
/// The only place that recognizes database constraint violations (AD-7): screens and stores never read SQL states or
/// constraint names themselves, and never check uniqueness with a query before saving.
/// </summary>
public static class DbErrors
{
    /// <summary>Unique index on <c>project (user_id, code)</c>: a Project code is unique per User (R1).</summary>
    public const string ProjectCodeIndex = "ix_project_user_id_code";

    /// <summary>Unique functional index on <c>work_type (user_id, lower(name))</c>: a Work type name is unique per User.</summary>
    public const string WorkTypeNameIndex = "ux_work_type_user_id_name_lower";

    /// <summary>
    /// <c>true</c> when <paramref name="exception"/> (or one of its inner exceptions) is a unique violation (23505) of
    /// the constraint or index <paramref name="constraintName"/>.
    /// </summary>
    public static bool IsUniqueViolation(Exception exception, string constraintName) =>
        Find(exception) is { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && string.Equals(postgres.ConstraintName, constraintName, StringComparison.Ordinal);

    /// <summary>
    /// <c>true</c> when <paramref name="exception"/> (or one of its inner exceptions) is a foreign key violation:
    /// 23503 (<c>foreign_key_violation</c>, e.g. an Entry pointing to a missing Dictionary item) or 23001
    /// (<c>restrict_violation</c>), which PostgreSQL reports for deleting a Dictionary item still used by an Entry,
    /// because the schema's foreign keys are <c>ON DELETE RESTRICT</c> (AD-7).
    /// </summary>
    public static bool IsForeignKeyViolation(Exception exception) =>
        Find(exception) is { SqlState: PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.RestrictViolation };

    private static PostgresException? Find(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }
        return null;
    }
}
