using System.Net;
using System.Net.Http.Json;
using Candidate.API.Contracts;
using Candidate.API.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Candidate.API.Tests;

public sealed class ProviderValidationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_with_missing_fields_returns_field_errors()
    {
        var response = await _client.PostAsJsonAsync("/api/providers", new { providerName = "   ", county = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await factory.ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains("providerName", problem.Errors.Keys);
        Assert.Contains("county", problem.Errors.Keys);
        Assert.Contains("status", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_with_unknown_status_returns_a_status_error()
    {
        var response = await _client.PostAsJsonAsync("/api/providers", new { providerName = "Valid Name", county = "Cobb", status = "Closed" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await factory.ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains("Active, Inactive, Pending", Assert.Single(problem.Errors["status"]));
    }

    [Fact]
    public async Task Create_trims_input_and_stamps_audit_columns_on_the_server()
    {
        var response = await _client.PostAsJsonAsync("/api/providers", new { providerName = "  Trimmed Name  ", county = " Cobb ", status = "pending" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await factory.ReadAsync<ProviderDetailDto>(response);
        Assert.Equal("Trimmed Name", created.ProviderName);
        Assert.Equal("Cobb", created.County);
        Assert.Equal(ProviderStatus.Pending, created.Status);
        Assert.Equal(ApiFactory.Now.UtcDateTime, created.CreatedDate);
        Assert.Equal(1, created.Version);
    }

    [Fact]
    public async Task Duplicate_name_in_the_same_county_is_a_conflict()
    {
        await _client.PostAsJsonAsync("/api/providers", new { providerName = "Twin Oaks Daycare", county = "Hall", status = "Active" });

        var duplicate = await _client.PostAsJsonAsync("/api/providers", new { providerName = "TWIN OAKS DAYCARE", county = "hall", status = "Active" });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Update_with_a_stale_version_is_a_conflict()
    {
        var created = await factory.ReadAsync<ProviderDetailDto>(
            await _client.PostAsJsonAsync("/api/providers", new { providerName = "Version Check", county = "Bibb", status = "Active" }));

        var first = await _client.PutAsJsonAsync($"/api/providers/{created.ProviderId}",
            new { providerName = "Version Check", county = "Bibb", status = "Inactive", version = created.Version });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(created.Version + 1, (await factory.ReadAsync<ProviderDetailDto>(first)).Version);

        // A second client still holding the original version must not overwrite the change.
        var stale = await _client.PutAsJsonAsync($"/api/providers/{created.ProviderId}",
            new { providerName = "Overwrite", county = "Bibb", status = "Active", version = created.Version });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Unknown_provider_returns_problem_details_404()
    {
        var response = await _client.GetAsync("/api/providers/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
