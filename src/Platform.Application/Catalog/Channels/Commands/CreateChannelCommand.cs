namespace Platform.Application.Catalog.Channels.Commands;

public sealed record CreateChannelCommand(
    string Code,
    string Name,
    string? HostName);
