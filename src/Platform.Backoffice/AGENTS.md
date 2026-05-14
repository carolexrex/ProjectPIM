# Backoffice Boundaries

- This project is an optional UI host over the admin API.
- Do not read or write persistence directly from the UI layer.
- Keep controllers focused on HTTP client orchestration, model binding, and view composition.
- If the UI needs new data or actions, add them to the admin API first rather than bypassing it.
