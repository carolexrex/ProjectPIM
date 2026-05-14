namespace Platform.Application.Catalog.Channels.Commands;

public sealed record UpdateChannelCommand(
    Guid ChannelId,
    string Code,
    string Name,
    string? HostName,
    string RowVersion);
