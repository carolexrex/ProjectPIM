namespace Platform.Backoffice.Configuration;

public sealed class AdminApiOptions
{
    public const string SectionName = "AdminApi";

    public string BaseUrl { get; init; } = string.Empty;
}
