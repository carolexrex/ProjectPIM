using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Security;

namespace Platform.Backoffice.Controllers;

[Route("auth")]
public sealed class AuthController : Controller
{
    private readonly IAdminAuthenticationClient _authenticationClient;

    public AuthController(IAdminAuthenticationClient authenticationClient)
    {
        _authenticationClient = authenticationClient;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var login = await _authenticationClient.LoginAsync(model.Username, model.Password, cancellationToken);
        if (login is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        if (!login.Roles.Any(role =>
                string.Equals(role, AdminRoles.PlatformAdmin, StringComparison.Ordinal)
                || string.Equals(role, AdminRoles.CatalogManager, StringComparison.Ordinal)
                || string.Equals(role, AdminRoles.CatalogViewer, StringComparison.Ordinal)
                || string.Equals(role, AdminRoles.PricingManager, StringComparison.Ordinal)
                || string.Equals(role, AdminRoles.CustomerService, StringComparison.Ordinal)
                || string.Equals(role, AdminRoles.InventoryManager, StringComparison.Ordinal)))
        {
            ModelState.AddModelError(string.Empty, "This account does not have backoffice access.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, login.Username),
            new("principal_type", login.PrincipalType),
            new("display_name", login.DisplayName),
            new("access_token", login.AccessToken),
            new("access_token_expires_at", login.ExpiresAtUtc.ToString("O"))
        };

        claims.AddRange(login.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = new DateTimeOffset(login.ExpiresAtUtc, TimeSpan.Zero)
            });

        return RedirectToLocal(model.ReturnUrl);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
