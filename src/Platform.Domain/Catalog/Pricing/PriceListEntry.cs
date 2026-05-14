namespace Platform.Domain.Catalog.Pricing;

public sealed class PriceListEntry
{
    private PriceListEntry()
    {
        Id = Guid.Empty;
        TargetType = string.Empty;
    }

    public PriceListEntry(
        Guid id,
        string targetType,
        Guid targetId,
        int minQuantity,
        decimal amount,
        decimal? compareAtAmount,
        DateTime? validFromUtc,
        DateTime? validToUtc)
    {
        Id = id;
        TargetType = NormalizeRequired(targetType);
        TargetId = targetId;
        MinQuantity = minQuantity;
        Amount = amount;
        CompareAtAmount = compareAtAmount;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
    }

    public Guid Id { get; private set; }
    public string TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public int MinQuantity { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? CompareAtAmount { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidToUtc { get; private set; }

    public void Update(
        string targetType,
        Guid targetId,
        int minQuantity,
        decimal amount,
        decimal? compareAtAmount,
        DateTime? validFromUtc,
        DateTime? validToUtc)
    {
        TargetType = NormalizeRequired(targetType);
        TargetId = targetId;
        MinQuantity = minQuantity;
        Amount = amount;
        CompareAtAmount = compareAtAmount;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
