namespace Candidate.API.Domain;

/// <summary>
/// License status. Used both for the status recorded by staff and for the effective
/// status computed in vw_LicenseEffectiveStatus (which also considers ExpirationDate).
/// </summary>
public enum LicenseStatus
{
    Active,
    Expired,
    Suspended
}
