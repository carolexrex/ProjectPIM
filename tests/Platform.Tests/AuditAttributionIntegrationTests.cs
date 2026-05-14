using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Auditing;
using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Media;
using Platform.Contracts.Catalog.Brands;
using Platform.Contracts.Security;
using Platform.Domain.Auditing;
using Platform.Infrastructure.Auditing;
using Platform.Infrastructure.Catalog.Brands;
using Platform.Infrastructure.Catalog.Media;
using Platform.Infrastructure.Persistence;

namespace Platform.Tests;

public sealed class AuditAttributionIntegrationTests
{
    [Fact]
    public async Task AuthenticatedWrite_PersistsAuditLogWithAuthenticatedActor()
    {
        var databaseName = $"audit-tests-{Guid.NewGuid():N}";

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Persistence:Provider"] = "InMemory",
                        ["AdminSecurity:Users:0:Username"] = "audit-admin",
                        ["AdminSecurity:Users:0:DisplayName"] = "Audit Admin",
                        ["AdminSecurity:Users:0:Password"] = "Audit123!",
                        ["AdminSecurity:Users:0:Roles:0"] = "PlatformAdmin",
                        ["AdminSecurity:Users:0:Roles:1"] = "CatalogManager"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IUnitOfWork>();
                    services.RemoveAll<IAuditLogRepository>();
                    services.RemoveAll<IBrandRepository>();
                    services.RemoveAll<IMediaAssetRepository>();
                    services.RemoveAll<PlatformDbContext>();
                    services.RemoveAll<DbContextOptions<PlatformDbContext>>();

                    services.AddDbContext<PlatformDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));

                    services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                    services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
                    services.AddScoped<IBrandRepository, EfBrandRepository>();
                    services.AddScoped<IMediaAssetRepository, EfMediaAssetRepository>();
                });
            });

        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/admin/auth/login",
            new AdminLoginRequest("audit-admin", "Audit123!"));
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, loginResponseBody);

        var login = System.Text.Json.JsonSerializer.Deserialize<AdminLoginResponse>(loginResponseBody, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        var createBrandResponse = await client.PostAsJsonAsync(
            "/api/admin/brands",
            new CreateBrandRequest
            {
                Code = "audit-brand",
                SortOrder = 10
            });
        createBrandResponse.EnsureSuccessStatusCode();

        var createdBrand = await createBrandResponse.Content.ReadFromJsonAsync<BrandDetailsDto>();
        Assert.NotNull(createdBrand);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var auditLog = await dbContext.AuditLogs
            .AsNoTracking()
            .SingleAsync(x => x.EntityType == "Brand" && x.EntityId == createdBrand!.Id.ToString());

        Assert.Equal("Created", auditLog.Action);
        Assert.Equal("audit-admin", auditLog.ActorIdentifier);
        Assert.Equal("Audit Admin", auditLog.ActorDisplayName);
        Assert.Equal("AdminUser", auditLog.ActorType);
    }
}
