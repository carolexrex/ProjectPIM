using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Markets;

public sealed class Market
{
    private readonly List<MarketCurrency> _currencies = [];
    private readonly List<MarketCulture> _cultures = [];
    private readonly List<MarketProductAssignment> _productAssignments = [];

    private Market()
    {
        Id = Guid.Empty;
        Code = string.Empty;
        Name = string.Empty;
        DefaultCurrency = string.Empty;
        DefaultCulture = string.Empty;
        VatMode = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public Market(
        Guid id,
        string code,
        string name,
        string defaultCurrency,
        string defaultCulture,
        string vatMode,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        DefaultCurrency = defaultCurrency;
        DefaultCulture = defaultCulture;
        VatMode = vatMode;
        Status = "Active";
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        SetCurrencies(defaultCurrency, [defaultCurrency]);
        SetCultures(defaultCulture, [defaultCulture]);
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string DefaultCurrency { get; private set; }
    public string DefaultCulture { get; private set; }
    public string VatMode { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<MarketCurrency> Currencies => _currencies;
    public IReadOnlyCollection<MarketCulture> Cultures => _cultures;
    public IReadOnlyCollection<MarketProductAssignment> ProductAssignments => _productAssignments;

    public void Update(string code, string name, string defaultCurrency, string defaultCulture, string vatMode, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Code = code;
        Name = name;
        DefaultCurrency = NormalizeRequiredValue(defaultCurrency);
        DefaultCulture = NormalizeRequiredValue(defaultCulture);
        VatMode = NormalizeRequiredValue(vatMode);
        SetCurrencies(DefaultCurrency, _currencies.Select(x => x.CurrencyCode).Append(DefaultCurrency));
        SetCultures(DefaultCulture, _cultures.Select(x => x.CultureCode).Append(DefaultCulture));
        Touch();
    }

    public void Archive()
    {
        Status = "Archived";
        Touch();
    }

    public void AssignCurrencies(string defaultCurrency, IEnumerable<string> currencies, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        SetCurrencies(defaultCurrency, currencies);
        Touch();
    }

    public void AssignCultures(string defaultCulture, IEnumerable<string> cultures, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        SetCultures(defaultCulture, cultures);
        Touch();
    }

    public void UpsertProductAssignment(Guid productId, string status, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var existing = _productAssignments.FirstOrDefault(x => x.ProductId == productId);
        if (existing is null)
        {
            _productAssignments.Add(new MarketProductAssignment(Guid.NewGuid(), productId, NormalizeRequiredValue(status)));
        }
        else
        {
            existing.UpdateStatus(NormalizeRequiredValue(status));
        }

        SortProductAssignments();
        Touch();
    }

    public void RemoveProductAssignment(Guid productId, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        var existing = _productAssignments.FirstOrDefault(x => x.ProductId == productId);
        if (existing is null)
        {
            return;
        }

        _productAssignments.Remove(existing);
        Touch();
    }

    private void SetCurrencies(string defaultCurrency, IEnumerable<string> currencies)
    {
        var normalizedDefault = NormalizeRequiredValue(defaultCurrency).ToUpperInvariant();
        var normalized = currencies
            .Select(x => NormalizeRequiredValue(x).ToUpperInvariant())
            .Append(normalizedDefault)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _currencies.Clear();
        _currencies.AddRange(normalized.Select(code => new MarketCurrency(Guid.NewGuid(), code, string.Equals(code, normalizedDefault, StringComparison.OrdinalIgnoreCase))));
        DefaultCurrency = normalizedDefault;
    }

    private void SetCultures(string defaultCulture, IEnumerable<string> cultures)
    {
        var normalizedDefault = NormalizeRequiredValue(defaultCulture);
        var normalized = cultures
            .Select(NormalizeRequiredValue)
            .Append(normalizedDefault)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _cultures.Clear();
        _cultures.AddRange(normalized.Select(code => new MarketCulture(Guid.NewGuid(), code, string.Equals(code, normalizedDefault, StringComparison.OrdinalIgnoreCase))));
        DefaultCulture = normalizedDefault;
    }

    private void SortProductAssignments()
    {
        var ordered = _productAssignments
            .OrderBy(x => x.Status, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ProductId)
            .ToList();

        _productAssignments.Clear();
        _productAssignments.AddRange(ordered);
    }

    private static string NormalizeRequiredValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The market has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
