using System.ComponentModel.DataAnnotations;
using Candidate.API.Contracts;
using Candidate.API.Domain;
using Candidate.API.Services.Providers;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Controllers;

/// <summary>Standard provider workflows. Soft-deleted providers are invisible here.</summary>
[ApiController]
[Route("api/providers")]
[Produces("application/json")]
public sealed class ProvidersController(IProviderQueryService queries, IProviderCommandService commands) : ControllerBase
{
    /// <summary>Lists providers that are not soft-deleted.</summary>
    /// <param name="search">Matches provider name or county (case-insensitive, contains).</param>
    /// <param name="status">Optional status filter.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProviderSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<ProviderSummaryDto>> List(
        [FromQuery, StringLength(200, ErrorMessage = "Search must be 200 characters or fewer.")] string? search,
        [FromQuery] ProviderStatus? status,
        CancellationToken cancellationToken) =>
        queries.ListAsync(search, status, cancellationToken);

    /// <summary>Gets one provider with its licenses (effective status included). 404 if soft-deleted.</summary>
    [HttpGet("{id:int}", Name = nameof(Get))]
    [ProducesResponseType<ProviderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ProviderDetailDto> Get(int id, CancellationToken cancellationToken) =>
        queries.GetAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType<ProviderDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProviderDetailDto>> Create(ProviderCreateRequest request, CancellationToken cancellationToken)
    {
        var created = await commands.CreateAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(Get), new { id = created.ProviderId }, created);
    }

    /// <summary>Updates a provider. Send the Version you last read; a stale Version returns 409.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProviderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ProviderDetailDto> Update(int id, ProviderUpdateRequest request, CancellationToken cancellationToken) =>
        commands.UpdateAsync(id, request, cancellationToken);

    /// <summary>Soft-deletes a provider. The record stays in the database for audit.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await commands.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Restores a soft-deleted provider.</summary>
    [HttpPost("{id:int}/restore")]
    [ProducesResponseType<ProviderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ProviderDetailDto> Restore(int id, CancellationToken cancellationToken) =>
        commands.RestoreAsync(id, cancellationToken);
}
