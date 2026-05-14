namespace Platform.Application.Integrations.Commands;

public sealed record CreateBrandExportJobCommand(
    string? Search,
    string? Status);
