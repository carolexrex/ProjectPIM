using Platform.Contracts.Catalog.Brands;
using Platform.Contracts.Catalog.Pricing;
using Platform.Contracts.Catalog.Products;

namespace Platform.Contracts.Integrations;

public sealed record BrandWebhookEventDto(
    DateTime OccurredAtUtc,
    string ChangeType,
    BrandDetailsDto Brand);

public sealed record ProductWebhookEventDto(
    DateTime OccurredAtUtc,
    string ChangeType,
    ProductDetailsDto Product);

public sealed record PriceListWebhookEventDto(
    DateTime OccurredAtUtc,
    string ChangeType,
    PriceListDetailsDto PriceList);
