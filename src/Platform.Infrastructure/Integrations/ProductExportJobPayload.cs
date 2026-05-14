namespace Platform.Infrastructure.Integrations;

public sealed record ProductExportJobPayload(
    string? Search,
    string? Status,
    string? ProductStatusCode,
    Guid? BrandId,
    bool? HasVariants);
