using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Inventory;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.InventoryRead)]
[Route("inventory-locations")]
public sealed class InventoryLocationsController : Controller
{
    private static readonly IReadOnlyList<string> LocationTypeOptions = ["Warehouse", "Store", "Supplier", "External"];
    private static readonly IReadOnlyList<string> AdjustmentTypeOptions = ["Adjustment"];

    private readonly IAdminApiClient _apiClient;

    public InventoryLocationsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListInventoryLocationsAsync(search, status, null, "code", cancellationToken);
        return View(new InventoryLocationListPageViewModel
        {
            Search = search,
            Status = status,
            Locations = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("new")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    public IActionResult New()
    {
        return View(new InventoryLocationCreateViewModel
        {
            TypeOptions = LocationTypeOptions,
            Type = LocationTypeOptions[0]
        });
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(InventoryLocationCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.TypeOptions = LocationTypeOptions;
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreateInventoryLocationAsync(
                new CreateInventoryLocationRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    Type = form.Type,
                    CountryCode = form.CountryCode
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Inventory location {created.Code} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            form.TypeOptions = LocationTypeOptions;
            return View(form);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("{id:guid}")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Location")] InventoryLocationUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, locationForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateInventoryLocationAsync(
                id,
                new UpdateInventoryLocationRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    Type = form.Type,
                    CountryCode = form.CountryCode,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Inventory location {updated.Code} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Location");
            var invalidPage = await BuildDetailsPageAsync(id, locationForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var archived = await _apiClient.ArchiveInventoryLocationAsync(id, cancellationToken);
            if (archived is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Inventory location {archived.Code} archived.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/markets")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertMarketAssignment(
        Guid id,
        [Bind(Prefix = "MarketAssignmentForm")] InventoryLocationMarketAssignmentCreateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, marketAssignmentForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpsertInventoryLocationMarketAssignmentAsync(
                id,
                new UpsertInventoryLocationMarketAssignmentRequest
                {
                    MarketId = form.MarketId!.Value,
                    Priority = form.Priority,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Market assignment saved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "MarketAssignmentForm");
            var invalidPage = await BuildDetailsPageAsync(id, marketAssignmentForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/markets/{marketId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMarketAssignment(Guid id, Guid marketId, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _apiClient.RemoveInventoryLocationMarketAssignmentAsync(
                id,
                marketId,
                new RemoveInventoryLocationMarketAssignmentRequest
                {
                    RowVersion = rowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Market assignment removed.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/balances")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertBalance(
        Guid id,
        [Bind(Prefix = "BalanceForm")] InventoryBalanceUpsertViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, balanceForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            await _apiClient.UpsertInventoryBalanceAsync(
                new UpsertInventoryBalanceRequest
                {
                    InventoryLocationId = id,
                    VariantId = form.VariantId!.Value,
                    OnHandQuantity = form.OnHandQuantity,
                    ReservedQuantity = form.ReservedQuantity,
                    IncomingQuantity = form.IncomingQuantity,
                    Backorderable = form.Backorderable,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            TempData["FlashMessage"] = "Inventory balance saved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "BalanceForm");
            var invalidPage = await BuildDetailsPageAsync(id, balanceForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/adjustments")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustInventory(
        Guid id,
        [Bind(Prefix = "AdjustmentForm")] InventoryAdjustmentCreateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, adjustmentForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            await _apiClient.AdjustInventoryAsync(
                new AdjustInventoryRequest
                {
                    InventoryLocationId = id,
                    VariantId = form.VariantId!.Value,
                    Type = form.Type,
                    QuantityDelta = form.QuantityDelta,
                    ReferenceType = form.ReferenceType,
                    ReferenceId = form.ReferenceId
                },
                cancellationToken);

            TempData["FlashMessage"] = "Inventory adjusted.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "AdjustmentForm");
            var invalidPage = await BuildDetailsPageAsync(id, adjustmentForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<InventoryLocationDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid inventoryLocationId,
        InventoryLocationUpdateViewModel? locationForm = null,
        InventoryLocationMarketAssignmentCreateViewModel? marketAssignmentForm = null,
        InventoryBalanceUpsertViewModel? balanceForm = null,
        InventoryAdjustmentCreateViewModel? adjustmentForm = null,
        CancellationToken cancellationToken = default)
    {
        var location = await _apiClient.GetInventoryLocationAsync(inventoryLocationId, cancellationToken);
        if (location is null)
        {
            return null;
        }

        var assignedMarketIds = location.Markets.Select(x => x.MarketId).ToHashSet();
        var availableMarkets = await _apiClient.ListMarketLookupsAsync(null, "Active", null, cancellationToken);
        var marketOptions = availableMarkets
            .Where(x => !assignedMarketIds.Contains(x.Id))
            .Select(x => new MarketLookupOptionViewModel(x.Id, x.Code, $"{x.Name} ({x.Code})"))
            .OrderBy(x => x.Label)
            .ToList();

        var variants = await _apiClient.ListVariantLookupsAsync(null, "Active", null, cancellationToken);
        var variantOptions = variants
            .Select(x => new VariantLookupOptionViewModel(x.Id, x.Sku, x.Sku))
            .OrderBy(x => x.Label)
            .ToList();

        locationForm ??= new InventoryLocationUpdateViewModel
        {
            Id = location.Id,
            Code = location.Code,
            Name = location.Name,
            Type = location.Type,
            CountryCode = location.CountryCode,
            RowVersion = location.RowVersion,
            Status = location.Status,
            CreatedAtUtc = location.CreatedAtUtc,
            UpdatedAtUtc = location.UpdatedAtUtc
        };
        locationForm.TypeOptions = LocationTypeOptions;

        marketAssignmentForm ??= new InventoryLocationMarketAssignmentCreateViewModel
        {
            InventoryLocationId = location.Id,
            RowVersion = location.RowVersion
        };
        marketAssignmentForm.MarketOptions = marketOptions;

        balanceForm ??= new InventoryBalanceUpsertViewModel
        {
            InventoryLocationId = location.Id
        };
        balanceForm.VariantOptions = variantOptions;

        adjustmentForm ??= new InventoryAdjustmentCreateViewModel
        {
            InventoryLocationId = location.Id,
            Type = AdjustmentTypeOptions[0]
        };
        adjustmentForm.TypeOptions = AdjustmentTypeOptions;
        adjustmentForm.VariantOptions = variantOptions;

        return new InventoryLocationDetailsPageViewModel
        {
            Location = locationForm,
            Markets = location.Markets,
            Balances = location.Balances,
            RecentTransactions = location.RecentTransactions,
            MarketAssignmentForm = marketAssignmentForm,
            BalanceForm = balanceForm,
            AdjustmentForm = adjustmentForm
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
