namespace Platform.Backoffice.Models;

public sealed record StatusOptionViewModel(
    Guid Id,
    string Code,
    string Name,
    bool IsBuyable);
