namespace Platform.Application.Security.AdminUsers.Queries;

public sealed record ListAdminUsersQuery(
    string? Search,
    string? Status,
    int Page,
    int PageSize,
    string? Sort);
