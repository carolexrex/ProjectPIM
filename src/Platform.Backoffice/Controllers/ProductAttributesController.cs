using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Attributes;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("product-attributes")]
public sealed class ProductAttributesController : Controller
{
    private static readonly IReadOnlyList<string> ScopeOptions = ["Variant", "Product"];
    private static readonly IReadOnlyList<string> DataTypeOptions = ["Select", "Text", "Number", "Boolean"];

    private readonly IAdminApiClient _apiClient;

    public ProductAttributesController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, string? scope, string? dataType, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListProductAttributesAsync(search, status, scope, dataType, "code", cancellationToken);

        return View(new ProductAttributeListPageViewModel
        {
            Search = search,
            Status = status,
            Scope = scope,
            DataType = dataType,
            Attributes = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    public IActionResult New()
    {
        return View(new ProductAttributeCreateViewModel
        {
            ScopeOptions = ScopeOptions,
            DataTypeOptions = DataTypeOptions,
            OptionsText = "BLACK=Black\nRED=Red"
        });
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(ProductAttributeCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!TryParseOptions(form.OptionsText, nameof(ProductAttributeCreateViewModel.OptionsText), out var options))
        {
            form.ScopeOptions = ScopeOptions;
            form.DataTypeOptions = DataTypeOptions;
            return View(form);
        }

        if (!ModelState.IsValid)
        {
            form.ScopeOptions = ScopeOptions;
            form.DataTypeOptions = DataTypeOptions;
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreateProductAttributeAsync(
                new CreateProductAttributeRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    Scope = form.Scope,
                    DataType = form.DataType,
                    IsVariantDefining = form.IsVariantDefining,
                    IsFilterable = form.IsFilterable,
                    IsRequired = form.IsRequired,
                    SortOrder = form.SortOrder,
                    Options = options
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Attribute {created.Code} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            form.ScopeOptions = ScopeOptions;
            form.DataTypeOptions = DataTypeOptions;
            return View(form);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildDetailsPageAsync(id, null, cancellationToken);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Attribute")] ProductAttributeUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!TryParseOptions(form.OptionsText, "Attribute.OptionsText", out var options))
        {
            form.ScopeOptions = ScopeOptions;
            form.DataTypeOptions = DataTypeOptions;
            var invalidPage = await BuildDetailsPageAsync(id, form, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        if (!ModelState.IsValid)
        {
            form.ScopeOptions = ScopeOptions;
            form.DataTypeOptions = DataTypeOptions;
            var invalidPage = await BuildDetailsPageAsync(id, form, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateProductAttributeAsync(
                id,
                new UpdateProductAttributeRequest
                {
                    Code = form.Code,
                    Name = form.Name,
                    Scope = form.Scope,
                    DataType = form.DataType,
                    IsVariantDefining = form.IsVariantDefining,
                    IsFilterable = form.IsFilterable,
                    IsRequired = form.IsRequired,
                    SortOrder = form.SortOrder,
                    RowVersion = form.RowVersion,
                    Options = options
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Attribute {updated.Code} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Attribute");
            form.ScopeOptions = ScopeOptions;
            form.DataTypeOptions = DataTypeOptions;
            var invalidPage = await BuildDetailsPageAsync(id, form, cancellationToken);
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
            var archived = await _apiClient.ArchiveProductAttributeAsync(id, cancellationToken);
            if (archived is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Attribute {archived.Code} archived.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<ProductAttributeDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid attributeId,
        ProductAttributeUpdateViewModel? form,
        CancellationToken cancellationToken)
    {
        var attribute = await _apiClient.GetProductAttributeAsync(attributeId, cancellationToken);
        if (attribute is null)
        {
            return null;
        }

        form ??= new ProductAttributeUpdateViewModel
        {
            Id = attribute.Id,
            Code = attribute.Code,
            Name = attribute.Name,
            Scope = attribute.Scope,
            DataType = attribute.DataType,
            IsVariantDefining = attribute.IsVariantDefining,
            IsFilterable = attribute.IsFilterable,
            IsRequired = attribute.IsRequired,
            SortOrder = attribute.SortOrder,
            RowVersion = attribute.RowVersion,
            Status = attribute.Status,
            CreatedAtUtc = attribute.CreatedAtUtc,
            UpdatedAtUtc = attribute.UpdatedAtUtc,
            OptionsText = string.Join(Environment.NewLine, attribute.Options.Select(x => $"{x.Code}={x.Value}"))
        };
        form.ScopeOptions = ScopeOptions;
        form.DataTypeOptions = DataTypeOptions;

        return new ProductAttributeDetailsPageViewModel
        {
            Attribute = form,
            Options = attribute.Options
        };
    }

    private bool TryParseOptions(string optionsText, string modelKey, out IReadOnlyList<AttributeOptionRequest> options)
    {
        var parsed = new List<AttributeOptionRequest>();
        var lines = optionsText
            .Split(["\r\n", "\n"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var separator = line.IndexOf('=');
            if (separator <= 0 || separator == line.Length - 1)
            {
                ModelState.AddModelError(modelKey, $"Option line {index + 1} must use CODE=Value format.");
                options = [];
                return false;
            }

            var code = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            if (!seenCodes.Add(code))
            {
                ModelState.AddModelError(modelKey, $"Duplicate option code '{code}'.");
                options = [];
                return false;
            }

            parsed.Add(new AttributeOptionRequest
            {
                Code = code,
                Value = value,
                SortOrder = (index + 1) * 10
            });
        }

        options = parsed;
        return true;
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
