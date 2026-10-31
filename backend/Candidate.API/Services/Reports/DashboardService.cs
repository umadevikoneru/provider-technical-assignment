using Candidate.API.Contracts;
using Candidate.API.Data;
using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services.Reports;

/// <summary>
/// Dashboard metrics, computed from the same views as the scenario reports so the two always agree.
/// Soft-deleted providers are excluded from every number except DeletedProviders.
/// </summary>
public sealed class DashboardService(AppDbContext db) : IDashboardService
{
    public const int ExpiringWithinDays = 60;

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var providersByStatus = await db.Providers
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var licensesByStatus = await db.LicenseViews
            .GroupBy(l => l.EffectiveStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var deletedProviders = await db.Providers.IgnoreQueryFilters().CountAsync(p => p.IsDeleted, cancellationToken);

        var activeWithExpired = await db.ActiveProvidersWithExpiredLicenses
            .Select(r => r.ProviderId)
            .Distinct()
            .CountAsync(cancellationToken);

        var stale = await db.LicenseViews.CountAsync(l => l.IsStatusStale, cancellationToken);

        var expiringSoon = await db.LicenseViews
            .Where(l => l.EffectiveStatus == LicenseStatus.Active
                        && l.DaysUntilExpiration >= 0
                        && l.DaysUntilExpiration <= ExpiringWithinDays)
            .OrderBy(l => l.DaysUntilExpiration)
            .Select(l => new ExpiringLicenseDto(l.ProviderId, l.ProviderName, l.LicenseNumber, l.ExpirationDate, l.DaysUntilExpiration))
            .ToListAsync(cancellationToken);

        // Always return every status (with zero counts) so charts have a stable shape.
        return new DashboardDto(
            TotalProviders: providersByStatus.Sum(s => s.Count),
            DeletedProviders: deletedProviders,
            TotalLicenses: licensesByStatus.Sum(s => s.Count),
            ActiveProvidersWithExpiredLicenses: activeWithExpired,
            StaleLicenseStatuses: stale,
            ProvidersByStatus: Enum.GetValues<ProviderStatus>()
                .Select(s => new CountByStatusDto(s.ToString(), providersByStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0))
                .ToList(),
            LicensesByEffectiveStatus: Enum.GetValues<LicenseStatus>()
                .Select(s => new CountByStatusDto(s.ToString(), licensesByStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0))
                .ToList(),
            ExpiringWithinDays: ExpiringWithinDays,
            ExpiringSoon: expiringSoon);
    }
}
