# Persistence Boundaries

- Keep provider-specific EF and PostgreSQL behavior in this area.
- Migrations, DbContext configuration, and concurrency mapping live here.
- Prefer provider-neutral domain and application contracts; adapt them here.
- If raw SQL is introduced, isolate it and document the PostgreSQL assumption beside the code.
