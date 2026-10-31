namespace Candidate.API.Domain;

public class Provider
{
    public int ProviderId { get; private set; }

    public string ProviderName { get; set; } = string.Empty;

    public string County { get; set; } = string.Empty;

    public ProviderStatus Status { get; set; } = ProviderStatus.Pending;

    // Audit columns are stamped by AuditStampInterceptor, never by callers.
    public DateTime CreatedDate { get; internal set; }

    public DateTime? UpdatedDate { get; internal set; }

    public string? UpdatedBy { get; internal set; }

    /// <summary>Optimistic-concurrency token; incremented on every update.</summary>
    public int Version { get; internal set; } = 1;

    // Soft delete. The flag is only changed through SoftDelete/Restore; AuditStampInterceptor
    // stamps DeletedDate/DeletedBy on save. The database CHECK constraint keeps all three consistent.
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedDate { get; internal set; }

    public string? DeletedBy { get; internal set; }

    public List<License> Licenses { get; } = [];

    public void SoftDelete() => IsDeleted = true;

    public void Restore() => IsDeleted = false;
}
