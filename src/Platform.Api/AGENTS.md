# API Boundaries

- Controllers translate HTTP to application commands and queries only.
- Keep validation at transport level and return DTOs or problem details.
- Do not place business rules, query shaping, or persistence code in controllers.
- New endpoints should reuse application services instead of introducing API-local workflow logic.
