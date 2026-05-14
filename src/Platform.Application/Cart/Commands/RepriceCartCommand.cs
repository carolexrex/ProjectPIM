namespace Platform.Application.Cart.Commands;

public sealed record RepriceCartCommand(Guid CartId, string RowVersion);
