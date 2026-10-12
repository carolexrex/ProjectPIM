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

Current implementation status:

- Implemented read endpoint is `GET /api/admin/product-statuses`.
- Current response DTO exposes `id`, `code`, `name`, and `isBuyable`.
- Create/update status-definition endpoints and the full visibility/search/default/sort contract below are target design, not current API.

### List Product Status Definitions

Current:

`GET /api/admin/product-statuses`

Query parameters:

- `entityType`

Target:

`GET /api/admin/product-status-definitions`

Additional target query parameters:

- `status`

### Create Product Status Definition

Target only:

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

Current implementation status:

- Current API supports price lists, market assignments, priorities/base-list flags, entries, list validity windows, and entry validity windows.
- Current API does not expose `companyId` on price lists. Company/customer-specific pricing is target design only.
- VAT rates are configured separately as VAT rules per market and tax category (see `VAT Rules`); `vatIncluded` only states whether entry amounts include VAT.

### List Price Lists

`GET /api/admin/price-lists`

Query parameters:

- `currencyCode`
- `status`
- `marketId`

Target/future query parameters:

- `companyId`

### Create Price List

`POST /api/admin/price-lists`

Request:

```json
{
  "code": "SE_BASE_GROSS",
  "name": "SE Base Gross",
  "currencyCode": "SEK",
  "vatIncluded": true,
  "validFromUtc": null,
  "validToUtc": null
}
```

Target/future field:

- `companyId`

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

## VAT Rules

VAT rules configure the rate cart pricing applies to a product, keyed by market and the product's `taxCategoryCode`. Cart pricing (admin reprice, storefront carts, checkout, manual orders) never assumes a rate: a cart line whose market has no rule for its product tax category fails with a `400` validation error under `errors.TaxCategoryCode`. Placed orders keep their VAT snapshot when rules change.

Authorization: reads require `PricingRead`; writes require `PricingWrite`.

Rule fields:

- `rate` is a fraction (`0.25` = 25%) from `0` up to but excluding `1`, with at most four decimals. `0` is valid and must be sent explicitly; an omitted `rate` is rejected.
- `taxCategoryCode` is trimmed and upper-cased; matching against products is case-insensitive.
- One rule per market and tax category; a duplicate create returns `409`.
- Updates and deletes require the current `rowVersion`; a stale value returns `409`.
- Market and tax category are immutable; to move a rule, delete it and create a new one.

### List VAT Rules

`GET /api/admin/vat-rules`

Query parameters:

- `marketId`
- `taxCategoryCode`
- `page`, `pageSize`

### Get VAT Rule

`GET /api/admin/vat-rules/{id}`

Response:

```json
{
  "id": "64100000-0000-0000-0000-000000000001",
  "marketId": "62000000-0000-0000-0000-000000000001",
  "marketCode": "SE",
  "taxCategoryCode": "STANDARD",
  "rate": 0.25,
  "description": "Sweden standard rate",
  "createdAtUtc": "2026-10-10T08:00:00Z",
  "updatedAtUtc": "2026-10-10T08:00:00Z",
  "rowVersion": "..."
}
```

### Create VAT Rule

`POST /api/admin/vat-rules` returns `201 Created`.

Request:

```json
{
  "marketId": "62000000-0000-0000-0000-000000000001",
  "taxCategoryCode": "STANDARD",
  "rate": 0.25,
  "description": "Sweden standard rate"
}
```

### Update VAT Rule

`PUT /api/admin/vat-rules/{id}`

Request:

```json
{
  "rate": 0.12,
  "description": "Reduced rate",
  "rowVersion": "..."
}
```

### Delete VAT Rule

`POST /api/admin/vat-rules/{id}/delete` returns `204 No Content`.

Request:

```json
{
  "rowVersion": "..."
}
```

Deleting a rule that products in the market still use makes their carts fail pricing until a rule is configured again.

