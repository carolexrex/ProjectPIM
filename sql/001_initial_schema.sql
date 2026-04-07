SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.ProductStatusDefinition (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    EntityType nvarchar(32) NOT NULL,
    Code nvarchar(64) NOT NULL,
    Name nvarchar(128) NOT NULL,
    IsDefault bit NOT NULL CONSTRAINT DF_ProductStatusDefinition_IsDefault DEFAULT (0),
    IsVisibleInBackoffice bit NOT NULL CONSTRAINT DF_ProductStatusDefinition_IsVisibleInBackoffice DEFAULT (1),
    IsVisibleInStorefront bit NOT NULL CONSTRAINT DF_ProductStatusDefinition_IsVisibleInStorefront DEFAULT (1),
    IsBuyable bit NOT NULL CONSTRAINT DF_ProductStatusDefinition_IsBuyable DEFAULT (0),
    IsSearchable bit NOT NULL CONSTRAINT DF_ProductStatusDefinition_IsSearchable DEFAULT (1),
    SortOrder int NOT NULL CONSTRAINT DF_ProductStatusDefinition_SortOrder DEFAULT (0),
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_ProductStatusDefinition PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_ProductStatusDefinition_EntityType CHECK (EntityType IN (N'Product', N'Variant')),
    CONSTRAINT UQ_ProductStatusDefinition_Tenant_EntityType_Code UNIQUE (TenantId, EntityType, Code)
);
GO

CREATE TABLE dbo.MediaAsset (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    StorageProvider nvarchar(64) NOT NULL,
    StorageKey nvarchar(512) NOT NULL,
    FileName nvarchar(256) NOT NULL,
    ContentType nvarchar(128) NOT NULL,
    FileSize bigint NOT NULL,
    Width int NULL,
    Height int NULL,
    AltText nvarchar(256) NULL,
    Title nvarchar(256) NULL,
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_MediaAsset PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE TABLE dbo.Brand (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    Code nvarchar(64) NOT NULL,
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Brand PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Brand_Tenant_Code UNIQUE (TenantId, Code)
);
GO

CREATE TABLE dbo.BrandTranslation (
    Id uniqueidentifier NOT NULL,
    BrandId uniqueidentifier NOT NULL,
    CultureCode nvarchar(16) NOT NULL,
    Name nvarchar(256) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_BrandTranslation PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_BrandTranslation_Brand_Culture UNIQUE (BrandId, CultureCode),
    CONSTRAINT FK_BrandTranslation_Brand FOREIGN KEY (BrandId) REFERENCES dbo.Brand(Id)
);
GO

CREATE TABLE dbo.Market (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    Code nvarchar(64) NOT NULL,
    Name nvarchar(256) NOT NULL,
    DefaultCurrency char(3) NOT NULL,
    DefaultCulture nvarchar(16) NOT NULL,
    VatMode nvarchar(32) NOT NULL,
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Market PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Market_Tenant_Code UNIQUE (TenantId, Code)
);
GO

CREATE TABLE dbo.MarketCurrency (
    Id uniqueidentifier NOT NULL,
    MarketId uniqueidentifier NOT NULL,
    CurrencyCode char(3) NOT NULL,
    IsDefault bit NOT NULL CONSTRAINT DF_MarketCurrency_IsDefault DEFAULT (0),
    CONSTRAINT PK_MarketCurrency PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_MarketCurrency_Market_Currency UNIQUE (MarketId, CurrencyCode),
    CONSTRAINT FK_MarketCurrency_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id)
);
GO

CREATE TABLE dbo.MarketCulture (
    Id uniqueidentifier NOT NULL,
    MarketId uniqueidentifier NOT NULL,
    CultureCode nvarchar(16) NOT NULL,
    IsDefault bit NOT NULL CONSTRAINT DF_MarketCulture_IsDefault DEFAULT (0),
    CONSTRAINT PK_MarketCulture PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_MarketCulture_Market_Culture UNIQUE (MarketId, CultureCode),
    CONSTRAINT FK_MarketCulture_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id)
);
GO

CREATE TABLE dbo.Category (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    ExternalId nvarchar(128) NULL,
    Code nvarchar(64) NOT NULL,
    ParentCategoryId uniqueidentifier NULL,
    SortOrder int NOT NULL CONSTRAINT DF_Category_SortOrder DEFAULT (0),
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Category PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Category_Tenant_Code UNIQUE (TenantId, Code),
    CONSTRAINT FK_Category_ParentCategory FOREIGN KEY (ParentCategoryId) REFERENCES dbo.Category(Id)
);
GO

CREATE TABLE dbo.CategoryTranslation (
    Id uniqueidentifier NOT NULL,
    CategoryId uniqueidentifier NOT NULL,
    CultureCode nvarchar(16) NOT NULL,
    Name nvarchar(256) NOT NULL,
    Slug nvarchar(256) NOT NULL,
    Description nvarchar(1024) NULL,
    CONSTRAINT PK_CategoryTranslation PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_CategoryTranslation_Category_Culture UNIQUE (CategoryId, CultureCode),
    CONSTRAINT FK_CategoryTranslation_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category(Id)
);
GO

