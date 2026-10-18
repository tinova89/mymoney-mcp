---
description: "Task list for 002-claude-ai-connector"
---

# Tasks: Connect from claude.ai

**Input**: [spec.md](spec.md) · [plan.md](plan.md) · [research.md](research.md) ·
[data-model.md](data-model.md) · [contracts/](contracts/) · [quickstart.md](quickstart.md)

**Tests**: required — Constitution IV (NON-NEGOTIABLE) and V (real host, real PostgreSQL). Write
each story's tests first and see them fail before implementing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1–US4 from spec.md

---

## Phase 1: Setup

**Purpose**: confirm the 001 baseline before changing it. No new project or package (plan.md).

- [X] T001 Run `dotnet build MymoneyMcp.slnx` (0 warnings) and `dotnet test MymoneyMcp.slnx` (all green) on the current `main` and note the test count

---

## Phase 2: Foundational (blocks every story)

**Purpose**: one per-request group resolution feeding the existing context (research R1, R3, R6).

### Tests (write first, must fail)

- [X] T002 [P] `tests/MymoneyMcp.Api.Tests/AccountGroup/AccountGroupResolverTests.cs` (unit, no host): one case per row of the table in [data-model.md](data-model.md) — `/mcp` + header `A` → `A`/`Header`; `/mcp/A` no header → `A`/`Url`; `/mcp/A` + header ` A ` → `A`/`Both`; `/mcp/A` + header `B` → `Mismatch`; `/mcp/%20` → `BlankUrlGroup`; `/mcp` and `/mcp/` without header → `Missing`; `/mcp/A` + header `"   "` → `A`/`Url` (blank header counts as absent); raw `/mcp/%20A%20` → `A`; raw `/mcp/Fam%C3%ADlia` → `Família`; raw `/mcp/a%2Fb` → `a/b`; raw `/mcp/%252F` → `%2F` (decoded once); raw `/mcp/A?x=1` → `A`; `HOME` ≠ `home`; REST path `/auth/other` with raw-looking segments never yields a URL group
- [X] T003 [P] Rename `tests/MymoneyMcp.Api.Tests/AccountGroup/HeaderAccountGroupContextTests.cs` to `RequestAccountGroupContextTests.cs` targeting `RequestAccountGroupContext`; keep its four cases unchanged (trimmed, case-sensitive, missing header → empty, no request → empty) and add: `Source` is `Url` for `/mcp/A`, `Header` for `/mcp` + header

### Implementation

- [X] T004 [P] Create `src/MymoneyMcp.Application/Common/AccountGroupSource.cs` (`None`, `Url`, `Header`, `Both`) and `src/MymoneyMcp.Application/Common/TokenIssuer.cs` (`None`, `Local`, `External`)
- [X] T005 Add `AccountGroupSource Source { get; }` to `src/MymoneyMcp.Application/Common/Interfaces/IAccountGroupContext.cs`; create `src/MymoneyMcp.Application/Common/Interfaces/ICallerContext.cs` (`TokenIssuer Issuer { get; }`)
- [X] T006 Create `src/MymoneyMcp.Api/Common/AccountGroupResolver.cs`: pure static `Resolve(HttpRequest)` returning a record (`Group`, `Source`, `Error`: `None`/`Missing`/`BlankUrlGroup`/`Mismatch`); URL group only for paths `/mcp/<one segment>`; read the segment from `IHttpRequestFeature.RawTarget` (query removed), falling back to `Request.Path.ToUriComponent()` when `RawTarget` is empty; `Uri.UnescapeDataString` once, then `Trim()`; header `X-Account-Group` trimmed, blank = absent; compare ordinal; plus `GetOrResolve(HttpContext)` caching the result in `HttpContext.Items` for that request only (research R1)
- [X] T007 Replace `src/MymoneyMcp.Api/Common/HeaderAccountGroupContext.cs` with `RequestAccountGroupContext.cs` (`IAccountGroupContext`): `Group`/`Source` from `AccountGroupResolver.GetOrResolve`, empty/`None` when there is no request or an error; keep `public const string HeaderName = "X-Account-Group"` and update its uses in `RequireAccountGroupMiddleware.cs` and `Program.cs` registration (scoped)
- [X] T008 [P] Create `src/MymoneyMcp.Api/Common/HttpCallerContext.cs` (`ICallerContext`): `Issuer` from `HttpContext.User.Identity` — authenticated with `AuthenticationType` `"Local"` → `Local`, `"External"` → `External`, otherwise `None`; never reads the token; register scoped in `src/MymoneyMcp.Api/Program.cs`
- [X] T009 Pin the stateless transport in `src/MymoneyMcp.Api/Program.cs`: `.WithHttpTransport(options => options.Stateless = true)` (research R3)
- [X] T010 Run `dotnet build` (0 warnings) and `dotnet test`: T002/T003 green, every 001 test still green

