namespace Platform.Domain.Integrations;

public static class WebhookDeliveryStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Abandoned = "Abandoned";
}