CREATE TABLE dbo.Product (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    ExternalId nvarchar(128) NULL,
    ProductType nvarchar(64) NOT NULL,
    ProductNumber nvarchar(64) NOT NULL,
    Slug nvarchar(256) NOT NULL,
    BrandId uniqueidentifier NULL,
    ProductStatusDefinitionId uniqueidentifier NOT NULL,
    TaxCategoryCode nvarchar(64) NOT NULL,
    UnitOfMeasure nvarchar(32) NOT NULL,
    PrimaryImageMediaAssetId uniqueidentifier NULL,
    HasVariants bit NOT NULL CONSTRAINT DF_Product_HasVariants DEFAULT (1),
    Weight decimal(18,4) NULL,
    Length decimal(18,4) NULL,
    Width decimal(18,4) NULL,
    Height decimal(18,4) NULL,
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    IsDeleted bit NOT NULL CONSTRAINT DF_Product_IsDeleted DEFAULT (0),
    CONSTRAINT PK_Product PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Product_Tenant_ProductNumber UNIQUE (TenantId, ProductNumber),
    CONSTRAINT UQ_Product_Tenant_Slug UNIQUE (TenantId, Slug),
    CONSTRAINT FK_Product_Brand FOREIGN KEY (BrandId) REFERENCES dbo.Brand(Id),
    CONSTRAINT FK_Product_ProductStatusDefinition FOREIGN KEY (ProductStatusDefinitionId) REFERENCES dbo.ProductStatusDefinition(Id),
    CONSTRAINT FK_Product_PrimaryImageMediaAsset FOREIGN KEY (PrimaryImageMediaAssetId) REFERENCES dbo.MediaAsset(Id)
);
GO

CREATE TABLE dbo.ProductTranslation (
    Id uniqueidentifier NOT NULL,
    ProductId uniqueidentifier NOT NULL,
    CultureCode nvarchar(16) NOT NULL,
    Name nvarchar(256) NOT NULL,
    ShortDescription nvarchar(1024) NULL,
    LongDescription nvarchar(max) NULL,
    SeoTitle nvarchar(256) NULL,
    SeoDescription nvarchar(512) NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_ProductTranslation PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_ProductTranslation_Product_Culture UNIQUE (ProductId, CultureCode),
    CONSTRAINT FK_ProductTranslation_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(Id)
);
GO

CREATE TABLE dbo.Variant (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    ProductId uniqueidentifier NOT NULL,
    ExternalId nvarchar(128) NULL,
    Sku nvarchar(64) NOT NULL,
    Ean nvarchar(64) NULL,
    Mpn nvarchar(64) NULL,
    Barcode nvarchar(64) NULL,
    ProductStatusDefinitionId uniqueidentifier NOT NULL,
    PrimaryImageMediaAssetId uniqueidentifier NULL,
    IsDefaultVariant bit NOT NULL CONSTRAINT DF_Variant_IsDefaultVariant DEFAULT (0),
    Weight decimal(18,4) NULL,
    Length decimal(18,4) NULL,
    Width decimal(18,4) NULL,
    Height decimal(18,4) NULL,
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    IsDeleted bit NOT NULL CONSTRAINT DF_Variant_IsDeleted DEFAULT (0),
    CONSTRAINT PK_Variant PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Variant_Tenant_Sku UNIQUE (TenantId, Sku),
    CONSTRAINT FK_Variant_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(Id),
    CONSTRAINT FK_Variant_ProductStatusDefinition FOREIGN KEY (ProductStatusDefinitionId) REFERENCES dbo.ProductStatusDefinition(Id),
    CONSTRAINT FK_Variant_PrimaryImageMediaAsset FOREIGN KEY (PrimaryImageMediaAssetId) REFERENCES dbo.MediaAsset(Id)
);
GO

CREATE TABLE dbo.ProductAttribute (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    Code nvarchar(64) NOT NULL,
    Name nvarchar(128) NOT NULL,
    Scope nvarchar(32) NOT NULL,
    DataType nvarchar(32) NOT NULL,
    IsVariantDefining bit NOT NULL CONSTRAINT DF_ProductAttribute_IsVariantDefining DEFAULT (0),
    IsFilterable bit NOT NULL CONSTRAINT DF_ProductAttribute_IsFilterable DEFAULT (0),
    IsRequired bit NOT NULL CONSTRAINT DF_ProductAttribute_IsRequired DEFAULT (0),
    SortOrder int NOT NULL CONSTRAINT DF_ProductAttribute_SortOrder DEFAULT (0),
    CONSTRAINT PK_ProductAttribute PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_ProductAttribute_Tenant_Code UNIQUE (TenantId, Code)
);
GO

CREATE TABLE dbo.AttributeOption (
    Id uniqueidentifier NOT NULL,
    ProductAttributeId uniqueidentifier NOT NULL,
    Code nvarchar(64) NOT NULL,
    Value nvarchar(128) NOT NULL,
    SortOrder int NOT NULL CONSTRAINT DF_AttributeOption_SortOrder DEFAULT (0),
    CONSTRAINT PK_AttributeOption PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_AttributeOption_Attribute_Code UNIQUE (ProductAttributeId, Code),
    CONSTRAINT FK_AttributeOption_ProductAttribute FOREIGN KEY (ProductAttributeId) REFERENCES dbo.ProductAttribute(Id)
);
GO

