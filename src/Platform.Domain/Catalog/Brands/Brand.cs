using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Brands;

public sealed class Brand
{
    private readonly List<BrandTranslation> _translations = [];

    private Brand()
    {
        Id = Guid.Empty;
        Code = string.Empty;
        WebsiteUrl = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public Brand(
        Guid id,
        string code,
        string? websiteUrl,
        Guid? logoMediaAssetId,
        int sortOrder,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        Code = code;
        WebsiteUrl = websiteUrl;
        LogoMediaAssetId = logoMediaAssetId;
        SortOrder = sortOrder;
        Status = "Active";
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public Guid? LogoMediaAssetId { get; private set; }
    public int SortOrder { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<BrandTranslation> Translations => _translations;

    public void Update(string code, string? websiteUrl, Guid? logoMediaAssetId, int sortOrder, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Code = code;
        WebsiteUrl = websiteUrl;
        LogoMediaAssetId = logoMediaAssetId;
        SortOrder = sortOrder;
        Touch();
    }

    public void Archive()
    {
        Status = "Archived";
        Touch();
    }

    public BrandTranslation UpsertTranslation(string cultureCode, string name, string slug, string? description)
    {
        var existing = _translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.Update(name, slug, description);
            Touch();
            return existing;
        }

        var translation = new BrandTranslation(Guid.NewGuid(), cultureCode, name, slug, description);
        _translations.Add(translation);
        Touch();
        return translation;
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The brand has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
