using Platform.Domain.Catalog.Channels;

namespace Platform.Application.Catalog.Channels;

public sealed record ChannelListResult(
    IReadOnlyList<Channel> Items,
    int Total);
