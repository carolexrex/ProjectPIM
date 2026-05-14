# Domain Boundaries

- Keep this project free of EF, HTTP, DTO, and UI concerns.
- Put business invariants and lifecycle rules here, not in controllers or repositories.
- Value objects and domain exceptions belong here when they protect model correctness.
- If persistence needs extra state, map it in infrastructure rather than weakening the domain model.
