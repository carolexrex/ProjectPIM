# Storefront API Contract v1

## Purpose

This document defines the first consumer-facing API contract for `Platform.StorefrontApi`.

It is intended for:

- CMS integrations such as Nexra
- storefront web/mobile clients
- search/browse consumers
- future shopping-agent consumers

It is not the admin API.

`Platform.Api` manages the system.

`Platform.StorefrontApi` exposes read and commerce-facing contracts for consuming the system.

## Scope

The first slice should focus on read models, not full checkout.

Included in this v1 slice:

- context resolution for channel, market, culture, and currency
- category navigation
- product browse/search
- product detail
- explicit price, availability, and buyability diagnostics
- cart creation, cart repricing, and cart checkout into an order

Deferred from the first storefront slice:

- payment initiation
- payment callbacks
- advanced search merchandising

## Naming Direction

Avoid vague naming such as `bootstrap`.

For storefront consumers, resource-oriented names are clearer:

- `context`
- `categories`
- `products`
- `carts`
- checkout as a cart action: `POST /api/storefront/carts/{id}/checkout`

Recommended base path:

- `/api/storefront`

Local development host:

- `http://localhost:5064`

Local development base URL:

- `http://localhost:5064/api/storefront`

`Platform.StorefrontApi` is a separate HTTP host from the admin API. Do not call storefront endpoints through the admin API host unless the deployment explicitly fronts both services behind one gateway.

When configuring a consumer such as Nexra, prefer the full base URL and append endpoint paths such as `/context`, `/categories`, and `/products`. If a connector is configured with only the host/origin, it must append `/api/storefront` exactly once.

The sample identifiers in this contract, such as `WEB-SE`, `SE`, and `example-drill`, are illustrative. In local development they are available in the in-memory demo store, but a freshly migrated PostgreSQL database only contains baseline metadata such as catalog status definitions. PostgreSQL smoke tests must create matching channel, market, catalog, pricing, inventory, and storefront projection data first.

## Design Rules

The storefront API should:

- return read-optimized DTOs, not admin DTOs
- resolve market, culture, and currency explicitly
- expose buyability and availability as structured fields
- support stable identifiers such as `slug`, `productNumber`, and `sku`
- remain safe for external consumption

The storefront API should not:

- expose admin workflow fields
- expose audit/internal operational details
- require the CMS to understand admin write models

## Security Direction

Admin and backoffice APIs must always be authenticated and authorization-checked. They should not have a public mode.

Storefront security should separate client/deployment access from shopper/cart ownership:

- storefront catalog reads may be public or protected depending on project, channel, and deployment needs
- storefront write and commerce endpoints must require shopper, customer, session, or cart ownership proof
- server-side consumers such as CMS integrations may use API keys or OAuth client credentials
- browser clients must not rely on a browser API key as a secret; a browser-visible key can identify a frontend app, but it cannot protect private catalog data

Current cart ownership proof:

- cart creation returns a signed `cartAccessToken`
- cart read, reprice, and checkout require that token in the `X-Storefront-Cart-Token` header
- `rowVersion` is still only optimistic concurrency control; it is not an ownership secret
- if `StorefrontSecurity:CartAccessToken:SigningKey` is not configured, the API uses an ephemeral process-local signing key suitable for local smoke only

Recommended future configuration shape:

```json
{
  "StorefrontSecurity": {
    "CatalogReadMode": "Public",
    "AllowedOrigins": ["https://www.example.com"],
    "RequireClientCredentials": false,
    "RateLimitPolicy": "StorefrontRead"
  }
}
```

Suggested `CatalogReadMode` values:

- `Public`: catalog read endpoints are open, with CORS, rate limits, and gateway controls.
- `TrustedClientsOnly`: catalog read endpoints require API key or OAuth client credentials, useful for CMS/server-side rendering/private partner consumers.
- `Private`: catalog read endpoints require an authenticated shopper/customer token, useful for B2B or private catalogs.

Local development can default to `Public`. Production defaults should be chosen per project/channel. Any private catalog must use real authentication rather than a browser-exposed API key.

## Initial Endpoints

### 1. Context

Recommended endpoint:

- `GET /api/storefront/context`

Purpose:

- resolve the commercial context for a consumer before browsing products

Example query:

- `/api/storefront/context?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK`

Suggested response shape:

```json
{
  "channel": {
    "code": "WEB-SE",
    "hostName": "se.example.com"
  },
  "market": {
    "code": "SE",
    "name": "Sweden",
    "defaultCurrencyCode": "SEK",
    "defaultCultureCode": "sv-SE",
    "priceDisplayMode": "Gross"
  },
  "activeCultureCode": "en-GB",
  "availableCultureCodes": ["sv-SE", "en-GB"],
  "availableCurrencyCodes": ["SEK"]
}
```

Why `context` instead of `bootstrap`:

- it describes what the resource is
- it avoids frontend-specific language
- it reads clearly for CMS, app, and agent consumers

### 2. Categories

Recommended endpoints:

