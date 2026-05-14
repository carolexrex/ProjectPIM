namespace Platform.Infrastructure.Integrations;

public sealed record BrandExportJobResult(
    DateTime ExportedAtUtc,
    int TotalCount,
    IReadOnlyList<BrandExportJobResultItem> Items);

public sealed record BrandExportJobResultItem(
    Guid Id,
    string Code,
    string? DefaultName,
    string Status,
    int SortOrder,
    DateTime UpdatedAtUtc);
