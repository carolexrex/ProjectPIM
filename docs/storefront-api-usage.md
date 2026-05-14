# Storefront API Usage

This guide describes how to consume the currently implemented `Platform.StorefrontApi` endpoints.

## Base Path

- `/api/storefront`

## 1. Resolve Context

Endpoint:

- `GET /api/storefront/context`

Purpose:

- resolve the active channel, market, culture, and currency before requesting catalog data

Example:

```http
GET /api/storefront/context?channel=WEB-SE&market=SE&culture=en-GB
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
GET /api/storefront/categories?channel=WEB-SE&market=SE&culture=en-GB
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
GET /api/storefront/categories/drills?channel=WEB-SE&market=SE&culture=en-GB
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
GET /api/storefront/products?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK&category=tools&brand=ACME&q=drill&sort=name&page=1&pageSize=24
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
GET /api/storefront/products/example-drill?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK
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
GET /api/storefront/products/by-number/SKU-EXAMPLE-1?channel=WEB-SE&market=SE&culture=en-GB&currency=SEK
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
