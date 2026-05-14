namespace Platform.Application.Abstractions.Security;

public sealed record AuthenticatedActor(
    string Identifier,
    string DisplayName,
    string ActorType,
    IReadOnlyList<string> Roles,
    bool IsAuthenticated);