**Checkpoint**: header calls behave exactly as in 001, now through the resolver.

---

## Phase 3: User Story 1 — Pick the group through the MCP URL (P1) 🎯 MVP

**Goal**: `/mcp/<group>` sets the call's group; isolation unchanged (FR-001–FR-004, FR-007).
**Independent Test**: with a token and no header, `/mcp/A` and `/mcp/B` each see only their group.

### Tests (write first, must fail)

- [X] T011 [P] [US1] `tests/MymoneyMcp.Api.Tests/AccountGroup/UrlAccountGroupTests.cs` (real host, `TestRequests.McpInitialize` extended with a `path` argument in `tests/MymoneyMcp.Api.Tests/Infrastructure/TestRequests.cs`): with local token and no header — `/mcp/A` → `200`; `/mcp/a%2Fb` → `200`; `/mcp/%20` → `400` `title: "Blank account group in MCP URL"` and the body does not contain the URL value; `/mcp/` without header → `400` `title: "Missing X-Account-Group header"`, with header → `200`; `/mcp/A/B` → `404`; without token `/mcp/%20` → `401` (BR-12)
- [X] T012 [P] [US1] Add to `tests/MymoneyMcp.Api.Tests/Accounts/AccountGroupIsolationTests.cs` (PostgreSQL): build `AppDbContext` with a `RequestAccountGroupContext` over a `DefaultHttpContext` whose raw target is `/mcp/A` (and `/mcp/%20A%20`) and no header → only group `A` accounts; `/mcp/B` → only `B` (US1 sc. 1, sc. 3, SC-002)

### Implementation

- [X] T013 [US1] Map the group route in `src/MymoneyMcp.Api/Program.cs`: `app.MapMcp("/mcp/{accountGroup}")` next to `app.MapMcp("/mcp")` (research R2)
- [X] T014 [US1] Rewrite `src/MymoneyMcp.Api/Common/RequireAccountGroupMiddleware.cs` on `AccountGroupResolver.GetOrResolve`: `Missing` → `400` with the 001 title `"Missing X-Account-Group header"` (unchanged constant); `BlankUrlGroup` → `400` `title: "Blank account group in MCP URL"`; problem details never echo the header or URL value; exempt routes unchanged ([contracts/mcp-endpoint.md](contracts/mcp-endpoint.md))
- [X] T015 [US1] Run `dotnet test`: T011, T012 and all 001 tests green

**Checkpoint**: Claude Code (header) and a group URL both work; MVP.

---

## Phase 4: User Story 2 — Header and URL never disagree (P2)

**Goal**: a URL group and a different header are rejected (FR-005).
**Independent Test**: `/mcp/A` with header `A`, `B`, ` A `.

### Tests (write first, must fail)

- [X] T016 [P] [US2] `tests/MymoneyMcp.Api.Tests/AccountGroup/GroupMismatchTests.cs` (real host, local token): `/mcp/A` + header `B` → `400` `title: "Account group mismatch"`, body contains neither `A` nor `B` as values; `/mcp/A` + header `A` and ` A ` → `200`; `/mcp/A` + header `a` → `400` (case-sensitive); without token `/mcp/A` + header `B` → `401` (BR-12); `/mcp` + header `HOME` → `200` (US2 sc. 3)

### Implementation

- [X] T017 [US2] In `src/MymoneyMcp.Api/Common/RequireAccountGroupMiddleware.cs` map `Mismatch` → `400` `title: "Account group mismatch"` (public constant next to the others)
- [X] T018 [US2] Run `dotnet test`: T016 green; 001 `RequireAccountGroupTests` unchanged and green (SC-004)

---

## Phase 5: User Story 3 — See which group the server received (P2)

**Goal**: read-only `whogroupami` returns group, source, issuer, visible accounts (FR-009, FR-010).
**Independent Test**: `whogroupami` through `/mcp/A`, `/mcp` + header `B`, `/mcp/A` + header `A`.

### Tests (write first, must fail)

