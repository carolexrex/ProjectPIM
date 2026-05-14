namespace Platform.Domain.Orders;

public sealed class OrderStatusHistory
{
    private OrderStatusHistory()
    {
        Id = Guid.Empty;
        OrderId = Guid.Empty;
        ToStatus = string.Empty;
        ChangedBy = string.Empty;
    }

    public OrderStatusHistory(
        Guid id,
        Guid orderId,
        string? fromStatus,
        string toStatus,
        string changedBy,
        DateTime changedAtUtc,
        string? comment)
    {
        Id = id;
        OrderId = orderId;
        FromStatus = NormalizeOptional(fromStatus);
        ToStatus = NormalizeRequired(toStatus);
        ChangedBy = NormalizeRequired(changedBy);
        ChangedAtUtc = changedAtUtc;
        Comment = NormalizeOptional(comment);
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string? FromStatus { get; private set; }
    public string ToStatus { get; private set; }
    public string ChangedBy { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public string? Comment { get; private set; }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
