namespace Platform.Backoffice.Models;

public sealed class ProductAttributeEditorViewModel
{
    public Guid ProductAttributeId { get; set; }
    public string AttributeCode { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public Guid? AttributeOptionId { get; set; }
    public string? ValueText { get; set; }
    public IReadOnlyList<ProductAttributeOptionViewModel> Options { get; set; } = [];
}
