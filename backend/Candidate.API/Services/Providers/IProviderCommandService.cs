using Candidate.API.Contracts;

namespace Candidate.API.Services.Providers;

/// <summary>Changes to providers. Deletion is always a soft delete.</summary>
public interface IProviderCommandService
{
    Task<ProviderDetailDto> CreateAsync(ProviderCreateRequest request, CancellationToken cancellationToken);

    Task<ProviderDetailDto> UpdateAsync(int id, ProviderUpdateRequest request, CancellationToken cancellationToken);

    /// <summary>Soft delete. The row and its licenses stay in the database.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken);

    Task<ProviderDetailDto> RestoreAsync(int id, CancellationToken cancellationToken);
}
