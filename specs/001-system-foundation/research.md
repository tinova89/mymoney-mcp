# Research: System Foundation

Source of each decision: **Repo** = this repo's `CLAUDE.md`/`README.md`/constitution;
**Spec** = `spec.md` clarifications; **Proposed** = chosen in this plan.

## R1. Stack (Repo)

- **Decision**: .NET 10, .NET Aspire (AppHost + ServiceDefaults), PostgreSQL via EF Core + Npgsql,
  MediatR, custom JWT, MCP and REST on the same API host. Solution file `MymoneyMcp.slnx`.
- **Rationale**: fixed by `CLAUDE.md` and the constitution. SDK 10.0.401 is installed locally.

## R2. MCP server library (Proposed)

- **Decision**: official C# MCP SDK (`ModelContextProtocol.AspNetCore`), Streamable HTTP transport,
  mapped at `/mcp`, tools discovered from the Api assembly (none in this feature).
- **Alternatives**: hand-written JSON-RPC endpoint — rejected, re-implements the protocol.
- **Risk**: claude.ai custom connectors do not let the user set arbitrary headers such as
  `X-Account-Group` or a static bearer token. Claude Code (`claude mcp add --transport http
  --header ...`) does. Connecting from claude.ai is out of scope here; tracked in
  `specs/backlog.md`.

## R3. MediatR licence (Proposed)

- **Decision**: pin MediatR **12.x** (last Apache-2.0 line).
- **Rationale**: MediatR 13+ requires a licence key (free community tier exists, but adds setup).
- **Alternatives**: MediatR 13+ with community licence — revisit if a 12.x limitation appears.

## R4. Validation (Proposed)

- **Decision**: FluentValidation, run by a MediatR `ValidationBehavior` before every handler
  (Constitution III).
- **Alternatives**: DataAnnotations — rejected, weak for cross-field rules and hard to test alone.

## R5. Group isolation (Spec FR-002, Constitution II)

- **Decision**: a scoped `IAccountGroupContext` reads and trims `X-Account-Group`. The
  `DbContext` applies an EF Core 10 **named query filter** `"AccountGroup"` on `Account`
  (`a.Group == context.Group`). Named filters let a later soft-delete filter coexist and be
  turned off independently.
- **Rationale**: one central place; features never write their own group `Where`.
- **Lookups**: use `FirstOrDefaultAsync` by id (filtered) — another group's id returns `null` →
  404, same as a missing id (FR-003).
- **Performance**: index on `accounts.group`. Child tables in later features carry their own
  indexed `group` column (decision deferred to each feature's plan, Constitution II).
- **Alternatives**: PostgreSQL row-level security — rejected for now: harder to test, couples
  auth to DB roles. Repository-level filtering — rejected, easy to forget.

## R6. Header enforcement and middleware order (Spec FR-004)

- **Decision**: order is `UseAuthentication` → `UseAuthorization` → `RequireAccountGroup` →
  endpoints. Unauthenticated calls get `401` first; authenticated calls without the header get
  `400` (`application/problem+json`). Exempt path prefixes: `/health`, `/alive`, `/openapi`,
  `/auth/`.
- **Rationale**: an anonymous caller learns nothing about header rules; matches spec US3.

## R7. Token issuance — own issuer (Spec FR-006/007, Proposed credentials)

- **Decision**: `POST /auth/token` uses **client credentials** (`clientId` + `clientSecret`)
  configured on the server (user-secrets locally, environment in production). Issues an HMAC-SHA256
  JWT (`sub` = clientId, `iss`, `aud`, `iat`, `exp`) plus an opaque refresh token.
- **Refresh**: `POST /auth/refresh` exchanges a refresh token for a new pair and revokes the old
  one (rotation). Refresh tokens are stored hashed (SHA-256) in `refresh_tokens`.
- **Defaults**: access token 60 min, refresh token 14 days, both configurable.
- **Rationale**: the spec has no user entity; callers are machines (Claude, other clients). A user
  store belongs with the backlog item "Restrict account groups per caller".
- **Alternatives**: username/password table — rejected, creates a user model before it is needed.

## R8. External issuer (Spec FR-007)

- **Decision**: a second JWT bearer scheme `External`, configured by `Auth:External:Authority`
  (OIDC discovery for signing keys) and `Auth:External:Audience`. A policy scheme picks `Local` or
  `External` by the token's `iss`. Registered only when configured.
- **Alternatives**: one scheme with many issuers — rejected, cannot fetch external keys cleanly.

## R9. Turning token checking off (Spec FR-014)

- **Decision**: `Auth:Enabled` (default `true`). When `false`, the fallback authorization policy
  allows anonymous calls and a warning is logged at startup. If `false` and
  `IHostEnvironment.IsProduction()`, startup throws.
- **Rationale**: ASP.NET Core treats an unset environment as `Production`, so the guard fails safe.

## R10. Database creation and migrations (Spec FR-011)

- **Decision**: AppHost provisions PostgreSQL (`AddPostgres("mymoneymcp-postgres")
  .AddDatabase("mymoneymcp-db")`, names from README). The API applies pending EF Core migrations
  at startup.
- **Failure**: fail fast — if the database is unreachable or a migration fails at startup, the
  API stops with the error logged. PostgreSQL rolls the failed migration back, so the schema stays
  on the previous version. Losing the database **after** startup does not stop the API: `/health`
  reports unhealthy (traffic stops), `/alive` stays OK, and traffic returns when it recovers.
- **Alternatives**: separate Aspire migration worker — rejected for now: an extra project
  (Constitution VIII) for a single-instance family app.

## R11. Health routes (Spec FR-012)

- **Decision**: map `/health` (includes a database check) and `/alive` in **every** environment.
  The Aspire ServiceDefaults template maps them only in Development, so that line is changed.
- **Note**: health responses expose status only, no details.

## R12. Testing (Constitution IV/V)

- **Decision**: xUnit; `MymoneyMcp.Application.Tests` for unit tests; `MymoneyMcp.Api.Tests` for
  integration tests with `WebApplicationFactory` + **Testcontainers PostgreSQL** (real engine for
  query filters). The MCP test uses the SDK's client over the test server.
- **Alternatives**: Aspire `DistributedApplicationTestingBuilder` — kept for one smoke test only;
  slower to start than `WebApplicationFactory`.

## R13. Package versions (Proposed)

- **Decision**: central package management (`Directory.Packages.props`); pick the latest stable
  version of each package at implementation time and record it there.
