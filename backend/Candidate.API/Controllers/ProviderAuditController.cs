using Candidate.API.Contracts;
using Candidate.API.Services.Providers;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

/// <summary>Audit and troubleshooting endpoints. Unlike standard endpoints, these include soft-deleted providers.</summary>
[ApiController]
[Route("api/providers")]
[Produces("application/json")]
public sealed class ProviderAuditController(IProviderAuditService audit) : ControllerBase
{
    /// <summary>Soft-deleted providers.</summary>
    [HttpGet("deleted")]
    public Task<IReadOnlyList<DeletedProviderDto>> ListDeleted(CancellationToken cancellationToken) =>
        audit.ListDeletedAsync(cancellationToken);

    /// <summary>Change history of a provider, including deleted ones.</summary>
    [HttpGet("{id:int}/audit")]
    [ProducesResponseType<IReadOnlyList<ProviderAuditDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ProviderAuditDto>> History(int id, CancellationToken cancellationToken) =>
        audit.GetHistoryAsync(id, cancellationToken);
}
