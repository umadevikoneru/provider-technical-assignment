using Candidate.API.Contracts;
using Candidate.API.Data;
using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services.Providers;

/// <summary>
/// Read side for providers. Every query goes through the soft-delete query filter,
/// so deleted providers behave as "not found".
/// </summary>
public sealed class ProviderQueryService(AppDbContext db) : IProviderQueryService
{
    public async Task<IReadOnlyList<ProviderSummaryDto>> ListAsync(
        string? search,
        ProviderStatus? status,
        CancellationToken cancellationToken)
    {
        var query = db.Providers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(p =>
                EF.Functions.Like(p.ProviderName, pattern, "\\") ||
                EF.Functions.Like(p.County, pattern, "\\"));
        }

        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        var providers = await query
            .OrderBy(p => EF.Functions.Collate(p.ProviderName, "NOCASE"))
            .Select(p => new { p.ProviderId, p.ProviderName, p.County, p.Status, p.CreatedDate, p.UpdatedDate, p.Version })
            .ToListAsync(cancellationToken);

        // License counts for all listed providers in one grouped query, instead of two
        // correlated subqueries per provider (which would evaluate the view 2N times).
        var providerIds = providers.Select(p => p.ProviderId).ToList();
        var licenseCounts = await db.LicenseViews
            .Where(l => providerIds.Contains(l.ProviderId))
            .GroupBy(l => l.ProviderId)
            .Select(g => new
            {
                ProviderId = g.Key,
                Total = g.Count(),
                Expired = g.Count(l => l.EffectiveStatus == LicenseStatus.Expired)
            })
            .ToDictionaryAsync(c => c.ProviderId, cancellationToken);

        return providers
            .Select(p =>
            {
                var counts = licenseCounts.GetValueOrDefault(p.ProviderId);
                return new ProviderSummaryDto(
                    p.ProviderId,
                    p.ProviderName,
                    p.County,
                    p.Status,
                    p.CreatedDate,
                    p.UpdatedDate,
                    p.Version,
                    counts?.Total ?? 0,
                    counts?.Expired ?? 0);
            })
            .ToList();
    }

    public async Task<ProviderDetailDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var provider = await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.ProviderId == id, cancellationToken)
            ?? throw new NotFoundException($"Provider {id} was not found.");

        var licenses = await db.LicenseViews
            .Where(l => l.ProviderId == id)
            .OrderBy(l => l.ExpirationDate)
            .Select(l => new LicenseDto(
                l.LicenseId,
                l.LicenseNumber,
                l.RecordedStatus,
                l.EffectiveStatus,
                l.ExpirationDate,
                l.DaysUntilExpiration,
                l.IsStatusStale))
            .ToListAsync(cancellationToken);

        return new ProviderDetailDto(
            provider.ProviderId,
            provider.ProviderName,
            provider.County,
            provider.Status,
            provider.CreatedDate,
            provider.UpdatedDate,
            provider.UpdatedBy,
            provider.Version,
            licenses);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
