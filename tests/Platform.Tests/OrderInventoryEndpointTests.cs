using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Platform.Contracts.Catalog.Inventory;
using Platform.Contracts.Orders;
using Platform.Contracts.Security;

namespace Platform.Tests;

public sealed class OrderInventoryEndpointTests
{
    private static readonly Guid MainLocationId = VatPricingTestRig.MainLocationId;
    private static readonly Guid VariantId = VatPricingTestRig.SeededVariantId;

    [Fact]
    public async Task ManualOrderLifecycle_ReservesRejectsShortStockAndShipsOnCompletion()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        // Seed: 25 on hand, 2 held externally, so 23 can be reserved.
        var tooMany = await client.PostAsJsonAsync("/api/admin/orders", ManualOrder(24m));
        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
        var problem = await tooMany.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(
            "Insufficient stock for SKU-EXAMPLE-1-BLACK in market 'SE': requested 24, available 23.",
            Assert.Single(problem!.Errors[$"VariantId.{VariantId}"]));

        var createdResponse = await client.PostAsJsonAsync("/api/admin/orders", ManualOrder(2.5m));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var order = await createdResponse.Content.ReadFromJsonAsync<OrderDetailsDto>();
        Assert.Equal((25m, 4.5m), await StockAsync(client));

        var processing = await ChangeStatusAsync(client, order!.Id, "Processing", order.RowVersion);
        var current = await client.GetFromJsonAsync<OrderDetailsDto>($"/api/admin/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.OK, processing.StatusCode);
        var completed = await ChangeStatusAsync(client, order.Id, "Completed", current!.RowVersion);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal((22.5m, 2m), await StockAsync(client));

        var transactions = (await client.GetFromJsonAsync<InventoryLocationDetailsDto>($"/api/admin/inventory-locations/{MainLocationId}"))!.RecentTransactions;
        var shipment = Assert.Single(transactions, x => x.Type == "Shipment");
        Assert.Equal((-2.5m, -2.5m, order.Id), (shipment.QuantityDelta, shipment.ReservedQuantityDelta, shipment.ReferenceId));
    }

    private static async Task<(decimal OnHand, decimal Reserved)> StockAsync(HttpClient client)
    {
        var snapshot = await client.GetFromJsonAsync<VariantInventorySnapshotDto>($"/api/admin/variants/{VariantId}/inventory");
        var location = Assert.Single(snapshot!.Locations);
        return (location.OnHandQuantity, location.ReservedQuantity);
    }

    private static Task<HttpResponseMessage> ChangeStatusAsync(HttpClient client, Guid orderId, string toStatus, string rowVersion)
    {
        return client.PostAsJsonAsync($"/api/admin/orders/{orderId}/status", new { toStatus, rowVersion });
    }

    private static object ManualOrder(decimal quantity)
    {
        return new
        {
            marketId = VatPricingTestRig.SeMarketId,
            currencyCode = "SEK",
            cultureCode = "sv-SE",
            email = "buyer@example.com",
            lines = new[] { new { variantId = VariantId, quantity } },
            addresses = Array.Empty<object>()
        };
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
                        ["AdminSecurity:Users:0:Username"] = "ops-admin",
                        ["AdminSecurity:Users:0:DisplayName"] = "Ops Admin",
                        ["AdminSecurity:Users:0:Password"] = "OpsAdmin123!",
                        ["AdminSecurity:Users:0:Roles:0"] = "PlatformAdmin"
                    });
                });
            });
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/admin/auth/login", new AdminLoginRequest("ops-admin", "OpsAdmin123!"));
        var body = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, body);
        var login = System.Text.Json.JsonSerializer.Deserialize<AdminLoginResponse>(
            body,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }
}
