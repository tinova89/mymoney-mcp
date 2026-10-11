---
description: "Task list for 001-system-foundation"
---

# Tasks: System Foundation

**Input**: [spec.md](spec.md) · [plan.md](plan.md) · [research.md](research.md) ·
[data-model.md](data-model.md) · [contracts/](contracts/) · [quickstart.md](quickstart.md)

**Tests**: required — Constitution IV (NON-NEGOTIABLE) and V (real PostgreSQL). Write each story's
tests first and see them fail before implementing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1–US4 from spec.md

---

## Phase 1: Setup

**Purpose**: empty solution that builds.

- [X] T001 Create `global.json` pinning SDK `10.0.401` (rollForward `latestFeature`) and `MymoneyMcp.slnx` at the repo root
- [X] T002 [P] Create `Directory.Build.props` (`net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`) and `Directory.Packages.props` with `ManagePackageVersionsCentrally=true` (latest stable versions; MediatR pinned to 12.x per research R3)
- [X] T003 [P] Create `.config/dotnet-tools.json` pinning `dotnet-ef` to the EF Core 10 line
- [X] T004 Create projects and add them to `MymoneyMcp.slnx`: `src/MymoneyMcp.AppHost` (Aspire AppHost), `src/MymoneyMcp.ServiceDefaults` (Aspire ServiceDefaults), `src/MymoneyMcp.Api` (ASP.NET Core empty), `src/MymoneyMcp.Application`, `src/MymoneyMcp.Domain`, `src/MymoneyMcp.Infrastructure.Data`, `src/MymoneyMcp.Infrastructure.Auth` (class libraries), `tests/MymoneyMcp.Application.Tests`, `tests/MymoneyMcp.Api.Tests` (xUnit)
- [X] T005 Add project references in each `src/*/*.csproj` and `tests/*/*.csproj` per plan.md layering: Api → Application, Infrastructure.Data, Infrastructure.Auth, ServiceDefaults; Application → Domain; Infrastructure.Data → Application; Infrastructure.Auth → Application; AppHost → Api; Application.Tests → Application; Api.Tests → Api, AppHost. Infrastructure projects MUST NOT reference each other; Domain references no package
- [X] T006 Run `dotnet build MymoneyMcp.slnx` and confirm 0 warnings, 0 errors

---

## Phase 2: Foundational (blocks every story)

- [X] T007 Configure `src/MymoneyMcp.AppHost/AppHost.cs`: `AddPostgres("mymoneymcp-postgres").AddDatabase("mymoneymcp-db")`; Api project `.WithReference(db).WaitFor(db)`
- [X] T008 [P] Configure `src/MymoneyMcp.ServiceDefaults/Extensions.cs`: OpenTelemetry, resilience, and `MapDefaultEndpoints` mapping `/health` and `/alive` in **every** environment, status only, no details (research R11)
- [X] T009 [P] Create `src/MymoneyMcp.Application/Common/Interfaces/IAppDbContext.cs` and `IAccountGroupContext.cs` (`string Group { get; }`)
- [X] T010 [P] Create `src/MymoneyMcp.Application/Common/Exceptions/ValidationException.cs` and `NotFoundException.cs`
- [X] T011 Create `src/MymoneyMcp.Application/Common/Behaviors/ValidationBehavior.cs` (runs every `IValidator<TRequest>`, throws `ValidationException`) and `src/MymoneyMcp.Application/DependencyInjection.cs` (`AddMediatR` from the Application assembly, validators, `ValidationBehavior`)
- [X] T012 Create `src/MymoneyMcp.Infrastructure.Data/AppDbContext.cs` (implements `IAppDbContext`, takes `IAccountGroupContext`) and `DependencyInjection.cs` registering it with the Aspire Npgsql EF Core integration for connection name `mymoneymcp-db` (adds the DB health check)
- [X] T013 Create `src/MymoneyMcp.Api/Common/ProblemDetailsExceptionHandler.cs` mapping `ValidationException` → `400` with field errors and `NotFoundException` → `404`, RFC 9457 bodies (contracts/http-conventions.md); field errors MUST NOT include the attempted value, so `clientSecret` and `refreshToken` never appear in a response (Constitution VIII)
- [X] T014 Create `src/MymoneyMcp.Api/Program.cs` skeleton: ServiceDefaults, Application, Infrastructure.Data registration, exception handler, `AddMcpServer()` with server name `mymoney-mcp`, `.WithHttpTransport().WithToolsFromAssembly()`, `MapMcp("/mcp")`, `MapDefaultEndpoints()`, and pending EF Core migrations applied at startup; if the database is unreachable or a migration fails, startup stops with the error logged (fail fast, research R10)
- [X] T022 `src/MymoneyMcp.Api/Common/HeaderAccountGroupContext.cs`: reads `X-Account-Group` via `IHttpContextAccessor`, trims it; register as scoped `IAccountGroupContext` in `Program.cs` (moved from US1: `AppDbContext` needs it for the host to start; ID kept to avoid renumbering)
- [X] T015 [P] Create `tests/MymoneyMcp.Api.Tests/Infrastructure/PostgresFixture.cs` (Testcontainers PostgreSQL, one container per test collection) and `ApiFactory.cs` (`WebApplicationFactory<Program>` pointing `ConnectionStrings:mymoneymcp-db` at the container, test JWT settings, environment `Development` by default)
- [X] T016 Run `dotnet build MymoneyMcp.slnx` (0 warnings) and `dotnet test MymoneyMcp.slnx` (empty suite passes)

