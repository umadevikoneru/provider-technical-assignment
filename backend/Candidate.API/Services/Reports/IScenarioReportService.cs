using Candidate.API.Contracts;

namespace Candidate.API.Services.Reports;

/// <summary>The required data scenarios. Soft-deleted providers are always excluded.</summary>
public interface IScenarioReportService
{
    Task<IReadOnlyList<ActiveLicenseRowDto>> ActiveProvidersActiveLicensesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ExpiredLicenseRowDto>> ActiveProvidersWithExpiredLicensesAsync(CancellationToken cancellationToken);
}
