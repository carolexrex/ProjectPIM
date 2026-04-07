# SQL Schema v1

## Purpose

This document defines the first relational schema for the platform based on [domain-spec-v1.md](c:\Users\SpotonAlexV\source\repos\Projekt PIM\docs\domain-spec-v1.md).

The schema is written to be:

- SQL Server-friendly first
- portable to PostgreSQL later
- explicit about keys, constraints, and indexes

This is a logical-plus-physical schema guide, not a final migration script.

## Database Choice

Recommended for v1:

- `SQL Server`

Reason:

- you already know it
- it removes avoidable delivery friction
- nothing in the current design requires PostgreSQL-specific features

Keep these portability rules:

- avoid provider-specific SQL unless the gain is clear
- keep JSON usage limited and optional
- avoid stored procedures as core business logic
- keep EF Core mappings provider-neutral where practical

## Naming Conventions

- schema: `dbo`
- primary key column: `Id`
- foreign key columns: `{ReferencedEntity}Id`
- UTC timestamps: `datetime2`
- money values: `decimal(18,4)`
- quantity values: `decimal(18,4)`
- booleans: `bit`
- codes: `nvarchar(64)` unless otherwise noted
- names: `nvarchar(256)` unless otherwise noted
- long text: `nvarchar(max)`

## Cross-Cutting Columns

Most transactional tables should include:

- `Id uniqueidentifier not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Optional where useful:

- `ExternalId nvarchar(128) null`
- `TenantId uniqueidentifier null`
- `IsDeleted bit not null default 0`

## Core Tables

## ProductStatusDefinition

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `EntityType nvarchar(32) not null`
- `Code nvarchar(64) not null`
- `Name nvarchar(128) not null`
- `IsDefault bit not null`
- `IsVisibleInBackoffice bit not null`
- `IsVisibleInStorefront bit not null`
- `IsBuyable bit not null`
- `IsSearchable bit not null`
- `SortOrder int not null default 0`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(TenantId, EntityType, Code)`
- check: `EntityType in ('Product','Variant')`

Indexes:

- `(TenantId, EntityType, IsDefault)`
- `(TenantId, EntityType, Status)`

## MediaAsset

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `StorageProvider nvarchar(64) not null`
- `StorageKey nvarchar(512) not null`
- `FileName nvarchar(256) not null`
- `ContentType nvarchar(128) not null`
- `FileSize bigint not null`
- `Width int null`
- `Height int null`
- `AltText nvarchar(256) null`
- `Title nvarchar(256) null`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Indexes:

- `(TenantId, StorageKey)`

## Brand

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `Code nvarchar(64) not null`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(TenantId, Code)`

## BrandTranslation

Columns:

- `Id uniqueidentifier pk`
- `BrandId uniqueidentifier not null fk -> Brand.Id`
- `CultureCode nvarchar(16) not null`
- `Name nvarchar(256) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`

Constraints:

- unique: `(BrandId, CultureCode)`

## Market

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `Code nvarchar(64) not null`
- `Name nvarchar(256) not null`
- `DefaultCurrency char(3) not null`
- `DefaultCulture nvarchar(16) not null`
- `VatMode nvarchar(32) not null`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(TenantId, Code)`

## MarketCurrency

Columns:

- `Id uniqueidentifier pk`
- `MarketId uniqueidentifier not null fk -> Market.Id`
- `CurrencyCode char(3) not null`
- `IsDefault bit not null`

Constraints:

- unique: `(MarketId, CurrencyCode)`

## MarketCulture

Columns:

- `Id uniqueidentifier pk`
- `MarketId uniqueidentifier not null fk -> Market.Id`
- `CultureCode nvarchar(16) not null`
- `IsDefault bit not null`

Constraints:

- unique: `(MarketId, CultureCode)`