**Checkpoint**: host starts, database is migrated, `/mcp` and `/health` are mapped.

---

## Phase 3: User Story 1 — Each group sees only its own accounts (P1) 🎯 MVP

**Goal**: one central filter makes another group's accounts invisible.
**Independent Test**: test data for groups A and B; reading as A returns only A (spec US1).

### Tests (write first, must fail)

- [X] T017 [P] [US1] `tests/MymoneyMcp.Api.Tests/Accounts/AccountGroupIsolationTests.cs` on PostgreSQL via `PostgresFixture`, with a fixed `IAccountGroupContext`: (a) accounts in `A` and `B`, list as `A` → only `A`; (b) `FirstOrDefaultAsync` by `B`'s id as `A` → `null`, same as an unknown id; (c) group with no accounts → empty list, no error (spec US1 scenarios 1–3, SC-001)
- [X] T018 [P] [US1] `tests/MymoneyMcp.Api.Tests/AccountGroup/HeaderAccountGroupContextTests.cs` (the class lives in `Api`): value is trimmed; `HOME` ≠ `home` (case-sensitive); missing header → empty string

### Implementation

- [X] T019 [P] [US1] `src/MymoneyMcp.Domain/Accounts/Account.cs`: `Id` (`uuid` PK, "Generated by the app."), `DisplayName` ("Required, trimmed, not blank."), `Group` ("Required, trimmed, not blank. Exact, case-sensitive match."), `CreatedAt` ("Set on insert (UTC).")
- [X] T020 [US1] `src/MymoneyMcp.Infrastructure.Data/Configurations/AccountConfiguration.cs`: table `accounts`; `DisplayName` `varchar(100)`; `Group` `varchar(100)`; `CreatedAt` `timestamptz`; index `ix_accounts_group` on `Group`
- [X] T021 [US1] In `src/MymoneyMcp.Infrastructure.Data/AppDbContext.cs`: `DbSet<Account> Accounts` and the EF Core 10 named query filter `"AccountGroup"`: `a => a.Group == accountGroupContext.Group` (research R5)
- [X] T023 [US1] Add migration `AddAccounts`: `dotnet ef migrations add AddAccounts --project src/MymoneyMcp.Infrastructure.Data --startup-project src/MymoneyMcp.Api`
- [X] T024 [US1] Run `tests/MymoneyMcp.Api.Tests/Accounts/AccountGroupIsolationTests.cs` and `tests/MymoneyMcp.Api.Tests/AccountGroup/HeaderAccountGroupContextTests.cs` (T017–T018) and confirm green

**Checkpoint**: isolation proven on real PostgreSQL.

---

