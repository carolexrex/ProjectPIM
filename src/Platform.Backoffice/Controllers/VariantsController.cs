using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Attributes;
using Platform.Contracts.Catalog.Variants;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("variants")]
public sealed class VariantsController : Controller
{
    private static readonly IReadOnlyList<string> MediaTypeOptions = ["Image", "Document"];
    private readonly IAdminApiClient _apiClient;

    public VariantsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildPageAsync(id, cancellationToken: cancellationToken);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        Guid id,
        [Bind(Prefix = "Variant")] VariantUpdateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildPageAsync(id, form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateVariantAsync(
                id,
                new UpdateVariantRequest
                {
                    Sku = form.Sku,
                    Ean = form.Ean,
                    Mpn = form.Mpn,
                    Barcode = form.Barcode,
                    ProductStatusDefinitionId = form.ProductStatusDefinitionId,
                    IsDefaultVariant = form.IsDefaultVariant,
                    Weight = form.Weight,
                    Length = form.Length,
                    Width = form.Width,
                    Height = form.Height,
                    AttributeValues = MapVariantAttributeRequests(form.AttributeEditors),
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Variant {updated.Sku} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Variant");
            var invalidPage = await BuildPageAsync(id, form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/media")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertMedia(
        Guid id,
        [Bind(Prefix = "MediaForm")] VariantMediaCreateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildPageAsync(id, mediaForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpsertVariantMediaAsync(
                id,
                new UpsertVariantMediaRequest
                {
                    MediaAssetId = form.MediaAssetId,
                    Type = form.Type,
                    SortOrder = form.SortOrder,
                    IsPrimary = form.IsPrimary,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Variant media saved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "MediaForm");
            var invalidPage = await BuildPageAsync(id, mediaForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/media/{variantMediaId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMedia(
        Guid id,
        Guid variantMediaId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _apiClient.RemoveVariantMediaAsync(
                id,
                variantMediaId,
                new RemoveVariantMediaRequest
                {
                    RowVersion = rowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Variant media removed.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, null);
            var invalidPage = await BuildPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<VariantDetailsPageViewModel?> BuildPageAsync(
        Guid variantId,
        VariantUpdateViewModel? form = null,
        VariantMediaCreateViewModel? mediaForm = null,
        CancellationToken cancellationToken = default)
    {
        var variantTask = _apiClient.GetVariantAsync(variantId, cancellationToken);
        var inventoryTask = _apiClient.GetVariantInventorySnapshotAsync(variantId, cancellationToken);
        var statusesTask = _apiClient.ListVariantStatusesAsync(cancellationToken);
        var variantAttributesTask = BuildVariantAttributeEditorsAsync([], cancellationToken);
        var mediaOptionsTask = LoadMediaAssetOptionsAsync(cancellationToken);

        await Task.WhenAll(variantTask, inventoryTask, statusesTask, variantAttributesTask, mediaOptionsTask);

        var variant = await variantTask;
        if (variant is null)
        {
            return null;
        }

        var statuses = (await statusesTask)
            .Select(x => new StatusOptionViewModel(x.Id, x.Code, x.Name, x.IsBuyable))
            .ToList();

        form ??= new VariantUpdateViewModel
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Sku = variant.Sku,
            Ean = variant.Ean,
            Mpn = variant.Mpn,
            Barcode = variant.Barcode,
            ProductStatusDefinitionId = variant.ProductStatus.Id,
            IsDefaultVariant = variant.IsDefaultVariant,
            Weight = variant.Weight,
            Length = variant.Length,
            Width = variant.Width,
            Height = variant.Height,
            RowVersion = variant.RowVersion,
            StatusName = variant.ProductStatus.Name,
            CreatedAtUtc = variant.CreatedAtUtc,
            UpdatedAtUtc = variant.UpdatedAtUtc
        };
        form.StatusOptions = statuses;
        form.AttributeEditors = (await BuildVariantAttributeEditorsAsync(variant.AttributeValues, cancellationToken)).ToList();
        mediaForm ??= new VariantMediaCreateViewModel
        {
            VariantId = variant.Id,
            RowVersion = variant.RowVersion,
            Type = MediaTypeOptions[0],
            IsPrimary = variant.Media.Count == 0
        };
        mediaForm.MediaTypeOptions = MediaTypeOptions;
        mediaForm.MediaAssetOptions = await mediaOptionsTask;

        return new VariantDetailsPageViewModel
        {
            Variant = form,
            ProductId = variant.ProductId,
            InventorySnapshot = await inventoryTask,
            Media = variant.Media,
            MediaForm = mediaForm
        };
    }

    private async Task<IReadOnlyList<MediaAssetLookupOptionViewModel>> LoadMediaAssetOptionsAsync(CancellationToken cancellationToken)
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

    private async Task<IReadOnlyList<VariantAttributeEditorViewModel>> BuildVariantAttributeEditorsAsync(
        IReadOnlyList<VariantAttributeValueDto> currentValues,
        CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListProductAttributesAsync(null, "Active", "Variant", null, "sortorder", cancellationToken);
        if (response.Items.Count == 0)
        {
            return [];
        }

        var detailTasks = response.Items
            .Select(item => _apiClient.GetProductAttributeAsync(item.Id, cancellationToken))
            .ToArray();

        await Task.WhenAll(detailTasks);

        var currentValueMap = currentValues.ToDictionary(x => x.ProductAttributeId);

        return detailTasks
            .Select(task => task.Result)
            .Where(x => x is not null)
            .Select(attribute =>
            {
                var currentValue = currentValueMap.GetValueOrDefault(attribute!.Id);
                return new VariantAttributeEditorViewModel
                {
                    ProductAttributeId = attribute.Id,
                    AttributeCode = attribute.Code,
                    AttributeName = attribute.Name,
                    DataType = attribute.DataType,
                    IsRequired = attribute.IsRequired,
                    AttributeOptionId = currentValue?.AttributeOptionId,
                    ValueText = currentValue?.ValueText,
                    Options = attribute.Options
                        .OrderBy(x => x.SortOrder)
                        .Select(x => new VariantAttributeOptionViewModel(x.Id, x.Code, x.Value))
                        .ToList()
                };
            })
            .ToList();
    }

    private static IReadOnlyList<VariantAttributeValueRequest> MapVariantAttributeRequests(IReadOnlyList<VariantAttributeEditorViewModel> editors)
    {
        return editors
            .Where(editor => editor.AttributeOptionId is not null || !string.IsNullOrWhiteSpace(editor.ValueText))
            .Select(editor => new VariantAttributeValueRequest
            {
                ProductAttributeId = editor.ProductAttributeId,
                AttributeOptionId = editor.AttributeOptionId,
                ValueText = string.IsNullOrWhiteSpace(editor.ValueText) ? null : editor.ValueText
            })
            .ToList();
    }

    private void ApplyApiErrors(AdminApiException exception, string? prefix)
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
