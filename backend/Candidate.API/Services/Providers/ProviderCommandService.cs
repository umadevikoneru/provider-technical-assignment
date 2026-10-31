using Candidate.API.Contracts;
using Candidate.API.Data;
using Candidate.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services.Providers;

/// <summary>
/// Write side for providers: business rules for create, update, soft delete and restore.
/// Responses are read back through <see cref="IProviderQueryService"/>, so the read
/// shape is defined in one place.
/// </summary>
public sealed class ProviderCommandService(AppDbContext db, IProviderQueryService queries) : IProviderCommandService
{
    public async Task<ProviderDetailDto> CreateAsync(ProviderCreateRequest request, CancellationToken cancellationToken)
    {
        var (name, county) = Normalize(request);
        await EnsureNoLiveDuplicateAsync(name, county, excludeProviderId: null, cancellationToken);

        var provider = new Provider
        {
            ProviderName = name,
            County = county,
            Status = request.ParseStatus()
        };

        db.Providers.Add(provider);
        await db.SaveChangesAsync(cancellationToken);

        return await queries.GetAsync(provider.ProviderId, cancellationToken);
    }

    public async Task<ProviderDetailDto> UpdateAsync(int id, ProviderUpdateRequest request, CancellationToken cancellationToken)
    {
        var provider = await FindLiveAsync(id, cancellationToken);

        if (provider.Version != request.Version)
        {
            throw StaleVersion();
        }

        var (name, county) = Normalize(request);
        await EnsureNoLiveDuplicateAsync(name, county, excludeProviderId: id, cancellationToken);

        provider.ProviderName = name;
        provider.County = county;
        provider.Status = request.ParseStatus();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone saved between our read and our write; the UPDATE ... WHERE Version = n matched nothing.
            throw StaleVersion();
        }

        return await queries.GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var provider = await FindLiveAsync(id, cancellationToken);

        // Who and when are stamped by AuditStampInterceptor on save.
        provider.SoftDelete();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProviderDetailDto> RestoreAsync(int id, CancellationToken cancellationToken)
    {
        // Restore is one of the few paths that must see deleted rows.
        var provider = await db.Providers.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ProviderId == id, cancellationToken)
            ?? throw NotFound(id);

        if (!provider.IsDeleted)
        {
            throw new ConflictException($"Provider {id} is not deleted.");
        }

        await EnsureNoLiveDuplicateAsync(provider.ProviderName, provider.County, excludeProviderId: id, cancellationToken);

        provider.Restore();
        await db.SaveChangesAsync(cancellationToken);

        return await queries.GetAsync(id, cancellationToken);
    }

    /// <summary>Tracked load of a non-deleted provider (the query filter hides deleted ones).</summary>
    private async Task<Provider> FindLiveAsync(int id, CancellationToken cancellationToken) =>
        await db.Providers.FirstOrDefaultAsync(p => p.ProviderId == id, cancellationToken)
            ?? throw NotFound(id);

    /// <summary>
    /// Friendly pre-check for the partial unique index UX_Provider_Name_County_NotDeleted.
    /// The index is still the real guarantee if two requests race.
    /// </summary>
    private async Task EnsureNoLiveDuplicateAsync(string name, string county, int? excludeProviderId, CancellationToken cancellationToken)
    {
        var existing = await db.Providers
            .Where(p => p.ProviderId != excludeProviderId
                        // Same NOCASE collation as the unique index, so SQLite can use the index for this lookup.
                        && EF.Functions.Collate(p.ProviderName, "NOCASE") == name
                        && EF.Functions.Collate(p.County, "NOCASE") == county)
            .Select(p => new { p.ProviderName, p.County })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            // Name the stored record (its own spelling), not the caller's input.
            throw new ConflictException($"A provider named '{existing.ProviderName}' already exists in {existing.County} County.");
        }
    }

    private static (string Name, string County) Normalize(ProviderWriteRequest request) =>
        (request.ProviderName!.Trim(), request.County!.Trim());

    private static NotFoundException NotFound(int id) => new($"Provider {id} was not found.");

    private static ConflictException StaleVersion() =>
        new("This provider was changed by someone else. Reload it and try again.");
}
