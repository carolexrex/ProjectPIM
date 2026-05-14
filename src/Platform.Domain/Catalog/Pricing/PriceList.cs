using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Pricing;

public sealed class PriceList
{
    private readonly List<PriceListEntry> _entries = [];
    private readonly List<PriceListMarketAssignment> _marketAssignments = [];

    private PriceList()
    {
        Id = Guid.Empty;
        Code = string.Empty;
        Name = string.Empty;
        CurrencyCode = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public PriceList(
        Guid id,
        string code,
        string name,
        string currencyCode,
        bool vatIncluded,
        DateTime? validFromUtc,
        DateTime? validToUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        Code = NormalizeRequired(code);
        Name = NormalizeRequired(name);
        CurrencyCode = NormalizeRequired(currencyCode).ToUpperInvariant();
        VatIncluded = vatIncluded;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        Status = "Active";
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string CurrencyCode { get; private set; }
    public bool VatIncluded { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidToUtc { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<PriceListEntry> Entries => _entries;
    public IReadOnlyCollection<PriceListMarketAssignment> MarketAssignments => _marketAssignments;

    public void Update(
        string code,
        string name,
        string currencyCode,
        bool vatIncluded,
        DateTime? validFromUtc,
        DateTime? validToUtc,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Code = NormalizeRequired(code);
        Name = NormalizeRequired(name);
        CurrencyCode = NormalizeRequired(currencyCode).ToUpperInvariant();
        VatIncluded = vatIncluded;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        Touch();
    }

    public void Archive()
    {
        Status = "Archived";
        Touch();
    }

    public void UpsertMarketAssignment(Guid marketId, int priority, bool isBasePriceList, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var existing = _marketAssignments.FirstOrDefault(x => x.MarketId == marketId);
        if (existing is null)
        {
            _marketAssignments.Add(new PriceListMarketAssignment(Guid.NewGuid(), marketId, priority, isBasePriceList));
        }
        else
        {
            existing.Update(priority, isBasePriceList);
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

    public PriceListEntry UpsertEntry(
        Guid? entryId,
        string targetType,
        Guid targetId,
        int minQuantity,
        decimal amount,
        decimal? compareAtAmount,
        DateTime? validFromUtc,
        DateTime? validToUtc,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var normalizedTargetType = NormalizeRequired(targetType);
        var existing = entryId is Guid explicitEntryId
            ? _entries.FirstOrDefault(x => x.Id == explicitEntryId)
            : _entries.FirstOrDefault(x =>
                string.Equals(x.TargetType, normalizedTargetType, StringComparison.OrdinalIgnoreCase)
                && x.TargetId == targetId
                && x.MinQuantity == minQuantity);

        if (existing is null)
        {
            existing = new PriceListEntry(Guid.NewGuid(), normalizedTargetType, targetId, minQuantity, amount, compareAtAmount, validFromUtc, validToUtc);
            _entries.Add(existing);
        }
        else
        {
            existing.Update(normalizedTargetType, targetId, minQuantity, amount, compareAtAmount, validFromUtc, validToUtc);
        }

        SortEntries();
        Touch();
        return existing;
    }

    public void RemoveEntry(Guid entryId, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var existing = _entries.FirstOrDefault(x => x.Id == entryId);
        if (existing is null)
        {
            return;
        }

        _entries.Remove(existing);
        Touch();
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The price list has changed since it was loaded.");
        }
    }

    private void SortEntries()
    {
        var ordered = _entries
            .OrderBy(x => x.TargetType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.TargetId)
            .ThenBy(x => x.MinQuantity)
            .ThenBy(x => x.ValidFromUtc)
            .ToList();

        _entries.Clear();
        _entries.AddRange(ordered);
    }

    private void SortMarketAssignments()
    {
        var ordered = _marketAssignments
            .OrderBy(x => x.Priority)
            .ThenByDescending(x => x.IsBasePriceList)
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
}
