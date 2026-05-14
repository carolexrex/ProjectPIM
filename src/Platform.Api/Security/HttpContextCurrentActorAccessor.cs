using System.Security.Claims;
using Platform.Application.Abstractions.Security;

namespace Platform.Api.Security;

public sealed class HttpContextCurrentActorAccessor : ICurrentActorAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentActorAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuthenticatedActor GetCurrentActor()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new AuthenticatedActor("system", "System", "System", [], false);
        }

        var identifier = principal.FindFirst("subject_id")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.Identity?.Name
            ?? "unknown";
        var displayName = principal.FindFirst("display_name")?.Value
            ?? principal.Identity?.Name
            ?? identifier;
        var actorType = principal.FindFirst("principal_type")?.Value ?? "AdminUser";
        var roles = principal.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();

        return new AuthenticatedActor(identifier, displayName, actorType, roles, true);
    }
}