## Phase 4: User Story 2 — Only authenticated callers get in (P2)

**Goal**: own client-credentials tokens, external issuer, refresh rotation, switch-off outside production.
**Independent Test**: no token / expired / valid → 401 / 401 / accepted (spec US2).

### Tests (write first, must fail)

- [X] T025 [P] [US2] `tests/MymoneyMcp.Application.Tests/Auth/IssueTokenCommandValidatorTests.cs` and `RefreshTokenCommandValidatorTests.cs`: blank `clientId`, `clientSecret`, `refreshToken` rejected
- [X] T026 [P] [US2] `tests/MymoneyMcp.Application.Tests/Auth/IssueTokenCommandHandlerTests.cs` and `RefreshTokenCommandHandlerTests.cs` with fakes: unknown client and wrong secret give the same `401` error; refresh revokes the old token and returns a new pair
- [X] T027 [P] [US2] `tests/MymoneyMcp.Api.Tests/Auth/AuthApiTests.cs`: `POST /auth/token` without group header → `200` + `TokenResponse`; bad secret → `401`; blank field → `400`; `POST /auth/refresh` once → `200`, reuse same token → `401`; two concurrent refreshes with the same token on PostgreSQL → exactly one `200`, the other `401`; JWT has no group claim; error responses and captured logs never contain the `clientSecret` or `refreshToken` values sent (contracts/auth-api.md)
- [X] T028 [P] [US2] `tests/MymoneyMcp.Api.Tests/Auth/ProtectedRouteTests.cs` on `/mcp` with the group header: no token → `401`; expired token → `401`; valid own token → accepted; token signed by a test external issuer (test OIDC metadata + key) → accepted; `/health`, `/alive`, `/openapi/v1.json` with no token → not `401` (spec US2 scenarios 1, 4; FR-005 exemptions)
- [X] T029 [P] [US2] `tests/MymoneyMcp.Api.Tests/Auth/AuthDisabledTests.cs`: `Auth:Enabled=false` in `Development` → `/mcp` with header and no token accepted, startup warning logged; in `Production` → host startup throws; auth disabled and no `X-Account-Group` header → `400` (header still required); auth enabled with `Jwt:SigningKey` missing or shorter than 32 bytes → host startup throws (spec US2 scenario 5, FR-013, FR-014)

### Implementation