CREATE TABLE dbo.VariantAttributeValue (
    Id uniqueidentifier NOT NULL,
    VariantId uniqueidentifier NOT NULL,
    ProductAttributeId uniqueidentifier NOT NULL,
    AttributeOptionId uniqueidentifier NULL,
    ValueText nvarchar(256) NULL,
    CONSTRAINT PK_VariantAttributeValue PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_VariantAttributeValue_Variant FOREIGN KEY (VariantId) REFERENCES dbo.Variant(Id),
    CONSTRAINT FK_VariantAttributeValue_ProductAttribute FOREIGN KEY (ProductAttributeId) REFERENCES dbo.ProductAttribute(Id),
    CONSTRAINT FK_VariantAttributeValue_AttributeOption FOREIGN KEY (AttributeOptionId) REFERENCES dbo.AttributeOption(Id)
);
GO

CREATE TABLE dbo.ProductCategory (
    ProductId uniqueidentifier NOT NULL,
    CategoryId uniqueidentifier NOT NULL,
    IsPrimary bit NOT NULL CONSTRAINT DF_ProductCategory_IsPrimary DEFAULT (0),
    SortOrder int NOT NULL CONSTRAINT DF_ProductCategory_SortOrder DEFAULT (0),
    CONSTRAINT PK_ProductCategory PRIMARY KEY CLUSTERED (ProductId, CategoryId),
    CONSTRAINT FK_ProductCategory_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(Id),
    CONSTRAINT FK_ProductCategory_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category(Id)
);
GO

CREATE TABLE dbo.ProductMedia (
    Id uniqueidentifier NOT NULL,
    ProductId uniqueidentifier NOT NULL,
    MediaAssetId uniqueidentifier NOT NULL,
    Type nvarchar(32) NOT NULL,
    SortOrder int NOT NULL CONSTRAINT DF_ProductMedia_SortOrder DEFAULT (0),
    IsPrimary bit NOT NULL CONSTRAINT DF_ProductMedia_IsPrimary DEFAULT (0),
    CultureCode nvarchar(16) NULL,
    MarketId uniqueidentifier NULL,
    CONSTRAINT PK_ProductMedia PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_ProductMedia_Product_Media_Type_Culture_Market UNIQUE (ProductId, MediaAssetId, Type, CultureCode, MarketId),
    CONSTRAINT FK_ProductMedia_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(Id),
    CONSTRAINT FK_ProductMedia_MediaAsset FOREIGN KEY (MediaAssetId) REFERENCES dbo.MediaAsset(Id),
    CONSTRAINT FK_ProductMedia_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id)
);
GO

CREATE TABLE dbo.VariantMedia (
    Id uniqueidentifier NOT NULL,
    VariantId uniqueidentifier NOT NULL,
    MediaAssetId uniqueidentifier NOT NULL,
    Type nvarchar(32) NOT NULL,
    SortOrder int NOT NULL CONSTRAINT DF_VariantMedia_SortOrder DEFAULT (0),
    IsPrimary bit NOT NULL CONSTRAINT DF_VariantMedia_IsPrimary DEFAULT (0),
    CONSTRAINT PK_VariantMedia PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_VariantMedia_Variant_Media_Type UNIQUE (VariantId, MediaAssetId, Type),
    CONSTRAINT FK_VariantMedia_Variant FOREIGN KEY (VariantId) REFERENCES dbo.Variant(Id),
    CONSTRAINT FK_VariantMedia_MediaAsset FOREIGN KEY (MediaAssetId) REFERENCES dbo.MediaAsset(Id)
);
GO

CREATE TABLE dbo.MarketCatalogAssignment (
    Id uniqueidentifier NOT NULL,
    MarketId uniqueidentifier NOT NULL,
    ProductId uniqueidentifier NOT NULL,
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_MarketCatalogAssignment PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_MarketCatalogAssignment_Market_Product UNIQUE (MarketId, ProductId),
    CONSTRAINT FK_MarketCatalogAssignment_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id),
    CONSTRAINT FK_MarketCatalogAssignment_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(Id)
);
GO

CREATE TABLE dbo.Customer (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    ExternalId nvarchar(128) NULL,
    UserId uniqueidentifier NULL,
    Email nvarchar(320) NOT NULL,
    NormalizedEmail nvarchar(320) NOT NULL,
    FirstName nvarchar(128) NOT NULL,
    LastName nvarchar(128) NOT NULL,
    Phone nvarchar(64) NULL,
    PreferredCulture nvarchar(16) NULL,
    DefaultMarketId uniqueidentifier NULL,
    Status nvarchar(32) NOT NULL,
    IsGuest bit NOT NULL CONSTRAINT DF_Customer_IsGuest DEFAULT (0),
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    IsDeleted bit NOT NULL CONSTRAINT DF_Customer_IsDeleted DEFAULT (0),
    CONSTRAINT PK_Customer PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Customer_DefaultMarket FOREIGN KEY (DefaultMarketId) REFERENCES dbo.Market(Id)
);
GO

