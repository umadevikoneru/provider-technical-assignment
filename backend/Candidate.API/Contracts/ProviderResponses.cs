using Candidate.API.Domain;

namespace Candidate.API.Contracts;

public sealed record ProviderSummaryDto(
    int ProviderId,
    string ProviderName,
    string County,
    ProviderStatus Status,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    int Version,
    int LicenseCount,
    int ExpiredLicenseCount);

public sealed record ProviderDetailDto(
    int ProviderId,
    string ProviderName,
    string County,
    ProviderStatus Status,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    string? UpdatedBy,
    int Version,
    IReadOnlyList<LicenseDto> Licenses);

/// <summary>
/// A license as the business should read it. RecordedStatus is what staff entered;
/// EffectiveStatus also accounts for ExpirationDate and is what decides validity.
/// </summary>
public sealed record LicenseDto(
    int LicenseId,
    string LicenseNumber,
    LicenseStatus RecordedStatus,
    LicenseStatus EffectiveStatus,
    DateOnly ExpirationDate,
    int DaysUntilExpiration,
    bool IsStatusStale);

public sealed record DeletedProviderDto(
    int ProviderId,
    string ProviderName,
    string County,
    ProviderStatus Status,
    DateTime CreatedDate,
    DateTime? DeletedDate,
    string? DeletedBy);

public sealed record ProviderAuditDto(
    int AuditId,
    string Action,
    string ProviderName,
    string County,
    ProviderStatus Status,
    bool IsDeleted,
    DateTime ChangedDate,
    string ChangedBy);