- [X] T019 [P] [US3] `tests/MymoneyMcp.Api.Tests/Diagnostics/WhoGroupAmIQueryHandlerTests.cs` (PostgreSQL, fixed `IAccountGroupContext`/`ICallerContext`): 2 accounts in `A`, 3 in `B` → as `A` returns `accountGroup "A"`, `visibleAccounts 2`; as `B` → `3`; unknown group → `0`; sources map to `url`/`header`/`both`; issuers map to `local`/`external`/`none`
- [X] T020 [P] [US3] `tests/MymoneyMcp.Api.Tests/Mcp/WhoAmIToolTests.cs` (real host, PostgreSQL, MCP SDK client; seed 2 accounts in `A`, 3 in `B` through `AppDbContext` as in `AccountGroupIsolationTests`): `/mcp/%20A%20`, no header → `{ accountGroup: "A", groupSource: "url", issuer: "local", visibleAccounts: 2 }`; `/mcp` + header `B` → `B`, `header`, `3`; `/mcp/A` + header `A` → `both`; one `HttpClient` and token calling `/mcp/A` then `/mcp/B` → second result is `B` (US1 sc. 2, FR-007); `Auth:Enabled=false` → `issuer: "none"`; the tool is listed with `readOnlyHint: true`; the response contains no token
- [X] T021 [US3] Update `tests/MymoneyMcp.Api.Tests/Mcp/McpEndpointTests.cs`: `tools/list` returns exactly one tool, `whogroupami` (was empty; contract change noted in plan.md)

### Implementation

- [X] T022 [P] [US3] Create `src/MymoneyMcp.Application/Diagnostics/WhoGroupAmI/WhoGroupAmIQuery.cs` (`sealed record`, no parameters, `IRequest<WhoGroupAmIResponse>`) and `WhoGroupAmIResponse.cs` (`AccountGroup` string, `GroupSource` string, `Issuer` string, `VisibleAccounts` int; serialized camelCase as in [contracts/mcp-endpoint.md](contracts/mcp-endpoint.md))
- [X] T023 [US3] Create `src/MymoneyMcp.Application/Diagnostics/WhoGroupAmI/WhoGroupAmIQueryHandler.cs`: `VisibleAccounts = await db.Accounts.CountAsync()` (group filter applies, no own `Where`); map `AccountGroupSource` → `url`/`header`/`both` and `TokenIssuer` → `local`/`external`/`none`; no validator (query has no input, research R7)
- [X] T024 [US3] Create `src/MymoneyMcp.Api/Mcp/Tools/WhoAmITool.cs`: `[McpServerToolType]` class; method `WhoGroupAmI(IMediator mediator, CancellationToken ct)` with `[McpServerTool(Name = "whogroupami", ReadOnly = true)]` and a `[Description]` ("Shows the account group this call resolved to, where it came from, which issuer accepted the token, and how many accounts the group can see."); only `mediator.Send(new WhoGroupAmIQuery(), ct)`
- [X] T025 [US3] Update the `ToolCollection` comment in `src/MymoneyMcp.Api/Program.cs` (tools now come from `WithToolsFromAssembly`)
- [X] T026 [US3] Run `dotnet test`: T019–T021 green

---

## Phase 6: User Story 4 — Sign in from claude.ai (P3)

**Goal**: RFC 9728 metadata naming the external issuer; MCP `401`s point to it (FR-011, FR-012, FR-016).
**Independent Test**: no token on an MCP URL → `401` pointing to metadata that names the issuer.

### Tests (write first, must fail)

- [X] T027 [P] [US4] `tests/MymoneyMcp.Api.Tests/Auth/OAuthMetadataTests.cs` (real host; external issuer set up as in `ProtectedRouteTests`): `POST /mcp/A` without token → `401` with `WWW-Authenticate` containing `resource_metadata="http://localhost/.well-known/oauth-protected-resource/mcp/A"`; `GET` that URL with no token and no header → `200` JSON with `resource` `http://localhost/mcp/A`, `authorization_servers` `["https://external.test/"]`, `bearer_methods_supported` `["header"]`; same for `/mcp`; `GET /.well-known/oauth-protected-resource/mcp/Fam%C3%ADlia` → `resource` `http://localhost/mcp/Fam%C3%ADlia` (still encoded, as the client sent it); external token on `/mcp/A` and on `/mcp/B` → `200` (FR-016); local token on `/mcp/A` → `200`; with `Auth:Enabled=false` the metadata is still served
- [X] T028 [P] [US4] `tests/MymoneyMcp.Api.Tests/Auth/NoExternalIssuerTests.cs` (real host): without `Auth:External` configured, `GET /.well-known/oauth-protected-resource/mcp/A` → `404` and `401` on `/mcp/A` has no `resource_metadata`

### Implementation

- [X] T029 [US4] In `src/MymoneyMcp.Api/Auth/AuthenticationSetup.cs`, when `external.IsConfigured`: `.AddMcp(options => …)` with `ResourceMetadata.AuthorizationServers = [external.Authority]`, `BearerMethodsSupported = ["header"]`, and `Events.OnResourceMetadataRequest` setting `Resource` to the absolute URL of the MCP path in the metadata request, taken from the raw request target (still percent-encoded, query removed), never from the decoded path; add a `"Challenge"` policy scheme as `DefaultChallengeScheme` forwarding to the MCP scheme when `Request.Path.StartsWithSegments("/mcp")` (so `/mcpx` is not matched) and to `Bearer` otherwise (research R4)
- [X] T030 [US4] Add `"/.well-known"` to `ExemptPrefixes` in `src/MymoneyMcp.Api/Common/RequireAccountGroupMiddleware.cs`
- [X] T031 [US4] Run `dotnet test`: T027, T028 and all 001 auth tests green

