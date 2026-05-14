namespace Platform.Application.Orders.Commands;

public sealed record ChangeOrderStatusCommand(Guid OrderId, string ToStatus, string? Comment, string RowVersion);
