namespace Platform.Infrastructure.Integrations;

public sealed record StorefrontProjectionRebuildJobPayload();

public sealed record StorefrontProjectionRebuildJobResult(
    DateTime RebuiltAtUtc,
    int ProjectionCount);
