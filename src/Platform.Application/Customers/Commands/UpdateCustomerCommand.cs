namespace Platform.Application.Customers.Commands;

public sealed record UpdateCustomerCommand(
    Guid CustomerId,
    string? ExternalId,
    string? UserId,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? PreferredCulture,
    Guid? DefaultMarketId,
    string Status,
    bool IsGuest,
    string RowVersion);
