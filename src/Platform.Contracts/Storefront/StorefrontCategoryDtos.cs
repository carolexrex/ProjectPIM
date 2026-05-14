namespace Platform.Contracts.Storefront;

public sealed record StorefrontCategoryBreadcrumbDto(
    Guid Id,
    string Code,
    string Slug,
    string Name);

public sealed record StorefrontCategoryNodeDto(
    Guid Id,
    string Code,
    string Slug,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    IReadOnlyList<StorefrontCategoryNodeDto> Children);

public sealed record StorefrontCategoryDetailsDto(
    Guid Id,
    string Code,
    string Slug,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    IReadOnlyList<StorefrontCategoryBreadcrumbDto> Breadcrumbs,
    IReadOnlyList<StorefrontCategoryNodeDto> Children);
