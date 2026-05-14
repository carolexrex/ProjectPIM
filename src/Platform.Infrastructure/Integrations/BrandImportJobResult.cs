namespace Platform.Infrastructure.Integrations;

public sealed record BrandImportJobResult(
    DateTime ImportedAtUtc,
    int TotalCount,
    int CreatedCount,
    int UpdatedCount,
    int FailedCount,
    IReadOnlyList<BrandImportJobResultItem> Items);

public sealed record BrandImportJobResultItem(
    int RowNumber,
    string Code,
    string Outcome,
    string? Error);
