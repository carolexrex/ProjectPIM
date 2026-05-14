using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Categories;
using Platform.Contracts.Catalog.Attributes;
using Platform.Contracts.Catalog.Brands;
using Platform.Contracts.Catalog.Products;
using Platform.Contracts.Catalog.Variants;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("products")]
public sealed class ProductsController : Controller
{
    private static readonly IReadOnlyList<string> RelationTypeOptions = ["RelatedProduct", "Accessory", "BundleComponent"];
    private static readonly IReadOnlyList<string> MediaTypeOptions = ["Image", "Document"];
    private readonly IAdminApiClient _apiClient;

    public ProductsController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? productStatusCode,
        bool? hasVariants,
        string? sort,
        CancellationToken cancellationToken)
    {
        var productsTask = _apiClient.ListProductsAsync(search, status, productStatusCode, hasVariants, sort, cancellationToken);
        var statusesTask = LoadStatusOptionsAsync("product", cancellationToken);

        await Task.WhenAll(productsTask, statusesTask);

        var response = await productsTask;

        return View(new ProductListPageViewModel
        {
            Search = search,
            Status = status,
            ProductStatusCode = productStatusCode,
            HasVariants = hasVariants,
            Sort = string.IsNullOrWhiteSpace(sort) ? "productnumber" : sort,
            ProductStatuses = await statusesTask,
            Products = response.Items,
            Total = response.Total
        });
    }

    [HttpGet("new")]
    public async Task<IActionResult> New(CancellationToken cancellationToken)
    {
        var productAttributeDefinitions = await LoadAttributeDefinitionsAsync("Product", cancellationToken);
        var viewModel = new ProductCreateViewModel
        {
            StatusOptions = await LoadStatusOptionsAsync("product", cancellationToken),
            BrandOptions = await LoadBrandOptionsAsync(cancellationToken, null),
            CategoryOptions = await LoadCategoryOptionsAsync(cancellationToken),
            AttributeEditors = BuildProductAttributeEditors(productAttributeDefinitions, []).ToList()
        };

        if (viewModel.StatusOptions.Count > 0)
        {
            viewModel.ProductStatusDefinitionId = viewModel.StatusOptions[0].Id;
        }

        return View(viewModel);
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(ProductCreateViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            viewModel.StatusOptions = await LoadStatusOptionsAsync("product", cancellationToken);
            viewModel.BrandOptions = await LoadBrandOptionsAsync(cancellationToken, viewModel.BrandId);
            viewModel.CategoryOptions = await LoadCategoryOptionsAsync(cancellationToken);
            if (viewModel.AttributeEditors.Count == 0)
            {
                var productAttributeDefinitions = await LoadAttributeDefinitionsAsync("Product", cancellationToken);
                viewModel.AttributeEditors = BuildProductAttributeEditors(productAttributeDefinitions, []).ToList();
            }
            return View(viewModel);
        }

        try
        {
            var created = await _apiClient.CreateProductAsync(
                new CreateProductRequest
                {
                    ProductType = viewModel.ProductType,
                    ProductNumber = viewModel.ProductNumber,
                    Slug = viewModel.Slug,
                    BrandId = viewModel.BrandId,
                    ProductStatusDefinitionId = viewModel.ProductStatusDefinitionId,
                    TaxCategoryCode = viewModel.TaxCategoryCode,
                    UnitOfMeasure = viewModel.UnitOfMeasure,
                    HasVariants = viewModel.HasVariants,
                    CategoryIds = viewModel.SelectedCategoryIds,
                    AttributeValues = MapProductAttributeRequests(viewModel.AttributeEditors),
                    Weight = viewModel.Weight,
                    Length = viewModel.Length,
                    Width = viewModel.Width,
                    Height = viewModel.Height
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Product {created.ProductNumber} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            viewModel.StatusOptions = await LoadStatusOptionsAsync("product", cancellationToken);
            viewModel.BrandOptions = await LoadBrandOptionsAsync(cancellationToken, viewModel.BrandId);
            viewModel.CategoryOptions = await LoadCategoryOptionsAsync(cancellationToken);
            if (viewModel.AttributeEditors.Count == 0)
            {
                var productAttributeDefinitions = await LoadAttributeDefinitionsAsync("Product", cancellationToken);
                viewModel.AttributeEditors = BuildProductAttributeEditors(productAttributeDefinitions, []).ToList();
            }
            return View(viewModel);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string? cultureCode, CancellationToken cancellationToken)
    {
        var page = await BuildDetailsPageAsync(id, translationCultureCode: cultureCode, cancellationToken: cancellationToken);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        Guid id,
        [Bind(Prefix = "Product")] ProductUpdateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, productForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateProductAsync(
                id,
                new UpdateProductRequest
                {
                    ProductType = form.ProductType,
                    Slug = form.Slug,
                    BrandId = form.BrandId,
                    ProductStatusDefinitionId = form.ProductStatusDefinitionId,
                    TaxCategoryCode = form.TaxCategoryCode,
                    UnitOfMeasure = form.UnitOfMeasure,
                    CategoryIds = form.SelectedCategoryIds,
                    AttributeValues = MapProductAttributeRequests(form.AttributeEditors),
                    Weight = form.Weight,
                    Length = form.Length,
                    Width = form.Width,
                    Height = form.Height,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Product {updated.ProductNumber} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Product");
            var invalidPage = await BuildDetailsPageAsync(id, productForm: form, cancellationToken: cancellationToken);
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
            var archived = await _apiClient.ArchiveProductAsync(id, cancellationToken);
            if (archived is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Product {archived.ProductNumber} archived.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/variants")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVariant(
        Guid id,
        [Bind(Prefix = "NewVariant")] VariantCreateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, variantForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var created = await _apiClient.CreateVariantAsync(
                id,
                new CreateVariantRequest
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
                    AttributeValues = MapVariantAttributeRequests(form.AttributeEditors)
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Variant {created.Sku} created.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "NewVariant");
            var invalidPage = await BuildDetailsPageAsync(id, variantForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/translations")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertTranslation(
        Guid id,
        [Bind(Prefix = "TranslationForm")] ProductTranslationUpsertViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, null, null, form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpsertProductTranslationAsync(
                id,
                form.CultureCode,
                new UpsertProductTranslationRequest
                {
                    Name = form.Name,
                    ShortDescription = form.ShortDescription,
                    LongDescription = form.LongDescription,
                    SeoTitle = form.SeoTitle,
                    SeoDescription = form.SeoDescription
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Translation {updated.CultureCode} saved.";
            return RedirectToAction(nameof(Details), new { id, cultureCode = updated.CultureCode });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "TranslationForm");
            var invalidPage = await BuildDetailsPageAsync(id, null, null, form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/media")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertMedia(
        Guid id,
        [Bind(Prefix = "MediaForm")] ProductMediaCreateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, mediaForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpsertProductMediaAsync(
                id,
                new UpsertProductMediaRequest
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

            TempData["FlashMessage"] = "Product media saved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "MediaForm");
            var invalidPage = await BuildDetailsPageAsync(id, mediaForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/media/{productMediaId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMedia(
        Guid id,
        Guid productMediaId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _apiClient.RemoveProductMediaAsync(
                id,
                productMediaId,
                new RemoveProductMediaRequest
                {
                    RowVersion = rowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Product media removed.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/relations")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertRelation(
        Guid id,
        [Bind(Prefix = "RelationForm")] ProductRelationCreateViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, relationForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpsertProductRelationAsync(
                id,
                new UpsertProductRelationRequest
                {
                    TargetProductId = form.TargetProductId,
                    RelationType = form.RelationType,
                    Quantity = form.Quantity,
                    SortOrder = form.SortOrder,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Product relation saved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "RelationForm");
            var invalidPage = await BuildDetailsPageAsync(id, relationForm: form, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    [HttpPost("{id:guid}/relations/{relationId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRelation(
        Guid id,
        Guid relationId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _apiClient.RemoveProductRelationAsync(
                id,
                relationId,
                new RemoveProductRelationRequest
                {
                    RowVersion = rowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = "Product relation removed.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            var invalidPage = await BuildDetailsPageAsync(id, cancellationToken: cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<ProductDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid productId,
        ProductUpdateViewModel? productForm = null,
        VariantCreateViewModel? variantForm = null,
        ProductTranslationUpsertViewModel? translationForm = null,
        ProductMediaCreateViewModel? mediaForm = null,
        ProductRelationCreateViewModel? relationForm = null,
        string? translationCultureCode = null,
        CancellationToken cancellationToken = default)
    {
        var productTask = _apiClient.GetProductAsync(productId, cancellationToken);
        var variantsTask = _apiClient.ListVariantsAsync(productId, cancellationToken);
        var productStatusesTask = LoadStatusOptionsAsync("product", cancellationToken);
        var variantStatusesTask = LoadStatusOptionsAsync("variant", cancellationToken);
        var categoryOptionsTask = LoadCategoryOptionsAsync(cancellationToken);
        var productOptionsTask = LoadProductOptionsAsync(productId, cancellationToken);
        var mediaOptionsTask = LoadMediaAssetOptionsAsync(cancellationToken);
        var productAttributeDefinitionsTask = LoadAttributeDefinitionsAsync("Product", cancellationToken);
        var variantAttributeDefinitionsTask = LoadAttributeDefinitionsAsync("Variant", cancellationToken);

        await Task.WhenAll(productTask, variantsTask, productStatusesTask, variantStatusesTask, categoryOptionsTask, productOptionsTask, mediaOptionsTask, productAttributeDefinitionsTask, variantAttributeDefinitionsTask);

        var product = await productTask;
        if (product is null)
        {
            return null;
        }

        var productStatuses = await productStatusesTask;
        var variantStatuses = await variantStatusesTask;
        var productAttributeDefinitions = await productAttributeDefinitionsTask;
        var variantAttributeDefinitions = await variantAttributeDefinitionsTask;

        productForm ??= new ProductUpdateViewModel
        {
            Id = product.Id,
            ProductNumber = product.ProductNumber,
            ProductType = product.ProductType,
            Slug = product.Slug,
            BrandId = product.BrandId,
            ProductStatusDefinitionId = product.ProductStatus.Id,
            TaxCategoryCode = product.TaxCategoryCode,
            UnitOfMeasure = product.UnitOfMeasure,
            HasVariants = product.HasVariants,
            SelectedCategoryIds = product.Categories.Select(x => x.CategoryId).ToList(),
            AttributeEditors = BuildProductAttributeEditors(productAttributeDefinitions, product.AttributeValues).ToList(),
            Weight = product.Weight,
            Length = product.Length,
            Width = product.Width,
            Height = product.Height,
            RowVersion = product.RowVersion,
            Status = product.Status,
            StatusName = product.ProductStatus.Name,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc
        };
        productForm.StatusOptions = productStatuses;
        productForm.BrandOptions = await LoadBrandOptionsAsync(cancellationToken, product.BrandId);
        productForm.CategoryOptions = await categoryOptionsTask;

        variantForm ??= new VariantCreateViewModel
        {
            ProductId = product.Id
        };
        variantForm.StatusOptions = variantStatuses;
        if (variantForm.AttributeEditors.Count == 0)
        {
            variantForm.AttributeEditors = BuildVariantAttributeEditors(variantAttributeDefinitions, []).ToList();
        }
        if (variantForm.ProductStatusDefinitionId == Guid.Empty && variantStatuses.Count > 0)
        {
            variantForm.ProductStatusDefinitionId = variantStatuses[0].Id;
        }

        translationForm ??= BuildTranslationForm(product, translationCultureCode);
        relationForm ??= new ProductRelationCreateViewModel
        {
            ProductId = product.Id,
            RowVersion = product.RowVersion,
            RelationType = RelationTypeOptions[0]
        };
        relationForm.RelationTypeOptions = RelationTypeOptions;
        relationForm.TargetProductOptions = await productOptionsTask;
        mediaForm ??= new ProductMediaCreateViewModel
        {
            ProductId = product.Id,
            RowVersion = product.RowVersion,
            Type = MediaTypeOptions[0],
            IsPrimary = product.Media.Count == 0
        };
        mediaForm.MediaTypeOptions = MediaTypeOptions;
        mediaForm.MediaAssetOptions = await mediaOptionsTask;

        return new ProductDetailsPageViewModel
        {
            Product = productForm,
            Categories = product.Categories,
            Media = product.Media,
            MediaForm = mediaForm,
            Relations = product.Relations,
            RelationForm = relationForm,
            Translations = product.Translations,
            TranslationForm = translationForm,
            NewVariant = variantForm,
            Variants = await variantsTask
        };
    }

    private static ProductTranslationUpsertViewModel BuildTranslationForm(ProductDetailsDto product, string? cultureCode)
    {
        var translation = product.Translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase));

        if (translation is null)
        {
            return new ProductTranslationUpsertViewModel
            {
                ProductId = product.Id,
                CultureCode = string.IsNullOrWhiteSpace(cultureCode) ? "en-GB" : cultureCode
            };
        }

        return new ProductTranslationUpsertViewModel
        {
            ProductId = product.Id,
            CultureCode = translation.CultureCode,
            Name = translation.Name,
            ShortDescription = translation.ShortDescription,
            LongDescription = translation.LongDescription,
            SeoTitle = translation.SeoTitle,
            SeoDescription = translation.SeoDescription
        };
    }

    private async Task<IReadOnlyList<StatusOptionViewModel>> LoadStatusOptionsAsync(string entityType, CancellationToken cancellationToken)
    {
        var items = string.Equals(entityType, "variant", StringComparison.OrdinalIgnoreCase)
            ? await _apiClient.ListVariantStatusesAsync(cancellationToken)
            : await _apiClient.ListProductStatusesAsync(cancellationToken);

        return items
            .Select(x => new StatusOptionViewModel(x.Id, x.Code, x.Name, x.IsBuyable))
            .ToList();
    }

    private async Task<IReadOnlyList<CategoryLookupOptionViewModel>> LoadCategoryOptionsAsync(CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListCategoriesAsync(null, "Active", null, "code", cancellationToken);
        return BuildCategoryOptions(response.Items);
    }

    private async Task<IReadOnlyList<BrandLookupOptionViewModel>> LoadBrandOptionsAsync(CancellationToken cancellationToken, Guid? selectedBrandId)
    {
        var activeBrandsTask = _apiClient.ListBrandsAsync(null, "Active", "code", cancellationToken);
        Task<BrandDetailsDto?>? selectedBrandTask = null;

        if (selectedBrandId is Guid brandId)
        {
            selectedBrandTask = _apiClient.GetBrandAsync(brandId, cancellationToken);
        }

        var activeBrands = await activeBrandsTask;
        var allBrands = activeBrands.Items.ToList();

        if (selectedBrandTask is not null)
        {
            var selectedBrand = await selectedBrandTask;
            if (selectedBrand is not null && allBrands.All(x => x.Id != selectedBrand.Id))
            {
                allBrands.Add(new BrandSummaryDto(
                    selectedBrand.Id,
                    selectedBrand.Code,
                    selectedBrand.Translations.FirstOrDefault()?.Name,
                    selectedBrand.WebsiteUrl,
                    selectedBrand.LogoMediaAssetId,
                    selectedBrand.LogoPublicUrl,
                    selectedBrand.SortOrder,
                    selectedBrand.Status,
                    selectedBrand.CreatedAtUtc,
                    selectedBrand.UpdatedAtUtc,
                    selectedBrand.RowVersion));
            }
        }

        return allBrands
            .Select(x => new BrandLookupOptionViewModel(
                x.Id,
                x.Code,
                string.IsNullOrWhiteSpace(x.DefaultName) ? x.Code : $"{x.DefaultName} ({x.Code})"))
            .OrderBy(x => x.Label)
            .ToList();
    }

    private async Task<IReadOnlyList<ProductLookupOptionViewModel>> LoadProductOptionsAsync(Guid currentProductId, CancellationToken cancellationToken)
    {
        var products = await _apiClient.ListProductLookupsAsync(null, "Active", null, currentProductId, cancellationToken);

        return products
            .Select(x => new ProductLookupOptionViewModel(
                x.Id,
                x.ProductNumber,
                string.IsNullOrWhiteSpace(x.DefaultName) ? x.ProductNumber : $"{x.ProductNumber} - {x.DefaultName}"))
            .OrderBy(x => x.Label)
            .ToList();
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

    private Task<IReadOnlyList<ProductAttributeEditorDefinitionDto>> LoadAttributeDefinitionsAsync(
        string scope,
        CancellationToken cancellationToken)
    {
        return _apiClient.ListProductAttributeEditorDefinitionsAsync(scope, "Active", cancellationToken);
    }

    private static IReadOnlyList<ProductAttributeEditorViewModel> BuildProductAttributeEditors(
        IReadOnlyList<ProductAttributeEditorDefinitionDto> definitions,
        IReadOnlyList<ProductAttributeValueDto> currentValues)
    {
        var currentValueMap = currentValues.ToDictionary(x => x.ProductAttributeId);

        return definitions
            .Select(attribute =>
            {
                var currentValue = currentValueMap.GetValueOrDefault(attribute.Id);
                return new ProductAttributeEditorViewModel
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
                        .Select(x => new ProductAttributeOptionViewModel(x.Id, x.Code, x.Value))
                        .ToList()
                };
            })
            .ToList();
    }

    private static IReadOnlyList<VariantAttributeEditorViewModel> BuildVariantAttributeEditors(
        IReadOnlyList<ProductAttributeEditorDefinitionDto> definitions,
        IReadOnlyList<VariantAttributeValueDto> currentValues)
    {
        var currentValueMap = currentValues.ToDictionary(x => x.ProductAttributeId);

        return definitions
            .Select(attribute =>
            {
                var currentValue = currentValueMap.GetValueOrDefault(attribute.Id);
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

    private static IReadOnlyList<CreateProductRequest.ProductAttributeValueRequest> MapProductAttributeRequests(
        IReadOnlyList<ProductAttributeEditorViewModel> editors)
    {
        return editors
            .Where(editor => editor.AttributeOptionId is not null || !string.IsNullOrWhiteSpace(editor.ValueText))
            .Select(editor => new CreateProductRequest.ProductAttributeValueRequest
            {
                ProductAttributeId = editor.ProductAttributeId,
                AttributeOptionId = editor.AttributeOptionId,
                ValueText = string.IsNullOrWhiteSpace(editor.ValueText) ? null : editor.ValueText
            })
            .ToList();
    }

    private static IReadOnlyList<CategoryLookupOptionViewModel> BuildCategoryOptions(IReadOnlyList<CategorySummaryDto> categories)
    {
        var categoriesById = categories.ToDictionary(x => x.Id);

        static string BuildLabel(CategorySummaryDto category, IReadOnlyDictionary<Guid, CategorySummaryDto> categoriesById)
        {
            var segments = new Stack<string>();
            var current = category;

            while (true)
            {
                var name = string.IsNullOrWhiteSpace(current.DefaultName) ? current.Code : current.DefaultName;
                segments.Push($"{name} ({current.Code})");

                if (current.ParentCategoryId is not Guid parentId || !categoriesById.TryGetValue(parentId, out current))
                {
                    break;
                }
            }

            return string.Join(" / ", segments);
        }

        return categories
            .Select(category => new CategoryLookupOptionViewModel(
                category.Id,
                category.Code,
                BuildLabel(category, categoriesById)))
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
