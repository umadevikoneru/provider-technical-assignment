using Candidate.API.Contracts;
using Candidate.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services.Providers;

/// <summary>
/// The only read service that bypasses the soft-delete filter. Keeping these queries
/// together makes every IgnoreQueryFilters() call easy to find and review.
/// </summary>
public sealed class ProviderAuditService(AppDbContext db) : IProviderAuditService
{
    public async Task<IReadOnlyList<DeletedProviderDto>> ListDeletedAsync(CancellationToken cancellationToken) =>
        await db.Providers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DeletedDate)
            .Select(p => new DeletedProviderDto(p.ProviderId, p.ProviderName, p.County, p.Status, p.CreatedDate, p.DeletedDate, p.DeletedBy))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProviderAuditDto>> GetHistoryAsync(int id, CancellationToken cancellationToken)
    {
        var exists = await db.Providers.IgnoreQueryFilters().AnyAsync(p => p.ProviderId == id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Provider {id} was not found.");
        }

        return await db.ProviderAudit
            .AsNoTracking()
            .Where(a => a.ProviderId == id)
            .OrderBy(a => a.AuditId)
            .Select(a => new ProviderAuditDto(a.AuditId, a.Action, a.ProviderName, a.County, a.Status, a.IsDeleted, a.ChangedDate, a.ChangedBy))
            .ToListAsync(cancellationToken);
    }
}
