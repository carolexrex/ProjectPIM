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

## Security Model

Current local smoke flows assume public storefront catalog reads. Production deployments should choose a storefront catalog read mode per project/channel:

- `Public`: open catalog reads, controlled with CORS, rate limits, and gateway rules.
- `TrustedClientsOnly`: API key or OAuth client credentials for CMS/server-side consumers.
- `Private`: authenticated shopper/customer token for private or B2B catalogs.

Browser-visible API keys are not secrets. They can identify the frontend app, but they do not protect private catalog data.

Cart creation returns a signed `cartAccessToken`. Storefront clients must send that token in `X-Storefront-Cart-Token` when reading, repricing, or checking out the cart. `rowVersion` is only optimistic concurrency control; it is not an ownership secret. Configure `StorefrontSecurity:CartAccessToken:SigningKey` for stable multi-instance or restart-safe cart tokens. If it is omitted, local development uses an ephemeral process-local signing key.

The `cartAccessToken` is a bearer capability, not an identity or session. It proves ownership of one cart and carries no shopper identity, so anyone holding it can read, reprice, and check out that cart until the cart expires. Clients must keep it confidential (treat it like a password for that cart) and must not log it or expose it to other users. A future shopper/customer/session login can layer on top of it.

### Storefront host protections (CORS and rate limiting)

`Platform.StorefrontApi` applies CORS and a coarse per-client rate limit at the host:

- `StorefrontSecurity:Cors:AllowedOrigins` — array of allowed browser origins. Outside Development, an empty list denies cross-origin browser access by default; in Development an empty list stays permissive for local smoke. Allowed methods are `GET` and `POST`.
- `StorefrontSecurity:RateLimiting` — `Enabled` (default `true`), `PermitLimit` (default `300`), and `WindowSeconds` (default `60`): a fixed-window limit partitioned by client IP, returning `429` with `Retry-After` when exceeded.

Behind a reverse proxy or gateway, configure forwarded headers so the real client IP is used for rate-limit partitioning, and keep gateway-level controls as the outer layer.

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
- a VAT rule (`/api/admin/vat-rules`) for market `SE` and each product `taxCategoryCode` sold there; carts are not priced without one
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

## 7. Create Cart

Endpoint:

- `POST /api/storefront/carts`

Purpose:

- create a priced storefront cart for the resolved market/culture/currency context

Example:

```http
POST http://localhost:5064/api/storefront/carts?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
```

