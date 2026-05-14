using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;

namespace Platform.Backoffice.Controllers;

[Authorize]
public sealed class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        if (User.IsInRole(AdminRoles.PlatformAdmin)
            || User.IsInRole(AdminRoles.CatalogManager)
            || User.IsInRole(AdminRoles.CatalogViewer))
        {
            return RedirectToAction("Index", "Products");
        }

        if (User.IsInRole(AdminRoles.PricingManager))
        {
            return RedirectToAction("Index", "PriceLists");
        }

        if (User.IsInRole(AdminRoles.InventoryManager))
        {
            return RedirectToAction("Index", "InventoryLocations");
        }

        if (User.IsInRole(AdminRoles.CustomerService))
        {
            return RedirectToAction("Index", "Customers");
        }

        return Forbid();
    }
}
