using System.Net.Http.Json;
using Candidate.API.Contracts;
using Candidate.API.Domain;

namespace Candidate.API.Tests;

/// <summary>
/// The required data scenarios, checked against the seed data in 03_seed.sql.
/// This class never writes, so the seed stays intact for every test.
/// </summary>
public sealed class ScenarioTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const int DeletedProviderId = 12;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Active_providers_with_active_licenses()
    {
        var rows = await _client.GetFromJsonAsync<List<ActiveLicenseRowDto>>(
            "/api/reports/active-providers-active-licenses", ApiFactory.Json);

        Assert.Equal(
            ["GA-BIB-8001", "GA-COB-3001", "GA-FUL-1001", "GA-FUL-1002", "GA-RIC-7001"],
            rows!.Select(r => r.LicenseNumber).Order());
        // Excluded: expired (2, 10), suspended (6), provider not Active (4, 9, 11), soft-deleted (12).
        Assert.DoesNotContain(rows!, r => r.ProviderId == DeletedProviderId);
    }

    [Fact]
    public async Task Active_providers_with_expired_licenses()
    {
        var rows = await _client.GetFromJsonAsync<List<ExpiredLicenseRowDto>>(
            "/api/reports/active-providers-expired-licenses", ApiFactory.Json);

        Assert.Equal([2, 3, 10], rows!.Select(r => r.ProviderId).Distinct().Order());
        // Provider 12 has an expired license too, but it is soft-deleted.
        Assert.DoesNotContain(rows!, r => r.ProviderId == DeletedProviderId);
        // Expiration is decided by the date, even when staff never updated the recorded status.
        Assert.Contains(rows!, r => r.ProviderId == 2 && r.RecordedStatus == LicenseStatus.Active && r.IsStatusStale);
    }

    [Fact]
    public async Task Standard_listing_excludes_soft_deleted_providers()
    {
        var providers = await _client.GetFromJsonAsync<List<ProviderSummaryDto>>("/api/providers", ApiFactory.Json);

        Assert.Equal(11, providers!.Count);
        Assert.DoesNotContain(providers, p => p.ProviderId == DeletedProviderId);
    }

    [Fact]
    public async Task Provider_status_does_not_imply_license_validity()
    {
        // Provider 9 is Inactive but its license is still valid.
        var inactive = await _client.GetFromJsonAsync<ProviderDetailDto>("/api/providers/9", ApiFactory.Json);
        Assert.Equal(ProviderStatus.Inactive, inactive!.Status);
        Assert.Equal(LicenseStatus.Active, Assert.Single(inactive.Licenses).EffectiveStatus);

        // Provider 6 is Active but its license is suspended.
        var suspended = await _client.GetFromJsonAsync<ProviderDetailDto>("/api/providers/6", ApiFactory.Json);
        Assert.Equal(ProviderStatus.Active, suspended!.Status);
        Assert.Equal(LicenseStatus.Suspended, Assert.Single(suspended.Licenses).EffectiveStatus);
    }

    [Fact]
    public async Task Search_matches_name_or_county_case_insensitively()
    {
        var byName = await _client.GetFromJsonAsync<List<ProviderSummaryDto>>("/api/providers?search=montessori", ApiFactory.Json);
        Assert.Equal([8], byName!.Select(p => p.ProviderId));

        var byCounty = await _client.GetFromJsonAsync<List<ProviderSummaryDto>>("/api/providers?search=henry", ApiFactory.Json);
        Assert.Empty(byCounty!); // the only Henry County provider is soft-deleted
    }

    [Fact]
    public async Task Dashboard_counts_exclude_soft_deleted_providers()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardDto>("/api/dashboard", ApiFactory.Json);

        Assert.Equal(11, dashboard!.TotalProviders);
        Assert.Equal(1, dashboard.DeletedProviders);
        Assert.Equal(12, dashboard.TotalLicenses);
        Assert.Equal(3, dashboard.ActiveProvidersWithExpiredLicenses);
        Assert.Equal(["GA-RIC-7001", "GA-FUL-1002"], dashboard.ExpiringSoon.Select(l => l.LicenseNumber));
    }
}
