using Candidate.API.Contracts;
using Candidate.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services.Reports;

/// <summary>
/// Reads the scenario views in 02_views.sql. The views own the rules (soft-delete exclusion,
/// effective license status), so the API, reports and ad-hoc SQL all give the same answer.
/// </summary>
public sealed class ScenarioReportService(AppDbContext db) : IScenarioReportService
{
    public async Task<IReadOnlyList<ActiveLicenseRowDto>> ActiveProvidersActiveLicensesAsync(CancellationToken cancellationToken) =>
        await db.ActiveProvidersActiveLicenses
            .OrderBy(r => r.ProviderName)
            .ThenBy(r => r.ExpirationDate)
            .Select(r => new ActiveLicenseRowDto(
                r.ProviderId, r.ProviderName, r.County, r.LicenseId, r.LicenseNumber, r.ExpirationDate, r.DaysUntilExpiration))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExpiredLicenseRowDto>> ActiveProvidersWithExpiredLicensesAsync(CancellationToken cancellationToken) =>
        await db.ActiveProvidersWithExpiredLicenses
            .OrderByDescending(r => r.DaysSinceExpiration)
            .Select(r => new ExpiredLicenseRowDto(
                r.ProviderId, r.ProviderName, r.County, r.LicenseId, r.LicenseNumber,
                r.RecordedStatus, r.ExpirationDate, r.DaysSinceExpiration, r.IsStatusStale))
            .ToListAsync(cancellationToken);
}
