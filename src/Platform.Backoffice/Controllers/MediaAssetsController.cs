using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Media;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("media-assets")]
public sealed class MediaAssetsController : Controller
{
    private readonly IAdminApiClient _apiClient;

    public MediaAssetsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, string? contentType, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListMediaAssetsAsync(search, status, contentType, "filename", cancellationToken);

        return View(new MediaAssetListPageViewModel
        {
            Search = search,
            Status = status,
            ContentType = contentType,
            Assets = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    public IActionResult New()
    {
        return View(new MediaAssetCreateViewModel());
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(MediaAssetCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreateMediaAssetAsync(
                new CreateMediaAssetRequest
                {
                    StorageProvider = form.StorageProvider,
                    StorageKey = form.StorageKey,
                    FileName = form.FileName,
                    ContentType = form.ContentType,
                    FileSize = form.FileSize,
                    Width = form.Width,
                    Height = form.Height,
                    PublicUrl = form.PublicUrl,
                    Title = form.Title,
                    AltText = form.AltText
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Media asset {created.FileName} created.";
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
        var page = await BuildPageAsync(id, null, cancellationToken);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Asset")] MediaAssetUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildPageAsync(id, form, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateMediaAssetAsync(
                id,
                new UpdateMediaAssetRequest
                {
                    FileName = form.FileName,
                    ContentType = form.ContentType,
                    FileSize = form.FileSize,
                    Width = form.Width,
                    Height = form.Height,
                    PublicUrl = form.PublicUrl,
                    Title = form.Title,
                    AltText = form.AltText,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Media asset {updated.FileName} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Asset");
            var invalidPage = await BuildPageAsync(id, form, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            var archived = await _apiClient.ArchiveMediaAssetAsync(
                id,
                new ArchiveMediaAssetRequest
                {
                    RowVersion = rowVersion
                },
                cancellationToken);

            if (archived is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Media asset {archived.FileName} archived.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildPageAsync(id, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<MediaAssetDetailsPageViewModel?> BuildPageAsync(Guid id, MediaAssetUpdateViewModel? form, CancellationToken cancellationToken)
    {
        var asset = await _apiClient.GetMediaAssetAsync(id, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        form ??= new MediaAssetUpdateViewModel
        {
            Id = asset.Id,
            StorageProvider = asset.StorageProvider,
            StorageKey = asset.StorageKey,
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            FileSize = asset.FileSize,
            Width = asset.Width,
            Height = asset.Height,
            PublicUrl = asset.PublicUrl,
            Title = asset.Title,
            AltText = asset.AltText,
            RowVersion = asset.RowVersion,
            Status = asset.Status,
            CreatedAtUtc = asset.CreatedAtUtc,
            UpdatedAtUtc = asset.UpdatedAtUtc
        };

        return new MediaAssetDetailsPageViewModel
        {
            Asset = form,
            Details = asset
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