## Product

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `ExternalId nvarchar(128) null`
- `ProductType nvarchar(64) not null`
- `ProductNumber nvarchar(64) not null`
- `Slug nvarchar(256) not null`
- `BrandId uniqueidentifier null fk -> Brand.Id`
- `ProductStatusDefinitionId uniqueidentifier not null fk -> ProductStatusDefinition.Id`
- `TaxCategoryCode nvarchar(64) not null`
- `UnitOfMeasure nvarchar(32) not null`
- `PrimaryImageMediaAssetId uniqueidentifier null fk -> MediaAsset.Id`
- `HasVariants bit not null`
- `Weight decimal(18,4) null`
- `Length decimal(18,4) null`
- `Width decimal(18,4) null`
- `Height decimal(18,4) null`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`
- `IsDeleted bit not null default 0`

Constraints:

- unique: `(TenantId, ProductNumber)`
- unique: `(TenantId, Slug)`

Indexes:

- `(TenantId, ProductStatusDefinitionId)`
- `(BrandId)`
- `(PrimaryImageMediaAssetId)`

## ProductTranslation

Columns:

- `Id uniqueidentifier pk`
- `ProductId uniqueidentifier not null fk -> Product.Id`
- `CultureCode nvarchar(16) not null`
- `Name nvarchar(256) not null`
- `ShortDescription nvarchar(1024) null`
- `LongDescription nvarchar(max) null`
- `SeoTitle nvarchar(256) null`
- `SeoDescription nvarchar(512) null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`

Constraints:

- unique: `(ProductId, CultureCode)`

## Variant

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `ProductId uniqueidentifier not null fk -> Product.Id`
- `ExternalId nvarchar(128) null`
- `Sku nvarchar(64) not null`
- `Ean nvarchar(64) null`
- `Mpn nvarchar(64) null`
- `Barcode nvarchar(64) null`
- `ProductStatusDefinitionId uniqueidentifier not null fk -> ProductStatusDefinition.Id`
- `PrimaryImageMediaAssetId uniqueidentifier null fk -> MediaAsset.Id`
- `IsDefaultVariant bit not null`
- `Weight decimal(18,4) null`
- `Length decimal(18,4) null`
- `Width decimal(18,4) null`
- `Height decimal(18,4) null`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`
- `IsDeleted bit not null default 0`

Constraints:

- unique: `(TenantId, Sku)`

Indexes:

- `(ProductId)`
- `(TenantId, ProductStatusDefinitionId)`
- `(Ean)`

## ProductAttribute

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `Code nvarchar(64) not null`
- `Name nvarchar(128) not null`
- `Scope nvarchar(32) not null`
- `DataType nvarchar(32) not null`
- `IsVariantDefining bit not null`
- `IsFilterable bit not null`
- `IsRequired bit not null`
- `SortOrder int not null default 0`

Constraints:

- unique: `(TenantId, Code)`

## AttributeOption

Columns:

- `Id uniqueidentifier pk`
- `ProductAttributeId uniqueidentifier not null fk -> ProductAttribute.Id`
- `Code nvarchar(64) not null`
- `Value nvarchar(128) not null`
- `SortOrder int not null default 0`

Constraints:

- unique: `(ProductAttributeId, Code)`

## VariantAttributeValue

Columns:

- `Id uniqueidentifier pk`
- `VariantId uniqueidentifier not null fk -> Variant.Id`
- `ProductAttributeId uniqueidentifier not null fk -> ProductAttribute.Id`
- `AttributeOptionId uniqueidentifier null fk -> AttributeOption.Id`
- `ValueText nvarchar(256) null`

Indexes:

- `(VariantId)`
- `(ProductAttributeId, AttributeOptionId)`

## Category

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `ExternalId nvarchar(128) null`
- `Code nvarchar(64) not null`
- `ParentCategoryId uniqueidentifier null fk -> Category.Id`
- `SortOrder int not null default 0`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(TenantId, Code)`

## CategoryTranslation

Columns:

- `Id uniqueidentifier pk`
- `CategoryId uniqueidentifier not null fk -> Category.Id`
- `CultureCode nvarchar(16) not null`
- `Name nvarchar(256) not null`
- `Slug nvarchar(256) not null`
- `Description nvarchar(1024) null`

Constraints:

- unique: `(CategoryId, CultureCode)`

## ProductCategory

Columns:

- `ProductId uniqueidentifier not null fk -> Product.Id`
- `CategoryId uniqueidentifier not null fk -> Category.Id`
- `IsPrimary bit not null`
- `SortOrder int not null default 0`

Primary key:

- `(ProductId, CategoryId)`

## ProductMedia

Columns:

- `Id uniqueidentifier pk`
- `ProductId uniqueidentifier not null fk -> Product.Id`
- `MediaAssetId uniqueidentifier not null fk -> MediaAsset.Id`
- `Type nvarchar(32) not null`
- `SortOrder int not null default 0`
- `IsPrimary bit not null`
- `CultureCode nvarchar(16) null`
- `MarketId uniqueidentifier null fk -> Market.Id`

Constraints:

- unique: `(ProductId, MediaAssetId, Type, CultureCode, MarketId)`

Indexes:

- `(ProductId, SortOrder)`
- `(ProductId, IsPrimary)`

## VariantMedia

Columns:

- `Id uniqueidentifier pk`
- `VariantId uniqueidentifier not null fk -> Variant.Id`
- `MediaAssetId uniqueidentifier not null fk -> MediaAsset.Id`
- `Type nvarchar(32) not null`
- `SortOrder int not null default 0`
- `IsPrimary bit not null`

Constraints:

- unique: `(VariantId, MediaAssetId, Type)`

## MarketCatalogAssignment

Columns:

- `Id uniqueidentifier pk`
- `MarketId uniqueidentifier not null fk -> Market.Id`
- `ProductId uniqueidentifier not null fk -> Product.Id`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`

