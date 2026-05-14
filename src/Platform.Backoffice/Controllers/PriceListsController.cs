using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Pricing;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.PricingRead)]
[Route("price-lists")]
public sealed class PriceListsController : Controller
{
    private readonly IAdminApiClient _apiClient;

    public PriceListsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? currencyCode, string? status, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListPriceListsAsync(search, currencyCode, status, null, "code", cancellationToken);

        return View(new PriceListListPageViewModel
        {
            Search = search,
            CurrencyCode = currencyCode,
            Status = status,
            PriceLists = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("new")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    public IActionResult New()
    {
        return View(new PriceListCreateViewModel());
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(PriceListCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreatePriceListAsync(
                new CreatePriceListRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    CurrencyCode = form.CurrencyCode,
                    VatIncluded = form.VatIncluded,
                    ValidFromUtc = form.ValidFromUtc,
                    ValidToUtc = form.ValidToUtc
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Price list {created.Code} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
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
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "PriceList")] PriceListUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, priceListForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdatePriceListAsync(
                id,
                new UpdatePriceListRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    CurrencyCode = form.CurrencyCode,
                    VatIncluded = form.VatIncluded,
                    ValidFromUtc = form.ValidFromUtc,
                    ValidToUtc = form.ValidToUtc,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Price list {updated.Code} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "PriceList");
            var invalidPage = await BuildDetailsPageAsync(id, priceListForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var archived = await _apiClient.ArchivePriceListAsync(id, cancellationToken);
            if (archived is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Price list {archived.Code} archived.";
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
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertMarketAssignment(Guid id, [Bind(Prefix = "MarketAssignmentForm")] PriceListMarketAssignmentCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, marketAssignmentForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpsertPriceListMarketAssignmentAsync(
                id,
                new UpsertPriceListMarketAssignmentRequest
                {
                    MarketId = form.MarketId!.Value,
                    Priority = form.Priority,
                    IsBasePriceList = form.IsBasePriceList,
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
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMarketAssignment(Guid id, Guid marketId, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _apiClient.RemovePriceListMarketAssignmentAsync(
                id,
                marketId,
                new RemovePriceListMarketAssignmentRequest
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

    [HttpPost("{id:guid}/entries")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertEntry(Guid id, [Bind(Prefix = "EntryForm")] PriceListEntryCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, entryForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpsertPriceListEntryAsync(
                id,
                new UpsertPriceListEntryRequest
                {
                    EntryId = form.EntryId,
                    TargetType = "Variant",
                    TargetId = form.TargetId!.Value,
                    MinQuantity = form.MinQuantity,
                    Amount = form.Amount,
                    CompareAtAmount = form.CompareAtAmount,
                    ValidFromUtc = form.ValidFromUtc,
                    ValidToUtc = form.ValidToUtc,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Price entry saved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "EntryForm");
            var invalidPage = await BuildDetailsPageAsync(id, entryForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/entries/{entryId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveEntry(Guid id, Guid entryId, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _apiClient.RemovePriceListEntryAsync(
                id,
                entryId,
                new RemovePriceListEntryRequest
                {
                    RowVersion = rowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Price entry removed.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<PriceListDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid priceListId,
        PriceListUpdateViewModel? priceListForm = null,
        PriceListMarketAssignmentCreateViewModel? marketAssignmentForm = null,
        PriceListEntryCreateViewModel? entryForm = null,
        CancellationToken cancellationToken = default)
    {
        var priceList = await _apiClient.GetPriceListAsync(priceListId, cancellationToken);
        if (priceList is null)
        {
            return null;
        }

        priceListForm ??= new PriceListUpdateViewModel
        {
            Id = priceList.Id,
            Code = priceList.Code,
            Name = priceList.Name,
            CurrencyCode = priceList.CurrencyCode,
            VatIncluded = priceList.VatIncluded,
            ValidFromUtc = priceList.ValidFromUtc,
            ValidToUtc = priceList.ValidToUtc,
            RowVersion = priceList.RowVersion,
            Status = priceList.Status,
            CreatedAtUtc = priceList.CreatedAtUtc,
            UpdatedAtUtc = priceList.UpdatedAtUtc
        };

        marketAssignmentForm ??= new PriceListMarketAssignmentCreateViewModel
        {
            PriceListId = priceList.Id,
            RowVersion = priceList.RowVersion
        };
        marketAssignmentForm.MarketOptions = await LoadMarketOptionsAsync(priceList, cancellationToken);

        entryForm ??= new PriceListEntryCreateViewModel
        {
            PriceListId = priceList.Id,
            MinQuantity = 1,
            RowVersion = priceList.RowVersion
        };
        entryForm.VariantOptions = await LoadVariantOptionsAsync(cancellationToken);

        return new PriceListDetailsPageViewModel
        {
            PriceList = priceListForm,
            Markets = priceList.Markets,
            Entries = priceList.Entries,
            MarketAssignmentForm = marketAssignmentForm,
            EntryForm = entryForm
        };
    }

    private async Task<IReadOnlyList<MarketLookupOptionViewModel>> LoadMarketOptionsAsync(PriceListDetailsDto priceList, CancellationToken cancellationToken)
    {
        var assignedIds = priceList.Markets.Select(x => x.MarketId).ToHashSet();
        var markets = await _apiClient.ListMarketLookupsAsync(null, "Active", priceList.CurrencyCode, cancellationToken);

        return markets
            .Where(x => !assignedIds.Contains(x.Id))
            .Select(x => new MarketLookupOptionViewModel(x.Id, x.Code, $"{x.Name} ({x.Code})"))
            .OrderBy(x => x.Label)
            .ToList();
    }

    private async Task<IReadOnlyList<VariantLookupOptionViewModel>> LoadVariantOptionsAsync(CancellationToken cancellationToken)
    {
        var variants = await _apiClient.ListVariantLookupsAsync(null, "Active", null, cancellationToken);

        return variants
            .Select(variant => new VariantLookupOptionViewModel(
                variant.Id,
                variant.Sku,
                string.IsNullOrWhiteSpace(variant.ProductDefaultName)
                    ? $"{variant.Sku} ({variant.ProductNumber})"
                    : $"{variant.Sku} ({variant.ProductNumber} - {variant.ProductDefaultName})"))
            .OrderBy(x => x.Label)
            .ToList();
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
