using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Platform.Application.Security;
using Platform.Backoffice.Configuration;
using Platform.Backoffice.Integration;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/auth/login";
        options.Cookie.Name = "Platform.Backoffice.Auth";
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = context =>
        {
            var expiresAtValue = context.Principal?.FindFirst("access_token_expires_at")?.Value;
            if (!DateTime.TryParse(expiresAtValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAtUtc)
                || expiresAtUtc <= DateTime.UtcNow)
            {
                context.RejectPrincipal();
            }

            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AdminPolicies.CatalogRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CatalogManager,
            AdminRoles.CatalogViewer));

    options.AddPolicy(
        AdminPolicies.CatalogWrite,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CatalogManager));

    options.AddPolicy(
        AdminPolicies.PricingRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.PricingManager));

    options.AddPolicy(
        AdminPolicies.PricingWrite,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.PricingManager));

    options.AddPolicy(
        AdminPolicies.CustomerRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CustomerService,
            AdminRoles.CatalogManager,
            AdminRoles.CatalogViewer));

    options.AddPolicy(
        AdminPolicies.CustomerWrite,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CustomerService,
            AdminRoles.CatalogManager));

    options.AddPolicy(
        AdminPolicies.InventoryRead,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CatalogManager,
            AdminRoles.CatalogViewer,
            AdminRoles.InventoryManager));

    options.AddPolicy(
        AdminPolicies.InventoryWrite,
        policy => policy.RequireAuthenticatedUser().RequireRole(
            AdminRoles.PlatformAdmin,
            AdminRoles.CatalogManager,
            AdminRoles.InventoryManager));
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".data-protection")))
    .SetApplicationName("Platform.Backoffice");

builder.Services.AddOptions<AdminApiOptions>()
    .BindConfiguration(AdminApiOptions.SectionName)
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "AdminApi:BaseUrl must be an absolute URI.")
    .ValidateOnStart();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AdminApiAuthenticationHandler>();
builder.Services.AddHttpClient<IAdminAuthenticationClient, AdminAuthenticationClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
});

builder.Services.AddHttpClient<IAdminApiClient, AdminApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
})
.AddHttpMessageHandler<AdminApiAuthenticationHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

app.Run();