Constraints:

- unique: `(MarketId, ProductId)`

## Customer

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `ExternalId nvarchar(128) null`
- `UserId uniqueidentifier null`
- `Email nvarchar(320) not null`
- `NormalizedEmail nvarchar(320) not null`
- `FirstName nvarchar(128) not null`
- `LastName nvarchar(128) not null`
- `Phone nvarchar(64) null`
- `PreferredCulture nvarchar(16) null`
- `DefaultMarketId uniqueidentifier null fk -> Market.Id`
- `Status nvarchar(32) not null`
- `IsGuest bit not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`
- `IsDeleted bit not null default 0`

Indexes:

- `(TenantId, NormalizedEmail)`

## CustomerAddress

Columns:

- `Id uniqueidentifier pk`
- `CustomerId uniqueidentifier not null fk -> Customer.Id`
- `Type nvarchar(32) not null`
- `Attention nvarchar(128) null`
- `FirstName nvarchar(128) not null`
- `LastName nvarchar(128) not null`
- `CompanyName nvarchar(256) null`
- `Line1 nvarchar(256) not null`
- `Line2 nvarchar(256) null`
- `PostalCode nvarchar(32) not null`
- `City nvarchar(128) not null`
- `Region nvarchar(128) null`
- `CountryCode char(2) not null`
- `Phone nvarchar(64) null`
- `Email nvarchar(320) null`
- `IsDefault bit not null`

Indexes:

- `(CustomerId, Type, IsDefault)`

## Company

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `ExternalId nvarchar(128) null`
- `Code nvarchar(64) not null`
- `Name nvarchar(256) not null`
- `LegalName nvarchar(256) null`
- `OrganizationNumber nvarchar(64) null`
- `VatNumber nvarchar(64) null`
- `Email nvarchar(320) null`
- `Phone nvarchar(64) null`
- `DefaultMarketId uniqueidentifier null fk -> Market.Id`
- `DefaultCurrency char(3) null`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`
- `IsDeleted bit not null default 0`

Constraints:

- unique: `(TenantId, Code)`

## CompanyAddress

Columns:

- `Id uniqueidentifier pk`
- `CompanyId uniqueidentifier not null fk -> Company.Id`
- `Type nvarchar(32) not null`
- `Attention nvarchar(128) null`
- `Line1 nvarchar(256) not null`
- `Line2 nvarchar(256) null`
- `PostalCode nvarchar(32) not null`
- `City nvarchar(128) not null`
- `Region nvarchar(128) null`
- `CountryCode char(2) not null`
- `Email nvarchar(320) null`
- `Phone nvarchar(64) null`
- `IsDefault bit not null`

Indexes:

- `(CompanyId, Type, IsDefault)`

## CompanyMembership

Columns:

- `Id uniqueidentifier pk`
- `CompanyId uniqueidentifier not null fk -> Company.Id`
- `CustomerId uniqueidentifier not null fk -> Customer.Id`
- `Role nvarchar(64) not null`
- `Status nvarchar(32) not null`
- `IsDefaultCompany bit not null`
- `CanPlaceOrders bit not null`
- `CanApproveOrders bit not null`
- `CanManageUsers bit not null`
- `ValidFromUtc datetime2 null`
- `ValidToUtc datetime2 null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(CompanyId, CustomerId)`

Indexes:

- `(CustomerId, Status)`
- `(CompanyId, Status)`

## PriceList

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `Code nvarchar(64) not null`
- `Name nvarchar(256) not null`
- `CurrencyCode char(3) not null`
- `VatIncluded bit not null`
- `Status nvarchar(32) not null`
- `ValidFromUtc datetime2 null`
- `ValidToUtc datetime2 null`
- `CompanyId uniqueidentifier null fk -> Company.Id`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(TenantId, Code)`