CREATE TABLE dbo.CustomerAddress (
    Id uniqueidentifier NOT NULL,
    CustomerId uniqueidentifier NOT NULL,
    Type nvarchar(32) NOT NULL,
    Attention nvarchar(128) NULL,
    FirstName nvarchar(128) NOT NULL,
    LastName nvarchar(128) NOT NULL,
    CompanyName nvarchar(256) NULL,
    Line1 nvarchar(256) NOT NULL,
    Line2 nvarchar(256) NULL,
    PostalCode nvarchar(32) NOT NULL,
    City nvarchar(128) NOT NULL,
    Region nvarchar(128) NULL,
    CountryCode char(2) NOT NULL,
    Phone nvarchar(64) NULL,
    Email nvarchar(320) NULL,
    IsDefault bit NOT NULL CONSTRAINT DF_CustomerAddress_IsDefault DEFAULT (0),
    CONSTRAINT PK_CustomerAddress PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CustomerAddress_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id)
);
GO

CREATE TABLE dbo.Company (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    ExternalId nvarchar(128) NULL,
    Code nvarchar(64) NOT NULL,
    Name nvarchar(256) NOT NULL,
    LegalName nvarchar(256) NULL,
    OrganizationNumber nvarchar(64) NULL,
    VatNumber nvarchar(64) NULL,
    Email nvarchar(320) NULL,
    Phone nvarchar(64) NULL,
    DefaultMarketId uniqueidentifier NULL,
    DefaultCurrency char(3) NULL,
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    IsDeleted bit NOT NULL CONSTRAINT DF_Company_IsDeleted DEFAULT (0),
    CONSTRAINT PK_Company PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Company_Tenant_Code UNIQUE (TenantId, Code),
    CONSTRAINT FK_Company_DefaultMarket FOREIGN KEY (DefaultMarketId) REFERENCES dbo.Market(Id)
);
GO

CREATE TABLE dbo.CompanyAddress (
    Id uniqueidentifier NOT NULL,
    CompanyId uniqueidentifier NOT NULL,
    Type nvarchar(32) NOT NULL,
    Attention nvarchar(128) NULL,
    Line1 nvarchar(256) NOT NULL,
    Line2 nvarchar(256) NULL,
    PostalCode nvarchar(32) NOT NULL,
    City nvarchar(128) NOT NULL,
    Region nvarchar(128) NULL,
    CountryCode char(2) NOT NULL,
    Email nvarchar(320) NULL,
    Phone nvarchar(64) NULL,
    IsDefault bit NOT NULL CONSTRAINT DF_CompanyAddress_IsDefault DEFAULT (0),
    CONSTRAINT PK_CompanyAddress PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CompanyAddress_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company(Id)
);
GO

CREATE TABLE dbo.CompanyMembership (
    Id uniqueidentifier NOT NULL,
    CompanyId uniqueidentifier NOT NULL,
    CustomerId uniqueidentifier NOT NULL,
    Role nvarchar(64) NOT NULL,
    Status nvarchar(32) NOT NULL,
    IsDefaultCompany bit NOT NULL CONSTRAINT DF_CompanyMembership_IsDefaultCompany DEFAULT (0),
    CanPlaceOrders bit NOT NULL CONSTRAINT DF_CompanyMembership_CanPlaceOrders DEFAULT (0),
    CanApproveOrders bit NOT NULL CONSTRAINT DF_CompanyMembership_CanApproveOrders DEFAULT (0),
    CanManageUsers bit NOT NULL CONSTRAINT DF_CompanyMembership_CanManageUsers DEFAULT (0),
    ValidFromUtc datetime2 NULL,
    ValidToUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_CompanyMembership PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_CompanyMembership_Company_Customer UNIQUE (CompanyId, CustomerId),
    CONSTRAINT FK_CompanyMembership_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company(Id),
    CONSTRAINT FK_CompanyMembership_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id)
);
GO

CREATE TABLE dbo.PriceList (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    Code nvarchar(64) NOT NULL,
    Name nvarchar(256) NOT NULL,
    CurrencyCode char(3) NOT NULL,
    VatIncluded bit NOT NULL CONSTRAINT DF_PriceList_VatIncluded DEFAULT (0),
    Status nvarchar(32) NOT NULL,
    ValidFromUtc datetime2 NULL,
    ValidToUtc datetime2 NULL,
    CompanyId uniqueidentifier NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_PriceList PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_PriceList_Tenant_Code UNIQUE (TenantId, Code),
    CONSTRAINT FK_PriceList_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company(Id)
);
GO

CREATE TABLE dbo.MarketPriceList (
    Id uniqueidentifier NOT NULL,
    MarketId uniqueidentifier NOT NULL,
    PriceListId uniqueidentifier NOT NULL,
    Priority int NOT NULL CONSTRAINT DF_MarketPriceList_Priority DEFAULT (0),
    IsBasePriceList bit NOT NULL CONSTRAINT DF_MarketPriceList_IsBasePriceList DEFAULT (0),
    CONSTRAINT PK_MarketPriceList PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_MarketPriceList_Market_PriceList UNIQUE (MarketId, PriceListId),
    CONSTRAINT FK_MarketPriceList_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id),
    CONSTRAINT FK_MarketPriceList_PriceList FOREIGN KEY (PriceListId) REFERENCES dbo.PriceList(Id)
);
GO

