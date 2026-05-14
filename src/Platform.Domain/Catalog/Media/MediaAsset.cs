using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Media;

public sealed class MediaAsset
{
    private MediaAsset()
    {
        Id = Guid.Empty;
        StorageProvider = string.Empty;
        StorageKey = string.Empty;
        FileName = string.Empty;
        ContentType = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
        PublicUrl = string.Empty;
    }

    public MediaAsset(
        Guid id,
        string storageProvider,
        string storageKey,
        string fileName,
        string contentType,
        long fileSize,
        int? width,
        int? height,
        string publicUrl,
        string? altText,
        string? title,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        StorageProvider = storageProvider;
        StorageKey = storageKey;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        Width = width;
        Height = height;
        PublicUrl = publicUrl;
        AltText = altText;
        Title = title;
        Status = "Active";
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    public Guid Id { get; private set; }
    public string StorageProvider { get; private set; }
    public string StorageKey { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long FileSize { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string PublicUrl { get; private set; }
    public string? AltText { get; private set; }
    public string? Title { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }

    public void Update(
        string fileName,
        string contentType,
        long fileSize,
        int? width,
        int? height,
        string publicUrl,
        string? altText,
        string? title,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        Width = width;
        Height = height;
        PublicUrl = publicUrl;
        AltText = altText;
        Title = title;
        Touch();
    }

    public void Archive(string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Status = "Archived";
        Touch();
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The media asset has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