```json
{
  "email": "buyer@example.com",
  "lines": [
    {
      "variantId": "50000000-0000-0000-0000-000000000011",
      "quantity": 1
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

Behavior notes:

- the endpoint resolves context with the same `channel`, `market`, `culture`, and `currency` parameters used by product reads
- cart lines must use storefront-visible and buyable variant ids from product detail responses
- line quantity must not exceed projected available quantity unless the variant is backorderable
- prices are resolved from active market price lists; each line's `vatRate` comes from the VAT rule for the cart market and the product tax category
- when the price list is VAT-inclusive (`vatIncluded: true` on product price metadata), the line `unitPrice` is the net amount `round(amount / (1 + vatRate), 2)`; `subtotal` is net, `vatTotal` is VAT, and `grandTotal` is gross
- a line whose tax category has no VAT rule in the market fails with `400` and `errors.TaxCategoryCode`; no default rate is assumed
- the response includes a `rowVersion` used by later cart actions and a `cartAccessToken` used as cart ownership proof

## 8. Get Cart

Endpoint:

- `GET /api/storefront/carts/{id}`

Purpose:

- return the current storefront cart snapshot

Example:

```http
GET http://localhost:5064/api/storefront/carts/{cartId}
X-Storefront-Cart-Token: {cartAccessToken}
```

## 9. Reprice Cart

Endpoint:

- `POST /api/storefront/carts/{id}/reprice`

Purpose:

- refresh line pricing before checkout or after a client-held cart becomes stale

Example:

```http
POST http://localhost:5064/api/storefront/carts/{cartId}/reprice
X-Storefront-Cart-Token: {cartAccessToken}
```

```json
{
  "rowVersion": "..."
}
```

## 10. Checkout Cart

Endpoint:

- `POST /api/storefront/carts/{id}/checkout`

Purpose:

- convert an active storefront cart into an order

Example:

```http
POST http://localhost:5064/api/storefront/carts/{cartId}/checkout
X-Storefront-Cart-Token: {cartAccessToken}
```

```json
{
  "rowVersion": "..."
}
```

Behavior notes:

- checkout reprices the cart and converts it to an order in the same application flow
- checkout requires the cart access token in `X-Storefront-Cart-Token`
- checkout requires cart email plus `Billing` and `Shipping` addresses
- checkout revalidates line visibility, status, and price against the current storefront projection before conversion; stock is not taken from the projection
- checkout reserves stock against live inventory balances in the same commit that creates the order: `ReservedQuantity` rises at the market's stock locations, on-hand stock is unchanged, and the variant projections are refreshed by the worker shortly after
- a stock shortfall returns `400` with one `VariantId.{variantId}` error per short variant, e.g. `Insufficient stock for SKU-EXAMPLE-1-BLACK in market 'SE': requested 3, available 1.`; nothing is committed and the same `rowVersion` can be retried later
- a backorderable balance accepts the shortfall instead of rejecting it
- if a concurrent checkout or stock change wins a race that the in-request retry cannot resolve, checkout returns `409` with an `errors.Checkout` message; nothing was committed and the client may retry with the same `rowVersion`
- checkout is idempotent by source cart id; repeating checkout for an already converted cart (including concurrent repeats) returns the existing order without reserving again, even when the projection now reports the variant out of stock
- payment initiation is not implemented in this slice; stock is held from order placement, not from payment authorization
- checkout reprices with the VAT rules in force at that moment; the order keeps that VAT snapshot even if rules change later, and a repeated checkout returns the original order
- tax covers configured market/tax-category VAT only; destination-based taxation, exemptions, and B2B reverse charge are not implemented

## Error Handling

Current error patterns:

- `400` when the storefront context is invalid
- `400` with `errors.TaxCategoryCode` when cart pricing finds no VAT rule for a line's market and tax category
- `400` with `errors["VariantId.{variantId}"]` when checkout finds insufficient stock (or no stock location in the market) for a variant
- `401` when cart access token ownership proof is missing or invalid
- `404` when the requested channel, market, category, brand, or product does not exist in the resolved context
- `409` with `errors.Checkout` when checkout loses a concurrent write race that the in-request retry could not resolve; retry with the same `rowVersion`

## Current Coverage

Implemented today:

1. storefront context resolution
2. category tree read
3. category detail by slug
4. product list/search
5. product detail by slug
6. product detail by product number
7. storefront cart creation/read/reprice
8. storefront cart checkout into an order

## Next Smoke

The next planned validation step is a live cart/checkout smoke from the consuming storefront or Nexra side against:

- `http://localhost:5064/api/storefront`

Smoke checklist:

1. resolve storefront context
2. read product detail for a visible and buyable variant
3. create a cart and store the returned `rowVersion` and `cartAccessToken`
4. call `GET /carts/{cartId}` with `X-Storefront-Cart-Token`
5. call `POST /carts/{cartId}/reprice` with `X-Storefront-Cart-Token` and the latest `rowVersion`
6. call `POST /carts/{cartId}/checkout` with `X-Storefront-Cart-Token` and the latest `rowVersion`
7. repeat checkout to verify idempotency by source cart id (and that reserved stock did not change again)
8. confirm missing or invalid `X-Storefront-Cart-Token` returns `401`
9. confirm the checkout reserved stock (admin `GET /api/admin/variants/{variantId}/inventory`) and that an order beyond available stock is rejected
10. cancel the smoke order (admin `POST /api/admin/orders/{orderId}/status`) and confirm the reservation is released

`scripts/seed-storefront-smoke.sh` runs all ten steps.

Do this before starting payment initiation/callbacks or fuller cart mutation endpoints.

## Nexra Smoke Status

The read-only Nexra smoke has passed against the local PostgreSQL smoke seed. Use [nexra-storefront-smoke.md](./nexra-storefront-smoke.md) for the tested seed data and URLs.