CREATE TABLE dbo.PriceListEntry (
    Id uniqueidentifier NOT NULL,
    PriceListId uniqueidentifier NOT NULL,
    TargetType nvarchar(32) NOT NULL,
    TargetId uniqueidentifier NOT NULL,
    MinQuantity decimal(18,4) NOT NULL,
    Amount decimal(18,4) NOT NULL,
    CompareAtAmount decimal(18,4) NULL,
    ValidFromUtc datetime2 NULL,
    ValidToUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_PriceListEntry PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_PriceListEntry_MinQuantity CHECK (MinQuantity > 0),
    CONSTRAINT CK_PriceListEntry_TargetType CHECK (TargetType IN (N'Variant')),
    CONSTRAINT FK_PriceListEntry_PriceList FOREIGN KEY (PriceListId) REFERENCES dbo.PriceList(Id)
);
GO

CREATE TABLE dbo.InventoryLocation (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    Code nvarchar(64) NOT NULL,
    Name nvarchar(256) NOT NULL,
    Type nvarchar(32) NOT NULL,
    Status nvarchar(32) NOT NULL,
    CountryCode char(2) NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_InventoryLocation PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_InventoryLocation_Tenant_Code UNIQUE (TenantId, Code)
);
GO

CREATE TABLE dbo.MarketInventoryLocation (
    Id uniqueidentifier NOT NULL,
    MarketId uniqueidentifier NOT NULL,
    InventoryLocationId uniqueidentifier NOT NULL,
    Priority int NOT NULL CONSTRAINT DF_MarketInventoryLocation_Priority DEFAULT (0),
    CONSTRAINT PK_MarketInventoryLocation PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_MarketInventoryLocation_Market_InventoryLocation UNIQUE (MarketId, InventoryLocationId),
    CONSTRAINT FK_MarketInventoryLocation_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id),
    CONSTRAINT FK_MarketInventoryLocation_InventoryLocation FOREIGN KEY (InventoryLocationId) REFERENCES dbo.InventoryLocation(Id)
);
GO

CREATE TABLE dbo.InventoryBalance (
    Id uniqueidentifier NOT NULL,
    InventoryLocationId uniqueidentifier NOT NULL,
    VariantId uniqueidentifier NOT NULL,
    OnHandQuantity decimal(18,4) NOT NULL,
    ReservedQuantity decimal(18,4) NOT NULL,
    IncomingQuantity decimal(18,4) NOT NULL,
    Backorderable bit NOT NULL CONSTRAINT DF_InventoryBalance_Backorderable DEFAULT (0),
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_InventoryBalance PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_InventoryBalance_Location_Variant UNIQUE (InventoryLocationId, VariantId),
    CONSTRAINT FK_InventoryBalance_InventoryLocation FOREIGN KEY (InventoryLocationId) REFERENCES dbo.InventoryLocation(Id),
    CONSTRAINT FK_InventoryBalance_Variant FOREIGN KEY (VariantId) REFERENCES dbo.Variant(Id)
);
GO

CREATE TABLE dbo.InventoryReservation (
    Id uniqueidentifier NOT NULL,
    InventoryLocationId uniqueidentifier NOT NULL,
    VariantId uniqueidentifier NOT NULL,
    CartId uniqueidentifier NULL,
    OrderId uniqueidentifier NULL,
    Quantity decimal(18,4) NOT NULL,
    Status nvarchar(32) NOT NULL,
    ExpiresAtUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_InventoryReservation PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_InventoryReservation_InventoryLocation FOREIGN KEY (InventoryLocationId) REFERENCES dbo.InventoryLocation(Id),
    CONSTRAINT FK_InventoryReservation_Variant FOREIGN KEY (VariantId) REFERENCES dbo.Variant(Id)
);
GO

CREATE TABLE dbo.InventoryTransaction (
    Id uniqueidentifier NOT NULL,
    InventoryLocationId uniqueidentifier NOT NULL,
    VariantId uniqueidentifier NOT NULL,
    Type nvarchar(32) NOT NULL,
    QuantityDelta decimal(18,4) NOT NULL,
    ReferenceType nvarchar(32) NOT NULL,
    ReferenceId uniqueidentifier NOT NULL,
    OccurredAtUtc datetime2 NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_InventoryTransaction PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_InventoryTransaction_InventoryLocation FOREIGN KEY (InventoryLocationId) REFERENCES dbo.InventoryLocation(Id),
    CONSTRAINT FK_InventoryTransaction_Variant FOREIGN KEY (VariantId) REFERENCES dbo.Variant(Id)
);
GO

