using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Integrations;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("storefront-operations")]
public sealed class StorefrontOperationsController : Controller
{
    private readonly IAdminApiClient _apiClient;

    public StorefrontOperationsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? status,
        string? sort,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return View(await BuildPageAsync(status, sort, page, pageSize, cancellationToken));
    }

    [HttpPost("{id:guid}/reset")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(
        Guid id,
        string rowVersion,
        string? status,
        string? sort,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reset = await _apiClient.ResetStorefrontProjectionRefreshMessageAsync(
                id,
                new ResetStorefrontProjectionRefreshMessageRequest
                {
                    RowVersion = rowVersion
                },
                cancellationToken);

            if (reset is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Storefront refresh message reset.";
            return RedirectToAction(nameof(Index), new { status, sort, page, pageSize });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            return View("Index", await BuildPageAsync(status, sort, page, pageSize, cancellationToken));
        }
    }

    private async Task<StorefrontRefreshMessageListPageViewModel> BuildPageAsync(
        string? status,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "open" : status;
        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "occurredAtUtc" : sort;
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > 500 ? 50 : pageSize;
        var response = await _apiClient.ListStorefrontProjectionRefreshMessagesAsync(
            normalizedStatus,
            normalizedSort,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);

        return new StorefrontRefreshMessageListPageViewModel
        {
            Status = normalizedStatus,
            Sort = normalizedSort,
            Page = response.Page,
            PageSize = response.PageSize,
            Messages = response.Items,
            Total = response.Total
        };
    }

    private void ApplyApiErrors(AdminApiException exception)
    {
        if (exception.Errors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return;
        }

        foreach (var error in exception.Errors)
        {
            foreach (var message in error.Value)
            {
                ModelState.AddModelError(error.Key, message);
            }
        }
    }
}
