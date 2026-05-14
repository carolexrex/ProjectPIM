namespace Platform.Backoffice.Models;

public sealed record CategoryListItemViewModel(
    Guid Id,
    string Code,
    string? DefaultName,
    string PathLabel,
    string? ParentLabel,
    int Depth,
    string Status,
    DateTime UpdatedAtUtc);
