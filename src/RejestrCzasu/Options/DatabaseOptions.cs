using Npgsql;

namespace RejestrCzasu.Options;

/// <summary>Connection parameters for the local PostgreSQL container (section <c>Database</c>).</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5432;
    public string Name { get; set; } = "rejestr";
    public string User { get; set; } = "rejestr";
    public string Password { get; set; } = "";
    public string ContainerName { get; set; } = "rejestr-czasu-db";

    /// <summary>Builds the Npgsql connection string; the single source of connection parameters.</summary>
    public string BuildConnectionString(int? timeoutSeconds = null)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Name,
            Username = User,
            Password = Password,
        };
        if (timeoutSeconds is { } timeout)
        {
            builder.Timeout = timeout;
        }
        return builder.ConnectionString;
    }
}