CREATE TABLE dbo.Cart (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    CartNumber nvarchar(64) NOT NULL,
    Status nvarchar(32) NOT NULL,
    CustomerId uniqueidentifier NULL,
    CompanyId uniqueidentifier NULL,
    MarketId uniqueidentifier NOT NULL,
    CurrencyCode char(3) NOT NULL,
    CultureCode nvarchar(16) NOT NULL,
    Email nvarchar(320) NULL,
    SelectedPaymentMethod nvarchar(64) NULL,
    SelectedShippingMethod nvarchar(64) NULL,
    PriceListContext nvarchar(256) NULL,
    ExpiresAtUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Cart PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Cart_Tenant_CartNumber UNIQUE (TenantId, CartNumber),
    CONSTRAINT FK_Cart_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id),
    CONSTRAINT FK_Cart_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company(Id),
    CONSTRAINT FK_Cart_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id)
);
GO

CREATE TABLE dbo.CartAddress (
    Id uniqueidentifier NOT NULL,
    CartId uniqueidentifier NOT NULL,
    Type nvarchar(32) NOT NULL,
    FirstName nvarchar(128) NOT NULL,
    LastName nvarchar(128) NOT NULL,
    CompanyName nvarchar(256) NULL,
    Line1 nvarchar(256) NOT NULL,
    Line2 nvarchar(256) NULL,
    PostalCode nvarchar(32) NOT NULL,
    City nvarchar(128) NOT NULL,
    Region nvarchar(128) NULL,
    CountryCode char(2) NOT NULL,
    Email nvarchar(320) NULL,
    Phone nvarchar(64) NULL,
    CONSTRAINT PK_CartAddress PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CartAddress_Cart FOREIGN KEY (CartId) REFERENCES dbo.Cart(Id)
);
GO

CREATE TABLE dbo.CartLine (
    Id uniqueidentifier NOT NULL,
    CartId uniqueidentifier NOT NULL,
    VariantId uniqueidentifier NOT NULL,
    Quantity decimal(18,4) NOT NULL,
    UnitPrice decimal(18,4) NOT NULL,
    VatRate decimal(9,4) NOT NULL,
    LineTotal decimal(18,4) NOT NULL,
    Comment nvarchar(512) NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_CartLine PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CartLine_Cart FOREIGN KEY (CartId) REFERENCES dbo.Cart(Id),
    CONSTRAINT FK_CartLine_Variant FOREIGN KEY (VariantId) REFERENCES dbo.Variant(Id)
);
GO

CREATE TABLE dbo.[Order] (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    OrderNumber nvarchar(64) NOT NULL,
    Status nvarchar(32) NOT NULL,
    CustomerId uniqueidentifier NULL,
    CompanyId uniqueidentifier NULL,
    MarketId uniqueidentifier NOT NULL,
    CurrencyCode char(3) NOT NULL,
    CultureCode nvarchar(16) NOT NULL,
    Email nvarchar(320) NOT NULL,
    Subtotal decimal(18,4) NOT NULL,
    VatTotal decimal(18,4) NOT NULL,
    GrandTotal decimal(18,4) NOT NULL,
    PlacedAtUtc datetime2 NOT NULL,
    PaymentStatus nvarchar(32) NOT NULL,
    FulfillmentStatus nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Order PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Order_Tenant_OrderNumber UNIQUE (TenantId, OrderNumber),
    CONSTRAINT FK_Order_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id),
    CONSTRAINT FK_Order_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company(Id),
    CONSTRAINT FK_Order_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id)
);
GO

ALTER TABLE dbo.InventoryReservation
ADD CONSTRAINT FK_InventoryReservation_Cart FOREIGN KEY (CartId) REFERENCES dbo.Cart(Id);
GO

ALTER TABLE dbo.InventoryReservation
ADD CONSTRAINT FK_InventoryReservation_Order FOREIGN KEY (OrderId) REFERENCES dbo.[Order](Id);
GO

CREATE TABLE dbo.OrderAddress (
    Id uniqueidentifier NOT NULL,
    OrderId uniqueidentifier NOT NULL,
    Type nvarchar(32) NOT NULL,
    FirstName nvarchar(128) NOT NULL,
    LastName nvarchar(128) NOT NULL,
    CompanyName nvarchar(256) NULL,
    Line1 nvarchar(256) NOT NULL,
    Line2 nvarchar(256) NULL,
    PostalCode nvarchar(32) NOT NULL,
    City nvarchar(128) NOT NULL,
    Region nvarchar(128) NULL,
    CountryCode char(2) NOT NULL,
    Email nvarchar(320) NULL,
    Phone nvarchar(64) NULL,
    CONSTRAINT PK_OrderAddress PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_OrderAddress_Order FOREIGN KEY (OrderId) REFERENCES dbo.[Order](Id)
);
GO

CREATE TABLE dbo.OrderLine (
    Id uniqueidentifier NOT NULL,
    OrderId uniqueidentifier NOT NULL,
    VariantId uniqueidentifier NOT NULL,
    Sku nvarchar(64) NOT NULL,
    ProductName nvarchar(256) NOT NULL,
    VariantDescription nvarchar(256) NULL,
    Quantity decimal(18,4) NOT NULL,
    UnitPrice decimal(18,4) NOT NULL,
    VatRate decimal(9,4) NOT NULL,
    LineTotal decimal(18,4) NOT NULL,
    CONSTRAINT PK_OrderLine PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_OrderLine_Order FOREIGN KEY (OrderId) REFERENCES dbo.[Order](Id),
    CONSTRAINT FK_OrderLine_Variant FOREIGN KEY (VariantId) REFERENCES dbo.Variant(Id)
);
GO

