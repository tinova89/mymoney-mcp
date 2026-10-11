# Backlog

| # | Feature | Status | Notes |
|---|---|---|---|
| 001 | [System foundation](001-system-foundation/spec.md) | Implemented | Solution, database, account-group isolation, auth, empty MCP host |
| — | Restrict account groups per caller | Planned | Next after 001. Today any authenticated caller may use any group (001 FR-009): a leaked token exposes every family's data. Store which groups each caller may use and reject the rest. |
| — | Connect from claude.ai | Planned | claude.ai custom connectors cannot send `X-Account-Group` or a static bearer token (001 research R2). Study how to authenticate and pass the group from claude.ai (e.g. OAuth for connectors, group chosen another way). |
| — | CSV statement import | Planned | Creates the `MymoneyMcp.Infrastructure.Import` project (one infrastructure project per concern, Constitution 2.2.0). Depends on account types and transactions existing first. |
| — | CI pipeline (GitHub Actions) | Planned | Run `dotnet build` and `dotnet test` on every push/PR. Use an `ubuntu-latest` runner: it ships with Docker, so the Testcontainers PostgreSQL integration tests run with no extra setup (Windows/macOS runners do not support them). |
| — | Persistent local database (AppHost) | Planned | Today `AddPostgres` has no data volume: every AppHost run starts with an empty database, and EF Core logs a harmless "Failed executing DbCommand" on `__EFMigrationsHistory` before creating it. Add `.WithDataVolume()` so data survives restarts (and that log only shows on the first run); optionally `.WithLifetime(ContainerLifetime.Persistent)` so the container stays up between runs and startup is faster. Update `specs/architecture.md` (Aspire resources). |
