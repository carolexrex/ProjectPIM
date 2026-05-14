namespace Platform.Application.Cart.Commands;

public sealed record ExpireCartCommand(Guid CartId, string RowVersion);