## MarketPriceList

Columns:

- `Id uniqueidentifier pk`
- `MarketId uniqueidentifier not null fk -> Market.Id`
- `PriceListId uniqueidentifier not null fk -> PriceList.Id`
- `Priority int not null default 0`
- `IsBasePriceList bit not null`

Constraints:

- unique: `(MarketId, PriceListId)`

## PriceListEntry

Columns:

- `Id uniqueidentifier pk`
- `PriceListId uniqueidentifier not null fk -> PriceList.Id`
- `TargetType nvarchar(32) not null`
- `TargetId uniqueidentifier not null`
- `MinQuantity decimal(18,4) not null`
- `Amount decimal(18,4) not null`
- `CompareAtAmount decimal(18,4) null`
- `ValidFromUtc datetime2 null`
- `ValidToUtc datetime2 null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`

Constraints:

- check: `MinQuantity > 0`
- check: `TargetType in ('Variant')`

Indexes:

- `(PriceListId, TargetType, TargetId, MinQuantity)`
- `(TargetType, TargetId)`

## InventoryLocation

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `Code nvarchar(64) not null`
- `Name nvarchar(256) not null`
- `Type nvarchar(32) not null`
- `Status nvarchar(32) not null`
- `CountryCode char(2) null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(TenantId, Code)`

## MarketInventoryLocation

Columns:

- `Id uniqueidentifier pk`
- `MarketId uniqueidentifier not null fk -> Market.Id`
- `InventoryLocationId uniqueidentifier not null fk -> InventoryLocation.Id`
- `Priority int not null default 0`

Constraints:

- unique: `(MarketId, InventoryLocationId)`

## InventoryBalance

Columns:

- `Id uniqueidentifier pk`
- `InventoryLocationId uniqueidentifier not null fk -> InventoryLocation.Id`
- `VariantId uniqueidentifier not null fk -> Variant.Id`
- `OnHandQuantity decimal(18,4) not null`
- `ReservedQuantity decimal(18,4) not null`
- `IncomingQuantity decimal(18,4) not null`
- `Backorderable bit not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(InventoryLocationId, VariantId)`

## InventoryReservation

Columns:

- `Id uniqueidentifier pk`
- `InventoryLocationId uniqueidentifier not null fk -> InventoryLocation.Id`
- `VariantId uniqueidentifier not null fk -> Variant.Id`
- `CartId uniqueidentifier null`
- `OrderId uniqueidentifier null`
- `Quantity decimal(18,4) not null`
- `Status nvarchar(32) not null`
- `ExpiresAtUtc datetime2 null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`

Indexes:

- `(CartId, Status)`
- `(OrderId, Status)`
- `(VariantId, Status, ExpiresAtUtc)`

## InventoryTransaction

Columns:

- `Id uniqueidentifier pk`
- `InventoryLocationId uniqueidentifier not null fk -> InventoryLocation.Id`
- `VariantId uniqueidentifier not null fk -> Variant.Id`
- `Type nvarchar(32) not null`
- `QuantityDelta decimal(18,4) not null`
- `ReferenceType nvarchar(32) not null`
- `ReferenceId uniqueidentifier not null`
- `OccurredAtUtc datetime2 not null`
- `CreatedAtUtc datetime2 not null`

Indexes:

- `(VariantId, OccurredAtUtc)`
- `(ReferenceType, ReferenceId)`

## Cart

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `CartNumber nvarchar(64) not null`
- `Status nvarchar(32) not null`
- `CustomerId uniqueidentifier null fk -> Customer.Id`
- `CompanyId uniqueidentifier null fk -> Company.Id`
- `MarketId uniqueidentifier not null fk -> Market.Id`
- `CurrencyCode char(3) not null`
- `CultureCode nvarchar(16) not null`
- `Email nvarchar(320) null`
- `SelectedPaymentMethod nvarchar(64) null`
- `SelectedShippingMethod nvarchar(64) null`
- `PriceListContext nvarchar(256) null`
- `ExpiresAtUtc datetime2 null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(TenantId, CartNumber)`

## CartAddress

Columns:

