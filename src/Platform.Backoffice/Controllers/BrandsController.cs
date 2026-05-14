using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Brands;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("brands")]
public sealed class BrandsController : Controller
{
    private readonly IAdminApiClient _apiClient;

    public BrandsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListBrandsAsync(search, status, "code", cancellationToken);

        return View(new BrandListPageViewModel
        {
            Search = search,
            Status = status,
            Brands = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    public async Task<IActionResult> New(CancellationToken cancellationToken)
    {
        return View(new BrandCreateViewModel
        {
            LogoOptions = await LoadLogoOptionsAsync(cancellationToken)
        });
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(BrandCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.LogoOptions = await LoadLogoOptionsAsync(cancellationToken);
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreateBrandAsync(
                new CreateBrandRequest
                {
                    Code = form.Code,
                    WebsiteUrl = form.WebsiteUrl,
                    LogoMediaAssetId = form.LogoMediaAssetId,
                    SortOrder = form.SortOrder
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Brand {created.Code} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            form.LogoOptions = await LoadLogoOptionsAsync(cancellationToken);
            return View(form);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string? cultureCode, CancellationToken cancellationToken)
    {
        var page = await BuildDetailsPageAsync(id, null, null, cultureCode, cancellationToken);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Brand")] BrandUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, form, null, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateBrandAsync(
                id,
                new UpdateBrandRequest
                {
                    Code = form.Code,
                    WebsiteUrl = form.WebsiteUrl,
                    LogoMediaAssetId = form.LogoMediaAssetId,
                    SortOrder = form.SortOrder,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Brand {updated.Code} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Brand");
            var invalidPage = await BuildDetailsPageAsync(id, form, null, null, cancellationToken);
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
            var archived = await _apiClient.ArchiveBrandAsync(id, cancellationToken);
            if (archived is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Brand {archived.Code} archived.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, null, null, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/translations")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertTranslation(Guid id, [Bind(Prefix = "TranslationForm")] BrandTranslationUpsertViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, null, form, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var translation = await _apiClient.UpsertBrandTranslationAsync(
                id,
                form.CultureCode,
                new UpsertBrandTranslationRequest
                {
                    Name = form.Name,
                    Slug = form.Slug,
                    Description = form.Description
                },
                cancellationToken);

            if (translation is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Brand translation {translation.CultureCode} saved.";
            return RedirectToAction(nameof(Details), new { id, cultureCode = translation.CultureCode });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "TranslationForm");
            var invalidPage = await BuildDetailsPageAsync(id, null, form, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<BrandDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid brandId,
        BrandUpdateViewModel? brandForm,
        BrandTranslationUpsertViewModel? translationForm,
        string? translationCultureCode,
        CancellationToken cancellationToken)
    {
        var brandTask = _apiClient.GetBrandAsync(brandId, cancellationToken);
        var logoOptionsTask = LoadLogoOptionsAsync(cancellationToken);

        await Task.WhenAll(brandTask, logoOptionsTask);

        var brand = await brandTask;
        if (brand is null)
        {
            return null;
        }

        brandForm ??= new BrandUpdateViewModel
        {
            Id = brand.Id,
            Code = brand.Code,
            WebsiteUrl = brand.WebsiteUrl,
            LogoMediaAssetId = brand.LogoMediaAssetId,
            LogoFileName = brand.LogoFileName,
            LogoPublicUrl = brand.LogoPublicUrl,
            SortOrder = brand.SortOrder,
            RowVersion = brand.RowVersion,
            Status = brand.Status,
            CreatedAtUtc = brand.CreatedAtUtc,
            UpdatedAtUtc = brand.UpdatedAtUtc
        };
        brandForm.LogoOptions = await logoOptionsTask;

        translationForm ??= BuildTranslationForm(brand, translationCultureCode);

        return new BrandDetailsPageViewModel
        {
            Brand = brandForm,
            Translations = brand.Translations,
            TranslationForm = translationForm
        };
    }

    private async Task<IReadOnlyList<MediaAssetLookupOptionViewModel>> LoadLogoOptionsAsync(CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListMediaAssetsAsync(null, "Active", null, "filename", cancellationToken);

        return response.Items
            .Select(x => new MediaAssetLookupOptionViewModel(
                x.Id,
                string.IsNullOrWhiteSpace(x.Title) ? x.FileName : $"{x.FileName} - {x.Title}",
                x.PublicUrl))
            .OrderBy(x => x.Label)
            .ToList();
    }

    private static BrandTranslationUpsertViewModel BuildTranslationForm(BrandDetailsDto brand, string? cultureCode)
    {
        var translation = brand.Translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase));

        if (translation is null)
        {
            return new BrandTranslationUpsertViewModel
            {
                BrandId = brand.Id,
                CultureCode = string.IsNullOrWhiteSpace(cultureCode) ? "en-GB" : cultureCode
            };
        }

        return new BrandTranslationUpsertViewModel
        {
            BrandId = brand.Id,
            CultureCode = translation.CultureCode,
            Name = translation.Name,
            Slug = translation.Slug,
            Description = translation.Description
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
