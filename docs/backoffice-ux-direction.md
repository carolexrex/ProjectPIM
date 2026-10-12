# Backoffice UX Direction

## Purpose

This document captures the intended Backoffice UX direction for ProjectPIM and the future Natrium product identity.

It should be read with:

- `ux-mockup.html`
- `docs/pim-roadmap-v1.md`
- `docs/admin-api-contract-v1.md`
- `docs/admin-api-implementation-design-v1.md`

The current static `ux-mockup.html` is a good product-management prototype for catalog list/detail workflows. The Nexra backoffice in `/Users/alexandervaldemarsson/Developer/Nexra/ui/backoffice` is the stronger source for the shared shell and visual theme.

## Product Naming Direction

The platform may later be renamed from ProjectPIM to Natrium.

Current implementation note:

- The running Backoffice shell currently shows `Natrium` / `Natrium Backoffice`.
- Code namespaces, project names, database names, scripts, and most current-host documentation still use `Platform` / `ProjectPIM`.
- Treat the current shell label as UX/product branding only until a deliberate technical rename plan exists.

Until that technical rename is explicitly scheduled:

- keep code namespaces, solution names, database names, docs that describe current hosts, and scripts on the existing ProjectPIM/Platform naming
- use Natrium only for user-facing product branding and forward-looking UX/product notes
- do not rename code, projects, databases, scripts, or operational docs opportunistically

When the rename starts, prefer this split:

- public product name: `Natrium`
- user-facing UI shell: `Natrium Backoffice`
- technical module names: migrate in a separate code/package plan after UX copy and docs are agreed

## Theme Alignment With Nexra

The Backoffice should align with the Nexra theme rather than the older warm MVC styling currently in `src/Platform.Backoffice/wwwroot/css/site.css`.

Observed Nexra tokens:

| Token role | Value |
|---|---|
| Base background | `#0D0D0F` |
| Topbar | `#131316` |
| Sidebar | `#18181C` |
| Panels | `#1E1E23` |
| Borders | `#2B2B31` |
| Primary text | `#EEEEF2` / `#F7F7FA` |
| Muted text | `#7A7A88` / `#5E5E6A` |
| Accent | amber `#F59E0B` |
| Accent hover | `#FBBF24` |
| Success | emerald `#10B981` |
| Sync/info | blue `#60A5FA` |
| Error | red `#F87171` |
| Radius | `6px` default, `10px` larger panels |
| Sidebar width | `236px` |
| Topbar height | `52px` |

Typography:

- use `Inter` or system sans for normal UI text
- use `JetBrains Mono`, `Fira Code`, or `Cascadia Code` for identifiers, status bars, badges, technical labels, and command surfaces
- keep the UI compact, dense, and operational

Interaction language:

- amber marks the active route, primary actions, publish/retry emphasis, and topbar brand line
- green marks healthy/live/succeeded state
- blue marks sync/running/informational state
- red marks failure, abandoned processing, destructive action, or validation risk
- mono badges should carry machine-like state such as `READY`, `OPEN`, `ABANDONED`, `SYNC`

## Shell Direction

Adopt Nexra's shell pattern for Backoffice:

- full-height grid shell
- topbar spanning the whole app
- dark sidebar with grouped navigation
- compact page headers
- dense tables and panels
- optional bottom status bar for operator/developer surfaces
- command palette where it improves navigation

Recommended main navigation groups:

- Catalog: Products, Variants, Brands, Categories, Attributes, Media
- Commerce: Price Lists, Inventory, Carts, Orders, Customers, Companies
- Operations: Storefront Ops, Integration Jobs, Webhooks, Imports/Exports, Terminal
- Configuration: Markets, Channels, Product Statuses, Admin Users
- AI: Suggestions, Jobs, Prompt Templates, only when the AI foundation exists

The current `ux-mockup.html` remains useful for:

- product list density
- filters and stat strips
- product detail tabs
- semantic status pills
- catalog/commerce grouping

It should be restyled toward the Nexra theme before it becomes the implementation source.

## API-First Boundary

Backoffice remains an optional UI host over the admin API.

Rules:

- UI screens must consume `Platform.Api` admin endpoints
- UI must not read EF/persistence directly
- workflow rules belong in application services, not only in UI components
- any missing data needed by Backoffice should first become an admin API/application-service capability

This is especially important for operator tools and terminal-like workflows.

## Terminal And Operator Console

