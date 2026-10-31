using Candidate.API.Contracts;

namespace Candidate.API.Services.Reports;

/// <summary>Read-only metrics for the dashboard.</summary>
public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken);
}
