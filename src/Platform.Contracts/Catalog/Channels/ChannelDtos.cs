namespace Platform.Contracts.Catalog.Channels;

public sealed record ChannelMarketAssignmentDto(
    Guid MarketId,
    string MarketCode,
    string MarketName);

public sealed record ChannelSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? HostName,
    string Status,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record ChannelDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? HostName,
    string Status,
    IReadOnlyList<ChannelMarketAssignmentDto> Markets,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
