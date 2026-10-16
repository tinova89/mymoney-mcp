# Research: Connect from claude.ai

Source of each decision: **Spec** = `spec.md` (incl. clarifications); **Proposed** = chosen in
this plan; **SDK** = verified in `ModelContextProtocol.AspNetCore` 2.2.0 (its XML docs).
001 decisions still apply: [001 research](../001-system-foundation/research.md).

## R1. Resolving the group (Spec FR-002–FR-007)

- **Decision**: one pure `AccountGroupResolver` in `Api/Common` turns (request path, raw target,
  header) into a resolution: `Group`, `Source` (`None`/`Url`/`Header`/`Both`) and an error
  (`Missing`, `BlankUrlGroup`, `Mismatch`). It runs once per request; the result is kept in
  `HttpContext.Items`, read by both `RequireAccountGroupMiddleware` and the scoped
  `IAccountGroupContext`. Nothing outlives the request.
- **URL group**: only for paths `/mcp/<segment>`. The segment is read from the **raw** request
  target (`IHttpRequestFeature.RawTarget`, query string removed) and percent-decoded **once**
  (`Uri.UnescapeDataString`), then trimmed.
- **Why raw target**: Kestrel decodes `Request.Path` except `%2F`; decoding `Path` again would
  double-decode (`%252F` → `/`), and using it as is would keep `%2F` literal (spec edge case).
- **Blank header**: a present but blank header counts as absent (as in 001), so `/mcp/A` with
  header `"  "` resolves to `A` from `url`.
- **Matching**: URL group and header are compared after trimming, ordinal and case-sensitive (BR-05).
- **Alternatives**: route value `{accountGroup}` — rejected: already decoded once, `%2F` handling
  is inconsistent. Endpoint filter on MCP only — rejected: must run before any handler and share
  the 001 middleware's `400` shape.

## R2. Routing (Spec FR-001, FR-008; clarification on `/mcp/`)

- **Decision**: `MapMcp("/mcp")` (unchanged) plus `MapMcp("/mcp/{accountGroup}")`. A group is one
  segment: `/mcp/A/B` matches nothing → `404`. `/mcp/` matches the plain route (routing ignores
  a trailing slash), so it has no URL group and the header decides.
- **SDK**: `MapMcp(pattern)` takes a route pattern prefix, so a parameter is allowed; a test proves
  `initialize` works on both routes.
- REST routes have no group segment, so the resolver never reads a URL group for them.

## R3. Stateless transport (Spec FR-007)

- **Decision**: `WithHttpTransport(o => o.Stateless = true)`, set explicitly.
- **SDK**: 2.2.0 already defaults to stateless (protocol `2026-07-28`, SEP-2567); pinning it keeps
  FR-007 from depending on an SDK default. No session, so nothing can carry a group between calls.
- **Cost**: no server-to-client requests (sampling, elicitation); no feature needs them.

## R4. Authorization details for claude.ai (Spec FR-011, FR-012, US4)

- **Decision**: register the SDK's MCP authentication scheme (`AddMcp`, `McpAuthenticationHandler`)
  **only when `Auth:External` is configured** (spec edge case). It:
  - serves RFC 9728 protected resource metadata at `/.well-known/oauth-protected-resource/<path>`
    (default `ResourceMetadataUri`, mirrors the requested MCP path), anonymous and group-free;
  - on challenge, answers `401` with `WWW-Authenticate: Bearer resource_metadata="<that URL>"`.
- **Metadata values**: `authorization_servers = [Auth:External:Authority]`; `resource` set per
  request in `Events.OnResourceMetadataRequest` to the absolute URL of the requested MCP path,
  because claude.ai checks it equals the connector URL; `bearer_methods_supported = ["header"]`.
- **Challenge only on MCP**: a second policy scheme `Challenge` becomes `DefaultChallengeScheme`;
  it forwards to the MCP scheme for `/mcp` paths and to `Bearer` (001 behavior) for the rest, so
  REST `401`s are unchanged (SC-004).
- **Exempt**: `/.well-known` joins the group-exempt prefixes. The handler answers during
  `UseAuthentication`, before authorization and the group check.
- **Auth off (BR-24)**: metadata is still served when the external issuer is configured.
- **Alternatives**: hand-written metadata endpoint — rejected, the SDK already implements it.

## R5. Token audience (Spec FR-016, clarification)

- **Decision**: unchanged validation: the `External` scheme accepts one configured
  `Auth:External:Audience`, the same for every MCP URL. Local tokens keep `Jwt:Audience`.
- **Constraint on the deferred IdP**: it must issue tokens with that one audience for every group
  URL (e.g. map the RFC 8707 `resource` of any `/mcp/<group>` to the same API). Recorded in
  `quickstart.md`.
- **Alternatives**: audience per group URL — rejected by the clarification.

## R6. Issuer and source for `whogroupami` (Spec FR-009)

- **Decision**: `IAccountGroupContext` gains `Source` (`AccountGroupSource`). A new
  `ICallerContext` (Application interface, Api implementation) exposes `Issuer`
  (`TokenIssuer.Local`/`External`/`None`) from `HttpContext.User.Identity.AuthenticationType`,
  which is the scheme name that validated the token; unauthenticated (auth off) → `None`.
- **Rationale**: handlers stay free of `HttpContext` (Constitution III); the token is never read.

## R7. `whogroupami` tool (Spec US3, FR-009, FR-010)

- **Decision**: `Application/Diagnostics/WhoGroupAmI/`: `WhoGroupAmIQuery` (no parameters),
  handler, `WhoGroupAmIResponse`. The handler counts `IAppDbContext.Accounts` (group filter applies,
  BR-03). Tool class `Api/Mcp/Tools/WhoAmITool.cs` (`[McpServerToolType]`) holds the "who am I"
  diagnostics; its method `WhoGroupAmI` is `[McpServerTool(Name = "whogroupami", ReadOnly = true)]`
  and only sends the query through `IMediator`. Later diagnostics become more methods on this class.
- **No validator**: the query has no input; `ValidationBehavior` finds none and passes through.
- **Response values**: lower-case strings fixed by the contract (`url`/`header`/`both`,
  `local`/`external`/`none`), mapped in the handler from the enums.

## R8. Testing (Constitution IV/V)

- **Unit** (`Api.Tests/AccountGroup`): resolver cases — encoded, spaces, `%2F`, `%252F`, blank,
  mismatch, trailing slash, query string, blank header.
- **Integration** (real host + Testcontainers PostgreSQL + MCP SDK client): every acceptance
  scenario; external tokens signed by the test issuer of 001 `ProtectedRouteTests`.
- **001 test that changes**: `McpEndpointTests` expects `tools/list` = `[]`; it now expects only
  `whogroupami` (001 FR-010 allows no *business* tools). All other 001 tests stay unchanged.
