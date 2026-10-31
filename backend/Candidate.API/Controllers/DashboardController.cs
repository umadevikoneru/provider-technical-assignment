using Candidate.API.Contracts;
using Candidate.API.Services.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>Read-only metrics for the dashboard. Soft-deleted providers are excluded except from DeletedProviders.</summary>
    [HttpGet]
    public Task<DashboardDto> Get(CancellationToken cancellationToken) =>
        dashboard.GetAsync(cancellationToken);
}
