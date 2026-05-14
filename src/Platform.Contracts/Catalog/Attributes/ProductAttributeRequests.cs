using System.ComponentModel.DataAnnotations;

namespace Platform.Contracts.Catalog.Attributes;

public sealed class AttributeOptionRequest
{
    public Guid? Id { get; init; }

    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Value { get; init; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }
}

public sealed class CreateProductAttributeRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Scope { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string DataType { get; init; } = string.Empty;

    public bool IsVariantDefining { get; init; }
    public bool IsFilterable { get; init; }
    public bool IsRequired { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    public IReadOnlyList<AttributeOptionRequest> Options { get; init; } = [];
}

public sealed class UpdateProductAttributeRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Scope { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string DataType { get; init; } = string.Empty;

    public bool IsVariantDefining { get; init; }
    public bool IsFilterable { get; init; }
    public bool IsRequired { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;

    public IReadOnlyList<AttributeOptionRequest> Options { get; init; } = [];
}
