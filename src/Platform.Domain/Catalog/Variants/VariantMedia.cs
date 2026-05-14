namespace Platform.Domain.Catalog.Variants;

public sealed class VariantMedia
{
    private VariantMedia()
    {
        Id = Guid.Empty;
        MediaAssetId = Guid.Empty;
        Type = string.Empty;
    }

    public VariantMedia(Guid id, Guid mediaAssetId, string type, int sortOrder, bool isPrimary)
    {
        Id = id;
        MediaAssetId = mediaAssetId;
        Type = type;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
    }

    public Guid Id { get; private set; }
    public Guid MediaAssetId { get; private set; }
    public string Type { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    public void Update(int sortOrder, bool isPrimary)
    {
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
    }
}
