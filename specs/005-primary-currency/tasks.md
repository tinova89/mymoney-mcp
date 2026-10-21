---
description: "Task list for 005-primary-currency"
---

# Tasks: Primary currency

**Input**: [spec.md](spec.md) · [plan.md](plan.md) · [research.md](research.md) ·
[data-model.md](data-model.md) · [contracts/](contracts/) · [quickstart.md](quickstart.md)

**Tests**: required — Constitution IV (NON-NEGOTIABLE). Write each story's tests first and see them
fail before implementing. No PostgreSQL behavior and no MCP tool, so Constitution V needs only
real-host startup tests (plan Constitution Check).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1–US3 from spec.md

---

## Phase 1: Setup

- [ ] T001 Run `dotnet build MymoneyMcp.slnx` (0 warnings) and `dotnet test MymoneyMcp.slnx` (all green) on the current `main` and note the test count

---

## Phase 2: Foundational (blocks every story)

**Purpose**: the currency code value object and the raw settings shape (research R1, R2).

### Tests (write first, must fail)

- [ ] T002 [P] `tests/MymoneyMcp.Application.Tests/Currencies/CurrencyCodeTests.cs` (unit): `TryParse` accepts `BRL`, `USD`, `EUR`, `XAU`, `XDR` → same `Value`; `usd` and ` usd ` → `USD`; rejects `XYZ`, `REAL`, `US`, `HRK` (withdrawn), `VEF` (withdrawn), `""`, `"   "`, `null`; `Parse("XYZ")` throws `ArgumentException`; two parses of `usd`/`USD` are equal; `ToString()` returns `Value`

### Implementation

- [ ] T003 [P] Create `src/MymoneyMcp.Domain/Currencies/Iso4217.cs`: `internal static class Iso4217` with a `FrozenSet<string>` (ordinal) of every **active** alphabetic code of ISO 4217 List One, fund and `X` codes included; header comment names the List One amendment number/date it was copied from (verify against the published list at implementation time; withdrawn codes such as `HRK`, `VEF` MUST be absent) (research R2)
- [ ] T004 Create `src/MymoneyMcp.Domain/Currencies/CurrencyCode.cs`: immutable value object with `Value` — "3 upper-case letters on ISO 4217 List One"; `static bool TryParse(string? value, out CurrencyCode code)` trims, upper-cases (`ToUpperInvariant`), accepts only codes in `Iso4217`; `static CurrencyCode Parse(string value)` throws `ArgumentException` when invalid; equality by `Value`; `ToString()` → `Value`; no packages (data-model.md)
- [ ] T005 [P] Create `src/MymoneyMcp.Application/Currencies/CurrencySettings.cs`: `public const string SectionName = "Currency"`; `string? Default` ("`null` = not configured (→ `BRL`)"); `List<GroupCurrencySetting> Groups = []`; `GroupCurrencySetting` with `string? Group`, `string? Code` (raw values, no validation here)
- [ ] T006 Run `dotnet build` (0 warnings) and `dotnet test`: T002 green

**Checkpoint**: valid currency codes exist as a Domain type.

---

## Phase 3: User Story 1 — One primary currency for every group (P1) 🎯 MVP

**Goal**: every group gets the default (`BRL` unless configured) through `IPrimaryCurrency` (FR-001–FR-003, FR-005, FR-008, FR-009).
**Independent Test**: start with and without `Currency:Default`; ask any group's primary currency.

### Tests (write first, must fail)

