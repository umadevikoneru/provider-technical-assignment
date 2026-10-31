using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Candidate.API.Data;

/// <summary>Reports healthy when the SQLite database can be opened and its schema is present.</summary>
public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Touches a real table, so a missing or empty database file is reported, not just a closed connection.
            await db.Providers.IgnoreQueryFilters().AnyAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database is reachable.");
        }
        // A probe that gave up (client timeout/disconnect) says nothing about the database; let it cancel.
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database is not reachable.", exception);
        }
    }
}
