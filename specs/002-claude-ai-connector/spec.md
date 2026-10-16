# Feature Specification: Connect from claude.ai

**Feature Branch**: `002-claude-ai-connector` | **Created**: 2026-10-12 | **Status**: Draft

**Input**: Backlog item "Connect from claude.ai": claude.ai connectors cannot send `X-Account-Group`
nor a static token (001 research R2); group in the MCP URL, OAuth sign-in via the external issuer.

## Clarifications

### Session 2026-10-16

- Q: Should a token from one group's connector work on other group URLs? → A: Yes; its audience is the whole server.
- Q: Is `/mcp/` (trailing slash, no group) a blank group or plain `/mcp`? → A: Plain `/mcp`; the header decides.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pick the group through the MCP URL (Priority: P1)

The user adds one claude.ai connector per family, each pointing at a URL that names the group
(e.g. `/mcp/<group>`). Every call on it sees only that group's data, as if it sent the header.

**Why this priority**: claude.ai cannot send the header, so without this it cannot name a group.

**Independent Test**: with a valid token and no header, `/mcp/A` and `/mcp/B` each see only their group.

**Acceptance Scenarios**:

1. **Given** accounts in groups `A` and `B`, **When** an authenticated call without the header is
   made to the MCP URL for `A`, **Then** only group `A` data is visible.
2. **Given** a call to the URL for `A`, **When** the next call on the same connection uses the URL
   for `B`, **Then** that call sees only group `B` (the group is never carried over).
3. **Given** a percent-encoded URL group or one with surrounding spaces, **When** the call is
   made, **Then** it is decoded, trimmed, and matched exactly like the header (BR-05).
4. **Given** a URL group that is blank after decoding and trimming, **When** the call is made,
   **Then** it is rejected as a bad request before any data is read.

### User Story 2 - Header and URL never disagree (Priority: P2)

A caller may send both the URL group and the header. If they differ, the call is rejected, so no
call ever runs with an ambiguous group.

**Why this priority**: an ambiguous group could show one family's data to a caller expecting another.

**Independent Test**: call `/mcp/A` with header `A`, with header `B`, and with header ` A `.

**Acceptance Scenarios**:

1. **Given** URL group `A` and header `B`, **When** the call is made, **Then** it is rejected as a
   bad request before any data is read.
2. **Given** URL group `A` and header `A` (or ` A `), **When** the call is made, **Then** it is
   accepted and sees group `A`.
3. **Given** the plain MCP URL with the header, **When** a call is made, **Then** it behaves
   exactly as in 001 (Claude Code keeps working unchanged).

### User Story 3 - See which group the server received (Priority: P2)

A developer (or the user in claude.ai) calls the `whogroupami` tool and sees the group the server
resolved, where it came from, which issuer accepted the token, and how many accounts it can see.

**Why this priority**: the only end-to-end proof that a connector reaches the intended group.

**Independent Test**: call `whogroupami` through `/mcp/A`, through `/mcp` with header `B`, and
through `/mcp/A` with header `A`; check each response.

**Acceptance Scenarios**:

1. **Given** a call through `/mcp/%20A%20`, **When** `whogroupami` runs, **Then** it returns group
   `A`, source `url`, and the issuer that accepted the token.
2. **Given** a call through `/mcp` with header `B`, **When** `whogroupami` runs, **Then** it
   returns group `B` and source `header`.
3. **Given** a call through `/mcp/A` with header `A`, **When** `whogroupami` runs, **Then** it
   returns source `both`.
4. **Given** 2 accounts in `A` and 3 in `B`, **When** `whogroupami` runs as `A`, **Then** it
   returns 2 visible accounts.

### User Story 4 - Sign in from claude.ai (Priority: P3)

When the user adds the connector, claude.ai finds out from the server where to sign in, sends the
user to the trusted external issuer, and then calls the server with the token it received.

**Why this priority**: claude.ai cannot use a static token, so without sign-in it cannot connect.

