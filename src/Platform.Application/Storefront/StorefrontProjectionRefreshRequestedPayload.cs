namespace Platform.Application.Storefront;

public sealed record StorefrontProjectionRefreshRequestedPayload(
    DateTime OccurredAtUtc,
    string Reason,
    IReadOnlyList<Guid> ProductIds,
    IReadOnlyList<Guid> VariantIds);
