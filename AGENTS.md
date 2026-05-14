# Project Boundaries

## Layer Rules

- `src/Platform.Domain`
  Contains business entities, invariants, and domain exceptions only.
- `src/Platform.Application`
  Contains use-case contracts, commands, queries, and persistence abstractions.
- `src/Platform.Infrastructure`
  Contains EF/persistence, external integrations, and runtime implementations of application abstractions.
- `src/Platform.Api`
  Admin and integration HTTP surface only. No business rules beyond transport concerns.
- `src/Platform.StorefrontApi`
  Read-optimized storefront HTTP surface only. No admin workflows.
- `src/Platform.Backoffice`
  UI host only. It must consume APIs rather than bypass them.
- `src/Platform.Worker`
  Background jobs and async processing only.
- `src/Platform.Contracts`
  Transport DTOs and validation attributes shared across API boundaries.

## Dependency Direction

- `Api`, `StorefrontApi`, `Worker`, and `Backoffice` may depend on `Application`, `Infrastructure`, and `Contracts`.
- `Infrastructure` may depend on `Application`, `Domain`, and `Contracts`.
- `Application` may depend on `Domain` and `Contracts`.
- `Domain` must not depend on any other project.

## Persistence Rules

- Keep domain behavior provider-neutral.
- Keep provider-specific code inside `Infrastructure`.
- Prefer EF configurations and migrations over handwritten provider-specific SQL for active runtime paths.
- If raw SQL is required, isolate it and document the provider assumption.

## HTTP Rules

- Controllers stay thin: validate transport, call one application service, return DTOs/problem details.
- Do not place query shaping, authorization policy decisions, or persistence logic in controllers.

## Change Policy

- New features should preserve the modular monolith shape.
- Cross-module shortcuts need an explicit reason.
- If a change weakens these boundaries, document the tradeoff in the PR or commit message.
