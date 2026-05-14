namespace Platform.Application.Orders.Commands;

public sealed record AddPaymentTransactionCommand(
    Guid OrderId,
    string Provider,
    string ProviderReference,
    string Type,
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc,
    string RowVersion);
