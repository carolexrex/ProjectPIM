using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Markets;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("markets")]
public sealed class MarketsController : Controller
{
    private static readonly IReadOnlyList<string> VatModeOptions = ["Gross", "Net"];
    private static readonly IReadOnlyList<string> ProductAssignmentStatusOptions = ["Active", "Inactive"];

    private readonly IAdminApiClient _apiClient;

    public MarketsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListMarketsAsync(search, status, "code", cancellationToken);

        return View(new MarketListPageViewModel
        {
            Search = search,
            Status = status,
            Markets = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    public IActionResult New()
    {
        return View(new MarketCreateViewModel
        {
            VatModeOptions = VatModeOptions
        });
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(MarketCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.VatModeOptions = VatModeOptions;
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreateMarketAsync(
                new CreateMarketRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    DefaultCurrency = form.DefaultCurrency,
                    DefaultCulture = form.DefaultCulture,
                    VatMode = form.VatMode
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Market {created.Code} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            form.VatModeOptions = VatModeOptions;
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
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Market")] MarketUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, marketForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateMarketAsync(
                id,
                new UpdateMarketRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    DefaultCurrency = form.DefaultCurrency,
                    DefaultCulture = form.DefaultCulture,
                    VatMode = form.VatMode,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Market {updated.Code} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Market");
            var invalidPage = await BuildDetailsPageAsync(id, marketForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var archived = await _apiClient.ArchiveMarketAsync(id, cancellationToken);
            if (archived is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Market {archived.Code} archived.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/currencies")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignCurrencies(
        Guid id,
        [Bind(Prefix = "CurrenciesForm")] MarketCurrenciesFormViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, currenciesForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.AssignMarketCurrenciesAsync(
                id,
                new AssignMarketCurrenciesRequest
                {
                    DefaultCurrency = form.DefaultCurrency,
                    Currencies = ParseCsv(form.CurrencyCodesCsv),
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Currencies updated for market {updated.Code}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "CurrenciesForm");
            var invalidPage = await BuildDetailsPageAsync(id, currenciesForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/cultures")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignCultures(
        Guid id,
        [Bind(Prefix = "CulturesForm")] MarketCulturesFormViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, culturesForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.AssignMarketCulturesAsync(
                id,
                new AssignMarketCulturesRequest
                {
                    DefaultCulture = form.DefaultCulture,
                    Cultures = ParseCsv(form.CultureCodesCsv),
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Cultures updated for market {updated.Code}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "CulturesForm");
            var invalidPage = await BuildDetailsPageAsync(id, culturesForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/products")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertProductAssignment(
        Guid id,
        [Bind(Prefix = "ProductAssignmentForm")] MarketProductAssignmentCreateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, productAssignmentForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpsertMarketProductAssignmentAsync(
                id,
                form.ProductId!.Value,
                new UpsertMarketProductAssignmentRequest
                {
                    Status = form.Status,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Market product assignment saved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "ProductAssignmentForm");
            var invalidPage = await BuildDetailsPageAsync(id, productAssignmentForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/products/{productId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProductAssignment(Guid id, Guid productId, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _apiClient.RemoveMarketProductAssignmentAsync(
                id,
                productId,
                new RemoveMarketProductAssignmentRequest
                {
                    RowVersion = rowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Market product assignment removed.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<MarketDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid marketId,
        MarketUpdateViewModel? marketForm = null,
        MarketCurrenciesFormViewModel? currenciesForm = null,
        MarketCulturesFormViewModel? culturesForm = null,
        MarketProductAssignmentCreateViewModel? productAssignmentForm = null,
        CancellationToken cancellationToken = default)
    {
        var market = await _apiClient.GetMarketAsync(marketId, cancellationToken);
        if (market is null)
        {
            return null;
        }

        marketForm ??= new MarketUpdateViewModel
        {
            Id = market.Id,
            Code = market.Code,
            Name = market.Name,
            DefaultCurrency = market.DefaultCurrency,
            DefaultCulture = market.DefaultCulture,
            VatMode = market.VatMode,
            RowVersion = market.RowVersion,
            Status = market.Status,
            CreatedAtUtc = market.CreatedAtUtc,
            UpdatedAtUtc = market.UpdatedAtUtc
        };
        marketForm.VatModeOptions = VatModeOptions;

        currenciesForm ??= new MarketCurrenciesFormViewModel
        {
            MarketId = market.Id,
            DefaultCurrency = market.DefaultCurrency,
            CurrencyCodesCsv = string.Join(", ", market.Currencies.Select(x => x.CurrencyCode)),
            RowVersion = market.RowVersion
        };

        culturesForm ??= new MarketCulturesFormViewModel
        {
            MarketId = market.Id,
            DefaultCulture = market.DefaultCulture,
            CultureCodesCsv = string.Join(", ", market.Cultures.Select(x => x.CultureCode)),
            RowVersion = market.RowVersion
        };

        productAssignmentForm ??= new MarketProductAssignmentCreateViewModel
        {
            MarketId = market.Id,
            Status = ProductAssignmentStatusOptions[0],
            RowVersion = market.RowVersion
        };
        productAssignmentForm.StatusOptions = ProductAssignmentStatusOptions;
        productAssignmentForm.ProductOptions = await LoadProductOptionsAsync(
            market.ProductAssignments.Select(x => x.ProductId),
            cancellationToken);

        return new MarketDetailsPageViewModel
        {
            Market = marketForm,
            Currencies = market.Currencies,
            Cultures = market.Cultures,
            ProductAssignments = market.ProductAssignments,
            CurrenciesForm = currenciesForm,
            CulturesForm = culturesForm,
            ProductAssignmentForm = productAssignmentForm
        };
    }

    private async Task<IReadOnlyList<ProductLookupOptionViewModel>> LoadProductOptionsAsync(
        IEnumerable<Guid> assignedProductIds,
        CancellationToken cancellationToken)
    {
        var assigned = assignedProductIds.ToHashSet();
        var products = await _apiClient.ListProductLookupsAsync(null, "Active", null, null, cancellationToken);

        return products
            .Where(x => !assigned.Contains(x.Id))
            .Select(x => new ProductLookupOptionViewModel(
                x.Id,
                x.ProductNumber,
                string.IsNullOrWhiteSpace(x.DefaultName) ? x.ProductNumber : $"{x.ProductNumber} - {x.DefaultName}"))
            .OrderBy(x => x.Label)
            .ToList();
    }

    private static IReadOnlyList<string> ParseCsv(string csv)
    {
        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
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
