using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;
using Platform.Contracts.Security;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;

namespace Platform.Tests;

public sealed class StorefrontProjectionRefreshMessagesIntegrationTests
{
    [Fact]
    public async Task ResetEndpoint_ResetsAbandonedStorefrontRefreshMessage()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var store = factory.Services.GetRequiredService<InMemoryCatalogStore>();
        var message = SeedAbandonedRefreshMessage(store);

        var listResponse = await client.GetFromJsonAsync<PagedResponse<StorefrontProjectionRefreshMessageSummaryDto>>(
            "/api/admin/storefront-projection-refresh-messages?status=open");

        Assert.NotNull(listResponse);
        Assert.Contains(listResponse!.Items, x => x.Id == message.Id && x.Status == StorefrontProjectionRefreshMessageStatuses.Abandoned);

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/storefront-projection-refresh-messages/{message.Id}/reset",
            new ResetStorefrontProjectionRefreshMessageRequest
            {
                RowVersion = message.RowVersion
            });

        response.EnsureSuccessStatusCode();
        var reset = await response.Content.ReadFromJsonAsync<StorefrontProjectionRefreshMessageDetailsDto>();
        Assert.NotNull(reset);
        Assert.Equal(StorefrontProjectionRefreshMessageStatuses.Pending, reset!.Status);
        Assert.Equal(0, reset.ProcessingAttemptCount);
        Assert.Null(store.OutboxMessages[message.Id].ProcessingAbandonedAtUtc);
    }

    [Fact]
    public async Task GetEndpoint_ReturnsNotFoundForNonRefreshOutboxMessage()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var store = factory.Services.GetRequiredService<InMemoryCatalogStore>();
        var message = new OutboxMessage(
            Guid.NewGuid(),
            WebhookEventTypes.ProductUpdated,
            "Product",
            Guid.NewGuid(),
            "{\"event\":\"product.updated\"}",
            DateTime.UtcNow);
        store.OutboxMessages[message.Id] = message;

        using var response = await client.GetAsync($"/api/admin/storefront-projection-refresh-messages/{message.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
                        ["AdminSecurity:Users:0:Username"] = "storefront-ops-admin",
                        ["AdminSecurity:Users:0:DisplayName"] = "Storefront Ops Admin",
                        ["AdminSecurity:Users:0:Password"] = "StorefrontOps123!",
                        ["AdminSecurity:Users:0:Roles:0"] = "PlatformAdmin",
                        ["AdminSecurity:Users:0:Roles:1"] = "CatalogManager"
                    });
                });
            });
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync(
            "/api/admin/auth/login",
            new AdminLoginRequest("storefront-ops-admin", "StorefrontOps123!"));
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, loginResponseBody);

        var login = System.Text.Json.JsonSerializer.Deserialize<AdminLoginResponse>(
            loginResponseBody,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

    private static OutboxMessage SeedAbandonedRefreshMessage(InMemoryCatalogStore store)
    {
        var message = new OutboxMessage(
            Guid.NewGuid(),
            WebhookEventTypes.StorefrontProjectionRefreshRequested,
            "Product",
            Guid.NewGuid(),
            "{\"aggregateType\":\"Product\"}",
            DateTime.UtcNow);
        message.MarkProcessingAbandoned("projection failure", message.RowVersion);
        store.OutboxMessages[message.Id] = message;
        return message;
    }
}