CREATE TABLE dbo.OrderStatusHistory (
    Id uniqueidentifier NOT NULL,
    OrderId uniqueidentifier NOT NULL,
    FromStatus nvarchar(32) NULL,
    ToStatus nvarchar(32) NOT NULL,
    ChangedBy nvarchar(128) NOT NULL,
    ChangedAtUtc datetime2 NOT NULL,
    Comment nvarchar(512) NULL,
    CONSTRAINT PK_OrderStatusHistory PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_OrderStatusHistory_Order FOREIGN KEY (OrderId) REFERENCES dbo.[Order](Id)
);
GO

CREATE TABLE dbo.PaymentTransaction (
    Id uniqueidentifier NOT NULL,
    OrderId uniqueidentifier NOT NULL,
    Provider nvarchar(64) NOT NULL,
    ProviderReference nvarchar(128) NOT NULL,
    Type nvarchar(32) NOT NULL,
    Status nvarchar(32) NOT NULL,
    Amount decimal(18,4) NOT NULL,
    CurrencyCode char(3) NOT NULL,
    RequestedAtUtc datetime2 NOT NULL,
    CompletedAtUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_PaymentTransaction PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_PaymentTransaction_Order FOREIGN KEY (OrderId) REFERENCES dbo.[Order](Id)
);
GO

CREATE TABLE dbo.CustomFieldDefinition (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    EntityType nvarchar(64) NOT NULL,
    [Key] nvarchar(64) NOT NULL,
    Label nvarchar(128) NOT NULL,
    DataType nvarchar(32) NOT NULL,
    IsRequired bit NOT NULL CONSTRAINT DF_CustomFieldDefinition_IsRequired DEFAULT (0),
    IsLocalized bit NOT NULL CONSTRAINT DF_CustomFieldDefinition_IsLocalized DEFAULT (0),
    IsMarketScoped bit NOT NULL CONSTRAINT DF_CustomFieldDefinition_IsMarketScoped DEFAULT (0),
    ValidationJson nvarchar(max) NULL,
    DefaultValue nvarchar(max) NULL,
    IsSearchable bit NOT NULL CONSTRAINT DF_CustomFieldDefinition_IsSearchable DEFAULT (0),
    Status nvarchar(32) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_CustomFieldDefinition PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_CustomFieldDefinition_Tenant_EntityType_Key UNIQUE (TenantId, EntityType, [Key])
);
GO

CREATE TABLE dbo.CustomFieldValue (
    Id uniqueidentifier NOT NULL,
    CustomFieldDefinitionId uniqueidentifier NOT NULL,
    EntityId uniqueidentifier NOT NULL,
    CultureCode nvarchar(16) NULL,
    MarketId uniqueidentifier NULL,
    ValueJson nvarchar(max) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_CustomFieldValue PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CustomFieldValue_CustomFieldDefinition FOREIGN KEY (CustomFieldDefinitionId) REFERENCES dbo.CustomFieldDefinition(Id),
    CONSTRAINT FK_CustomFieldValue_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id)
);
GO

