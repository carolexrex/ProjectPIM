SET NOCOUNT ON;

DECLARE @Now datetime2 = SYSUTCDATETIME();

DECLARE @ProductStatusDraft uniqueidentifier = '10000000-0000-0000-0000-000000000001';
DECLARE @ProductStatusReady uniqueidentifier = '10000000-0000-0000-0000-000000000002';
DECLARE @ProductStatusComingSoon uniqueidentifier = '10000000-0000-0000-0000-000000000003';
DECLARE @ProductStatusDiscontinued uniqueidentifier = '10000000-0000-0000-0000-000000000004';

DECLARE @VariantStatusDraft uniqueidentifier = '10000000-0000-0000-0000-000000000101';
DECLARE @VariantStatusReady uniqueidentifier = '10000000-0000-0000-0000-000000000102';
DECLARE @VariantStatusComingSoon uniqueidentifier = '10000000-0000-0000-0000-000000000103';
DECLARE @VariantStatusDiscontinued uniqueidentifier = '10000000-0000-0000-0000-000000000104';

DECLARE @MarketSe uniqueidentifier = '20000000-0000-0000-0000-000000000001';
DECLARE @MarketCurrencySek uniqueidentifier = '20000000-0000-0000-0000-000000000011';
DECLARE @MarketCurrencyEur uniqueidentifier = '20000000-0000-0000-0000-000000000012';
DECLARE @MarketCultureSvSe uniqueidentifier = '20000000-0000-0000-0000-000000000021';
DECLARE @MarketCultureEnGb uniqueidentifier = '20000000-0000-0000-0000-000000000022';

DECLARE @PriceListSeBase uniqueidentifier = '30000000-0000-0000-0000-000000000001';
DECLARE @MarketPriceListSeBase uniqueidentifier = '30000000-0000-0000-0000-000000000011';

INSERT INTO dbo.ProductStatusDefinition (
    Id, TenantId, EntityType, Code, Name, IsDefault, IsVisibleInBackoffice,
    IsVisibleInStorefront, IsBuyable, IsSearchable, SortOrder, Status, CreatedAtUtc, UpdatedAtUtc
)
SELECT src.Id, NULL, src.EntityType, src.Code, src.Name, src.IsDefault, src.IsVisibleInBackoffice,
       src.IsVisibleInStorefront, src.IsBuyable, src.IsSearchable, src.SortOrder, N'Active', @Now, @Now
FROM (
    VALUES
        (@ProductStatusDraft, N'Product', N'DRAFT', N'Draft', 1, 1, 0, 0, 0, 10),
        (@ProductStatusReady, N'Product', N'READY', N'Ready', 0, 1, 1, 1, 1, 20),
        (@ProductStatusComingSoon, N'Product', N'COMING_SOON', N'Coming Soon', 0, 1, 1, 0, 1, 30),
        (@ProductStatusDiscontinued, N'Product', N'DISCONTINUED', N'Discontinued', 0, 1, 0, 0, 0, 40),
        (@VariantStatusDraft, N'Variant', N'DRAFT', N'Draft', 1, 1, 0, 0, 0, 10),
        (@VariantStatusReady, N'Variant', N'READY', N'Ready', 0, 1, 1, 1, 1, 20),
        (@VariantStatusComingSoon, N'Variant', N'COMING_SOON', N'Coming Soon', 0, 1, 1, 0, 1, 30),
        (@VariantStatusDiscontinued, N'Variant', N'DISCONTINUED', N'Discontinued', 0, 1, 0, 0, 0, 40)
) AS src (Id, EntityType, Code, Name, IsDefault, IsVisibleInBackoffice, IsVisibleInStorefront, IsBuyable, IsSearchable, SortOrder)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ProductStatusDefinition psd
    WHERE psd.EntityType = src.EntityType
      AND psd.Code = src.Code
);

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Market
    WHERE Code = N'SE'
)
BEGIN
    INSERT INTO dbo.Market (
        Id, TenantId, Code, Name, DefaultCurrency, DefaultCulture, VatMode, Status, CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        @MarketSe, NULL, N'SE', N'Sweden', 'SEK', N'sv-SE', N'Inclusive', N'Active', @Now, @Now
    );
END;

INSERT INTO dbo.MarketCurrency (Id, MarketId, CurrencyCode, IsDefault)
SELECT src.Id, @MarketSe, src.CurrencyCode, src.IsDefault
FROM (
    VALUES
        (@MarketCurrencySek, 'SEK', 1),
        (@MarketCurrencyEur, 'EUR', 0)
) AS src (Id, CurrencyCode, IsDefault)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.MarketCurrency mc
    WHERE mc.MarketId = @MarketSe
      AND mc.CurrencyCode = src.CurrencyCode
);

INSERT INTO dbo.MarketCulture (Id, MarketId, CultureCode, IsDefault)
SELECT src.Id, @MarketSe, src.CultureCode, src.IsDefault
FROM (
    VALUES
        (@MarketCultureSvSe, N'sv-SE', 1),
        (@MarketCultureEnGb, N'en-GB', 0)
) AS src (Id, CultureCode, IsDefault)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.MarketCulture mc
    WHERE mc.MarketId = @MarketSe
      AND mc.CultureCode = src.CultureCode
);

IF NOT EXISTS (
    SELECT 1
    FROM dbo.PriceList
    WHERE Code = N'SE_BASE_GROSS'
)
BEGIN
    INSERT INTO dbo.PriceList (
        Id, TenantId, Code, Name, CurrencyCode, VatIncluded, Status,
        ValidFromUtc, ValidToUtc, CompanyId, CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        @PriceListSeBase, NULL, N'SE_BASE_GROSS', N'SE Base Gross', 'SEK', 1, N'Active',
        NULL, NULL, NULL, @Now, @Now
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.MarketPriceList
    WHERE MarketId = @MarketSe AND PriceListId = @PriceListSeBase
)
BEGIN
    INSERT INTO dbo.MarketPriceList (
        Id, MarketId, PriceListId, Priority, IsBasePriceList
    )
    VALUES (
        @MarketPriceListSeBase, @MarketSe, @PriceListSeBase, 0, 1
    );
END;
