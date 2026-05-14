using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public sealed record IntegrationJobListResult(IReadOnlyList<IntegrationJob> Items, int Total);
