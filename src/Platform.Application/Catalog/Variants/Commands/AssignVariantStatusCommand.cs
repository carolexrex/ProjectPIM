namespace Platform.Application.Catalog.Variants.Commands;

public sealed record AssignVariantStatusCommand(
    Guid VariantId,
    Guid ProductStatusDefinitionId,
    string? Comment);