---

## Phase 7: Polish & Closing

- [X] T032 `dotnet build MymoneyMcp.slnx` (0 warnings) and `dotnet test MymoneyMcp.slnx` fully green; record the new test count
- [X] T033 Run [quickstart.md](quickstart.md) scenarios 1–7, 9, 10 against the AppHost and add a "Validation record" section; mark scenario 8 (real claude.ai + IdP) as pending until the IdP is chosen
- [X] T034 Review (`/code-review`): thin tool, DTO-only response, no token or group value in logs/problem details
- [X] T035 [P] Update `specs/domain/business-rules.md`: BR-02 (group from header or MCP URL), BR-10 (group in URL or header), BR-11 (+ `/.well-known/oauth-protected-resource/*`), new rules for blank URL group, URL/header mismatch, URL decoding (once, then trim), no carry-over, token audience = whole server (FR-015, FR-016)
- [X] T036 [P] Update `specs/domain/mcp-tools.md`: MCP endpoint table (`/mcp` and `/mcp/<group>`, stateless), tool `whogroupami` (since 002, no parameters, behavior), REST table row for `GET /.well-known/oauth-protected-resource/*`, links to this feature's contracts
- [X] T037 [P] Update `specs/domain/glossary.md`: Account group (from header or MCP URL), URL group, Group source (`url`/`header`/`both`), Token issuer (`local`/`external`/`none`)
- [X] T038 [P] Update `specs/architecture.md`: MCP routes, stateless transport, MCP and `Challenge` schemes, middleware order with metadata, `RequestAccountGroupContext`/`HttpCallerContext`
- [X] T039 Set `Status: Implemented` in `specs/002-claude-ai-connector/spec.md` and 002 to `Implemented` in `specs/backlog.md`

---

## Dependencies & Execution Order

- **Phase 1 → Phase 2** → stories. Phase 2 blocks every story.
- **US1** needs Phase 2. **US2** needs US1 (the group route and middleware rewrite).
- **US3** needs US1 (`/mcp/<group>`); its `both` case needs US2's mismatch rule to be meaningful.
- **US4** needs Phase 2 only (metadata and challenge work on `/mcp` too); its `/mcp/A` cases need US1.
- **Phase 7** after all stories; docs T035–T038 are the last step before T039.

Recommended order: US1 → US2 → US3 → US4.

## Parallel Examples

```text
Phase 2: T002, T003 (tests) → T004, T008 (code)
US1:     T011, T012 (tests) → T013 → T014
US3:     T019, T020 (tests) → T022 → T023 → T024
US4:     T027, T028 (tests) → T029 → T030
Phase 7: T035, T036, T037, T038
```

## Implementation Strategy

1. **MVP**: Phases 1–3 — a group URL works and is isolated on PostgreSQL. Stop and validate.
2. Add US2 (mismatch) → US3 (`whogroupami`, the end-to-end proof) → US4 (claude.ai sign-in).
3. Close with Phase 7: docs before setting `Implemented`. SC-001 (real claude.ai) is confirmed
   once the IdP is chosen.

## Notes

- Commit after each task or logical group; stop at any checkpoint to validate.
- Tools and middleware stay thin; the count lives in the handler (Constitution III).

## Implementation notes (2026-10-16)

- T011: `/mcp/A/B` → `404` is asserted **with** the header. Without it the group middleware answers
  `400` first, because it runs before endpoint matching (same as any unknown route in 001).
- T016: the "echoes neither value" check uses `ALPHA`/`BRAVO`, because the title
  "Account group mismatch" itself contains the letter `A` (analysis I1).
- T024: `options.ToolCollection = []` was removed from `Program.cs`; it replaced the tools found by
  `WithToolsFromAssembly`, so `whogroupami` was not listed.
- T008/R6: the JWT schemes now set `TokenValidationParameters.AuthenticationType` to their scheme
  name, so `HttpCallerContext` can tell `local` from `external`.
- U4 resolved: `TestServer` sets the raw target, so `%2F` is tested on the same path as Kestrel
  (`WhoAmIToolTests.Encoded_slash_is_part_of_the_group_name`).
- Test helpers added: `FixedContexts.cs`, `McpTestClient.cs`, `TestExternalIssuer.cs`; the 001
  `ProtectedRouteTests` are unchanged.
- T034: self-review only (thin tool, DTO-only response, no token or group value in logs or
  problem details); `/code-review` not run.