- [ ] T007 [P] [US1] `tests/MymoneyMcp.Application.Tests/Currencies/PrimaryCurrencyTableTests.cs` (unit): `Build` with `Default = null` → `Default` is `BRL`, no problems; `"USD"` → `USD`; `"usd"` → `USD`; `Lookup("any")` returns the default when the group has no entry
- [ ] T008 [P] [US1] `tests/MymoneyMcp.Application.Tests/Currencies/PrimaryCurrencyServiceTests.cs` (unit, a local fake `IAccountGroupContext`): table with default `USD` → `GetForCurrentGroup()` is `USD` for groups `A` and `B`; context with empty group → `InvalidOperationException` (research R4)
- [ ] T009 [P] [US1] `tests/MymoneyMcp.Api.Tests/Currencies/PrimaryCurrencyStartupTests.cs` (real host via `ApiFactory` with `Environment = "Testing"`, so the developer's user-secrets are not loaded; class marked `[Collection(PostgresCollection.Name)]`, because `ApiFactory` needs a database; `ConfigureServices` replacing `IAccountGroupContext` with `FixedAccountGroup`; resolve `IPrimaryCurrency` from `factory.Services.CreateScope()`): no override → `BRL` (from `appsettings.json`); `Currency:Default` = `usd` → `USD`; after start, set `Currency:Default` = `EUR` through the host's `IConfiguration` indexer and call `((IConfigurationRoot)configuration).Reload()` → a new scope still resolves `USD` (FR-008)

### Implementation

- [ ] T010 [US1] Create `src/MymoneyMcp.Application/Currencies/PrimaryCurrencyTable.cs`: immutable; `CurrencyCode Default`; `PrimaryCurrencyLookup Lookup(string group)` returning `Configured(CurrencyCode)` or `Misconfigured` (default when no entry); `static PrimaryCurrencyBuildResult Build(CurrencySettings settings)` → `(PrimaryCurrencyTable? Table, IReadOnlyList<PrimaryCurrencyProblem> Problems)`; `PrimaryCurrencyProblem(Kind, Severity, SettingPath, Group?)` with kinds `InvalidDefault` (Fatal), `BlankGroupName` (Warning), `DuplicateGroup` (Error), `InvalidGroupCode` (Error). In this task implement only rule 1 of research R3 (default: `null` → `BRL`, else parse); groups come in US2/US3
- [ ] T011 [US1] Create `src/MymoneyMcp.Application/Currencies/IPrimaryCurrency.cs` (`CurrencyCode GetForCurrentGroup()`) and `PrimaryCurrencyService.cs` (scoped; reads `IAccountGroupContext.Group` and the table; empty group → `InvalidOperationException`); register `IPrimaryCurrency` scoped in `src/MymoneyMcp.Application/DependencyInjection.cs`
- [ ] T012 [US1] Create `src/MymoneyMcp.Api/Common/PrimaryCurrencyStartup.cs`: `static PrimaryCurrencyBuildResult Build(IConfiguration)` binds section `Currency` with `Get<CurrencySettings>()` (read once, no options monitor; absent `Default` = `configuration["Currency:Default"] is null`, so an empty string stays blank, not absent) and calls `PrimaryCurrencyTable.Build`; wire it in `src/MymoneyMcp.Api/Program.cs` right after `AuthStartupGuard.Validate` and register the table as a singleton (plan Startup Order)
- [ ] T013 [P] [US1] Create `src/MymoneyMcp.Api/appsettings.json` with only `{ "Currency": { "Default": "BRL" } }` (contracts/currency-configuration.md)
- [ ] T014 [US1] Run `dotnet build` (0 warnings) and `dotnet test`: T007–T009 green

**Checkpoint**: every group resolves the configured default (MVP).

---

## Phase 4: User Story 2 — A different primary currency for one group (P2)

**Goal**: per-group entries, matched like the header, never leaking across groups (FR-003, FR-004, FR-010).
**Independent Test**: default `BRL`, group `A` = `EUR`; ask as `A` and as `B`.

### Tests (write first, must fail)

- [ ] T015 [P] [US2] Add to `PrimaryCurrencyTableTests.cs`: default `BRL` + `A`=`EUR` → `Lookup("A")` is `EUR`, `Lookup("B")` is `BRL`; entry `HOME`=`EUR` → `Lookup("home")` is the default (BR-05); entry ` Silva `=`eur` → `Lookup("Silva")` is `EUR`; entry equal to the default is valid, no problems
- [ ] T016 [P] [US2] Add to `PrimaryCurrencyServiceTests.cs`: one table with `A`=`EUR`, `B` absent → service for context `A` gets `EUR`, for `B` gets `BRL`; tables differing only in `A` (`EUR` vs `USD`) give `B` the same result (US2 sc. 3, FR-010, SC-002)
- [ ] T017 [P] [US2] Add to `PrimaryCurrencyStartupTests.cs` (`Environment = "Testing"` as in T009): settings `Currency:Groups:0:Group`=`A`, `Currency:Groups:0:Code`=`EUR` → group `A` resolves `EUR`, group `B` resolves `BRL`

### Implementation

- [ ] T018 [US2] Extend `PrimaryCurrencyTable.Build` in `src/MymoneyMcp.Application/Currencies/PrimaryCurrencyTable.cs`: research R3 rules 3 and 6 (trim `Group`, ordinal case-sensitive grouping, one valid entry → `Configured(code)`)
- [ ] T019 [US2] Run `dotnet build` (0 warnings) and `dotnet test`: T015–T017 green

**Checkpoint**: groups can override the default; other groups unaffected.

---

## Phase 5: User Story 3 — A bad configuration is contained (P2)

**Goal**: invalid default stops startup; invalid, duplicate or nameless group entries affect only that entry's group (FR-007, FR-014–FR-018).
**Independent Test**: default `XYZ`; then default `BRL` with `A`=`REAL`, `B`=`EUR`, asking as `A`, `B`, `C`.

### Tests (write first, must fail)

- [ ] T020 [P] [US3] Add to `PrimaryCurrencyTableTests.cs`: `Default` `"XYZ"`, `""`, `"   "` → one `InvalidDefault` Fatal problem with path `Currency:Default`; `A`=`REAL` and `A`=`""` → `Lookup("A")` is `Misconfigured`, one `InvalidGroupCode` Error with path `Currency:Groups:0` and group `A`; two `Silva` entries (`EUR`/`USD`, and `EUR`/`EUR`) → `Misconfigured`, one `DuplicateGroup` Error **per entry** (paths `Currency:Groups:0` and `Currency:Groups:1`), each naming `Silva`; `Silva` + ` Silva ` → duplicate; `Silva` + `silva` → two valid groups; blank `Group` (`""`, `"  "`, `null`) → `BlankGroupName` Warning with its path, entry ignored; with `A` misconfigured, `B`=`EUR` and unlisted `C` still resolve `EUR`/default
- [ ] T021 [P] [US3] Add to `PrimaryCurrencyServiceTests.cs`: misconfigured group → `PrimaryCurrencyMisconfiguredException`, never the default; its message contains neither the group name nor the configured code
- [ ] T022 [P] [US3] Add to `PrimaryCurrencyStartupTests.cs` (pattern of `AuthDisabledTests`; `Environment = "Testing"` as in T009): `Currency:Default` = `XYZ` and `""` → `factory.CreateClient()` throws, flattened message contains `Currency:Default`; `Currency:Groups:0` = `Silva`/`REAL` → host starts, `factory.Logs` has an Error containing `Currency:Groups:0` and `Silva`, group `Silva` throws `PrimaryCurrencyMisconfiguredException`, group `B` resolves `BRL`; two `Silva` entries → one Error log per entry (`Currency:Groups:0`, `Currency:Groups:1`), each naming `Silva`; blank `Group` → Warning containing `Currency:Groups:0`; with `Silva` misconfigured, `whogroupami` on `/mcp/Silva` (`McpTestClient`, PostgreSQL) still succeeds (FR-016)

### Implementation

- [ ] T023 [P] [US3] Create `src/MymoneyMcp.Application/Common/Exceptions/PrimaryCurrencyMisconfiguredException.cs`: message "The primary currency of this account group is misconfigured." (no group, no code) (research R4)
- [ ] T024 [US3] Complete `PrimaryCurrencyTable.Build`: research R3 rules 1 (blank/invalid default → Fatal), 2 (blank group → Warning, ignored), 4 (more than one entry → one `DuplicateGroup` per entry, misconfigured), 5 (blank/invalid code → `InvalidGroupCode`, misconfigured); `PrimaryCurrencyService` throws `PrimaryCurrencyMisconfiguredException` on `Misconfigured`
- [ ] T025 [US3] In `src/MymoneyMcp.Api/Common/PrimaryCurrencyStartup.cs`: throw `InvalidOperationException` naming `Currency:Default` on a Fatal problem (before `builder.Build()`); add `LogProblems(result, ILogger)` logging each Error/Warning with its setting path and group (no other configuration value); call it in `src/MymoneyMcp.Api/Program.cs` after `AuthStartupGuard.WarnIfDisabled`
- [ ] T026 [US3] Run `dotnet build` (0 warnings) and `dotnet test`: T020–T022 green

**Checkpoint**: one family's typo never takes the others offline; a bad default never starts.

---

## Phase 6: Polish & Closing

- [ ] T027 Run `dotnet build MymoneyMcp.slnx` (0 warnings) and `dotnet test MymoneyMcp.slnx`: all green, test count = T001 + the new tests
- [ ] T028 Run quickstart.md manual scenarios 2, 4 and 7 against the AppHost (startup failure, error log, warning log)
- [ ] T029 Review (`/code-review`): no feature reads `Currency:*` except `PrimaryCurrencyStartup`; logs carry only setting paths, group names and codes; no tool or route added (FR-011)
- [ ] T030 [P] Update `specs/domain/glossary.md`: Currency (ISO 4217 code, `CurrencyCode`), Primary currency, Default currency, `Currency:*` configuration
- [ ] T031 [P] Update `specs/domain/business-rules.md`: new section "Primary currency (since 005)" — default `BRL`; per-group override matched like BR-05; codes any case, any active ISO 4217 code; invalid default stops startup; invalid or duplicate group entry → only that group misconfigured, error, never the default; blank group name ignored with warning; read once (restart to apply); changing it changes no stored data
- [ ] T032 [P] Update `specs/domain/mcp-tools.md`: note that no MCP tool or REST route reads or changes the primary currency (configuration only, link to contracts/currency-configuration.md)
- [ ] T033 [P] Update `specs/architecture.md`: Domain `Currencies`, `Currency` configuration (`appsettings.json` base, user-secrets, env vars), startup order with `PrimaryCurrencyStartup`, `IPrimaryCurrency` scoped over a singleton table
- [ ] T034 Set `Status: Implemented` in `specs/005-primary-currency/spec.md` and 005 to `Implemented` in `specs/backlog.md`

---

## Dependencies & Execution Order

- **Phase 1 → Phase 2** → stories. Phase 2 blocks every story.
- **US1** needs Phase 2. **US2** needs US1 (table, service, startup wiring).
- **US3** needs US1; its duplicate and "others unaffected" cases build on US2's group entries.
- **Phase 6** after all stories; docs T030–T033 are the last step before T034.

Recommended order: US1 → US2 → US3.

## Parallel Examples

```text
Phase 2: T002 (test) → T003, T005 (code) → T004
US1:     T007, T008, T009 (tests) → T010 → T011 → T012, T013
US2:     T015, T016, T017 (tests) → T018
US3:     T020, T021, T022 (tests) → T023 → T024 → T025
Phase 6: T030, T031, T032, T033
```

## Implementation Strategy

1. **MVP**: Phases 1–3 — every group resolves the default through `IPrimaryCurrency`. Stop and validate.
2. Add US2 (per-group override) → US3 (containment of bad configuration).
3. Close with Phase 6: docs before setting `Implemented`. Caller-facing error mapping is 006's (research R5).

## Notes

- Currency host tests run with `Environment = "Testing"`: user-secrets load only in `Development`, so a developer's `Currency:*` secrets cannot change the results. `appsettings.json` still loads (the `BRL` base). The rule "absent `Default` → `BRL`" is proven by the T007 unit test.
- Commit after each task or logical group; stop at any checkpoint to validate.