Out of scope for VAT rules today: destination-based taxation, customer/company exemptions, and B2B reverse charge.

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
  "backorderable": false,
  "rowVersion": "..."
}
```

Rules:

- `reservedQuantity` covers open order allocations plus any holds recorded outside the platform. It cannot be set below the quantity open orders hold on the balance (`400` with `errors.ReservedQuantity`); orders release and ship only their own allocated quantities.
- `rowVersion` is optional. When supplied it must match (`409` otherwise). When omitted, a checkout or settlement that changes the balance between this request's read and write causes the write to be re-run against the fresh row, so the reserved-floor rule is re-checked and no reservation is lost.
- `onHandQuantity` may fall below `reservedQuantity` (a stock count found less than is promised). Orders on that balance can then still be cancelled, but completing them is refused until stock is restored.

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

Adjustments change on-hand stock only and are validated before anything changes (a non-backorderable balance cannot go below zero on hand). A concurrent checkout on the same balance makes the adjustment re-run against the fresh row instead of overwriting the reservation.

Inventory transactions in location details carry `quantityDelta` (on-hand change) and `reservedQuantityDelta` (reserved change). Types: `Adjustment` (on hand), `Reservation` (+reserved, order placement), `ReservationRelease` (−reserved, order cancellation), `Shipment` (−on hand and −reserved, order completion). Order-driven transactions use `referenceType` `Order` and the order id.

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

### Create Order

`POST /api/admin/orders`

Use sparingly in v1. Prefer cart-to-order conversion.

- Cart conversion: send `cartId` and `cartRowVersion`. Repeating it for a converted cart returns the original order.
- Manual order: omit `cartId` and send `marketId`, `currencyCode`, `cultureCode`, `email`, and `lines` (`variantId`, `quantity`, `comment`). Optional ids may be omitted or `null`; when present they must be non-empty GUIDs.

Both paths price through the shared cart pricing path and reserve stock in the same commit (see `InventoryAllocation` in `domain-spec-v1.md`). Errors:

- `400` with `errors["VariantId.{variantId}"]` when a variant is short in the order market, e.g. `Insufficient stock for SKU-EXAMPLE-1-BLACK in market 'SE': requested 24, available 23.`, or is not stocked at any active location assigned to the market, or the quantity has more than 4 decimal places
- `400` with `errors.TaxCategoryCode` when no VAT rule exists
- `409` when a concurrent write could not be resolved by the in-request retry; nothing was committed

### Change Order Status

`POST /api/admin/orders/{id}/status`

Request:

```json
{
  "toStatus": "Processing",
  "comment": "Picked up by warehouse.",
  "rowVersion": "..."
}
```

Transitions: `Placed` → `Processing` | `Cancelled`; `Processing` → `Completed` | `Cancelled`.

Inventory effects:

- `Cancelled` releases every outstanding allocation once (`reservedQuantity` decreases).
- `Completed` ships every outstanding allocation once (`onHandQuantity` and `reservedQuantity` decrease). It returns `400` with `errors["VariantId.{variantId}"]`, changing nothing, when a balance no longer holds the allocated quantity on hand (for example backordered stock that has not arrived, or stock counted away).
- Orders placed before inventory allocations existed change no stock when cancelled or completed.
- A stale `rowVersion` returns `409`.

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

## Assisted Import Sources

This is a target contract for the staged source-onboarding workflow in `import-source-onboarding-design-v1.md`; it is not implemented by the current product import job.

Supported source families should grow incrementally:

- existing file/product import formats
- conventional REST APIs with OpenAPI or representative samples
- authorized website discovery through feeds, sitemaps, JSON-LD, and bounded page extraction
- purpose-built adapters for legacy systems that cannot be represented safely by the generic connector definition

Recommended resource shape:

- `POST /api/admin/import-sources`
- `GET /api/admin/import-sources`
- `GET /api/admin/import-sources/{id}`
- `POST /api/admin/import-sources/{id}/discover`
- `GET /api/admin/import-sources/{id}/connector-definitions`
- `POST /api/admin/import-sources/{id}/connector-definitions/{version}/approve`
- `POST /api/admin/import-sources/{id}/dry-runs`
- `GET /api/admin/import-batches/{id}`
- `GET /api/admin/import-batches/{id}/candidates`
- `POST /api/admin/import-batches/{id}/approve`
- `POST /api/admin/import-batches/{id}/execute`

Source creation should accept source type, base location, operational limits, and an opaque credential reference. It must not return secret material or copy raw credentials into integration-job payloads.

Discovery responses should identify:

- sampled endpoints or pages
- detected source capabilities
- proposed product, variant, category, media, price, and relationship mappings
- evidence and confidence for AI-assisted decisions
- blocking safety or compatibility issues

Dry-run responses should include discovered, staged, create, update, unchanged, skipped, warning, and failure counts. Candidate reads should expose source lineage, proposed action, validation issues, and field-level differences without requiring operators to inspect raw worker logs.

Approval and execution rules:

- discovery is bounded and read-only by default
- a connector-definition version must be approved before a production import
- execution pins that version and runs as an `IntegrationJob`
- applying candidates uses normal application services and authorization policies
- repeated approval or execution requests require idempotency protection
- website sources require recorded confirmation that the operator is authorized to scan and import the source
- source credentials and sensitive samples must be redacted from audit, error, and AI-provider payloads

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