**Independent Test**: call an MCP URL without a token and check the response points to the
server's published authorization details, which name the external issuer.

**Acceptance Scenarios**:

1. **Given** a call to an MCP URL without a token, **When** it is made, **Then** it is rejected as
   unauthenticated and tells the client where the server's authorization details are.
2. **Given** a client asking for the authorization details without token or group, **When** it
   asks, **Then** it gets them, naming the trusted external issuer as where to sign in.
3. **Given** a valid token from either issuer (BR-21), **When** a caller uses the MCP URL for a
   group, **Then** the call is accepted.

### Edge Cases

- Unauthenticated call with a blank or conflicting group: rejected as unauthenticated first (BR-12).
- URL group containing an encoded `/`: decoded into the group name, never treated as a sub-route.
- No group in the URL (`/mcp` or `/mcp/`) and no header: rejected as a bad request, as in 001 (BR-10).
- URL group naming a group with no accounts: empty results and 0 visible accounts (BR-06).
- Token checking turned off (BR-24): the URL group works the same; `whogroupami` reports issuer `none`.
- External issuer not configured: no authorization details, so claude.ai cannot sign in; local tokens still work.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The MCP server MUST also be reachable at `/mcp/<group>`; plain `/mcp` keeps working.
- **FR-002**: The URL group MUST set the call's group through the same central mechanism as the header (BR-03).
- **FR-003**: The URL group MUST be percent-decoded, then trimmed and matched exactly like the header (BR-05).
- **FR-004**: A URL group that is blank after decoding and trimming (e.g. `/mcp/%20`) MUST get `400`
  before any handler runs; `/mcp/` with nothing after the slash is plain `/mcp`.
- **FR-005**: A URL group and header that differ after trimming MUST get `400` before any handler runs.
- **FR-006**: A call with no group in the URL nor the header MUST get `400` (BR-10, amended).
- **FR-007**: The group MUST NOT be remembered between calls, connections, or sessions.
- **FR-008**: The URL group applies to MCP only; REST routes keep requiring the header.
- **FR-009**: The `whogroupami` tool MUST return the resolved group, its source (`url`, `header`,
  or `both`), the issuer that accepted the token (`local`, `external`, or `none`), and the number
  of the group's visible accounts.
- **FR-010**: `whogroupami` MUST be read-only, in every environment, never returning the token or other groups' data.
- **FR-011**: The server MUST publish, without token or group, authorization details naming the external issuer.
- **FR-012**: An unauthenticated call to any MCP URL MUST get `401` pointing to those details.
- **FR-013**: The authentication rules of 001 (BR-20 to BR-26) MUST stay unchanged; tokens carry no group.
- **FR-014**: Any authenticated caller MAY use any group via the URL (BR-07); it is not a security boundary.
- **FR-016**: A token's audience MUST be the whole server; one valid token works on every MCP URL.
- **FR-015**: BR-10 and BR-11 MUST be updated to the URL group when this feature is implemented.

### Key Entities

- **URL group**: the group named in the MCP URL path; same meaning as the header; never stored.
- **Authorization details**: public info telling a client which issuer to sign in with, for which server.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The user can add a claude.ai connector for a group, sign in, and see `whogroupami`
  return that group in under 5 minutes, with no header or static token.
- **SC-002**: In the test suite, 100% of calls through a group URL return zero records from another group.
- **SC-003**: 100% of calls with a blank or disagreeing URL group are rejected before any data is read.
- **SC-004**: All 001 tests for the header and authentication pass unchanged.

## Assumptions

- The group is a single URL path segment; any character a group may contain is percent-encoded.
- A single user for now; restricting groups per caller stays a backlog item.
- The identity provider is deferred and chosen at deployment; it handles sign-in and client
  registration, and this server only points to it and validates its tokens.
- `whogroupami` is a diagnostic tool, not a business tool.
- Constitution v2.3.0 (Principles II, V, VII) already allows the group in the MCP URL.