- `GET /api/storefront/categories`
- `GET /api/storefront/categories/{slug}`

Purpose:

- expose navigation trees and localized category landing data

Suggested fields:

- `id`
- `code`
- `slug`
- `name`
- `description`
- `parent`
- `children`
- `productCount`

### 3. Products

Recommended endpoints:

- `GET /api/storefront/products`
- `GET /api/storefront/products/{slug}`
- `GET /api/storefront/products/by-number/{productNumber}`

Purpose:

- expose browse/search and product detail in consumer-facing form

Suggested browse filters:

- `market`
- `culture`
- `category`
- `brand`
- `q`
- `sort`
- `page`
- `pageSize`

Suggested product summary fields:

- `id`
- `productNumber`
- `slug`
- `name`
- `brand`
- `primaryImageUrl`
- `price`
- `availability`
- `buyability`
- filter metadata for category/brand facets and supported sorts

Suggested product detail fields:

- core identity: `id`, `productNumber`, `slug`, `productType`
- localized content: `name`, `shortDescription`, `longDescription`
- media
- brand
- categories
- attributes
- variants
- related products
- price diagnostics
- availability diagnostics
- buyability diagnostics

## Commerce Diagnostics

For CMS and agent consumers, the response should not collapse everything into a single boolean.

Recommended fields:

```json
{
  "buyability": {
    "isVisible": true,
    "isBuyable": true,
    "reasons": []
  },
  "availability": {
    "status": "InStock",
    "availableQuantity": 25
  },
  "price": {
    "currencyCode": "SEK",
    "amount": 1499.00,
    "compareAtAmount": 1699.00,
    "vatIncluded": true
  }
}
```

This matters because Nexra and other CMS consumers need structured decisions, not text blobs.

## Nexra-Oriented First Slice

The first read-only Nexra smoke has passed against the local PostgreSQL smoke seed using the base URL `http://localhost:5064/api/storefront`.

For Nexra integration, the smallest useful storefront slice is:

1. `GET /api/storefront/context`
2. `GET /api/storefront/categories`
3. `GET /api/storefront/products`
4. `GET /api/storefront/products/{slug}`
5. `GET /api/storefront/products/by-number/{productNumber}`

That is enough for:

- commerce-aware listing pages
- product detail pages
- category navigation
- localized market-aware rendering
- targeted product refresh and stable product references through product-number lookup

It is not enough for:

- payment ownership
- full transactional commerce flows beyond order placement

## Recommended Implementation Order

1. context endpoint
2. category tree endpoint
3. product list/search endpoint
4. product detail endpoint
5. cart creation and repricing
6. cart checkout into an order
7. only after that, payment and fulfillment surfaces

## Cart And Checkout

Implemented endpoints:

- `POST /api/storefront/carts`
- `GET /api/storefront/carts/{id}`
- `POST /api/storefront/carts/{id}/reprice`
- `POST /api/storefront/carts/{id}/checkout`

Cart creation resolves the same storefront context as catalog reads:

```http
POST /api/storefront/carts?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
```

Example request:

```json
{
  "email": "buyer@example.com",
  "lines": [
    {
      "variantId": "50000000-0000-0000-0000-000000000011",
      "quantity": 1,
      "comment": null
    }
  ],
  "addresses": [
    {
      "type": "Billing",
      "firstName": "Alicia",
      "lastName": "Buyer",
      "line1": "Sveavagen 10",
      "postalCode": "11157",
      "city": "Stockholm",
      "countryCode": "SE",
      "email": "buyer@example.com"
    },
    {
      "type": "Shipping",
      "firstName": "Alicia",
      "lastName": "Buyer",
      "line1": "Sveavagen 10",
      "postalCode": "11157",
      "city": "Stockholm",
      "countryCode": "SE",
      "email": "buyer@example.com"
    }
  ]
}
```

Cart line variants must be visible and buyable in the resolved storefront context. The cart service validates against `StorefrontProductProjection`, including variant buyability and available quantity unless the variant is backorderable.

Cart responses include:

- `id`
- `marketId`
- `currencyCode`
- `cultureCode`
- `email`
- `status`
- `subtotal`
- `vatTotal`
- `grandTotal`
- `expiresAtUtc`
- `lines`
- `addresses`
- `rowVersion`
- `cartAccessToken`

Repricing is explicit, row-version guarded, and cart-token guarded:

```http
POST /api/storefront/carts/{id}/reprice
X-Storefront-Cart-Token: {cartAccessToken}
```

```json
{
  "rowVersion": "..."
}
```

Checkout converts an active cart into an order. It is also row-version guarded, cart-token guarded, and idempotent by source cart id:

```http
POST /api/storefront/carts/{id}/checkout
X-Storefront-Cart-Token: {cartAccessToken}
```

```json
{
  "rowVersion": "..."
}
```

Checkout requires a cart email plus at least one `Billing` address and one `Shipping` address. It validates the cart lines against the current storefront projection before conversion, then returns the placed order snapshot. Payment initiation and payment callbacks are still separate future work.

