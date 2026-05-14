namespace Platform.Domain.Orders;

public sealed class OrderLine
{
    private OrderLine()
    {
        Id = Guid.Empty;
        OrderId = Guid.Empty;
        VariantId = Guid.Empty;
        Sku = string.Empty;
        ProductName = string.Empty;
    }

    public OrderLine(
        Guid id,
        Guid orderId,
        Guid variantId,
        string sku,
        string productName,
        string? variantDescription,
        decimal quantity,
        decimal unitPrice,
        decimal vatRate)
    {
        if (quantity <= 0m)
        {
            throw new InvalidOperationException("Order line quantity must be positive.");
        }

        Id = id;
        OrderId = orderId;
        VariantId = variantId;
        Sku = NormalizeRequired(sku);
        ProductName = NormalizeRequired(productName);
        VariantDescription = NormalizeOptional(variantDescription);
        Quantity = quantity;
        UnitPrice = unitPrice;
        VatRate = vatRate;
        LineTotal = decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid VariantId { get; private set; }
    public string Sku { get; private set; }
    public string ProductName { get; private set; }
    public string? VariantDescription { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal LineTotal { get; private set; }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
