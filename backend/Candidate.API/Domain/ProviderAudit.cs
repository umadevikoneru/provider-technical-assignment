namespace Candidate.API.Domain;

/// <summary>Read-only row of the ProviderAudit table, which is written by database triggers.</summary>
public class ProviderAudit
{
    public int AuditId { get; private set; }

    public int ProviderId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string ProviderName { get; private set; } = string.Empty;

    public string County { get; private set; } = string.Empty;

    public ProviderStatus Status { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime ChangedDate { get; private set; }

    public string ChangedBy { get; private set; } = string.Empty;
}
