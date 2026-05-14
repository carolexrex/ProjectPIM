namespace Platform.Domain.Catalog.Markets;

public sealed class MarketProductAssignment
{
    private MarketProductAssignment()
    {
        Id = Guid.Empty;
        ProductId = Guid.Empty;
        Status = string.Empty;
    }

    public MarketProductAssignment(Guid id, Guid productId, string status)
    {
        Id = id;
        ProductId = productId;
        Status = status;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Status { get; private set; }

    public void UpdateStatus(string status)
    {
        Status = status;
    }
}
