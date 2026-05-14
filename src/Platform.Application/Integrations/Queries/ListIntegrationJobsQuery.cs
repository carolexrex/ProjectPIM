namespace Platform.Application.Integrations.Queries;

public sealed record ListIntegrationJobsQuery(
    string? Type,
    string? Status,
    string? RequestedBy,
    int Page,
    int PageSize,
    string? Sort);
