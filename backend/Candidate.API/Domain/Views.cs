namespace Candidate.API.Domain;

// Keyless read models mapped to the SQLite views in 02_views.sql.
// The views already exclude soft-deleted providers.

/// <summary>vw_LicenseEffectiveStatus: one row per license of a non-deleted provider.</summary>
public class LicenseView
{
    public int LicenseId { get; private set; }

    public int ProviderId { get; private set; }

    public string ProviderName { get; private set; } = string.Empty;

    public string County { get; private set; } = string.Empty;

    public ProviderStatus ProviderStatus { get; private set; }

    public string LicenseNumber { get; private set; } = string.Empty;

    public LicenseStatus RecordedStatus { get; private set; }

    public DateOnly ExpirationDate { get; private set; }

    public LicenseStatus EffectiveStatus { get; private set; }

    public int DaysUntilExpiration { get; private set; }

    public bool IsStatusStale { get; private set; }
}

/// <summary>vw_ActiveProvidersActiveLicenses (scenario 1).</summary>
public class ActiveProviderActiveLicense
{
    public int ProviderId { get; private set; }

    public string ProviderName { get; private set; } = string.Empty;

    public string County { get; private set; } = string.Empty;

    public int LicenseId { get; private set; }

    public string LicenseNumber { get; private set; } = string.Empty;

    public DateOnly ExpirationDate { get; private set; }

    public int DaysUntilExpiration { get; private set; }
}

/// <summary>vw_ActiveProvidersWithExpiredLicenses (scenario 2).</summary>
public class ActiveProviderExpiredLicense
{
    public int ProviderId { get; private set; }

    public string ProviderName { get; private set; } = string.Empty;

    public string County { get; private set; } = string.Empty;

    public int LicenseId { get; private set; }

    public string LicenseNumber { get; private set; } = string.Empty;

    public LicenseStatus RecordedStatus { get; private set; }

    public DateOnly ExpirationDate { get; private set; }

    public int DaysSinceExpiration { get; private set; }

    public bool IsStatusStale { get; private set; }
}
