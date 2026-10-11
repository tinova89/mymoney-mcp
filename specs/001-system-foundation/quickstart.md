# Quickstart: System Foundation

Validates the feature end to end. Contracts: [contracts/](contracts/). Data: [data-model.md](data-model.md).

## Prerequisites

.NET 10 SDK, Docker running, Aspire workload — see [README](../../README.md).

## Setup

```bash
cd src/MymoneyMcp.Api
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-random-bytes>"
dotnet user-secrets set "Auth:Clients:0:ClientId" "claude-code"
dotnet user-secrets set "Auth:Clients:0:ClientSecret" "<secret>"
```

## Scenarios

| # | Do | Expect | Spec |
|---|---|---|---|
| 1 | `dotnet run --project src/MymoneyMcp.AppHost` | Dashboard opens; database created; `GET /health` → `200 Healthy` | US4, FR-011/012 |
| 2 | `POST /auth/token` with the client above, no group header | `200` + tokens | US2, FR-006 |
| 3 | `POST /auth/refresh` with the refresh token; repeat with the same token | `200`, then `401` | US2 |
| 4 | `POST /mcp` `initialize` with no token | `401` | US2, FR-005 |
| 5 | Same with token, no `X-Account-Group` | `400` | US3, FR-004 |
| 6 | Same with token + header; then `tools/list` | success; `[]` | US4, FR-010 |
| 7 | Set `Auth:Enabled=false`, environment `Development`; call `/mcp` with header only | accepted; startup log has a warning | US2, FR-014 |
| 8 | Set `Auth:Enabled=false`, environment `Production`; start the API | startup fails | FR-014 |
| 9 | `dotnet test` | all green, including cross-group isolation tests | US1, SC-001 |

## Validation record (2026-10-10)

Every scenario is covered by an automated test that runs the real host (`WebApplicationFactory` or
the Aspire AppHost) against real PostgreSQL. `dotnet test MymoneyMcp.slnx`: **58 passed, 0 failed**.

| # | Result | Evidence |
|---|---|---|
| 1 | ✅ | `AppHostSmokeTests` (AppHost starts PostgreSQL + API, `/health` 200), `HealthTests` |
| 2 | ✅ | `AuthApiTests.Token_is_issued_without_the_group_header` |
| 3 | ✅ | `AuthApiTests.Refresh_works_once_and_reuse_is_401`, `Concurrent_refreshes_...` |
| 4 | ✅ | `ProtectedRouteTests.No_token_is_401` |
| 5 | ✅ | `RequireAccountGroupTests.Missing_header_is_400_with_the_contract_title` |
| 6 | ✅ | `McpEndpointTests.Client_connects_and_the_server_lists_no_tools` |
| 7 | ✅ | `AuthDisabledTests.Disabled_outside_production_accepts_calls_without_a_token_and_warns` |
| 8 | ✅ | `AuthDisabledTests.Disabled_in_production_stops_startup` |
| 9 | ✅ | full suite green, including `AccountGroupIsolationTests` |

Scenario 1 was not run by hand with `dotnet run`, because that needs the developer's own
user-secrets; the AppHost smoke test runs the same path with the signing key passed as an
environment variable.
