using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Categories;

public sealed class Category
{
    private readonly List<CategoryTranslation> _translations = [];

    private Category()
    {
        Id = Guid.Empty;
        Code = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public Category(
        Guid id,
        string code,
        Guid? parentCategoryId,
        int sortOrder,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        Code = code;
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
        Status = "Active";
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public int SortOrder { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<CategoryTranslation> Translations => _translations;

    public void Update(string code, Guid? parentCategoryId, int sortOrder, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Code = code;
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
        Touch();
    }

    public void Archive()
    {
        Status = "Archived";
        Touch();
    }

    public CategoryTranslation UpsertTranslation(string cultureCode, string name, string slug, string? description)
    {
        var existing = _translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.Update(name, slug, description);
            Touch();
            return existing;
        }

        var translation = new CategoryTranslation(Guid.NewGuid(), cultureCode, name, slug, description);
        _translations.Add(translation);
        Touch();
        return translation;
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The category has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