- `Id uniqueidentifier pk`
- `CartId uniqueidentifier not null fk -> Cart.Id`
- `Type nvarchar(32) not null`
- `FirstName nvarchar(128) not null`
- `LastName nvarchar(128) not null`
- `CompanyName nvarchar(256) null`
- `Line1 nvarchar(256) not null`
- `Line2 nvarchar(256) null`
- `PostalCode nvarchar(32) not null`
- `City nvarchar(128) not null`
- `Region nvarchar(128) null`
- `CountryCode char(2) not null`
- `Email nvarchar(320) null`
- `Phone nvarchar(64) null`

Indexes:

- `(CartId, Type)`

## CartLine

Columns:

- `Id uniqueidentifier pk`
- `CartId uniqueidentifier not null fk -> Cart.Id`
- `VariantId uniqueidentifier not null fk -> Variant.Id`
- `Quantity decimal(18,4) not null`
- `UnitPrice decimal(18,4) not null`
- `VatRate decimal(9,4) not null`
- `LineTotal decimal(18,4) not null`
- `Comment nvarchar(512) null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`

Indexes:

- `(CartId)`
- `(VariantId)`

## [Order]

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `OrderNumber nvarchar(64) not null`
- `Status nvarchar(32) not null`
- `CustomerId uniqueidentifier null fk -> Customer.Id`
- `CompanyId uniqueidentifier null fk -> Company.Id`
- `MarketId uniqueidentifier not null fk -> Market.Id`
- `CurrencyCode char(3) not null`
- `CultureCode nvarchar(16) not null`
- `Email nvarchar(320) not null`
- `Subtotal decimal(18,4) not null`
- `VatTotal decimal(18,4) not null`
- `GrandTotal decimal(18,4) not null`
- `PlacedAtUtc datetime2 not null`
- `PaymentStatus nvarchar(32) not null`
- `FulfillmentStatus nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(TenantId, OrderNumber)`

## OrderAddress

Columns:

- `Id uniqueidentifier pk`
- `OrderId uniqueidentifier not null fk -> [Order].Id`
- `Type nvarchar(32) not null`
- `FirstName nvarchar(128) not null`
- `LastName nvarchar(128) not null`
- `CompanyName nvarchar(256) null`
- `Line1 nvarchar(256) not null`
- `Line2 nvarchar(256) null`
- `PostalCode nvarchar(32) not null`
- `City nvarchar(128) not null`
- `Region nvarchar(128) null`
- `CountryCode char(2) not null`
- `Email nvarchar(320) null`
- `Phone nvarchar(64) null`

Indexes:

- `(OrderId, Type)`

## OrderLine

Columns:

- `Id uniqueidentifier pk`
- `OrderId uniqueidentifier not null fk -> [Order].Id`
- `VariantId uniqueidentifier not null fk -> Variant.Id`
- `Sku nvarchar(64) not null`
- `ProductName nvarchar(256) not null`
- `VariantDescription nvarchar(256) null`
- `Quantity decimal(18,4) not null`
- `UnitPrice decimal(18,4) not null`
- `VatRate decimal(9,4) not null`
- `LineTotal decimal(18,4) not null`

Indexes:

- `(OrderId)`
- `(VariantId)`

## OrderStatusHistory

Columns:

- `Id uniqueidentifier pk`
- `OrderId uniqueidentifier not null fk -> [Order].Id`
- `FromStatus nvarchar(32) null`
- `ToStatus nvarchar(32) not null`
- `ChangedBy nvarchar(128) not null`
- `ChangedAtUtc datetime2 not null`
- `Comment nvarchar(512) null`

Indexes:

- `(OrderId, ChangedAtUtc)`

## PaymentTransaction

Columns:

- `Id uniqueidentifier pk`
- `OrderId uniqueidentifier not null fk -> [Order].Id`
- `Provider nvarchar(64) not null`
- `ProviderReference nvarchar(128) not null`
- `Type nvarchar(32) not null`
- `Status nvarchar(32) not null`
- `Amount decimal(18,4) not null`
- `CurrencyCode char(3) not null`
- `RequestedAtUtc datetime2 not null`
- `CompletedAtUtc datetime2 null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`

Indexes:

- `(OrderId)`
- `(Provider, ProviderReference)`

## Custom Field Tables

## CustomFieldDefinition

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `EntityType nvarchar(64) not null`
- `Key nvarchar(64) not null`
- `Label nvarchar(128) not null`
- `DataType nvarchar(32) not null`
- `IsRequired bit not null`
- `IsLocalized bit not null`
- `IsMarketScoped bit not null`
- `ValidationJson nvarchar(max) null`
- `DefaultValue nvarchar(max) null`
- `IsSearchable bit not null`
- `Status nvarchar(32) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`
- `RowVersion rowversion not null`

