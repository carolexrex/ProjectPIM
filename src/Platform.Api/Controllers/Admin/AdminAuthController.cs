using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Contracts.Security;
using Platform.Api.Security;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[AllowAnonymous]
[Route("api/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly AdminConfiguredUserAuthenticationService _authenticationService;
    private readonly AdminAccessTokenService _tokenService;

    public AdminAuthController(
        AdminConfiguredUserAuthenticationService authenticationService,
        AdminAccessTokenService tokenService)
    {
        _authenticationService = authenticationService;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AdminLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _authenticationService.AuthenticateAsync(request.Username, request.Password, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed.",
                Detail = "Invalid username or password.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var payload = _tokenService.CreateToken(user, out var accessToken);

        return Ok(new AdminLoginResponse(
            accessToken,
            payload.ExpiresAtUtc,
            payload.PrincipalType,
            payload.Username,
            payload.DisplayName,
            payload.Roles));
    }
}
