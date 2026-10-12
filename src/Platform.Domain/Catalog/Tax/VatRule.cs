using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Tax;

/// <summary>
/// Configured VAT rate for one market and one product tax category code.
/// The rate is a fraction (0.25 = 25%); zero is a valid, explicitly configured rate.
/// </summary>
public sealed class VatRule
{
    public const int MaxTaxCategoryCodeLength = 64;
    public const int MaxDescriptionLength = 256;
    public const int RateDecimalPlaces = 4;

    private VatRule()
    {
        Id = Guid.Empty;
        MarketId = Guid.Empty;
        TaxCategoryCode = string.Empty;
        RowVersion = string.Empty;
    }

    public VatRule(
        Guid id,
        Guid marketId,
        string taxCategoryCode,
        decimal rate,
        string? description,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        if (marketId == Guid.Empty)
        {
            throw new ArgumentException("A VAT rule must belong to a market.", nameof(marketId));
        }

        var normalizedCode = NormalizeTaxCategoryCode(taxCategoryCode);
        if (normalizedCode.Length == 0 || normalizedCode.Length > MaxTaxCategoryCodeLength)
        {
            throw new ArgumentException($"Tax category code is required and must not exceed {MaxTaxCategoryCodeLength} characters.", nameof(taxCategoryCode));
        }

        EnsureValidRate(rate);

        Id = id;
        MarketId = marketId;
        TaxCategoryCode = normalizedCode;
        Rate = rate;
        Description = NormalizeOptional(description);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public Guid MarketId { get; private set; }
    public string TaxCategoryCode { get; private set; }
    public decimal Rate { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }

    public void Update(decimal rate, string? description, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        EnsureValidRate(rate);
        Rate = rate;
        Description = NormalizeOptional(description);
        Touch();
    }

    public void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The VAT rule has changed since it was loaded.");
        }
    }

    public static bool IsValidRate(decimal rate)
    {
        return rate >= 0m && rate < 1m && decimal.Round(rate, RateDecimalPlaces) == rate;
    }

    /// <summary>
    /// Tax category codes are matched case-insensitively against product codes, so both sides
    /// are compared in this normalized form.
    /// </summary>
    public static string NormalizeTaxCategoryCode(string? taxCategoryCode)
    {
        return string.IsNullOrWhiteSpace(taxCategoryCode) ? string.Empty : taxCategoryCode.Trim().ToUpperInvariant();
    }

    private static void EnsureValidRate(decimal rate)
    {
        if (!IsValidRate(rate))
        {
            throw new ArgumentOutOfRangeException(nameof(rate), rate, $"VAT rate must be a fraction from 0 up to (but excluding) 1 with at most {RateDecimalPlaces} decimal places.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = NewRowVersion();
    }

    private static string NewRowVersion()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
