using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Platform.Api.Security;

public sealed class AdminAccessTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDataProtector _protector;
    private readonly IOptions<AdminIdentityTokenOptions> _tokenOptions;

    public AdminAccessTokenService(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<AdminIdentityTokenOptions> tokenOptions)
    {
        _protector = dataProtectionProvider.CreateProtector("Platform.Api.Security.AdminAccessToken.v1");
        _tokenOptions = tokenOptions;
    }

    public AdminAccessTokenPayload CreateToken(AuthenticatedAdminUser user, out string accessToken)
    {
        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(_tokenOptions.Value.AccessTokenLifetimeMinutes);
        var payload = new AdminAccessTokenPayload(
            user.SubjectId,
            user.PrincipalType,
            user.Username,
            user.DisplayName,
            user.Roles,
            issuedAtUtc,
            expiresAtUtc);

        var protectedBytes = _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions));
        accessToken = Base64UrlTextEncoder.Encode(protectedBytes);

        return payload;
    }

    public ClaimsPrincipal? Validate(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            var protectedBytes = Base64UrlTextEncoder.Decode(accessToken);
            var payloadBytes = _protector.Unprotect(protectedBytes);
            var payload = JsonSerializer.Deserialize<AdminAccessTokenPayload>(payloadBytes, JsonOptions);
            if (payload is null || payload.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return null;
            }

            var claims = new List<Claim>
            {
                new("subject_id", payload.SubjectId),
                new("principal_type", payload.PrincipalType),
                new(ClaimTypes.Name, payload.Username),
                new("display_name", payload.DisplayName),
                new("access_token_expires_at", payload.ExpiresAtUtc.ToString("O"))
            };

            claims.AddRange(payload.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, AdminAccessTokenAuthenticationHandler.SchemeName);
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }
}
