namespace Platform.Domain.Cart;

public sealed record CartPriceUpdate(
    Guid CartLineId,
    string Sku,
    string ProductName,
    string? VariantDescription,
    decimal UnitPrice,
    decimal VatRate,
    string? Comment);
