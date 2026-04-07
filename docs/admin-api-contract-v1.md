# Admin API Contract v1

## Purpose

This document defines the first admin API contract for the platform backoffice and integration-facing administration workflows.

It covers:

- catalog administration
- markets
- pricing
- inventory
- customers
- companies
- carts
- orders
- common API rules

This is the admin/internal operational API, not the storefront API.

## Scope

The admin API in v1 is intended for:

- backoffice UI
- internal tools
- operational automation
- trusted integrations with scoped permissions

The admin API in v1 is not intended for:

- storefront browsing
- storefront cart rendering
- anonymous consumers

## Base Rules

Base path:

- `/api/admin`

Authentication:

- authenticated admin user
- or trusted integration client

Authorization:

- role and policy based

Examples:

- `PlatformAdmin`
- `CatalogManager`
- `PricingManager`
- `InventoryManager`
- `CustomerService`
- `IntegrationClient`

## Common Conventions

## IDs

- all entity IDs are `Guid`
- IDs are opaque

## Standard Resource Shape

Example:

```json
{
  "id": "50000000-0000-0000-0000-000000000001",
  "createdAtUtc": "2026-03-11T10:00:00Z",
  "updatedAtUtc": "2026-03-11T10:30:00Z",
  "rowVersion": "AAAAAAAAB9E="
}
```

## List Responses

Use:

```json
{
  "items": [],
  "total": 0,
  "page": 1,
  "pageSize": 50
}
```

## Filtering

List endpoints should support:

- paging
- sorting
- text search where useful
- status filters
- market filters where relevant

## Concurrency

Mutable resources should support optimistic concurrency through:

- `rowVersion`

Update requests should send the last-known `rowVersion`.

## Errors

Use standard problem details shape:

```json
{
  "type": "https://example.local/problems/validation-error",
  "title": "Validation error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "sku": ["SKU already exists."]
  }
}
```

## Catalog API

## Products

### List Products

`GET /api/admin/products`

Query parameters:

- `search`
- `status`
- `productStatusCode`
- `brandId`
- `categoryId`
- `marketId`
- `hasVariants`
- `page`
- `pageSize`
- `sort`

Response item example:

```json
{
  "id": "50000000-0000-0000-0000-000000000001",
  "productNumber": "SKU-EXAMPLE-1",
  "slug": "example-drill",
  "productType": "Hardware",
  "status": "Active",
  "productStatus": {
    "id": "10000000-0000-0000-0000-000000000002",
    "code": "READY",
    "name": "Ready",
    "isBuyable": true
  },
  "brand": {
    "id": "70000000-0000-0000-0000-000000000001",
    "name": "Acme"
  },
  "defaultName": "Example Drill",
  "primaryImageUrl": "/media/example-drill.jpg",
  "hasVariants": true,
  "createdAtUtc": "2026-03-11T10:00:00Z",
  "updatedAtUtc": "2026-03-11T10:30:00Z",
  "rowVersion": "AAAAAAAAB9E="
}
```

### Get Product

`GET /api/admin/products/{id}`

Response should include:

- base product fields
- translations
- categories
- media
- custom fields
- variants summary
- market assignments

### Create Product

`POST /api/admin/products`

Request:

```json
{
  "productType": "Hardware",
  "productNumber": "SKU-EXAMPLE-1",
  "slug": "example-drill",
  "brandId": "70000000-0000-0000-0000-000000000001",
  "productStatusDefinitionId": "10000000-0000-0000-0000-000000000002",
  "taxCategoryCode": "STANDARD",
  "unitOfMeasure": "pcs",
  "hasVariants": true,
  "weight": 1.8,
  "length": 28.0,
  "width": 8.0,
  "height": 22.0
}
```

### Update Product

`PUT /api/admin/products/{id}`

Request:

