using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Security;
using Platform.Backoffice.Integration;
using Platform.Backoffice.Models;
using Platform.Contracts.Catalog.Categories;

namespace Platform.Backoffice.Controllers;

[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("categories")]
public sealed class CategoriesController : Controller
{
    private readonly IAdminApiClient _apiClient;

    public CategoriesController(IAdminApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var responseTask = _apiClient.ListCategoriesAsync(search, status, null, "code", cancellationToken);
        var hierarchyTask = _apiClient.ListCategoriesAsync(null, null, null, "code", cancellationToken);

        await Task.WhenAll(responseTask, hierarchyTask);

        var response = await responseTask;
        var hierarchy = await hierarchyTask;
        var hierarchyMap = hierarchy.Items.ToDictionary(x => x.Id);

        return View(new CategoryListPageViewModel
        {
            Search = search,
            Status = status,
            Categories = response.Items.Select(category => BuildListItem(category, hierarchyMap)).ToList(),
            Total = response.Total
        });
    }

    [HttpGet("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    public async Task<IActionResult> New(CancellationToken cancellationToken)
    {
        return View(await BuildCreateViewModelAsync(cancellationToken));
    }

    [HttpPost("new")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(CategoryCreateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.ParentOptions = await LoadParentOptionsAsync(form.ParentCategoryId, null, cancellationToken);
            return View(form);
        }

        try
        {
            var created = await _apiClient.CreateCategoryAsync(
                new CreateCategoryRequest
                {
                    Code = form.Code,
                    ParentCategoryId = form.ParentCategoryId,
                    SortOrder = form.SortOrder
                },
                cancellationToken);

            TempData["FlashMessage"] = $"Category {created.Code} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception);
            form.ParentOptions = await LoadParentOptionsAsync(form.ParentCategoryId, null, cancellationToken);
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
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Category")] CategoryUpdateViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, form, null, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var updated = await _apiClient.UpdateCategoryAsync(
                id,
                new UpdateCategoryRequest
                {
                    Code = form.Code,
                    ParentCategoryId = form.ParentCategoryId,
                    SortOrder = form.SortOrder,
                    RowVersion = form.RowVersion
                },
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Category {updated.Code} updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "Category");
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
            var archived = await _apiClient.ArchiveCategoryAsync(id, cancellationToken);
            if (archived is null)
            {
                return NotFound();
            }

            TempData["FlashMessage"] = $"Category {archived.Code} archived.";
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
    public async Task<IActionResult> UpsertTranslation(Guid id, [Bind(Prefix = "TranslationForm")] CategoryTranslationUpsertViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildDetailsPageAsync(id, null, form, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }

        try
        {
            var translation = await _apiClient.UpsertCategoryTranslationAsync(
                id,
                form.CultureCode,
                new UpsertCategoryTranslationRequest
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

            TempData["FlashMessage"] = $"Category translation {translation.CultureCode} saved.";
            return RedirectToAction(nameof(Details), new { id, cultureCode = translation.CultureCode });
        }
        catch (AdminApiException exception)
        {
            ApplyApiErrors(exception, "TranslationForm");
            var invalidPage = await BuildDetailsPageAsync(id, null, form, null, cancellationToken);
            return invalidPage is null ? NotFound() : View("Details", invalidPage);
        }
    }

    private async Task<CategoryCreateViewModel> BuildCreateViewModelAsync(CancellationToken cancellationToken)
    {
        return new CategoryCreateViewModel
        {
            ParentOptions = await LoadParentOptionsAsync(null, null, cancellationToken)
        };
    }

    private async Task<CategoryDetailsPageViewModel?> BuildDetailsPageAsync(
        Guid categoryId,
        CategoryUpdateViewModel? categoryForm,
        CategoryTranslationUpsertViewModel? translationForm,
        string? translationCultureCode,
        CancellationToken cancellationToken)
    {
        var categoryTask = _apiClient.GetCategoryAsync(categoryId, cancellationToken);
        var parentsTask = LoadParentOptionsAsync(null, categoryId, cancellationToken);

        await Task.WhenAll(categoryTask, parentsTask);

        var category = await categoryTask;
        if (category is null)
        {
            return null;
        }

        categoryForm ??= new CategoryUpdateViewModel
        {
            Id = category.Id,
            Code = category.Code,
            ParentCategoryId = category.ParentCategoryId,
            SortOrder = category.SortOrder,
            RowVersion = category.RowVersion,
            Status = category.Status,
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc
        };
        categoryForm.ParentOptions = await parentsTask;

        translationForm ??= BuildTranslationForm(category, translationCultureCode);

        return new CategoryDetailsPageViewModel
        {
            Category = categoryForm,
            Translations = category.Translations,
            TranslationForm = translationForm
        };
    }

    private async Task<IReadOnlyList<CategoryLookupOptionViewModel>> LoadParentOptionsAsync(Guid? selectedId, Guid? excludedCategoryId, CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListCategoriesAsync(null, "Active", null, "code", cancellationToken);
        var categoriesById = response.Items.ToDictionary(x => x.Id);

        return response.Items
            .Where(x => x.Id != excludedCategoryId)
            .Select(x => new CategoryLookupOptionViewModel(x.Id, x.Code, BuildCategoryLabel(x, categoriesById)))
            .OrderBy(x => x.Label)
            .ToList();
    }

    private static CategoryTranslationUpsertViewModel BuildTranslationForm(CategoryDetailsDto category, string? cultureCode)
    {
        var translation = category.Translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase));

        if (translation is null)
        {
            return new CategoryTranslationUpsertViewModel
            {
                CategoryId = category.Id,
                CultureCode = string.IsNullOrWhiteSpace(cultureCode) ? "en-GB" : cultureCode
            };
        }

        return new CategoryTranslationUpsertViewModel
        {
            CategoryId = category.Id,
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

    private static CategoryListItemViewModel BuildListItem(
        CategorySummaryDto category,
        IReadOnlyDictionary<Guid, CategorySummaryDto> categoriesById)
    {
        var pathLabel = BuildCategoryLabel(category, categoriesById);
        var parentLabel = category.ParentCategoryId is Guid parentId && categoriesById.TryGetValue(parentId, out var parent)
            ? BuildCategoryLabel(parent, categoriesById)
            : null;
        var depth = Math.Max(0, pathLabel.Split(" / ", StringSplitOptions.RemoveEmptyEntries).Length - 1);

        return new CategoryListItemViewModel(
            category.Id,
            category.Code,
            category.DefaultName,
            pathLabel,
            parentLabel,
            depth,
            category.Status,
            category.UpdatedAtUtc);
    }

    private static string BuildCategoryLabel(CategorySummaryDto category, IReadOnlyDictionary<Guid, CategorySummaryDto> categoriesById)
    {
        var segments = new Stack<string>();
        var visited = new HashSet<Guid>();
        var current = category;

        while (true)
        {
            if (!visited.Add(current.Id))
            {
                segments.Push("[Cycle]");
                break;
            }

            var name = string.IsNullOrWhiteSpace(current.DefaultName) ? current.Code : current.DefaultName;
            segments.Push($"{name} ({current.Code})");

            if (current.ParentCategoryId is not Guid parentId || !categoriesById.TryGetValue(parentId, out current))
            {
                break;
            }
        }

        return string.Join(" / ", segments);
    }
}
