# Storefront API Usage

This guide describes how to consume the currently implemented `Platform.StorefrontApi` endpoints.

## Base Path

- `/api/storefront`

Local development host/origin:

- `http://localhost:5064`

Local development base URL:

- `http://localhost:5064/api/storefront`

The storefront API is hosted by `Platform.StorefrontApi`, separate from the admin API host on `http://localhost:5053`.

Use one URL convention in a connector:

- full base URL: `http://localhost:5064/api/storefront`, then call `{baseUrl}/context`
- host plus path: `http://localhost:5064` plus `/api/storefront/context`

Do not combine both forms into `http://localhost:5064/api/storefront/api/storefront/...`.

## Data Prerequisites

The examples below use the demo identifiers `WEB-SE`, `SE`, `tools`, `example-drill`, and `SKU-EXAMPLE-1`.

Those identifiers exist in the in-memory demo store used by tests and contract smoke runs. The default local development configuration uses PostgreSQL, and EF migrations currently seed catalog status definitions only. A freshly migrated PostgreSQL database will not contain the demo channel, market, products, categories, or storefront product projections.

For a quick contract smoke against the demo data, run `Platform.StorefrontApi` with:

```powershell
$env:Persistence__Provider = "InMemory"
$env:ASPNETCORE_URLS = "http://localhost:5064"
dotnet run --project .\src\Platform.StorefrontApi\Platform.StorefrontApi.csproj
```

For a live PostgreSQL smoke, create at least:

- market `SE` with supported culture/currency
- channel `WEB-SE` assigned to market `SE`
- visible categories, products, variants, prices, and inventory
- storefront product projections, either through the projection rebuild job or worker-processed refresh requests

Product browse/detail endpoints read from `StorefrontProductProjection`, so admin writes alone are not enough for product smoke tests until projections have been built.

The supported local PostgreSQL seed path is documented in [nexra-storefront-smoke.md](./nexra-storefront-smoke.md).

## 1. Resolve Context

Endpoint:

- `GET /api/storefront/context`

Purpose:

- resolve the active channel, market, culture, and currency before requesting catalog data

Example:

```http
GET http://localhost:5064/api/storefront/context?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
```

Response shape:

- `channel`
- `market`
- `activeCultureCode`
- `activeCurrencyCode`
- `availableCultureCodes`
- `availableCurrencyCodes`

Behavior notes:

- if an unsupported culture is requested, the API falls back to the market default culture
- if an unsupported currency is requested, the API falls back to the market default currency
- if `channel` is omitted, the API can resolve the channel from the request host when a host mapping exists
- if a channel maps to multiple markets, the client must pass `market`

## 2. List Categories

Endpoint:

- `GET /api/storefront/categories`

Purpose:

- return the localized storefront category tree

Recommended query parameters:

- `channel`
- `market`
- `culture`
- `currency`

Example:

```http
GET http://localhost:5064/api/storefront/categories?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
```

Response shape:

- recursive category nodes with:
  - `id`
  - `code`
  - `slug`
  - `name`
  - `description`
  - `parentCategoryId`
  - `sortOrder`
  - `children`

Behavior notes:

- category names, slugs, and descriptions are resolved against the active culture
- if the requested culture is missing on a category, the API falls back to `en-GB`, then to the first available translation, then to the category code

## 3. Get Category By Slug

Endpoint:

- `GET /api/storefront/categories/{slug}`

Purpose:

- return one localized category with breadcrumb data and child categories

Example:

```http
GET http://localhost:5064/api/storefront/categories/drills?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
```

Response shape:

- `id`
- `code`
- `slug`
- `name`
- `description`
- `parentCategoryId`
- `sortOrder`
- `breadcrumbs`
- `children`

Behavior notes:

- breadcrumb names and slugs are localized using the same culture fallback rules as the category tree

## 4. List Products

Endpoint:

- `GET /api/storefront/products`

Purpose:

- return paged storefront product summaries for listing, category landing, and search-like browse scenarios

Recommended query parameters:

- `channel`
- `market`
- `culture`
- `currency`
- `category`
- `brand`
- `q`
- `sort`
- `page`
- `pageSize`

Example:

```http
GET http://localhost:5064/api/storefront/products?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK&category=tools&brand=ACME&q=drill&sort=name&page=1&pageSize=24
```

Response shape:

- paged response fields:
  - `items`
  - `total`
  - `page`
  - `pageSize`
- browse metadata:
  - `appliedFilters`
  - `facets.categories`
  - `facets.brands`
  - `facets.sortOptions`
- each product item includes:
  - `id`
  - `productNumber`
  - `slug`
  - `name`
  - `shortDescription`
  - `brand`
  - `primaryImageUrl`
  - `hasVariants`
  - `price`
  - `availability`
  - `buyability`

Behavior notes:

- `category` is resolved by category slug and includes descendant categories, not only direct assignments
- `brand` is resolved by brand code
- supported `sort` values are `productnumber`, `-productnumber`, `name`, and `-name`
- products must be active and actively assigned to the resolved market to appear in the list
- product names and descriptions follow the same culture fallback pattern as other storefront reads: requested culture, then `en-GB`, then first available translation
- product `price` is resolved from the first matching active market price list for the active currency
- product `availability` aggregates variant inventory from inventory locations assigned to the resolved market
- `buyability.reasons` returns machine-readable diagnostic codes such as `MissingPrice`, `OutOfStock`, and `NoBuyableVariants`
- facets are derived from the current market-visible result set after text query filtering; category facets also respect the selected brand, and brand facets respect the selected category tree

## 5. Get Product By Slug

Endpoint:

- `GET /api/storefront/products/{slug}`

Purpose:

- return a storefront product detail payload with localized content, categories, attributes, media, variants, and commerce diagnostics

Example:

```http
GET http://localhost:5064/api/storefront/products/example-drill?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
```

Response shape:

- core fields:
  - `id`
  - `productNumber`
  - `slug`
  - `productType`
  - `name`
  - `shortDescription`
  - `longDescription`
  - `seoTitle`
  - `seoDescription`
- linked data:
  - `brand`
  - `categories`
  - `media`
  - `attributes`
  - `variants`
- commerce diagnostics:
  - `price`
  - `availability`
  - `buyability`

Behavior notes:

- the detail endpoint only returns products visible in the resolved market
- variant entries include their own media, attribute values, price, availability, and buyability diagnostics
- `availability.status` is one of `InStock`, `Backorderable`, `OutOfStock`, or `Unavailable`
- top-level `price` reflects the lowest currently resolved visible variant price

## 6. Get Product By Product Number

Endpoint:

- `GET /api/storefront/products/by-number/{productNumber}`

Purpose:

- resolve a product through a stable commerce identifier when slug is not the integration key

Example:

```http
GET http://localhost:5064/api/storefront/products/by-number/SKU-EXAMPLE-1?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
```

Behavior notes:

- this endpoint returns the same detail payload as slug lookup
- market visibility rules are identical to `GET /api/storefront/products/{slug}`

## Error Handling

Current error patterns:

- `400` when the storefront context is invalid
- `404` when the requested channel, market, category, brand, or product does not exist in the resolved context

## Current Coverage

Implemented today:

1. storefront context resolution
2. category tree read
3. category detail by slug
4. product list/search
5. product detail by slug
6. product detail by product number

## Nexra Smoke Status

The read-only Nexra smoke has passed against the local PostgreSQL smoke seed. Use [nexra-storefront-smoke.md](./nexra-storefront-smoke.md) for the tested seed data and URLs.
