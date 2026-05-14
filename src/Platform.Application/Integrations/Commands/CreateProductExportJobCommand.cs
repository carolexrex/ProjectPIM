namespace Platform.Application.Integrations.Commands;

public sealed record CreateProductExportJobCommand(
    string? Search,
    string? Status,
    string? ProductStatusCode,
    Guid? BrandId,
    bool? HasVariants);
