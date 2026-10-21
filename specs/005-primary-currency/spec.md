# Feature Specification: Primary currency

**Feature Branch**: `005-primary-currency` | **Created**: 2026-10-18 | **Status**: Draft

**Input**: Backlog item 005 "Primary currency": each account group has a primary currency, set in
server configuration only, that 006 uses as the default currency for new accounts.

## Clarifications

### Session 2026-10-18

- Q: What happens when a configured currency is invalid? → A: An invalid default stops the server at startup; an invalid group currency invalidates only that group, and the other groups keep working.
- Q: What if the same group has two entries (e.g. `Silva` = `EUR` and `Silva` = `USD`)? → A: The group is misconfigured, like an invalid code.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One primary currency for every group (Priority: P1)

The server operator sets one default currency in the server configuration. Every group then has
that currency as its primary currency; with nothing configured, it is `BRL`.

**Why this priority**: 006 needs a primary currency for every group before it can create accounts.

**Independent Test**: start the server with and without a default currency; ask for the primary
currency of any group.

**Acceptance Scenarios**:

1. **Given** no currency configured, **When** a feature asks for the current group's primary
   currency, **Then** it gets `BRL`.
2. **Given** a configured default of `USD`, **When** a feature asks for any group's primary
   currency, **Then** it gets `USD`.
3. **Given** a configured default of `usd`, **When** the server starts, **Then** it is accepted
   and the primary currency is `USD`.

---

### User Story 2 - A different primary currency for one group (Priority: P2)

The operator sets a primary currency for a specific group. That group uses it; every other group
keeps the default.

**Why this priority**: families living in different countries must not share one currency.

**Independent Test**: configure default `BRL` and group `A` = `EUR`; ask as `A` and as `B`.

**Acceptance Scenarios**:

1. **Given** default `BRL` and group `A` set to `EUR`, **When** a feature asks as group `A`,
   **Then** it gets `EUR`.
2. **Given** the same setup, **When** a feature asks as group `B`, **Then** it gets `BRL`.
3. **Given** group `A` changed from `EUR` to `USD` and the server restarted, **When** a feature
   asks as group `B`, **Then** it still gets `BRL` (changing one group never affects another).
4. **Given** group `HOME` set to `EUR`, **When** a feature asks as group `home`, **Then** it gets
   the default (group names match exactly, BR-05).

---

### User Story 3 - A bad configuration is contained (Priority: P2)

An invalid default currency stops the server at startup, because it would affect every group.
An invalid group currency affects only that group: the server starts, the other groups keep
working, and that group cannot get a primary currency until the configuration is fixed.

**Why this priority**: a wrong currency on a new account is silent money corruption, but one
family's typo must not take the other families offline.

**Independent Test**: start the server with default `XYZ`; then with default `BRL`, group `A` =
`REAL` and group `B` = `EUR`, and ask as `A`, `B` and `C`.

**Acceptance Scenarios**:

1. **Given** a configured default of `XYZ` (or blank), **When** the server starts, **Then** it
   refuses to start and the error names the default currency setting.
2. **Given** group `A` set to `REAL` (or blank), **When** the server starts, **Then** it starts
   and logs an error naming group `A`'s setting.
3. **Given** that setup, **When** a feature asks for group `A`'s primary currency, **Then** it
   gets an error saying the group's primary currency is misconfigured, never the default.
4. **Given** that setup, **When** a feature asks as group `B` or `C`, **Then** it gets `EUR` or
   `BRL` as usual.
5. **Given** two entries for group `Silva` (with the same or different codes), **When** the server
   starts, **Then** it starts, logs an error naming `Silva`, and `Silva` is misconfigured.
6. **Given** a per-group entry with a blank group name, **When** the server starts, **Then** it
   starts, logs a warning, and ignores that entry.

### Edge Cases

- A group set to the same currency as the default: valid, behaves like the default.
- A configured group that has no accounts: valid; its primary currency is still returned (BR-06).
- A group configured but never used by a call: harmless, no error.
- Changing the configuration while the server runs: no effect until restart.
- A misconfigured group: only calls that need its primary currency fail; its other calls (e.g. `whogroupami`) keep working.
- Fixing a misconfigured group: takes effect after a restart, like any other change.
- Existing accounts after a primary-currency change: they keep their own currency (006).
- The error at startup never prints other secrets from the configuration (Constitution VIII).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every account group MUST have exactly one primary currency, identified only by its ISO 4217 alphabetic code, unless it is misconfigured (FR-014–FR-018).
- **FR-002**: The server configuration MUST allow one default currency and, optionally, one primary currency per group.
- **FR-003**: A group with its own setting MUST use it; any other group MUST use the configured default, or `BRL` when none is configured.
- **FR-004**: Group names in the configuration MUST be matched like the request's group (BR-05: exact, case-sensitive, trimmed).
- **FR-005**: Currency codes MUST be accepted in any letter case and returned in uppercase.
- **FR-006**: Any code on the current ISO 4217 list MUST be accepted; the operator maintains no list of allowed currencies, the system follows the ISO list.
- **FR-007**: The server MUST refuse to start when the configured default code is blank or not valid ISO 4217, naming the setting.
- **FR-008**: The primary currency MUST be read once at startup; a change takes effect only after a restart.
- **FR-009**: One shared service MUST give any feature the primary currency of the current call's group; features MUST NOT read the configuration directly.
- **FR-010**: The primary currency MUST be determined only by the current call's group, never by another group's setting.
- **FR-011**: There MUST be no MCP tool or REST endpoint to read or change the primary currency.
- **FR-012**: Changing a primary currency MUST only change the default for accounts created afterwards; it MUST NOT change any stored data.
- **FR-013**: `glossary.md`, `business-rules.md` and `mcp-tools.md` MUST describe the primary currency when this feature is implemented.
- **FR-014**: A group whose own code is blank or not valid ISO 4217 MUST NOT stop the server; it MUST be logged at startup as an error naming the group.
- **FR-015**: Asking for a misconfigured group's primary currency MUST return an error saying it is misconfigured; it MUST NOT fall back to the default.
- **FR-016**: A misconfigured group MUST NOT affect any other group's primary currency, nor its own calls that do not need a primary currency.
- **FR-017**: A per-group entry with a blank group name MUST be ignored with a startup warning.
- **FR-018**: A group with more than one entry (names matched as in FR-004) MUST be treated as misconfigured (FR-014–FR-016).

### Key Entities

- **Currency**: an ISO 4217 alphabetic code (e.g. `BRL`, `USD`, `EUR`); a value, not a stored entity.
- **Primary currency**: the currency a group uses by default; configured per group or inherited from the default.
- **Default currency**: the primary currency of every group without its own setting; `BRL` unless configured.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of groups that are not misconfigured get a primary currency, including groups absent from the configuration.
- **SC-002**: In the test suite, a group's primary currency never comes from another group's setting.
- **SC-003**: 100% of invalid default codes stop the server at startup; 100% of invalid group codes are reported at startup and fail only that group's primary-currency requests.
- **SC-004**: The operator can change a group's primary currency with one configuration change and one restart.

## Assumptions

- The operator is the single user (backlog 011) and edits server configuration directly.
- "Valid ISO 4217" means a code on the current ISO 4217 list; withdrawn codes are rejected.
- Minor units, symbols and formatting are out of scope; amounts are not converted between currencies.
- No existing data is touched: the system has no accounts with a currency yet (006 adds them).
- Nothing is shown to MCP callers in this feature; 006 is the first visible consumer.
