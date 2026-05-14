using Platform.Domain.Common;

namespace Platform.Domain.Cart;

public sealed class CartLine
{
    private CartLine()
    {
        Id = Guid.Empty;
        CartId = Guid.Empty;
        VariantId = Guid.Empty;
        Sku = string.Empty;
        ProductName = string.Empty;
    }

    public CartLine(
        Guid id,
        Guid cartId,
        Guid variantId,
        string sku,
        string productName,
        string? variantDescription,
        decimal quantity,
        decimal unitPrice,
        decimal vatRate,
        string? comment)
    {
        if (quantity <= 0m)
        {
            throw new InvalidOperationException("Cart line quantity must be positive.");
        }

        Id = id;
        CartId = cartId;
        VariantId = variantId;
        Sku = NormalizeRequired(sku);
        ProductName = NormalizeRequired(productName);
        VariantDescription = NormalizeOptional(variantDescription);
        Quantity = quantity;
        UnitPrice = unitPrice;
        VatRate = vatRate;
        Comment = NormalizeOptional(comment);
        LineTotal = decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);
    }

    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid VariantId { get; private set; }
    public string Sku { get; private set; }
    public string ProductName { get; private set; }
    public string? VariantDescription { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Comment { get; private set; }

    public void Reprice(string sku, string productName, string? variantDescription, decimal unitPrice, decimal vatRate, string? comment)
    {
        Sku = NormalizeRequired(sku);
        ProductName = NormalizeRequired(productName);
        VariantDescription = NormalizeOptional(variantDescription);
        UnitPrice = unitPrice;
        VatRate = vatRate;
        Comment = NormalizeOptional(comment);
        LineTotal = decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);
    }

    public void EnsureCart(Guid cartId)
    {
        if (CartId != cartId)
        {
            throw new ConcurrencyException("Cart line does not belong to the expected cart.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
