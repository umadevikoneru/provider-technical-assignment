using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Candidate.API.Data;

/// <summary>
/// Stamps audit columns (created, updated, deleted), the concurrency version, and refuses hard
/// deletes on every save. This is the only place that decides "who and when", so no code path
/// can forget it or record it differently.
/// </summary>
public sealed class AuditStampInterceptor(TimeProvider clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = TruncateToSeconds(clock.GetUtcNow().UtcDateTime);
        var user = currentUser.Name;

        foreach (var entry in context.ChangeTracker.Entries<Provider>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    entry.Entity.UpdatedBy = user;
                    entry.Entity.Version = 1;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedDate = now;
                    entry.Entity.UpdatedBy = user;
                    entry.Entity.Version = entry.Property(p => p.Version).OriginalValue + 1;
                    StampSoftDelete(entry.Entity, wasDeleted: entry.Property(p => p.IsDeleted).OriginalValue, now, user);
                    break;
                case EntityState.Deleted:
                    throw new InvalidOperationException("Providers cannot be hard-deleted. Use Provider.SoftDelete().");
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<License>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedDate = now;
                    break;
                case EntityState.Deleted:
                    throw new InvalidOperationException("Licenses cannot be hard-deleted.");
            }
        }
    }

    private static void StampSoftDelete(Provider provider, bool wasDeleted, DateTime now, string user)
    {
        if (provider.IsDeleted && !wasDeleted)
        {
            provider.DeletedDate = now;
            provider.DeletedBy = user;
        }
        else if (!provider.IsDeleted && wasDeleted)
        {
            // Restored: the deletion is still recorded in ProviderAudit.
            provider.DeletedDate = null;
            provider.DeletedBy = null;
        }
    }

    // Matches the second precision of timestamps written by the SQL scripts.
    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
}