```json
{
  "productType": "Hardware",
  "slug": "example-drill",
  "brandId": "70000000-0000-0000-0000-000000000001",
  "productStatusDefinitionId": "10000000-0000-0000-0000-000000000002",
  "taxCategoryCode": "STANDARD",
  "unitOfMeasure": "pcs",
  "weight": 1.8,
  "length": 28.0,
  "width": 8.0,
  "height": 22.0,
  "rowVersion": "AAAAAAAAB9E="
}
```

### Archive Product

`POST /api/admin/products/{id}/archive`

This updates operational status, not hard delete.

### Assign Product Status

`POST /api/admin/products/{id}/status`

Request:

```json
{
  "productStatusDefinitionId": "10000000-0000-0000-0000-000000000003",
  "comment": "Move to Coming Soon before launch."
}
```

## Product Translations

### Upsert Product Translation

`PUT /api/admin/products/{id}/translations/{cultureCode}`

Request:

```json
{
  "name": "Example Drill",
  "shortDescription": "Compact and powerful drill for demanding work.",
  "longDescription": "Longer product description here.",
  "seoTitle": "Example Drill | Acme",
  "seoDescription": "Compact and powerful drill for demanding work."
}
```

### Get Product Translation

`GET /api/admin/products/{id}/translations/{cultureCode}`

## Variants

### List Variants For Product

`GET /api/admin/products/{id}/variants`

### Create Variant

`POST /api/admin/products/{id}/variants`

Request:

```json
{
  "sku": "SKU-EXAMPLE-1-BLACK",
  "ean": "1234567890123",
  "mpn": "ACME-DRILL-BLK",
  "barcode": "1234567890123",
  "productStatusDefinitionId": "10000000-0000-0000-0000-000000000102",
  "isDefaultVariant": true,
  "weight": 1.8,
  "attributeValues": [
    {
      "productAttributeId": "71000000-0000-0000-0000-000000000001",
      "attributeOptionId": "72000000-0000-0000-0000-000000000001"
    }
  ]
}
```

### Update Variant

`PUT /api/admin/variants/{id}`

### Assign Variant Status

`POST /api/admin/variants/{id}/status`

## Categories

### List Categories

`GET /api/admin/categories`

### Create Category

`POST /api/admin/categories`

### Update Category

`PUT /api/admin/categories/{id}`

### Upsert Category Translation

`PUT /api/admin/categories/{id}/translations/{cultureCode}`

## Brands

### List Brands

`GET /api/admin/brands`

### Create Brand

`POST /api/admin/brands`

### Update Brand

`PUT /api/admin/brands/{id}`

## Media

### Upload Media Metadata

`POST /api/admin/media`

Purpose:

- register uploaded file metadata
- link storage key to media asset

### Attach Product Media

`POST /api/admin/products/{id}/media`

Request:

```json
{
  "mediaAssetId": "73000000-0000-0000-0000-000000000001",
  "type": "Image",
  "sortOrder": 0,
  "isPrimary": true,
  "cultureCode": null,
  "marketId": null
}
```

### Attach Variant Media

`POST /api/admin/variants/{id}/media`

## Product Status Definitions

### List Product Status Definitions

`GET /api/admin/product-status-definitions`

Query parameters:

- `entityType`
- `status`

### Create Product Status Definition

`POST /api/admin/product-status-definitions`

Request:

```json
{
  "entityType": "Product",
  "code": "COMING_SOON",
  "name": "Coming Soon",
  "isDefault": false,
  "isVisibleInBackoffice": true,
  "isVisibleInStorefront": true,
  "isBuyable": false,
  "isSearchable": true,
  "sortOrder": 30,
  "status": "Active"
}
```

## Market API

## Markets

### List Markets

`GET /api/admin/markets`

### Get Market

`GET /api/admin/markets/{id}`

### Create Market

`POST /api/admin/markets`

### Update Market

`PUT /api/admin/markets/{id}`

### Assign Market Currencies

`PUT /api/admin/markets/{id}/currencies`

Request:

