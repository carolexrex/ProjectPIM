namespace Platform.Application.Catalog.Products.Commands;

public sealed record AssignProductStatusCommand(
    Guid ProductId,
    Guid ProductStatusDefinitionId,
    string? Comment);