- [X] T030 [P] [US2] `src/MymoneyMcp.Domain/Auth/RefreshToken.cs`: `Id` (`uuid` PK), `ClientId` ("Required."), `TokenHash` ("SHA-256 hex of the opaque token. Unique index."), `ExpiresAt` ("Required."), `CreatedAt` ("Required."), `RevokedAt` ("Set when used or revoked."); valid when "`RevokedAt is null and ExpiresAt > now`"
- [X] T031 [US2] `src/MymoneyMcp.Infrastructure.Data/Configurations/RefreshTokenConfiguration.cs`: table `refresh_tokens`; `ClientId` `varchar(100)`; `TokenHash` `varchar(64)` unique index; timestamps `timestamptz`, `RevokedAt` nullable; **no** group filter; add `DbSet<RefreshToken>` to `AppDbContext`
- [X] T032 [US2] Add migration `AddRefreshTokens` in `src/MymoneyMcp.Infrastructure.Data/Migrations/` (same command as T023)
- [X] T033 [P] [US2] `src/MymoneyMcp.Application/Common/Interfaces/ITokenService.cs` (create access token, create/hash refresh token) and `IClientCredentialStore.cs` (constant-time secret check)
- [X] T034 [US2] `src/MymoneyMcp.Application/Auth/IssueToken/`: `IssueTokenCommand`, `IssueTokenCommandValidator`, `IssueTokenCommandHandler`, `TokenResponse` (`accessToken`, `tokenType`, `expiresIn`, `refreshToken`); never log `clientSecret`, `refreshToken`, or issued tokens (Constitution VIII)
- [X] T035 [US2] `src/MymoneyMcp.Application/Auth/RefreshToken/`: `RefreshTokenCommand`, validator, handler — revoke the used row with a conditional update (`UPDATE ... SET RevokedAt = now WHERE Id = @id AND RevokedAt IS NULL`); if 0 rows are affected, return `401`; insert the new row in the same database transaction
- [X] T036 [P] [US2] `src/MymoneyMcp.Infrastructure.Auth/JwtTokenService.cs` (HMAC-SHA256; claims `sub`, `iss`, `aud`, `iat`, `exp`; defaults 60 min access / 14 days refresh), `ClientCredentialStore.cs` (reads `Auth:Clients`), `JwtOptions.cs`, `DependencyInjection.cs`
- [X] T037 [US2] `src/MymoneyMcp.Api/Auth/AuthEndpoints.cs`: `POST /auth/token` and `POST /auth/refresh`, thin (build command → `IMediator.Send`), anonymous
- [X] T038 [US2] In `src/MymoneyMcp.Api/Program.cs`: bearer schemes `Local` and optional `External` (`Auth:External:Authority`/`Audience`), policy scheme choosing by `iss`; fallback policy requires an authenticated user unless `Auth:Enabled=false`; mark `/health`, `/alive` (from `MapDefaultEndpoints`) and `/openapi` (from `MapOpenApi`) with `AllowAnonymous()` so the fallback policy does not apply to them (spec FR-005 exemptions); `UseAuthentication` → `UseAuthorization` (research R6–R9)
- [X] T039 [US2] `src/MymoneyMcp.Api/Auth/AuthStartupGuard.cs`: `Auth:Enabled=false` + `IsProduction()` → throw at startup; otherwise log a warning when disabled. When auth is enabled, `Jwt:SigningKey` missing or shorter than 32 bytes → throw at startup (FR-013)
- [X] T040 [US2] Run `tests/MymoneyMcp.Application.Tests/Auth/` and `tests/MymoneyMcp.Api.Tests/Auth/` (T025–T029) and confirm green

**Checkpoint**: tokens issued, refreshed, validated, and switchable off only outside production.

---

## Phase 5: User Story 3 — Every call names its account group (P3)

**Goal**: calls without `X-Account-Group` are rejected before any processing.
**Independent Test**: protected route with and without the header (spec US3).

### Tests (write first, must fail)

- [X] T041 [P] [US3] `tests/MymoneyMcp.Api.Tests/AccountGroup/RequireAccountGroupTests.cs`: valid token + missing header → `400` problem `title: "Missing X-Account-Group header"`; blank/whitespace header → `400`; no token + no header → `401` (token checked first, needs US2); `/health`, `/alive`, `/openapi/v1.json`, `/auth/token` without header → not rejected for the header

### Implementation

- [X] T042 [US3] `src/MymoneyMcp.Api/Common/RequireAccountGroupMiddleware.cs`: exempt prefixes `/health`, `/alive`, `/openapi` and the exact routes `/auth/token`, `/auth/refresh` (Constitution VII; any other `/auth/*` route still needs the header); missing/blank → `400` `application/problem+json`
- [X] T043 [US3] In `src/MymoneyMcp.Api/Program.cs`: register the middleware after `UseAuthorization` and before endpoints; add `AddOpenApi()` + `MapOpenApi()` (Development only)
- [X] T044 [US3] Run `tests/MymoneyMcp.Api.Tests/AccountGroup/RequireAccountGroupTests.cs` (T041) and confirm green

---

## Phase 6: User Story 4 — The system runs locally with one command (P4)

**Goal**: AppHost starts API, MCP, and database; MCP lists no tools.
**Independent Test**: start the AppHost; `/health` healthy; MCP `tools/list` empty (spec US4).

### Tests (write first, must fail)

- [X] T045 [P] [US4] `tests/MymoneyMcp.Api.Tests/Mcp/McpEndpointTests.cs`: MCP SDK client over the test server with token + header → `initialize` succeeds, server name `mymoney-mcp`, `tools/list` → `[]` (contracts/mcp-endpoint.md)
- [X] T046 [P] [US4] `tests/MymoneyMcp.Api.Tests/Health/HealthTests.cs`: `/health` → `200 Healthy` in `Production` environment too; database stopped **after** startup → `/health` unhealthy, `/alive` OK, API still running (spec edge case); database unreachable at startup → host startup throws (research R10)
- [X] T047 [P] [US4] `tests/MymoneyMcp.Api.Tests/AppHostSmokeTests.cs`: Aspire `DistributedApplicationTestingBuilder` starts the AppHost; Api `/health` → `200` (research R12)

