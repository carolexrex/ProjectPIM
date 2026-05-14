using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Inventory;

public sealed class InventoryLocation
{
    private readonly List<InventoryLocationMarketAssignment> _marketAssignments = [];

    private InventoryLocation()
    {
        Id = Guid.Empty;
        Code = string.Empty;
        Name = string.Empty;
        Type = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public InventoryLocation(
        Guid id,
        string code,
        string name,
        string type,
        string? countryCode,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        Code = NormalizeRequired(code).ToUpperInvariant();
        Name = NormalizeRequired(name);
        Type = NormalizeRequired(type);
        CountryCode = NormalizeCountryCode(countryCode);
        Status = "Active";
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Type { get; private set; }
    public string? CountryCode { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<InventoryLocationMarketAssignment> MarketAssignments => _marketAssignments;

    public void Update(string code, string name, string type, string? countryCode, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Code = NormalizeRequired(code).ToUpperInvariant();
        Name = NormalizeRequired(name);
        Type = NormalizeRequired(type);
        CountryCode = NormalizeCountryCode(countryCode);
        Touch();
    }

    public void Archive()
    {
        Status = "Archived";
        Touch();
    }

    public void UpsertMarketAssignment(Guid marketId, int priority, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var existing = _marketAssignments.FirstOrDefault(x => x.MarketId == marketId);
        if (existing is null)
        {
            _marketAssignments.Add(new InventoryLocationMarketAssignment(Guid.NewGuid(), marketId, priority));
        }
        else
        {
            existing.Update(priority);
        }

        SortMarketAssignments();
        Touch();
    }

    public void RemoveMarketAssignment(Guid marketId, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        var existing = _marketAssignments.FirstOrDefault(x => x.MarketId == marketId);
        if (existing is null)
        {
            return;
        }

        _marketAssignments.Remove(existing);
        Touch();
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The inventory location has changed since it was loaded.");
        }
    }

    private void SortMarketAssignments()
    {
        var ordered = _marketAssignments
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.MarketId)
            .ToList();

        _marketAssignments.Clear();
        _marketAssignments.AddRange(ordered);
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeCountryCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant();
    }
}
