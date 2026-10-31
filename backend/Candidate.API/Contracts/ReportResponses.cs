using Candidate.API.Domain;

namespace Candidate.API.Contracts;

public sealed record ActiveLicenseRowDto(
    int ProviderId,
    string ProviderName,
    string County,
    int LicenseId,
    string LicenseNumber,
    DateOnly ExpirationDate,
    int DaysUntilExpiration);

public sealed record ExpiredLicenseRowDto(
    int ProviderId,
    string ProviderName,
    string County,
    int LicenseId,
    string LicenseNumber,
    LicenseStatus RecordedStatus,
    DateOnly ExpirationDate,
    int DaysSinceExpiration,
    bool IsStatusStale);

public sealed record CountByStatusDto(string Status, int Count);

public sealed record ExpiringLicenseDto(
    int ProviderId,
    string ProviderName,
    string LicenseNumber,
    DateOnly ExpirationDate,
    int DaysUntilExpiration);

public sealed record DashboardDto(
    int TotalProviders,
    int DeletedProviders,
    int TotalLicenses,
    int ActiveProvidersWithExpiredLicenses,
    int StaleLicenseStatuses,
    IReadOnlyList<CountByStatusDto> ProvidersByStatus,
    IReadOnlyList<CountByStatusDto> LicensesByEffectiveStatus,
    int ExpiringWithinDays,
    IReadOnlyList<ExpiringLicenseDto> ExpiringSoon);
