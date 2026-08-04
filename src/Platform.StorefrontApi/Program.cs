using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Platform.Infrastructure.DependencyInjection;
using Platform.Infrastructure.Storefront;

var builder = WebApplication.CreateBuilder(args);

const string StorefrontCorsPolicy = "StorefrontCors";

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddCatalogPersistence(builder.Configuration);

// Outside Development the cart access-token signing key must be configured explicitly.
// Without it the token service falls back to a per-process ephemeral key, so cart tokens
// stop validating after a restart or across scaled-out instances. Fail fast instead.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddOptions<StorefrontCartAccessTokenOptions>()
        .Validate(
            options => !string.IsNullOrWhiteSpace(options.SigningKey),
            $"{StorefrontCartAccessTokenOptions.SectionName}:SigningKey must be configured outside Development.")
        .ValidateOnStart();
}

// Storefront catalog reads are browser-facing, so the host owns CORS and a coarse per-client
// rate limit. Origins and limits are configuration-driven; a gateway can still layer on top.
// (Behind a reverse proxy, configure forwarded headers so the client IP partitions correctly.)
var allowedOrigins = builder.Configuration
    .GetSection("StorefrontSecurity:Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(StorefrontCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .WithMethods("GET", "POST");
        }
        else if (builder.Environment.IsDevelopment())
        {
            // No origins configured in local dev: stay permissive so smoke/storefront clients work.
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .WithMethods("GET", "POST");
        }

        // Outside Development with no configured origins the policy allows no cross-origin
        // browser access by default, which is the safe choice for an unconfigured deployment.
    });
});

var rateLimiting = builder.Configuration.GetSection("StorefrontSecurity:RateLimiting");
var rateLimitingEnabled = rateLimiting.GetValue("Enabled", true);
var permitLimit = rateLimiting.GetValue("PermitLimit", 300);
var windowSeconds = rateLimiting.GetValue("WindowSeconds", 60);

if (rateLimitingEnabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0
            });
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
            }

            await context.HttpContext.Response.WriteAsync(
                "Too many storefront requests. Try again later.", cancellationToken);
        };
    });
}

var app = builder.Build();

if (rateLimitingEnabled)
{
    app.UseRateLimiter();
}

app.UseCors(StorefrontCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
