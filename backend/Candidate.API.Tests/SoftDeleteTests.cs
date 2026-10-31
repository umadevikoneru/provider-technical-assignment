using System.Net;
using System.Net.Http.Json;
using Candidate.API.Contracts;
using Microsoft.Data.Sqlite;

namespace Candidate.API.Tests;

public sealed class SoftDeleteTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Delete_hides_provider_from_standard_reads_but_keeps_the_row()
    {
        var created = await CreateAsync("Soft Delete Daycare");

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/providers/{created.ProviderId}");
        request.Headers.Add("X-User-Name", "tester");
        var delete = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/providers/{created.ProviderId}")).StatusCode);

        var list = await _client.GetFromJsonAsync<List<ProviderSummaryDto>>("/api/providers", ApiFactory.Json);
        Assert.DoesNotContain(list!, p => p.ProviderId == created.ProviderId);

        var deleted = await _client.GetFromJsonAsync<List<DeletedProviderDto>>("/api/providers/deleted", ApiFactory.Json);
        Assert.Contains(deleted!, p => p.ProviderId == created.ProviderId && p.DeletedBy == "tester");

        // The row is still physically present, flagged and stamped.
        using var connection = factory.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT IsDeleted, DeletedDate, DeletedBy FROM Provider WHERE ProviderId = $id";
        command.Parameters.AddWithValue("$id", created.ProviderId);
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.False(reader.IsDBNull(1));
        Assert.Equal("tester", reader.GetString(2));
    }

    [Fact]
    public async Task Deleted_provider_cannot_be_updated_or_deleted_again()
    {
        var created = await CreateAsync("Read Only After Delete");
        await _client.DeleteAsync($"/api/providers/{created.ProviderId}");

        var update = await _client.PutAsJsonAsync($"/api/providers/{created.ProviderId}",
            new { providerName = "Changed", county = "Fulton", status = "Active", version = 2 });
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/providers/{created.ProviderId}")).StatusCode);
    }

    [Fact]
    public async Task Restore_brings_provider_back_and_audit_records_every_step()
    {
        var created = await CreateAsync("Restorable Preschool");
        await _client.DeleteAsync($"/api/providers/{created.ProviderId}");

        var restore = await _client.PostAsync($"/api/providers/{created.ProviderId}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/providers/{created.ProviderId}")).StatusCode);

        var audit = await _client.GetFromJsonAsync<List<ProviderAuditDto>>($"/api/providers/{created.ProviderId}/audit", ApiFactory.Json);
        Assert.Equal(["Created", "SoftDeleted", "Restored"], audit!.Select(a => a.Action));
    }

    [Fact]
    public async Task A_deleted_provider_does_not_block_creating_the_same_provider_again()
    {
        var first = await CreateAsync("Reopened Learning Center");
        await _client.DeleteAsync($"/api/providers/{first.ProviderId}");

        var second = await _client.PostAsJsonAsync("/api/providers",
            new { providerName = "Reopened Learning Center", county = "Fulton", status = "Pending" });

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public void Database_rejects_hard_delete_even_outside_the_api()
    {
        using var connection = factory.OpenConnection();

        var provider = Assert.Throws<SqliteException>(() => Execute(connection, "DELETE FROM Provider WHERE ProviderId = 1"));
        Assert.Contains("Hard delete is not allowed", provider.Message);

        var license = Assert.Throws<SqliteException>(() => Execute(connection, "DELETE FROM License WHERE ProviderId = 1"));
        Assert.Contains("Hard delete is not allowed", license.Message);
    }

    [Fact]
    public void Database_rejects_edits_to_a_soft_deleted_provider()
    {
        using var connection = factory.OpenConnection();

        // Provider 12 is soft-deleted by the seed script.
        var error = Assert.Throws<SqliteException>(() =>
            Execute(connection, "UPDATE Provider SET ProviderName = 'Changed' WHERE ProviderId = 12"));
        Assert.Contains("soft-deleted", error.Message);
    }

    private async Task<ProviderDetailDto> CreateAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/providers", new { providerName = name, county = "Fulton", status = "Active" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await factory.ReadAsync<ProviderDetailDto>(response);
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