```json
{
  "defaultCurrency": "SEK",
  "currencies": ["SEK", "EUR"]
}
```

### Assign Market Cultures

`PUT /api/admin/markets/{id}/cultures`

Request:

```json
{
  "defaultCulture": "sv-SE",
  "cultures": ["sv-SE", "en-GB"]
}
```

### Assign Product To Market

`PUT /api/admin/markets/{marketId}/products/{productId}`

Request:

```json
{
  "status": "Active"
}
```

### Remove Product From Market

`DELETE /api/admin/markets/{marketId}/products/{productId}`

## Pricing API

## Price Lists

### List Price Lists

`GET /api/admin/price-lists`

Query parameters:

- `currencyCode`
- `status`
- `companyId`
- `marketId`

### Create Price List

`POST /api/admin/price-lists`

Request:

```json
{
  "code": "SE_BASE_GROSS",
  "name": "SE Base Gross",
  "currencyCode": "SEK",
  "vatIncluded": true,
  "status": "Active",
  "companyId": null
}
```

### Update Price List

`PUT /api/admin/price-lists/{id}`

### Assign Price List To Market

`PUT /api/admin/markets/{marketId}/price-lists/{priceListId}`

Request:

```json
{
  "priority": 0,
  "isBasePriceList": true
}
```

## Price Entries

### List Price Entries

`GET /api/admin/price-lists/{id}/entries`

Query parameters:

- `targetType`
- `targetId`

### Upsert Price Entry

`PUT /api/admin/price-lists/{id}/entries/{entryId}`

Request:

```json
{
  "targetType": "Variant",
  "targetId": "50000000-0000-0000-0000-000000000011",
  "minQuantity": 1,
  "amount": 1499.00,
  "compareAtAmount": 1699.00,
  "validFromUtc": null,
  "validToUtc": null
}
```

### Bulk Upsert Price Entries

`POST /api/admin/price-lists/{id}/entries/bulk`

Use for imports and mass pricing updates.

## Inventory API

## Inventory Locations

### List Inventory Locations

`GET /api/admin/inventory-locations`

### Create Inventory Location

`POST /api/admin/inventory-locations`

### Update Inventory Location

`PUT /api/admin/inventory-locations/{id}`

### Assign Inventory Location To Market

`PUT /api/admin/markets/{marketId}/inventory-locations/{inventoryLocationId}`

Request:

```json
{
  "priority": 0
}
```

## Inventory Balances

### Get Variant Inventory Snapshot

`GET /api/admin/variants/{id}/inventory`

Response:

```json
{
  "variantId": "50000000-0000-0000-0000-000000000011",
  "locations": [
    {
      "inventoryLocationId": "74000000-0000-0000-0000-000000000001",
      "code": "MAIN",
      "onHandQuantity": 25,
      "reservedQuantity": 2,
      "incomingQuantity": 10,
      "availableQuantity": 23,
      "backorderable": false
    }
  ]
}
```

### Upsert Inventory Balance

`PUT /api/admin/inventory-balances`

Request:

```json
{
  "inventoryLocationId": "74000000-0000-0000-0000-000000000001",
  "variantId": "50000000-0000-0000-0000-000000000011",
  "onHandQuantity": 25,
  "reservedQuantity": 2,
  "incomingQuantity": 10,
  "backorderable": false
}
```

### Adjust Inventory

`POST /api/admin/inventory-transactions`

Request:

```json
{
  "inventoryLocationId": "74000000-0000-0000-0000-000000000001",
  "variantId": "50000000-0000-0000-0000-000000000011",
  "type": "Adjustment",
  "quantityDelta": 5,
  "referenceType": "ManualAdjustment",
  "referenceId": "75000000-0000-0000-0000-000000000001"
}
```

## Customer API

## Customers

### List Customers

`GET /api/admin/customers`

Query parameters:

- `search`
- `status`
- `isGuest`
- `defaultMarketId`

### Get Customer

`GET /api/admin/customers/{id}`

