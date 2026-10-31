using Microsoft.Data.Sqlite;

namespace Candidate.API.Data;

/// <summary>
/// Creates the SQLite database from the SQL scripts when the file does not exist yet,
/// so a fresh checkout (or a deleted .db) can be rebuilt the same way every time.
/// An existing database is never modified.
/// </summary>
public static class DatabaseInitializer
{
    public const string ConnectionStringName = "ProvidersDb";

    /// <summary>Scripts run in this order. 04_support_queries.sql is for people, not for setup.</summary>
    public static readonly string[] SetupScripts = ["01_schema.sql", "02_views.sql", "03_seed.sql"];

    /// <summary>Resolves a relative Data Source against the content root, so the app finds the
    /// same file whatever the working directory is.</summary>
    public static string ResolveConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var raw = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        var builder = new SqliteConnectionStringBuilder(raw);
        if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.GetFullPath(Path.Combine(environment.ContentRootPath, builder.DataSource));
        }

        return builder.ToString();
    }

    public static string ScriptsDirectory(IHostEnvironment environment) =>
        Path.Combine(environment.ContentRootPath, "Database", "Scripts");

    public static void EnsureCreated(string connectionString, string scriptsDirectory, ILogger logger)
    {
        var databasePath = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (File.Exists(databasePath) && new FileInfo(databasePath).Length > 0)
        {
            return;
        }

        logger.LogInformation("Database not found at {Path}. Creating it from {Scripts}.", databasePath, scriptsDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            foreach (var script in SetupScripts)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = File.ReadAllText(Path.Combine(scriptsDirectory, script));
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            // Don't leave a half-built file behind; the next start would treat it as valid.
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
            throw;
        }

        logger.LogInformation("Database created.");
    }
}
