namespace Platform.Infrastructure.Integrations;

public sealed record ProductImportJobResult(
    DateTime ImportedAtUtc,
    int TotalCount,
    int CreatedCount,
    int UpdatedCount,
    int FailedCount,
    IReadOnlyList<ProductImportJobResultItem> Items);

public sealed record ProductImportJobResultItem(
    int RowNumber,
    string ProductNumber,
    string Outcome,
    string? Error);
