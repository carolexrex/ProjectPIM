using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Platform.Contracts.Catalog.Tax;
using Platform.Contracts.Common;
using Platform.Contracts.Security;

namespace Platform.Tests;

public sealed class VatRulesEndpointTests
{
    [Fact]
    public async Task VatRules_RequireAuthenticationAndPricingWritePolicy()
    {
        await using var factory = CreateFactory();
        using var anonymous = factory.CreateClient();
        using var viewer = factory.CreateClient();
        await AuthenticateAsync(viewer, "vat-viewer", "Viewer123!");

        var anonymousList = await anonymous.GetAsync("/api/admin/vat-rules");
        var viewerCreate = await viewer.PostAsJsonAsync("/api/admin/vat-rules", new { marketId = VatPricingTestRig.SeMarketId, taxCategoryCode = "FOOD", rate = 0.12m });

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousList.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerCreate.StatusCode);
    }

    [Fact]
    public async Task VatRules_SupportCreateConflictUpdateAndDeleteWithRowVersion()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "vat-admin", "VatAdmin123!");

        var missingRate = await client.PostAsJsonAsync("/api/admin/vat-rules", new { marketId = VatPricingTestRig.SeMarketId, taxCategoryCode = "FOOD" });
        Assert.Equal(HttpStatusCode.BadRequest, missingRate.StatusCode);
        var missingRateProblem = await missingRate.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Rate", missingRateProblem!.Errors.Keys);

        var createResponse = await client.PostAsJsonAsync("/api/admin/vat-rules", new { marketId = VatPricingTestRig.SeMarketId, taxCategoryCode = "food", rate = 0.12m });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<VatRuleDto>();
        Assert.Equal("FOOD", created!.TaxCategoryCode);
        Assert.Equal("SE", created.MarketCode);

        var duplicate = await client.PostAsJsonAsync("/api/admin/vat-rules", new { marketId = VatPricingTestRig.SeMarketId, taxCategoryCode = "FOOD", rate = 0.06m });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var list = await client.GetFromJsonAsync<PagedResponse<VatRuleDto>>($"/api/admin/vat-rules?marketId={VatPricingTestRig.SeMarketId}");
        Assert.Equal(["FOOD", "STANDARD"], list!.Items.Select(x => x.TaxCategoryCode).ToArray());

        var updateResponse = await client.PutAsJsonAsync($"/api/admin/vat-rules/{created.Id}", new { rate = 0m, rowVersion = created.RowVersion });
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<VatRuleDto>();
        Assert.Equal(0m, updated!.Rate);

        var staleUpdate = await client.PutAsJsonAsync($"/api/admin/vat-rules/{created.Id}", new { rate = 0.25m, rowVersion = created.RowVersion });
        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
        var staleDelete = await client.PostAsJsonAsync($"/api/admin/vat-rules/{created.Id}/delete", new { rowVersion = created.RowVersion });
        Assert.Equal(HttpStatusCode.Conflict, staleDelete.StatusCode);

        var delete = await client.PostAsJsonAsync($"/api/admin/vat-rules/{created.Id}/delete", new { rowVersion = updated.RowVersion });
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var afterDelete = await client.GetAsync($"/api/admin/vat-rules/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Persistence:Provider"] = "InMemory",
                        ["AdminSecurity:Users:0:Username"] = "vat-admin",
                        ["AdminSecurity:Users:0:DisplayName"] = "VAT Admin",
                        ["AdminSecurity:Users:0:Password"] = "VatAdmin123!",
                        ["AdminSecurity:Users:0:Roles:0"] = "PricingManager",
                        ["AdminSecurity:Users:1:Username"] = "vat-viewer",
                        ["AdminSecurity:Users:1:DisplayName"] = "Catalog Viewer",
                        ["AdminSecurity:Users:1:Password"] = "Viewer123!",
                        ["AdminSecurity:Users:1:Roles:0"] = "CatalogViewer"
                    });
                });
            });
    }

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/admin/auth/login", new AdminLoginRequest(username, password));
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, loginResponseBody);

        var login = System.Text.Json.JsonSerializer.Deserialize<AdminLoginResponse>(
            loginResponseBody,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }
}
