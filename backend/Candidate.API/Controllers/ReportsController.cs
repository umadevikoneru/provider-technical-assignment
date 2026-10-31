using Candidate.API.Contracts;
using Candidate.API.Services.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public sealed class ReportsController(IScenarioReportService reports) : ControllerBase
{
    /// <summary>Scenario: providers with Status = Active and their currently valid licenses.</summary>
    [HttpGet("active-providers-active-licenses")]
    public Task<IReadOnlyList<ActiveLicenseRowDto>> ActiveProvidersActiveLicenses(CancellationToken cancellationToken) =>
        reports.ActiveProvidersActiveLicensesAsync(cancellationToken);

    /// <summary>Scenario: providers with Status = Active that hold at least one expired license (one row per license).</summary>
    [HttpGet("active-providers-expired-licenses")]
    public Task<IReadOnlyList<ExpiredLicenseRowDto>> ActiveProvidersWithExpiredLicenses(CancellationToken cancellationToken) =>
        reports.ActiveProvidersWithExpiredLicensesAsync(cancellationToken);
}
