using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Platform.Api.Infrastructure.ErrorHandling;
using Platform.Api.Security;
using Platform.Application.Abstractions.Security;
using Platform.Application.Security;
using Platform.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions<AdminSecurityOptions>()
    .BindConfiguration(AdminSecurityOptions.SectionName)
    .Validate(options => options.Users.Count > 0, "AdminSecurity:Users must contain at least one configured user.")
    .Validate(
        options => options.Users.All(user => BootstrapCredentialVerifier.HasConfiguredSecret(user.Password, user.PasswordHash)),
        "AdminSecurity:Users must define Password or PasswordHash.")
    .ValidateOnStart();
builder.Services.AddOptions<AdminIdentityTokenOptions>()
    .BindConfiguration(AdminIdentityTokenOptions.SectionName)
    .Validate(options => options.AccessTokenLifetimeMinutes > 0, "AdminIdentityToken:AccessTokenLifetimeMinutes must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".data-protection")))
    .SetApplicationName("Platform.Api");
builder.Services.AddScoped<AdminConfiguredUserAuthenticationService>();
builder.Services.AddSingleton<AdminAccessTokenService>();
builder.Services.AddScoped<ICurrentActorAccessor, HttpContextCurrentActorAccessor>();
builder.Services
    .AddAuthentication(AdminAccessTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AdminAccessTokenAuthenticationHandler>(
        AdminAccessTokenAuthenticationHandler.SchemeName,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AdminPolicies.CatalogRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CatalogManager,
            AdminRoles.CatalogViewer,
            AdminRoles.IntegrationClient));

    options.AddPolicy(
        AdminPolicies.CatalogWrite,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CatalogManager,
            AdminRoles.IntegrationClient));

    options.AddPolicy(
        AdminPolicies.PricingRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.PricingManager,
            AdminRoles.IntegrationClient));

    options.AddPolicy(
        AdminPolicies.PricingWrite,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.PricingManager,
            AdminRoles.IntegrationClient));

    options.AddPolicy(
        AdminPolicies.CustomerRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CustomerService,
            AdminRoles.CatalogManager,
            AdminRoles.CatalogViewer,
            AdminRoles.IntegrationClient));

    options.AddPolicy(
        AdminPolicies.CustomerWrite,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CustomerService,
            AdminRoles.CatalogManager,
            AdminRoles.IntegrationClient));

    options.AddPolicy(
        AdminPolicies.InventoryRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CatalogManager,
            AdminRoles.CatalogViewer,
            AdminRoles.InventoryManager,
            AdminRoles.IntegrationClient));

    options.AddPolicy(
        AdminPolicies.InventoryWrite,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CatalogManager,
            AdminRoles.InventoryManager,
            AdminRoles.IntegrationClient));

    options.AddPolicy(
        AdminPolicies.AuditRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(AdminRoles.PlatformAdmin));

    options.AddPolicy(
        AdminPolicies.IdentityRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(AdminRoles.PlatformAdmin));

    options.AddPolicy(
        AdminPolicies.IdentityWrite,
        policy => policy.RequireAuthenticatedUser().RequireRole(AdminRoles.PlatformAdmin));
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddCatalogPersistence(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
