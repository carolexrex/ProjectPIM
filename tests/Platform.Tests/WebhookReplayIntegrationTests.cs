using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Contracts.Integrations;
using Platform.Contracts.Security;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;

namespace Platform.Tests;

public sealed class WebhookReplayIntegrationTests
{
    [Fact]
    public async Task ReplayEndpoint_ReturnsBadRequestWhenReplayIsDisabled()
    {
        await using var factory = CreateFactory(manualReplayEnabled: false, manualReplayDelaySeconds: 60);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var store = factory.Services.GetRequiredService<InMemoryCatalogStore>();
        var delivery = SeedFailedDelivery(store);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/webhook-deliveries/{delivery.Id}/replay",
            new ReplayWebhookDeliveryRequest
            {
                RowVersion = delivery.RowVersion
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("ManualReplayEnabled", problem!.Errors.Keys);
    }

    [Fact]
    public async Task ReplayEndpoint_ReplaysAbandonedDeliveryUsingConfiguredDelay()
    {
        await using var factory = CreateFactory(manualReplayEnabled: true, manualReplayDelaySeconds: 90);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var store = factory.Services.GetRequiredService<InMemoryCatalogStore>();
        var delivery = SeedAbandonedDelivery(store);
        var lowerBound = DateTime.UtcNow.AddSeconds(90);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/webhook-deliveries/{delivery.Id}/replay",
            new ReplayWebhookDeliveryRequest
            {
                RowVersion = delivery.RowVersion
            });

        var upperBound = DateTime.UtcNow.AddSeconds(90);

        response.EnsureSuccessStatusCode();
        var replayed = await response.Content.ReadFromJsonAsync<WebhookDeliveryDetailsDto>();
        Assert.NotNull(replayed);
        Assert.Equal(WebhookDeliveryStatuses.Failed, replayed!.Status);
        Assert.NotNull(replayed.NextAttemptAtUtc);
        Assert.InRange(replayed.NextAttemptAtUtc!.Value, lowerBound.AddSeconds(-1), upperBound.AddSeconds(1));

        var stored = store.WebhookDeliveries[delivery.Id];
        Assert.Equal(WebhookDeliveryStatuses.Failed, stored.Status);
        Assert.Equal(replayed.RowVersion, stored.RowVersion);
    }

    [Fact]
    public async Task ReplayEndpoint_ReturnsBadRequestForSucceededDelivery()
    {
        await using var factory = CreateFactory(manualReplayEnabled: true, manualReplayDelaySeconds: 60);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var store = factory.Services.GetRequiredService<InMemoryCatalogStore>();
        var delivery = SeedSucceededDelivery(store);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/webhook-deliveries/{delivery.Id}/replay",
            new ReplayWebhookDeliveryRequest
            {
                RowVersion = delivery.RowVersion
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("WebhookDeliveryId", problem!.Errors.Keys);
    }

    private static WebApplicationFactory<Program> CreateFactory(bool manualReplayEnabled, int manualReplayDelaySeconds)
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
                        ["Webhooks:ManualReplayEnabled"] = manualReplayEnabled.ToString(),
                        ["Webhooks:ManualReplayDelaySeconds"] = manualReplayDelaySeconds.ToString(),
                        ["AdminSecurity:Users:0:Username"] = "replay-admin",
                        ["AdminSecurity:Users:0:DisplayName"] = "Replay Admin",
                        ["AdminSecurity:Users:0:Password"] = "Replay123!",
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
            new AdminLoginRequest("replay-admin", "Replay123!"));
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, loginResponseBody);

        var login = System.Text.Json.JsonSerializer.Deserialize<AdminLoginResponse>(
            loginResponseBody,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

    private static WebhookDelivery SeedFailedDelivery(InMemoryCatalogStore store)
    {
        var now = DateTime.UtcNow;
        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            "Replay subscription",
            "https://example.test/hooks/replay",
            "secret",
            [WebhookEventTypes.ProductUpdated],
            true,
            now);
        store.WebhookSubscriptions[subscription.Id] = subscription;

        var delivery = new WebhookDelivery(
            Guid.NewGuid(),
            subscription.Id,
            Guid.NewGuid(),
            WebhookEventTypes.ProductUpdated,
            "{\"event\":\"product.updated\"}",
            now);
        delivery.Start(delivery.RowVersion);
        delivery.MarkFailed(500, "{\"error\":true}", now.AddMinutes(5), delivery.RowVersion);
        store.WebhookDeliveries[delivery.Id] = delivery;
        return delivery;
    }

    private static WebhookDelivery SeedAbandonedDelivery(InMemoryCatalogStore store)
    {
        var now = DateTime.UtcNow;
        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            "Replay subscription",
            "https://example.test/hooks/replay",
            "secret",
            [WebhookEventTypes.ProductUpdated],
            true,
            now);
        store.WebhookSubscriptions[subscription.Id] = subscription;

        var delivery = new WebhookDelivery(
            Guid.NewGuid(),
            subscription.Id,
            Guid.NewGuid(),
            WebhookEventTypes.ProductUpdated,
            "{\"event\":\"product.updated\"}",
            now);
        delivery.Start(delivery.RowVersion);
        delivery.MarkAbandoned(410, "{\"gone\":true}", delivery.RowVersion);
        store.WebhookDeliveries[delivery.Id] = delivery;
        return delivery;
    }

    private static WebhookDelivery SeedSucceededDelivery(InMemoryCatalogStore store)
    {
        var now = DateTime.UtcNow;
        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            "Replay subscription",
            "https://example.test/hooks/replay",
            "secret",
            [WebhookEventTypes.ProductUpdated],
            true,
            now);
        store.WebhookSubscriptions[subscription.Id] = subscription;

        var delivery = new WebhookDelivery(
            Guid.NewGuid(),
            subscription.Id,
            Guid.NewGuid(),
            WebhookEventTypes.ProductUpdated,
            "{\"event\":\"product.updated\"}",
            now);
        delivery.Start(delivery.RowVersion);
        delivery.MarkSucceeded(200, "{\"ok\":true}", delivery.RowVersion);
        store.WebhookDeliveries[delivery.Id] = delivery;
        return delivery;
    }
}