### Create Customer

`POST /api/admin/customers`

### Update Customer

`PUT /api/admin/customers/{id}`

### Add Customer Address

`POST /api/admin/customers/{id}/addresses`

## Company API

## Companies

### List Companies

`GET /api/admin/companies`

Query parameters:

- `search`
- `status`
- `defaultMarketId`

### Get Company

`GET /api/admin/companies/{id}`

### Create Company

`POST /api/admin/companies`

### Update Company

`PUT /api/admin/companies/{id}`

### Add Company Address

`POST /api/admin/companies/{id}/addresses`

## Memberships

### List Company Memberships

`GET /api/admin/companies/{id}/memberships`

### Create Company Membership

`POST /api/admin/companies/{id}/memberships`

Request:

```json
{
  "customerId": "76000000-0000-0000-0000-000000000001",
  "role": "Buyer",
  "status": "Active",
  "isDefaultCompany": true,
  "canPlaceOrders": true,
  "canApproveOrders": false,
  "canManageUsers": false,
  "validFromUtc": null,
  "validToUtc": null
}
```

### Update Company Membership

`PUT /api/admin/company-memberships/{id}`

## Cart API

## Carts

### List Carts

`GET /api/admin/carts`

Query parameters:

- `status`
- `customerId`
- `companyId`
- `marketId`
- `createdFromUtc`
- `createdToUtc`

### Get Cart

`GET /api/admin/carts/{id}`

### Reprice Cart

`POST /api/admin/carts/{id}/reprice`

### Expire Cart

`POST /api/admin/carts/{id}/expire`

## Order API

## Orders

### List Orders

`GET /api/admin/orders`

Query parameters:

- `status`
- `paymentStatus`
- `fulfillmentStatus`
- `customerId`
- `companyId`
- `marketId`
- `placedFromUtc`
- `placedToUtc`
- `search`

### Get Order

`GET /api/admin/orders/{id}`

Response should include:

- order summary
- lines
- addresses
- payment transactions
- status history

### Create Manual Order

`POST /api/admin/orders`

Use sparingly in v1. Prefer cart-to-order conversion.

### Change Order Status

`POST /api/admin/orders/{id}/status`

Request:

```json
{
  "toStatus": "Processing",
  "comment": "Picked up by warehouse."
}
```

### Add Payment Transaction

`POST /api/admin/orders/{id}/payment-transactions`

### Get Order Status History

`GET /api/admin/orders/{id}/status-history`

## Custom Field API

## Field Definitions

### List Custom Field Definitions

`GET /api/admin/custom-fields`

Query parameters:

- `entityType`
- `status`
- `aiCapability`

### Create Custom Field Definition

`POST /api/admin/custom-fields`

Request:

```json
{
  "entityType": "Product",
  "key": "marketingBadge",
  "label": "Marketing Badge",
  "dataType": "Text",
  "isRequired": false,
  "isLocalized": true,
  "isMarketScoped": false,
  "validationJson": null,
  "defaultValue": null,
  "isSearchable": true,
  "aiCapability": "Generate",
  "status": "Active"
}
```

### Update Custom Field Definition

`PUT /api/admin/custom-fields/{id}`

## Bulk Operations

V1 should support bulk endpoints for:

- product import/update
- price import/update
- inventory import/update
- translation import/update

Recommended shape:

`POST /api/admin/{resource}/bulk`

Response:

```json
{
  "jobId": "77000000-0000-0000-0000-000000000001",
  "status": "Pending"
}
```

## Audit Expectations

Important write operations should record:

- actor
- entity type
- entity id
- action
- before/after summary where practical
- timestamp

High-priority audit areas:

- product status changes
- price changes
- inventory adjustments
- membership changes
- order status changes
- AI suggestion acceptance

## Recommended Next Step

After this contract, the next useful artifact is:

1. EF Core resource and command model design
2. admin API controller/application service design
3. backoffice screen flows for catalog, pricing, and inventory
