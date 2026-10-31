using Candidate.API.Contracts;
using Candidate.API.Domain;

namespace Candidate.API.Services.Providers;

/// <summary>Standard (non-deleted) provider reads.</summary>
public interface IProviderQueryService
{
    Task<IReadOnlyList<ProviderSummaryDto>> ListAsync(string? search, ProviderStatus? status, CancellationToken cancellationToken);

    /// <summary>Throws <see cref="NotFoundException"/> if the provider does not exist or is soft-deleted.</summary>
    Task<ProviderDetailDto> GetAsync(int id, CancellationToken cancellationToken);
}
