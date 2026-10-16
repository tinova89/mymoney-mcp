# Implementation Plan: Connect from claude.ai

**Branch**: `002-claude-ai-connector` | **Date**: 2026-10-16 | **Spec**: [spec.md](spec.md)

## Summary

Let an MCP call name its account group in the URL (`/mcp/<group>`) as well as the header, resolved
once per request into the same `IAccountGroupContext`; reject blank or mismatched groups with `400`;
publish RFC 9728 metadata pointing to the external issuer so claude.ai can sign in; add the
read-only `whogroupami` diagnostic tool. No data changes. Decisions: [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10 (unchanged from 001)

**Primary Dependencies**: `ModelContextProtocol.AspNetCore` 2.2.0 (now also its MCP authentication
scheme), JwtBearer, MediatR 12.x — no new packages

**Storage**: PostgreSQL; no schema change, no migration

**Testing**: xUnit, `WebApplicationFactory`, Testcontainers PostgreSQL, MCP SDK client

**Target Platform**: ASP.NET Core host; reachable over HTTPS for claude.ai

**Project Type**: web service (REST + MCP on one host)

**Performance Goals**: group resolution is string work on the request; no extra queries

**Constraints**: REST behavior unchanged; tokens carry no group; IdP choice deferred (R5)

**Scale/Scope**: single user, a few groups, one connector per group

## Constitution Check

*Gate before Phase 0; re-checked after Phase 1.*

| Principle | How this plan complies | Status |
|---|---|---|
| I. Specs first, concise | Spec 150 lines; this plan under 200; closing doc tasks below | ✅ |
| II. Accounts are the root | URL group feeds the same scoped context and named filter (BR-03); no handler filters by group; `whogroupami` gets a cross-group count test. No new tables, so no group column decision | ✅ |
| III. Thin edges, CQRS | Tool only sends `WhoGroupAmIQuery`; group resolution is a header/route check in middleware (allowed); no validator because the query has no input (R7) | ✅ |
| IV. Test-backed | Resolver unit tests; integration test per acceptance scenario and edge case | ✅ |
| V. Real engine | Isolation through `/mcp/<group>` and `whogroupami` tested on PostgreSQL through the real host and MCP client, with header and URL (v2.3.0) | ✅ |
| VI. Data integrity | No money, dates, deletes, or destructive operations | ✅ N/A |
| VII. Contracts & security | DTO response; group from URL or header, never cached (stateless, R3); mismatch/blank → `400`; `/.well-known` exempt; JWT carries no group; issuer URL from configuration | ✅ |
| VIII. Simplicity | No new project or package; SDK's own auth handler instead of a custom one | ✅ |

**Post-design re-check (after Phase 1)**: unchanged. Contract change to note (Constitution VII):
`tools/list` was `[]` and now lists `whogroupami`; called out in [contracts/mcp-endpoint.md](contracts/mcp-endpoint.md).

## Project Structure

### Documentation (this feature)

```text
specs/002-claude-ai-connector/
├── spec.md · plan.md · research.md · data-model.md · quickstart.md
├── contracts/  (mcp-endpoint.md, oauth-metadata.md)
├── checklists/requirements.md
└── tasks.md    (/speckit-tasks)
```

### Source Code (changes only)

```text
src/
  MymoneyMcp.Api/
    Program.cs                          # 2nd MapMcp route, Stateless, AddMcp when external set
    Auth/AuthenticationSetup.cs         # MCP scheme + "Challenge" policy scheme (R4)
    Common/AccountGroupResolver.cs      # new, pure (R1)
    Common/RequestAccountGroupContext.cs # replaces HeaderAccountGroupContext; adds Source
    Common/HttpCallerContext.cs         # new: ICallerContext from the auth scheme (R6)
    Common/RequireAccountGroupMiddleware.cs # uses resolver; new 400 titles; /.well-known exempt
    Mcp/Tools/WhoAmITool.cs             # new, thin; method WhoGroupAmI → tool whogroupami
  MymoneyMcp.Application/
    Common/Interfaces/IAccountGroupContext.cs # + Source
    Common/Interfaces/ICallerContext.cs # new
    Common/AccountGroupSource.cs, TokenIssuer.cs # new enums
    Diagnostics/WhoGroupAmI/            # query, handler, response
tests/
  MymoneyMcp.Api.Tests/
    AccountGroup/AccountGroupResolverTests.cs   # new, unit
    AccountGroup/RequestAccountGroupContextTests.cs # renamed from HeaderAccountGroupContextTests, same cases
    AccountGroup/UrlAccountGroupTests.cs        # US1, US2, edge cases
    Mcp/WhoAmIToolTests.cs                      # US3 (PostgreSQL, MCP client)
    Auth/OAuthMetadataTests.cs                  # US4, FR-016
    Mcp/McpEndpointTests.cs                     # tools/list now [whogroupami]
    Diagnostics/WhoGroupAmIQueryHandlerTests.cs # handler on PostgreSQL with fixed contexts
```

**Structure Decision**: the `CLAUDE.md` layout; all HTTP concerns stay in `Api`, the tool's logic in
`Application`. No infrastructure project changes.

## Key Technical Decisions

| Topic | Decision | Research |
|---|---|---|
| Group source | One resolver per request from raw target + header; result in `HttpContext.Items` | R1 |
| Decoding | Raw segment decoded once; `%2F` becomes `/` | R1 |
| Routes | `/mcp` and `/mcp/{accountGroup}`; `/mcp/` = `/mcp`; extra segments `404` | R2 |
| No carry-over | Stateless Streamable HTTP pinned | R3 |
| Sign-in | SDK `AddMcp` metadata + challenge on MCP paths only; registered when external is set | R4 |
| Audience | One external audience for all MCP URLs; IdP must honor it | R5 |
| Diagnostics | `ICallerContext` + `IAccountGroupContext.Source`; `whogroupami` query | R6, R7 |

## Middleware Order

`UseAuthentication` (MCP scheme may answer `/.well-known/...` here) → `UseAuthorization` (`401`) →
`RequireAccountGroup` (`400`: missing, blank, mismatch) → endpoints. Same as 001 R6 plus metadata.

## Testing Map

| Spec item | Test (project) |
|---|---|
| US1 sc. 1, SC-002 | `/mcp/A` and `/mcp/B`, no header → each sees only its accounts (Api, PostgreSQL) |
| US1 sc. 2, FR-007 | `/mcp/A` then `/mcp/B` with the same client and token → `B` only (Api) |
| US1 sc. 3–4, FR-003/004 | `%20A%20` → `A`; `%20` → `400` blank (resolver unit + Api) |
| US2, FR-005, SC-003 | URL `A` + header `B` → `400`; header `A`/` A ` → accepted (Api) |
| US2 sc. 3, SC-004 | 001 header tests unchanged and green |
| US3, FR-009/010 | `whogroupami`: `url`/`header`/`both`; 2 vs 3 accounts → `2`; auth off → `none` (Api, PostgreSQL) |
| US4 sc. 1, FR-012 | No token on `/mcp/A` → `401` + `resource_metadata`; REST `401` header unchanged (Api) |
| US4 sc. 2, FR-011 | Metadata route: anonymous, no header, `resource` + issuer; absent when external unset (Api) |
| US4 sc. 3, FR-016 | External test token on `/mcp/A` and `/mcp/B`; local token on `/mcp/A` (Api) |
| Edge cases | Encoded `/`, `/mcp/`, `/mcp/A/B` → `404`, unauthenticated + mismatch → `401` (resolver + Api) |
| Handler | Enum → contract strings; count uses the filtered set (Api, PostgreSQL: `DbSet` cannot be faked) |

## Closing Doc Tasks (Constitution I)

`tasks.md` MUST end with tasks that update:
- `specs/domain/business-rules.md` — BR-02, BR-10, BR-11 (URL group, `/.well-known`), new rules for
  mismatch/blank and token audience (FR-015).
- `specs/domain/mcp-tools.md` — `/mcp/<group>`, metadata route, `whogroupami`.
- `specs/domain/glossary.md` — Account group (now from URL or header), URL group, group source, token issuer.
- `specs/architecture.md` — routes, `Challenge`/MCP schemes, stateless transport, middleware order.
- `specs/backlog.md` and spec `Status:` — 002 `Implemented`.