A terminal can fit the Backoffice plan, but it should be introduced as an operator console in the Operations area, not as an unrestricted browser-to-host shell.

Recommended path:

1. Command palette
   - Navigate Backoffice routes.
   - Trigger safe UI actions such as open product, view order, start projection rebuild, or replay an abandoned refresh.

2. API command console
   - Let privileged users run saved/admin-approved API commands against `Platform.Api`.
   - Show request, response, actor, correlation id, and audit trail.
   - Support examples for common operations such as projection rebuild, webhook replay, job lookup, and smoke diagnostics.

3. Operations terminal
   - Add an interactive terminal-like screen under Operations only after stronger identity, audit logging, and operator permissions are complete.
   - Prefer commands that call explicit admin APIs or enqueue worker jobs.
   - Avoid direct database writes, direct EF access, or bypassing application services.

4. Optional host shell
   - Treat a real server shell as a separate self-hosted/developer feature, disabled by default.
   - Gate it behind `PlatformAdmin` or a stricter future `OperatorAdmin` policy.
   - Require full audit logging, session recording, environment labeling, and deployment-level opt-in.
   - Prefer an allowlisted command profile before allowing arbitrary shell execution.

Plan fit:

- earliest useful slice: after the current Storefront Ops view, as an Operations command palette/API console
- required before a real terminal: stronger identity/audit, admin user policies, correlation ids, and explicit operator APIs
- not part of storefront/public APIs
- not part of AI chat workflows

## Backoffice Implementation Phases

### Phase A: Theme Documentation And Mockup Alignment

Status: documented.

Scope:

- preserve the current static mockup as the commerce workflow reference
- adopt Nexra tokens as the target visual language
- document Natrium naming direction without renaming code yet

### Phase B: MVC Theme Refresh

Goal: make the existing Razor Backoffice look and feel like the Nexra shell while keeping current routes.

Status: partially implemented.

Scope:

- replace warm beige/rounded styling with dark Nexra-style tokens
- change sidebar/topbar layout toward the Nexra shell
- group navigation by Catalog, Commerce, Operations, and Configuration
- keep all current API-backed screens working

### Phase C: Backoffice UX Completion

Goal: make Backoffice feel like a complete operational product surface.

Scope:

- refine product list/detail screens using `ux-mockup.html`
- add product-level effective price and storefront buyability diagnostics so operators can answer "what will this product cost?" and "why is this not buyable?" without calling storefront APIs by hand
- replace large product/variant/category selects with search-as-you-type pickers before real catalog volume lands
- add UI for product status definitions and admin users when the corresponding admin API surface is complete enough
- add consistent filters, tables, status badges, empty states, and pagination
- improve Storefront Ops and integration-job visibility
- add an Import workspace for source setup, connector mapping review, dry-run summaries, candidate conflicts, validation issues, and import progress
- keep AI-assisted source mappings visibly provisional until approved; show source evidence and confidence where it helps an operator resolve ambiguity
- prepare AI review screens only after AI contracts are implemented

### Phase D: Operations Console

Goal: add a safe terminal-like operator workflow.

Scope:

- command palette
- saved API commands
- request/response console
- job/webhook/projection diagnostics
- full audit of commands and outputs where practical

### Phase E: Optional Host Terminal

Goal: support advanced self-hosted operations only when explicitly enabled.

Scope:

- separate server-side terminal service or worker capability
- strict policy gate
- environment warnings
- session audit/recording
- command allowlists or profiles

This phase should not start until the Operations Console proves which commands users actually need.

## Agent Pickup Notes

After reviewing the current docs, the active workstream is still:

1. commerce correctness gates are done: order-placement inventory reservation with cancellation release and completion shipment, tax-category/market VAT resolution, and projection price entry windows with time-based refresh (VAT rules and order inventory allocations are API-only; the location transaction table now shows reserved-quantity deltas, but there is no allocation screen yet)
2. validate the live storefront cart/checkout smoke from Nexra or another consuming storefront
3. choose the next commerce slice: payment initiation/callbacks or fuller cart line mutation operations
4. keep Backoffice API-first and improve the UX around existing admin APIs, especially effective-price and buyability diagnostics
5. defer AI to the proposal/review workflow described in `docs/ai-api-backoffice-contract.md`

The old handover remains useful, but it predates the current desire to align the Backoffice visual language with Nexra and the future Natrium naming direction.
