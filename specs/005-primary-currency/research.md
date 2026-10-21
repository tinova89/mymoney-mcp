# Research: Primary currency

Source of each decision: **Spec** = `spec.md` (incl. clarifications); **User** = agreed in the
2026-10-18 design review; **Proposed** = chosen in this plan.

## R1. Configuration shape (Spec FR-002, FR-004; User)

- **Decision**: one section `Currency` with `Default` (string) and `Groups` (list of
  `{ Group, Code }`). Contract: [contracts/currency-configuration.md](contracts/currency-configuration.md).
- **Sources**: the normal .NET configuration chain, like `Auth:*`: `appsettings.json` (base,
  `Default: BRL`), `appsettings.Development.json` or user-secrets locally, environment variables
  in production (`Currency__Default`, `Currency__Groups__0__Group`, `Currency__Groups__0__Code`).
  Currency is not a secret, so it may be committed.
- **Why a list**: .NET configuration keys are case-insensitive, so a dictionary keyed by group
  would merge `HOME` and `home` (breaks BR-05); group names with spaces or symbols also cannot be
  environment-variable key segments.
- **Alternatives**: dictionary `Groups: { "Silva": "EUR" }` — rejected (above). A database table
  — rejected: the backlog says configuration only, no MCP tool, no REST.

## R2. Valid ISO 4217 codes (Spec FR-005, FR-006; Assumptions)

- **Decision**: a static set of the active alphabetic codes of ISO 4217 List One, embedded in
  `Domain/Currencies/Iso4217.cs`, read by the `CurrencyCode` value object. The file header names
  the List One amendment it was copied from. Withdrawn codes (e.g. `HRK`, `VEF`) are absent.
- **Normalization**: trim, then upper-case invariant; then look up in the set (`usd` → `USD`).
- **All List One codes count**, including fund and `X` codes (`XAU`, `XDR`), as the spec says
  "any code on the current list".
- **Updating**: a new ISO amendment is a small change to that file plus its tests.
- **Alternatives**: `RegionInfo.ISOCurrencySymbol` — rejected: only country currencies, depends
  on ICU data (fails in globalization-invariant containers). A NuGet currency package — rejected:
  a new dependency for a list of ~180 strings (Constitution VIII).

## R3. Building the table once at startup (Spec FR-003, FR-007, FR-008, FR-014–FR-018)

- **Decision**: a pure `PrimaryCurrencyTable.Build(CurrencySettings)` in Application returns the
  table plus a list of problems. Rules, in order:
  1. `Default` absent (null) → `BRL`; present but blank or invalid → fatal problem.
  2. Entries with a blank `Group` → warning, entry ignored.
  3. `Group` trimmed; entries grouped by ordinal, case-sensitive name (BR-05).
  4. A group with more than one entry → misconfigured, whatever the codes; one error per entry,
     so each log line points to its own `Currency:Groups:<index>`.
  5. A group with one entry whose code is blank or invalid → misconfigured (error).
  6. Otherwise the group maps to its normalized code.
- **Startup** (`Api/Common/PrimaryCurrencyStartup.cs`, same split as `AuthStartupGuard`):
  `Build` runs before `builder.Build()`; a fatal problem throws `InvalidOperationException`
  naming `Currency:Default`, so the host never starts. The table is registered as a singleton.
  After `builder.Build()`, the other problems are logged (error or warning), each naming the
  setting path (`Currency:Groups:<index>`) and the group.
- **Read once**: the section is read with `Get<CurrencySettings>()`, not `IOptionsMonitor`, so a
  configuration change has no effect until restart (FR-008).
- **Logs**: currency codes and group names are not secrets (Constitution VIII); no other
  configuration value is ever printed.
- **Alternatives**: `ValidateOnStart` options validation — rejected: it can only fail the whole
  host, and a bad group must not (Spec clarification). Failing silently to the default for a bad
  group — rejected by FR-015.

## R4. The shared service (Spec FR-009, FR-010, FR-015, FR-016)

- **Decision**: `IPrimaryCurrency` with `CurrencyCode GetForCurrentGroup()` in
  `Application/Currencies`, implemented by the scoped `PrimaryCurrencyService`, which reads
  `IAccountGroupContext.Group` and the singleton table. Registered by `AddApplication`; the table
  by the Api (it needs configuration). Features inject only `IPrimaryCurrency`.
- **Misconfigured group** → throws `PrimaryCurrencyMisconfiguredException` (Application
  exceptions folder); message without the group name or code. Nothing else of that group is
  affected, because only callers of `IPrimaryCurrency` see it (FR-016).
- **No group in the context** (startup, exempt routes) → `InvalidOperationException`: a feature
  asking outside a group call is a bug, not a user error.
- **Isolation**: the lookup key is only the current request's group (FR-010); a test proves
  group `B` never gets group `A`'s setting.

## R5. Error mapping to callers (Spec FR-011, FR-015)

- **Decision**: deferred to 006, the first feature that calls `IPrimaryCurrency` from a tool.
  005 adds no tool or route, so a mapping here could not be tested through the real host
  (Constitution V) and would be dead code (Constitution VIII).
- **Hint for 006**: map `PrimaryCurrencyMisconfiguredException` to a tool error / problem details
  that says the group's primary currency is misconfigured, never echoing values (BR-17 style).

## R6. Tests (Constitution IV, V)

- **Unit** (`MymoneyMcp.Application.Tests/Currencies`): `CurrencyCode`, `PrimaryCurrencyTable`
  rules, `PrimaryCurrencyService` with a fixed group context. No database involved, so no
  PostgreSQL test is required for these rules.
- **Host** (`MymoneyMcp.Api.Tests/Currencies`): the real host with `Currency:*` settings —
  invalid default fails startup; invalid/duplicate group starts, logs the error, and the service
  resolved in a request scope behaves as specified; `whogroupami` on a misconfigured group keeps
  working (FR-016).
- No MCP tool, so no MCP tool integration test (Constitution V applies from 006).
- Host tests run with environment `Testing`: user-secrets load only in `Development`, so a
  developer's `Currency:*` secrets cannot change results; `appsettings.json` still loads.
- Read once (FR-008) is proven by changing the configuration and calling `Reload()` after start.
