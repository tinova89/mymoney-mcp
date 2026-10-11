# Architecture

Current technical shape of the system. Decisions and alternatives per feature live in each
feature's `research.md` (first: [001 research](001-system-foundation/research.md)).

## Projects

| Project | Role | References |
|---|---|---|
| `MymoneyMcp.AppHost` | Aspire orchestration: PostgreSQL + API | Api |
| `MymoneyMcp.ServiceDefaults` | Telemetry, resilience, `/health` + `/alive` in every environment | — |
| `MymoneyMcp.Api` | REST + MCP host, auth schemes, middleware | Application, Infrastructure.Data, Infrastructure.Auth, ServiceDefaults |
| `MymoneyMcp.Application` | MediatR handlers, validators, interfaces (one folder per feature) | Domain |
| `MymoneyMcp.Domain` | Entities (`Account`, `RefreshToken`) — no packages | — |
| `MymoneyMcp.Infrastructure.Data` | EF Core `AppDbContext`, configurations, migrations, refresh-token store | Application |
| `MymoneyMcp.Infrastructure.Auth` | JWT and refresh-token issuance, client credentials | Application |

Infrastructure projects never reference each other (Constitution, architecture constraints).
Package versions: `Directory.Packages.props` (MediatR pinned to 12.x, the last Apache-2.0 line).

## Aspire resources

| Resource | Kind | Notes |
|---|---|---|
| `mymoneymcp-postgres` | PostgreSQL container | — |
| `mymoneymcp-db` | Database | Connection string injected as `ConnectionStrings:mymoneymcp-db` |
| `mymoneymcp-api` | Project | Waits for the database; health check on `/health` |

## Persistence

- `AppDbContext` is registered without pooling (it depends on the scoped group context) and
  enriched by Aspire (retries, telemetry, database health check).
- Named query filter `AccountGroup` on `Account`: `Group == current request group`. Later filters
  (e.g. soft delete) use their own names so they can be turned off independently.
- Tables: `accounts` (index `ix_accounts_group`), `refresh_tokens` (unique `token_hash`).
- Migrations are applied at startup; failure stops the host (fail fast).
- Refresh rotation: conditional `UPDATE ... WHERE revoked_at IS NULL` plus insert, in one
  transaction inside the EF execution strategy.

## Authentication

| Scheme | Accepts | Notes |
|---|---|---|
| `Bearer` (policy) | — | Picks `External` when the token's `iss` matches the external issuer, else `Local` |
| `Local` | Tokens issued by `/auth/token` | HMAC-SHA256, `Jwt:*` settings |
| `External` | Trusted issuer tokens | OIDC discovery via `Auth:External:Authority`; registered only when configured |

- Fallback authorization policy requires an authenticated caller when `Auth:Enabled=true`.
- Startup guard: auth off in Production, or auth on without a 32-byte signing key → startup fails.

## Request pipeline

1. `UseExceptionHandler` — application exceptions → RFC 9457 problem details (no attempted values).
2. `UseAuthentication` → `UseAuthorization` — `401` for missing/invalid tokens.
3. `RequireAccountGroupMiddleware` — `400` when `X-Account-Group` is missing or blank.
4. Endpoints: `/health`, `/alive`, `/openapi` (Development), `/auth/*`, `/mcp`.

## Testing

- `MymoneyMcp.Application.Tests`: validators and handlers with fakes.
- `MymoneyMcp.Api.Tests`: real host (`WebApplicationFactory`) on real PostgreSQL (Testcontainers,
  Docker required), MCP SDK client, and an Aspire AppHost smoke test.
- Runner: Microsoft Testing Platform (`global.json` → `test.runner`), xUnit v3.