CREATE TABLE dbo.WebhookSubscription (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    Name nvarchar(128) NOT NULL,
    EndpointUrl nvarchar(2048) NOT NULL,
    Secret nvarchar(256) NOT NULL,
    EventTypes nvarchar(max) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_WebhookSubscription_IsActive DEFAULT (1),
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_WebhookSubscription PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE TABLE dbo.WebhookDelivery (
    Id uniqueidentifier NOT NULL,
    WebhookSubscriptionId uniqueidentifier NOT NULL,
    EventId uniqueidentifier NOT NULL,
    Status nvarchar(32) NOT NULL,
    AttemptCount int NOT NULL CONSTRAINT DF_WebhookDelivery_AttemptCount DEFAULT (0),
    LastAttemptAtUtc datetime2 NULL,
    NextAttemptAtUtc datetime2 NULL,
    ResponseCode int NULL,
    ResponseBody nvarchar(max) NULL,
    CreatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_WebhookDelivery PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_WebhookDelivery_WebhookSubscription FOREIGN KEY (WebhookSubscriptionId) REFERENCES dbo.WebhookSubscription(Id)
);
GO

CREATE TABLE dbo.IntegrationJob (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    Type nvarchar(64) NOT NULL,
    Direction nvarchar(32) NOT NULL,
    Status nvarchar(32) NOT NULL,
    RequestedBy nvarchar(128) NOT NULL,
    StartedAtUtc datetime2 NULL,
    CompletedAtUtc datetime2 NULL,
    PayloadReference nvarchar(512) NULL,
    ResultSummary nvarchar(max) NULL,
    CreatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_IntegrationJob PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE TABLE dbo.OutboxMessage (
    Id uniqueidentifier NOT NULL,
    TenantId uniqueidentifier NULL,
    EventType nvarchar(128) NOT NULL,
    AggregateType nvarchar(64) NOT NULL,
    AggregateId uniqueidentifier NOT NULL,
    OccurredAtUtc datetime2 NOT NULL,
    Payload nvarchar(max) NOT NULL,
    PublishedAtUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL,
    CONSTRAINT PK_OutboxMessage PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE INDEX IX_MediaAsset_TenantId_StorageKey ON dbo.MediaAsset (TenantId, StorageKey);
GO
CREATE INDEX IX_ProductStatusDefinition_TenantId_EntityType_IsDefault ON dbo.ProductStatusDefinition (TenantId, EntityType, IsDefault);
GO
CREATE INDEX IX_Product_ProductStatusDefinitionId ON dbo.Product (ProductStatusDefinitionId);
GO
CREATE INDEX IX_Product_BrandId ON dbo.Product (BrandId);
GO
CREATE INDEX IX_Product_PrimaryImageMediaAssetId ON dbo.Product (PrimaryImageMediaAssetId);
GO
CREATE INDEX IX_ProductTranslation_CultureCode_Name ON dbo.ProductTranslation (CultureCode, Name);
GO
CREATE INDEX IX_Variant_ProductId ON dbo.Variant (ProductId);
GO
CREATE INDEX IX_Variant_ProductStatusDefinitionId ON dbo.Variant (ProductStatusDefinitionId);
GO
CREATE INDEX IX_Variant_Ean ON dbo.Variant (Ean);
GO
CREATE INDEX IX_VariantAttributeValue_VariantId ON dbo.VariantAttributeValue (VariantId);
GO
CREATE INDEX IX_Category_ParentCategoryId_SortOrder ON dbo.Category (ParentCategoryId, SortOrder);
GO
CREATE INDEX IX_ProductCategory_CategoryId_SortOrder ON dbo.ProductCategory (CategoryId, SortOrder);
GO
CREATE INDEX IX_ProductMedia_ProductId_SortOrder ON dbo.ProductMedia (ProductId, SortOrder);
GO
CREATE INDEX IX_VariantMedia_VariantId_SortOrder ON dbo.VariantMedia (VariantId, SortOrder);
GO
CREATE INDEX IX_MarketCatalogAssignment_MarketId_Status ON dbo.MarketCatalogAssignment (MarketId, Status);
GO
CREATE INDEX IX_Customer_TenantId_NormalizedEmail ON dbo.Customer (TenantId, NormalizedEmail);
GO
CREATE INDEX IX_CompanyMembership_CustomerId_Status ON dbo.CompanyMembership (CustomerId, Status);
GO
CREATE INDEX IX_CompanyMembership_CompanyId_Status ON dbo.CompanyMembership (CompanyId, Status);
GO
CREATE INDEX IX_MarketPriceList_MarketId_Priority ON dbo.MarketPriceList (MarketId, Priority);
GO
CREATE INDEX IX_PriceListEntry_PriceListId_TargetType_TargetId_MinQuantity ON dbo.PriceListEntry (PriceListId, TargetType, TargetId, MinQuantity);
GO
CREATE INDEX IX_MarketInventoryLocation_MarketId_Priority ON dbo.MarketInventoryLocation (MarketId, Priority);
GO
CREATE INDEX IX_InventoryBalance_VariantId ON dbo.InventoryBalance (VariantId);
GO
CREATE INDEX IX_InventoryReservation_CartId_Status ON dbo.InventoryReservation (CartId, Status);
GO
CREATE INDEX IX_InventoryReservation_OrderId_Status ON dbo.InventoryReservation (OrderId, Status);
GO
CREATE INDEX IX_InventoryTransaction_VariantId_OccurredAtUtc ON dbo.InventoryTransaction (VariantId, OccurredAtUtc);
GO
CREATE INDEX IX_Cart_CustomerId_Status ON dbo.Cart (CustomerId, Status);
GO
CREATE INDEX IX_Cart_CompanyId_Status ON dbo.Cart (CompanyId, Status);
GO
CREATE INDEX IX_Cart_MarketId_Status ON dbo.Cart (MarketId, Status);
GO
CREATE INDEX IX_CartLine_CartId ON dbo.CartLine (CartId);
GO
CREATE INDEX IX_Order_CustomerId_PlacedAtUtc ON dbo.[Order] (CustomerId, PlacedAtUtc);
GO
CREATE INDEX IX_Order_CompanyId_PlacedAtUtc ON dbo.[Order] (CompanyId, PlacedAtUtc);
GO
CREATE INDEX IX_Order_MarketId_PlacedAtUtc ON dbo.[Order] (MarketId, PlacedAtUtc);
GO
CREATE INDEX IX_PaymentTransaction_Provider_ProviderReference ON dbo.PaymentTransaction (Provider, ProviderReference);
GO
CREATE INDEX IX_CustomFieldValue_CustomFieldDefinitionId_EntityId ON dbo.CustomFieldValue (CustomFieldDefinitionId, EntityId);
GO
CREATE INDEX IX_WebhookDelivery_WebhookSubscriptionId_Status ON dbo.WebhookDelivery (WebhookSubscriptionId, Status);
GO
CREATE INDEX IX_IntegrationJob_Type_Status ON dbo.IntegrationJob (Type, Status);
GO
CREATE INDEX IX_OutboxMessage_PublishedAtUtc ON dbo.OutboxMessage (PublishedAtUtc);
GO
