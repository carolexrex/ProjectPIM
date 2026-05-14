namespace Platform.Application.Abstractions.Security;

public interface ICurrentActorAccessor
{
    AuthenticatedActor GetCurrentActor();
}
