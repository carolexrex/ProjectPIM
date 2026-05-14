using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Orders;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CustomerRead)]
[Route("orders")]
public sealed class OrdersController : Controller
{
    private static readonly IReadOnlyList<string> StatusOptions = ["Placed", "Processing", "Completed", "Cancelled"];
    private static readonly IReadOnlyList<string> PaymentTypeOptions = ["Authorize", "Capture", "Refund"];
    private static readonly IReadOnlyList<string> PaymentStatusOptions = ["Pending", "Authorized", "Paid", "Failed", "Refunded"];

    private readonly IAdminApiClient _apiClient;

    public OrdersController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, string? paymentStatus, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListOrdersAsync(status, paymentStatus, null, null, null, null, null, null, search, "-placedAtUtc", cancellationToken);
        return View(new OrderListPageViewModel
        {
            Search = search,
            Status = status,
            PaymentStatus = paymentStatus,
            Orders = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(Guid id, [Bind(Prefix = "StatusForm")] OrderStatusChangeViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, form, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var history = await _apiClient.ChangeOrderStatusAsync(
                id,
                new ChangeOrderStatusRequest
                {
                    ToStatus = form.ToStatus,
                    Comment = form.Comment,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (history is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Order moved to {history.ToStatus}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "StatusForm");
            var invalidPage = await BuildDetailsPageAsync(id, form, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/payment-transactions")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPaymentTransaction(
        Guid id,
        [Bind(Prefix = "PaymentForm")] OrderPaymentTransactionCreateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, null, form, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var transaction = await _apiClient.AddOrderPaymentTransactionAsync(
                id,
                new AddPaymentTransactionRequest
                {
                    Provider = form.Provider,
                    ProviderReference = form.ProviderReference,
                    Type = form.Type,
                    Status = form.Status,
                    Amount = form.Amount,
                    CurrencyCode = form.CurrencyCode,
                    RequestedAtUtc = form.RequestedAtUtc,
                    CompletedAtUtc = form.CompletedAtUtc,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (transaction is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Payment transaction {transaction.ProviderReference} recorded.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "PaymentForm");
            var invalidPage = await BuildDetailsPageAsync(id, null, form, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<OrderDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid orderId,
        OrderStatusChangeViewModel? statusForm = null,
        OrderPaymentTransactionCreateViewModel? paymentForm = null,
        CancellationToken cancellationToken = default)
    {
        var order = await _apiClient.GetOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        statusForm ??= new OrderStatusChangeViewModel
        {
            OrderId = order.Id,
            RowVersion = order.RowVersion,
            ToStatus = order.Status == "Placed" ? "Processing" : order.Status
        };
        statusForm.StatusOptions = StatusOptions;

        paymentForm ??= new OrderPaymentTransactionCreateViewModel
        {
            OrderId = order.Id,
            Amount = order.GrandTotal,
            CurrencyCode = order.CurrencyCode,
            RowVersion = order.RowVersion
        };
        paymentForm.TypeOptions = PaymentTypeOptions;
        paymentForm.StatusOptions = PaymentStatusOptions;

        return new OrderDetailsPageViewModel
        {
            Order = order,
            StatusForm = statusForm,
            PaymentForm = paymentForm
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
