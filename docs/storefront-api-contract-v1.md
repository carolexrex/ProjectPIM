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

Deferred from the first storefront slice:

- cart mutation endpoints
- checkout initiation
- payment callbacks
- advanced search merchandising

## Naming Direction

Avoid vague naming such as `bootstrap`.

For storefront consumers, resource-oriented names are clearer:

- `context`
- `categories`
- `products`
- later `carts`
- later `checkout`

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

That is enough for:

- commerce-aware listing pages
- product detail pages
- category navigation
- localized market-aware rendering

It is not enough for:

- cart/checkout ownership
- full transactional commerce flows

## Recommended Implementation Order

1. context endpoint
2. category tree endpoint
3. product list/search endpoint
4. product detail endpoint
5. only after that, cart and checkout surfaces
