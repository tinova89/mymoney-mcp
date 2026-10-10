# Feature Specification: System Foundation

**Feature Branch**: `001-system-foundation` | **Created**: 2026-10-10 | **Status**: Draft

**Input**: User description: "Fundação do sistema: estrutura da solução, Postgres via Aspire, account como raiz dos dados com isolamento por X-Account-Group, autenticação JWT e host MCP sem tools de negócio."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Each group sees only its own accounts (Priority: P1)

A family (an account group) uses the system and sees only its own accounts. Another family's
accounts are never visible, and asking for one by its identifier behaves as if it does not exist.

**Why this priority**: data isolation is the base rule of the system (Constitution, Principle II).
Every later feature depends on it.

**Independent Test**: automated tests load accounts for two groups as test data, read as each
group, and confirm only that group's accounts come back (no caller-facing account operations yet).

**Acceptance Scenarios**:

1. **Given** accounts in groups `A` and `B`, **When** a caller reads accounts as group `A`,
   **Then** only group `A` accounts are returned.
2. **Given** an account in group `B`, **When** a caller asks for it by identifier as group `A`,
   **Then** the response is "not found", with no hint that the account exists.
3. **Given** a group with no accounts, **When** a caller reads accounts as that group, **Then**
   the result is empty, not an error.

### User Story 2 - Only authenticated callers get in (Priority: P2)

A caller (Claude or another client) must present a valid access token to use the MCP server or
the REST API. Tokens come from this system or from a trusted external issuer. Getting a token does
not require an account group. The server operator can turn token checking off.

**Why this priority**: family financial data must never be reachable without authentication.

**Independent Test**: call a protected route without a token, with an expired token, and with a
valid token; only the last one succeeds.

**Acceptance Scenarios**:

1. **Given** no token or an invalid/expired token, **When** a caller uses MCP or REST, **Then**
   the call is rejected as unauthenticated and no data is read.
2. **Given** valid credentials, **When** a caller requests a token without the group header,
   **Then** a token is issued.
3. **Given** a valid token, **When** a caller requests a new one through the refresh route without
   the group header, **Then** a new token is issued.
4. **Given** a valid token from the trusted external issuer, **When** a caller uses MCP or REST,
   **Then** the call is accepted.
5. **Given** token checking turned off on a non-production server, **When** a caller without a
   token uses MCP or REST with the group header, **Then** the call is accepted.

### User Story 3 - Every call names its account group (Priority: P3)

Every MCP and REST call must carry the `X-Account-Group` header. Calls without it are rejected
before any processing.

**Why this priority**: the header is how the system knows which accounts the call may see.

**Independent Test**: call a protected route with and without the header and check the result.

**Acceptance Scenarios**:

1. **Given** an authenticated call with a missing or blank header, **When** it reaches MCP or REST,
   **Then** it is rejected as a bad request and no data is read.
2. **Given** a call to a health-check, API-docs, or authentication route, **When** it has no
   header, **Then** it is not rejected for that reason.

### User Story 4 - The system runs locally with one command (Priority: P4)

A developer starts the whole system — API, MCP server, and database — with one command, and can
see that it is healthy.

**Why this priority**: every later feature is built and tested on top of this.

**Independent Test**: on a clean machine with the prerequisites, start the system and check the
health route and the MCP server.

**Acceptance Scenarios**:

1. **Given** the prerequisites from the README, **When** the developer starts the system, **Then**
   the database is created and up to date, and the health route reports healthy.
2. **Given** the running system, **When** an MCP client connects with a valid token and header,
   **Then** the connection succeeds and the server lists no business tools.

### Edge Cases

- Header with leading/trailing spaces or different letter case (see Assumptions).
- Header present but naming a group that has no accounts: empty results, not an error.
- Database unavailable at startup: the health route reports unhealthy instead of the API crashing.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST store accounts, each belonging to exactly one account group.
- **FR-002**: Every read of account-owned data MUST return only data of the caller's account
  group, enforced in one central place rather than in each feature.
- **FR-003**: A request for an account of another group MUST behave exactly like a request for an
  account that does not exist.
- **FR-004**: Every MCP and REST call MUST be rejected when the `X-Account-Group` header is
  missing or blank, before any data is read. Health-check, API-docs, and authentication routes are
  exempt.
- **FR-005**: Every MCP and REST call except authentication, health-check and API-docs routes MUST
  require a valid, unexpired access token, unless token checking is turned off (FR-014).
- **FR-006**: The system MUST provide routes to obtain and refresh an access token. Tokens MUST
  NOT carry or depend on an account group.
- **FR-007**: Access tokens MUST be accepted from this system and from one trusted external issuer
  configured on the server.
- **FR-008**: This feature MUST NOT offer any way to create or read accounts from outside;
  accounts exist only as test data.
- **FR-009**: An authenticated caller MAY use any account group. Restricting groups per caller is
  a separate backlog item (see `specs/backlog.md`).
- **FR-010**: The MCP server MUST accept connections and expose no business tools.
- **FR-011**: The whole system MUST start locally with one command, creating or upgrading the
  database automatically.
- **FR-012**: The system MUST expose a health route that reports database availability.
- **FR-013**: Secrets (such as the token signing key) MUST come from configuration outside the
  repository.
- **FR-014**: Token checking MUST be on by default and MAY be turned off only through server
  configuration, never by a caller, and never in production: the server MUST refuse to start there
  with it off. While off, it MUST log a startup warning; the group header is still required.

### Key Entities

- **Account**: a financial account owned by one account group; has a display name. The root of
  all other financial data (transactions, categories, budgets come in later features).
- **Account Group**: a free-text value (e.g. a family name) that groups accounts and defines what
  a caller can see. It is not stored in the token.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In the automated test suite, 100% of cross-group reads return zero records from
  another group, and 100% of cross-group lookups return "not found".
- **SC-002**: With token checking on, 100% of calls without a valid token or without the group
  header are rejected before any data is read.
- **SC-003**: A developer with the prerequisites installed can start the full system with one
  command in under 10 minutes.
- **SC-004**: A later feature that adds account-owned data needs no isolation logic of its own to
  pass its cross-group test.

## Assumptions

- Group matching is exact and case-sensitive after trimming surrounding spaces (`HOME` ≠ `home`).
- No business tools, transactions, categories, or budgets are part of this feature.
- The refresh route issues a new token and invalidates the previous refresh token.
- Turning token checking off is allowed only outside production (FR-014).
- Account types (checking, credit card, etc.) are defined in a later feature.
