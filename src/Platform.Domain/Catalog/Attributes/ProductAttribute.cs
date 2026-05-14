using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Attributes;

public sealed class ProductAttribute
{
    private readonly List<AttributeOption> _options = [];

    private ProductAttribute()
    {
        Id = Guid.Empty;
        Code = string.Empty;
        Name = string.Empty;
        Scope = string.Empty;
        DataType = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public ProductAttribute(
        Guid id,
        string code,
        string name,
        string scope,
        string dataType,
        bool isVariantDefining,
        bool isFilterable,
        bool isRequired,
        int sortOrder,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        IEnumerable<AttributeOption> options)
    {
        Id = id;
        Code = code;
        Name = name;
        Scope = scope;
        DataType = dataType;
        IsVariantDefining = isVariantDefining;
        IsFilterable = isFilterable;
        IsRequired = isRequired;
        SortOrder = sortOrder;
        Status = "Active";
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        ReplaceOptions(options);
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Scope { get; private set; }
    public string DataType { get; private set; }
    public bool IsVariantDefining { get; private set; }
    public bool IsFilterable { get; private set; }
    public bool IsRequired { get; private set; }
    public int SortOrder { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<AttributeOption> Options => _options;

    public void Update(
        string code,
        string name,
        string scope,
        string dataType,
        bool isVariantDefining,
        bool isFilterable,
        bool isRequired,
        int sortOrder,
        IEnumerable<AttributeOption> options,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Code = code;
        Name = name;
        Scope = scope;
        DataType = dataType;
        IsVariantDefining = isVariantDefining;
        IsFilterable = isFilterable;
        IsRequired = isRequired;
        SortOrder = sortOrder;
        ReplaceOptions(options);
        Touch();
    }

    public void Archive()
    {
        Status = "Archived";
        Touch();
    }

    private void ReplaceOptions(IEnumerable<AttributeOption> options)
    {
        var nextOptions = options
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var duplicateCode = nextOptions
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateCode is not null)
        {
            throw new InvalidOperationException($"Duplicate attribute option code '{duplicateCode.Key}'.");
        }

        _options.Clear();
        _options.AddRange(nextOptions);
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The attribute has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
