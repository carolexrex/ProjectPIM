using Platform.Application.Abstractions.Security;

namespace Platform.Infrastructure.Security;

public sealed class SystemCurrentActorAccessor : ICurrentActorAccessor
{
    private static readonly AuthenticatedActor SystemActor = new(
        "system",
        "System",
        "System",
        [],
        false);

    public AuthenticatedActor GetCurrentActor()
    {
        return SystemActor;
    }
}
