# Nexra Storefront Smoke

This guide seeds a local PostgreSQL development database with the minimum catalog data Nexra needs for a read-only storefront contract smoke.

## Purpose

Use this when Nexra is pointed at the live `Platform.StorefrontApi` local host:

- `http://localhost:5064`

Canonical local Storefront API base URL:

- `http://localhost:5064/api/storefront`

The host/origin is `http://localhost:5064`; `/api/storefront` is part of the Storefront API base URL. Connectors should avoid appending `api/storefront` twice.

Nexra's connector has been checked with ProjectPIM and now accepts both host-only and full-base-url configuration forms. ProjectPIM documentation should still prefer the full base URL form.

The seed path creates the demo identifiers used by the storefront examples:

- channel `WEB-SE`
- market `SE`
- category slugs `tools` and `drills`
- brand `ACME`
- product slug `example-drill`
- product number `SKU-EXAMPLE-1`
- variant SKU `SKU-EXAMPLE-1-BLACK`

It also creates pricing, inventory, media metadata, market/channel assignments, and requests a storefront projection rebuild.

## Prerequisites

Start the full local stack:

```powershell
./scripts/start-dev.ps1
```

This starts:

- PostgreSQL
- Admin API on `http://localhost:5053`
- Storefront API on `http://localhost:5064`
- Backoffice on `http://localhost:5168`
- Worker for projection rebuilds and refresh processing

The default seed script login is:

```text
Username: test
Password: Test123!
```

## Seed PostgreSQL

Run:

```powershell
./scripts/seed-storefront-smoke.ps1
```

The script is designed to be rerunnable. It reuses existing smoke records when they already exist and upserts the pieces that need current row versions.

## Smoke URLs

After the script completes, Nexra can use the canonical base URL plus endpoint paths:

```text
Base URL: http://localhost:5064/api/storefront
```

```http
GET {baseUrl}/context?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
GET {baseUrl}/categories?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
GET {baseUrl}/categories/drills?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
GET {baseUrl}/products?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK&page=1&pageSize=24
GET {baseUrl}/products/example-drill?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
GET {baseUrl}/products/by-number/SKU-EXAMPLE-1?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
```

Expanded local URLs:

```http
GET http://localhost:5064/api/storefront/context?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
GET http://localhost:5064/api/storefront/categories?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
GET http://localhost:5064/api/storefront/categories/drills?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
GET http://localhost:5064/api/storefront/products?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK&page=1&pageSize=24
GET http://localhost:5064/api/storefront/products/example-drill?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
GET http://localhost:5064/api/storefront/products/by-number/SKU-EXAMPLE-1?channel=WEB-SE&market=SE&culture=sv-SE&currency=SEK
```

## Nexra Result

The read-only Nexra smoke has completed successfully against the local PostgreSQL smoke seed using:

- base URL `http://localhost:5064/api/storefront`
- channel `WEB-SE`
- market `SE`
- culture `sv-SE`
- currency `SEK`
- product slug `example-drill`
- product number `SKU-EXAMPLE-1`

No ProjectPIM storefront contract gap was identified from this smoke. The only integration note was connector configuration terminology: ProjectPIM treats `http://localhost:5064/api/storefront` as the Storefront API base URL, while `http://localhost:5064` is only the host/origin.

## Troubleshooting

If context works but product endpoints are empty, the storefront projection rebuild probably has not completed. Make sure `Platform.Worker` is running and rerun:

```powershell
./scripts/seed-storefront-smoke.ps1
```

To queue data without waiting for the projection rebuild:

```powershell
./scripts/seed-storefront-smoke.ps1 -ProjectionWaitSeconds 0
```

To skip endpoint checks:

```powershell
./scripts/seed-storefront-smoke.ps1 -SkipSmokeCheck
```

## Boundary

This is a local smoke seed path only. It uses admin APIs to create normal platform data; it is not a production migration and should not be treated as customer or tenant bootstrap logic.
