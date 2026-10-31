using Candidate.API.Contracts;

namespace Candidate.API.Services.Providers;

/// <summary>Audit and troubleshooting reads. These deliberately include soft-deleted providers.</summary>
public interface IProviderAuditService
{
    Task<IReadOnlyList<DeletedProviderDto>> ListDeletedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ProviderAuditDto>> GetHistoryAsync(int id, CancellationToken cancellationToken);
}