Constraints:

- unique: `(TenantId, EntityType, Key)`

## CustomFieldValue

Columns:

- `Id uniqueidentifier pk`
- `CustomFieldDefinitionId uniqueidentifier not null fk -> CustomFieldDefinition.Id`
- `EntityId uniqueidentifier not null`
- `CultureCode nvarchar(16) null`
- `MarketId uniqueidentifier null fk -> Market.Id`
- `ValueJson nvarchar(max) not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`

Indexes:

- `(CustomFieldDefinitionId, EntityId)`
- `(EntityId)`

## Integration Tables

## WebhookSubscription

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `Name nvarchar(128) not null`
- `EndpointUrl nvarchar(2048) not null`
- `Secret nvarchar(256) not null`
- `EventTypes nvarchar(max) not null`
- `IsActive bit not null`
- `CreatedAtUtc datetime2 not null`
- `UpdatedAtUtc datetime2 not null`

## WebhookDelivery

Columns:

- `Id uniqueidentifier pk`
- `WebhookSubscriptionId uniqueidentifier not null fk -> WebhookSubscription.Id`
- `EventId uniqueidentifier not null`
- `Status nvarchar(32) not null`
- `AttemptCount int not null`
- `LastAttemptAtUtc datetime2 null`
- `NextAttemptAtUtc datetime2 null`
- `ResponseCode int null`
- `ResponseBody nvarchar(max) null`
- `CreatedAtUtc datetime2 not null`

Indexes:

- `(WebhookSubscriptionId, Status)`
- `(EventId)`

## IntegrationJob

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `Type nvarchar(64) not null`
- `Direction nvarchar(32) not null`
- `Status nvarchar(32) not null`
- `RequestedBy nvarchar(128) not null`
- `StartedAtUtc datetime2 null`
- `CompletedAtUtc datetime2 null`
- `PayloadReference nvarchar(512) null`
- `ResultSummary nvarchar(max) null`
- `CreatedAtUtc datetime2 not null`

Indexes:

- `(Type, Status)`
- `(CreatedAtUtc)`

## OutboxMessage

Columns:

- `Id uniqueidentifier pk`
- `TenantId uniqueidentifier null`
- `EventType nvarchar(128) not null`
- `AggregateType nvarchar(64) not null`
- `AggregateId uniqueidentifier not null`
- `OccurredAtUtc datetime2 not null`
- `Payload nvarchar(max) not null`
- `PublishedAtUtc datetime2 null`
- `CreatedAtUtc datetime2 not null`

Indexes:

- `(PublishedAtUtc)`
- `(AggregateType, AggregateId)`

## Backoffice Notes

Backoffice should operate on the same domain tables through admin APIs, permissions, audit logging, and workflow/status transitions.

Recommended backoffice areas:

- catalog
- media
- product statuses
- markets
- pricing
- inventory
- customers
- companies
- orders
- integrations

## First DDL Implementation Order

1. `ProductStatusDefinition`
2. `MediaAsset`
3. `Brand`
4. `BrandTranslation`
5. `Market`
6. `MarketCurrency`
7. `MarketCulture`
8. `Product`
9. `ProductTranslation`
10. `Variant`
11. `ProductAttribute`
12. `AttributeOption`
13. `VariantAttributeValue`
14. `Category`
15. `CategoryTranslation`
16. `ProductCategory`
17. `ProductMedia`
18. `VariantMedia`
19. `MarketCatalogAssignment`
20. `Customer`
21. `CustomerAddress`
22. `Company`
23. `CompanyAddress`
24. `CompanyMembership`
25. `PriceList`
26. `MarketPriceList`
27. `PriceListEntry`
28. `InventoryLocation`
29. `MarketInventoryLocation`
30. `InventoryBalance`
31. `InventoryReservation`
32. `InventoryTransaction`
33. `Cart`
34. `CartAddress`
35. `CartLine`
36. `[Order]`
37. `OrderAddress`
38. `OrderLine`
39. `OrderStatusHistory`
40. `PaymentTransaction`
41. `CustomFieldDefinition`
42. `CustomFieldValue`
43. `WebhookSubscription`
44. `WebhookDelivery`
45. `IntegrationJob`
46. `OutboxMessage`

## Recommended Next Step

The next useful artifact after this is one of:

1. SQL Server `CREATE TABLE` scripts
2. EF Core entity/configuration classes
3. an ERD based on this schema
