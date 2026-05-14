using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Orders;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CustomerRead)]
[Route("carts")]
public sealed class CartsController : Controller
{
    private static readonly IReadOnlyList<string> StatusOptions = ["Active", "Expired", "Converted"];

    private readonly IAdminApiClient _apiClient;

    public CartsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? status, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListCartsAsync(status, null, null, null, null, null, "-createdAtUtc", cancellationToken);
        return View(new CartListPageViewModel
        {
            Status = status,
            Carts = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildDetailsPageAsync(id, null, null, null, cancellationToken);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("{id:guid}/reprice")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reprice(Guid id, [Bind(Prefix = "RepriceForm")] CartActionViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, form, null, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var cart = await _apiClient.RepriceCartAsync(id, new Platform.Contracts.Cart.RepriceCartRequest { RowVersion = form.RowVersion }, cancellationToken);
            if (cart is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Cart repriced.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "RepriceForm");
            var invalidPage = await BuildDetailsPageAsync(id, form, null, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/expire")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Expire(Guid id, [Bind(Prefix = "ExpireForm")] CartActionViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, null, form, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var cart = await _apiClient.ExpireCartAsync(id, new Platform.Contracts.Cart.ExpireCartRequest { RowVersion = form.RowVersion }, cancellationToken);
            if (cart is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Cart expired.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "ExpireForm");
            var invalidPage = await BuildDetailsPageAsync(id, null, form, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/create-order")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder(Guid id, [Bind(Prefix = "CreateOrderForm")] OrderFromCartCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, null, null, form, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var order = await _apiClient.CreateOrderAsync(
                new CreateOrderRequest
                {
                    CartId = id,
                    CartRowVersion = form.RowVersion
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Order {order.OrderNumber} created from cart.";
            return RedirectToAction("Details", "Orders", new { id = order.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "CreateOrderForm");
            var invalidPage = await BuildDetailsPageAsync(id, null, null, form, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<CartDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid cartId,
        CartActionViewModel? repriceForm,
        CartActionViewModel? expireForm,
        OrderFromCartCreateViewModel? createOrderForm,
        CancellationToken cancellationToken)
    {
        var cart = await _apiClient.GetCartAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        repriceForm ??= new CartActionViewModel
        {
            CartId = cart.Id,
            RowVersion = cart.RowVersion
        };

        expireForm ??= new CartActionViewModel
        {
            CartId = cart.Id,
            RowVersion = cart.RowVersion
        };

        createOrderForm ??= new OrderFromCartCreateViewModel
        {
            CartId = cart.Id,
            RowVersion = cart.RowVersion
        };

        return new CartDetailsPageViewModel
        {
            Cart = cart,
            RepriceForm = repriceForm,
            ExpireForm = expireForm,
            CreateOrderForm = createOrderForm
        };
    }

    private void ApplyApiErrors(AdminApiException exception, string? prefix = null)
    {
        if (exception.Errors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return;
        }

        foreach (var error in exception.Errors)
        {
            var key = string.IsNullOrWhiteSpace(prefix) ? error.Key : $"{prefix}.{error.Key}";
            foreach (var message in error.Value)
            {
                ModelState.AddModelError(key, message);
            }
        }
    }
}