### Implementation

- [X] T048 [US4] Fix anything T045–T047 expose in `src/MymoneyMcp.AppHost/AppHost.cs`, `src/MymoneyMcp.ServiceDefaults/Extensions.cs`, or `src/MymoneyMcp.Api/Program.cs`; run them and confirm green

---

## Phase 7: Polish & Closing Docs (Constitution I)

- [X] T049 [P] Update `README.md` setup: user-secrets for `Jwt:SigningKey`, `Auth:Clients:0:ClientId`, `Auth:Clients:0:ClientSecret`; `Auth:Enabled` and `Auth:External:*` settings
- [X] T050 Run `dotnet build MymoneyMcp.slnx` (0 warnings) and `dotnet test MymoneyMcp.slnx` (all green)
- [X] T051 Run every scenario in `specs/001-system-foundation/quickstart.md` and record the result
- [X] T052 [P] Create `specs/domain/business-rules.md`: group isolation (incl. "not found" for other groups), `X-Account-Group` rule and exempt routes, token switch-off rule (FR-014)
- [X] T053 [P] Create `specs/domain/mcp-tools.md`: `/mcp` endpoint (no tools yet) and REST routes `/auth/token`, `/auth/refresh` (link the contracts)
- [X] T054 [P] Create `specs/domain/glossary.md`: Account, Account Group, Refresh Token, Client
- [X] T055 [P] Create `specs/architecture.md`: Aspire resources, projects and references, persistence and named filters, auth schemes, middleware order
- [X] T056 Set `**Status**` to `Implemented` in `specs/001-system-foundation/spec.md` and the 001 row in `specs/backlog.md`

---

## Dependencies & Execution Order

- **Setup (1)** → **Foundational (2)** → stories.
- **US1** needs only Phase 2.
- **US2** needs Phase 2 (uses `/mcp` from T014 as its protected route).
- **US3** needs Phase 2; its "401 before 400" check (T041) needs US2.
- **US4** needs Phase 2; T045 sends a token, so run after US2 (or with `Auth:Enabled=false`).
- **Polish (7)** after all stories. Docs tasks T052–T055 are the last code-adjacent step.

Recommended order: US1 → US2 → US3 → US4.

## Parallel Examples

```text
Phase 1: T002, T003
Phase 2: T008, T009, T010, T015
US1:     T017, T018 (tests) → T019 (code)
US2:     T025–T029 (tests) → T030, T033, T036 (code)
US4:     T045, T046, T047
Phase 7: T049, T052, T053, T054, T055
```

## Implementation Strategy

1. **MVP**: Phases 1–3 (isolation proven on PostgreSQL). Stop and validate.
2. Add US2 (auth) → validate; then US3 (header) → validate; then US4 (run + MCP) → validate.
3. Close with Phase 7: docs before setting `Implemented`.

## Notes

- Commit after each task or logical group; stop at any checkpoint to validate.
- Every handler stays behind MediatR; tools/endpoints stay thin (Constitution III).

## Implementation notes (2026-10-10)

- T029's "auth off without the group header → `400`" case lives in `RequireAccountGroupTests`
  (T041), because it needs the US3 middleware.
- `AddOpenApi`/`MapOpenApi` (T043) was added during US2, because T028 checks that `/openapi` is
  exempt from the token.
- Refresh-token persistence is behind `IRefreshTokenStore` (Application) so the conditional
  revoke + insert runs in one transaction and handlers stay unit-testable with fakes.
- Tooling: repo `nuget.config` (nuget.org only, required by central package management);
  `global.json` opts `dotnet test` into Microsoft Testing Platform (xUnit v3);
  `AspireUseCliBundle=true` in the AppHost to clear warning ASPIRE010.
