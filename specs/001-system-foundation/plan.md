# Implementation Plan: System Foundation

**Branch**: `001-system-foundation` | **Date**: 2026-10-10 | **Spec**: [spec.md](spec.md)

## Summary

Create the .NET 10 / Aspire solution from the `CLAUDE.md` layout with PostgreSQL, an `Account`
entity isolated by `X-Account-Group` through one EF Core named query filter, JWT auth (own client
credentials issuer + optional external issuer, switchable off outside production), and an MCP
endpoint with no tools. Decisions and alternatives: [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10 (SDK 10.0.401)

**Primary Dependencies**: .NET Aspire, EF Core + Npgsql, MediatR 12.x, FluentValidation,
`ModelContextProtocol.AspNetCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`

**Storage**: PostgreSQL (Aspire resource locally; connection string from configuration elsewhere)

**Testing**: xUnit, `WebApplicationFactory`, Testcontainers PostgreSQL, MCP SDK client

**Target Platform**: Linux container or Windows host running ASP.NET Core

**Project Type**: web service (REST + MCP on one host)

**Performance Goals**: family scale — group filter served by an index; no other targets yet

**Constraints**: no secrets in the repo; health routes in every environment; auth off is
forbidden in Production

**Scale/Scope**: a few groups, under 10 concurrent callers, 2 tables in this feature

## Constitution Check

*Gate before Phase 0; re-checked after Phase 1.*

| Principle | How this plan complies | Status |
|---|---|---|
| I. Specs first, concise | Spec 150 lines; this plan under 200; closing doc tasks listed below | ✅ |
| II. Accounts are the root | `Account.Group` + named filter `"AccountGroup"`; index on `group`; other group → 404; cross-group tests | ✅ |
| III. Thin edges, CQRS | `/auth/token` and `/auth/refresh` send `IssueTokenCommand`/`RefreshTokenCommand`; `ValidationBehavior` before handlers | ✅ |
| IV. Test-backed | Unit tests for validators, token service, startup guard; integration tests for every acceptance scenario | ✅ |
| V. Real engine | Isolation and refresh rotation tested on Testcontainers PostgreSQL; MCP tested through the real host | ✅ |
| VI. Data integrity | No money in scope; timestamps are `timestamptz`; no delete operation yet, so no soft delete needed | ✅ N/A parts |
| VII. Contracts & security | DTOs only; header required (auth/infra exempt); JWT has no group; secrets via user-secrets/env | ✅ |
| VIII. Simplicity | Projects follow the layout (`Infrastructure.Data` + `.Auth` by convention); new dependencies justified below | ✅ justified |

**Post-design re-check (after Phase 1)**: unchanged — data model, contracts, and quickstart add no
new violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-system-foundation/
├── spec.md · plan.md · research.md · data-model.md · quickstart.md
├── contracts/  (http-conventions.md, auth-api.md, mcp-endpoint.md)
├── checklists/requirements.md
└── tasks.md    (/speckit-tasks)
```

### Source Code

```text
MymoneyMcp.slnx
Directory.Build.props            # net10.0, nullable, TreatWarningsAsErrors
Directory.Packages.props         # central package versions
src/
  MymoneyMcp.AppHost/            # AddPostgres + AddDatabase; Api WithReference/WaitFor
  MymoneyMcp.ServiceDefaults/    # telemetry, resilience, /health + /alive in all environments
  MymoneyMcp.Api/
    Program.cs                   # auth schemes, middleware order, MapMcp("/mcp"), auth endpoints
    Common/RequireAccountGroupMiddleware.cs
    Common/HeaderAccountGroupContext.cs  # reads X-Account-Group (HTTP concern)
    Common/ProblemDetailsExceptionHandler.cs
    Auth/AuthEndpoints.cs        # thin: build command → IMediator.Send
    Auth/AuthStartupGuard.cs     # Auth:Enabled=false + Production → throw
  MymoneyMcp.Application/
    Common/Behaviors/ValidationBehavior.cs
    Common/Interfaces/IAccountGroupContext.cs, IAppDbContext.cs, ITokenService.cs
    Auth/IssueToken/   (command, handler, validator, TokenResponse)
    Auth/RefreshToken/ (command, handler, validator)
    DependencyInjection.cs       # AddMediatR + validators + behaviors
  MymoneyMcp.Domain/
    Accounts/Account.cs
    Auth/RefreshToken.cs
  MymoneyMcp.Infrastructure.Data/  # persistence only
    AppDbContext.cs              # named filter "AccountGroup"
    Configurations/              # AccountConfiguration, RefreshTokenConfiguration
    Migrations/
    DependencyInjection.cs
  MymoneyMcp.Infrastructure.Auth/  # token issuance only
    JwtTokenService.cs, ClientCredentialStore.cs
    DependencyInjection.cs
tests/
  MymoneyMcp.Application.Tests/  # validators, token handlers (fakes), guard
  MymoneyMcp.Api.Tests/          # WebApplicationFactory + Testcontainers PostgreSQL
```

**Structure Decision**: the `CLAUDE.md` layout, with one `Infrastructure.<Concern>` project per
concern: `Data` and `Auth` in this feature. Token validation (`AddJwtBearer`) stays in `Api`.

## Key Technical Decisions

| Topic | Decision | Research |
|---|---|---|
| Isolation | EF Core 10 named query filter on `Account`, group from scoped header context | R5 |
| Middleware order | authentication → authorization → group header → endpoints | R6 |
| Own tokens | client credentials from configuration; HMAC JWT; rotating refresh tokens stored hashed | R7 |
| External tokens | second bearer scheme via OIDC discovery, chosen by `iss`; optional | R8 |
| Auth off | `Auth:Enabled=false` → anonymous fallback + warning; Production → startup fails | R9 |
| Migrations | API applies pending migrations at startup | R10 |
| Health | `/health` (with DB check) and `/alive` in all environments | R11 |

## Testing Map

| Spec item | Test (project) |
|---|---|
| US1 / FR-002/003, SC-001 | Accounts of groups A and B; read as A; lookup B's id as A → null (Api.Tests, PostgreSQL) |
| US1 scenario 3 | Group with no accounts → empty (Api.Tests) |
| US2 / FR-005/006 | No token / expired / valid → 401/401/200 (Api.Tests) |
| US2 scenario 3 | Refresh works once, reused token → 401 (Api.Tests, PostgreSQL) |
| US2 scenario 4 / FR-007 | Token signed by a test external issuer accepted (Api.Tests) |
| US2 scenario 5 / FR-014 | Auth off in Development → anonymous accepted; Production → startup throws (Api.Tests + unit) |
| US3 / FR-004 | Missing/blank header → 400; exempt routes not rejected (Api.Tests) |
| US4 / FR-010 | MCP client `initialize` + `tools/list` → `[]` (Api.Tests) |
| Validators | Blank `clientId`/`clientSecret`/`refreshToken` rejected (Application.Tests) |

## Closing Doc Tasks (Constitution I)

`tasks.md` MUST end with tasks that create:
- `specs/domain/business-rules.md` — group isolation, header rule, auth-off rule.
- `specs/domain/mcp-tools.md` — `/mcp` endpoint (no tools) and `/auth/*` REST routes.
- `specs/domain/glossary.md` — Account, Account Group, Refresh Token.
- `specs/architecture.md` — Aspire resources, persistence, auth schemes, middleware order.
- `specs/backlog.md` — set 001 to `Implemented`.

## Complexity Tracking

| Addition | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| FluentValidation | Constitution III requires validation as a pipeline step | DataAnnotations: weak cross-field rules |
| `ModelContextProtocol.AspNetCore` | MCP server is the product | Hand-written JSON-RPC re-implements the protocol |
| Testcontainers PostgreSQL | Constitution V requires the real engine for query filters | SQLite/in-memory cannot prove PostgreSQL behavior |
| `refresh_tokens` table | Spec assumes refresh-token rotation | Stateless refresh JWT cannot be revoked after use |
