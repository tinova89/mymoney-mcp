# Implementation Plan: Primary currency

**Branch**: `005-primary-currency` | **Date**: 2026-10-20 | **Spec**: [spec.md](spec.md)

## Summary

Give every account group a primary currency from server configuration (`Currency:Default` plus a
`Currency:Groups` list), validated once at startup: an invalid default stops the host, an invalid
or duplicated group entry only marks that group misconfigured. Features read it through one
scoped service, `IPrimaryCurrency`, keyed by the current request's group. Currency codes are a
Domain value object over an embedded ISO 4217 list. No data change. Decisions: [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10 (unchanged)

**Primary Dependencies**: Microsoft.Extensions.Configuration binder (already in the host) — no new packages

**Storage**: none; no schema change, no migration

**Testing**: xUnit v3; unit tests in `Application.Tests`; real host (`WebApplicationFactory`) in `Api.Tests`

**Target Platform**: ASP.NET Core host (unchanged)

**Project Type**: web service (REST + MCP on one host)

**Performance Goals**: lookup is an in-memory dictionary read; no I/O per call

**Constraints**: configuration read once (restart to apply); one group's error never affects another

**Scale/Scope**: single user, a few groups, ~180 ISO codes

## Constitution Check

*Gate before Phase 0; re-checked after Phase 1.*

| Principle | How this plan complies | Status |
|---|---|---|
| I. Specs first, concise | Spec ≤ 150 lines; plan ≤ 200; closing doc tasks below | ✅ |
| II. Accounts are the root | Lookup keyed only by `IAccountGroupContext.Group`; isolation test (group `B` never gets `A`'s setting). No tables, so no group column decision | ✅ |
| III. Thin edges, CQRS | No tool or endpoint; the rule lives in the Application service `PrimaryCurrencyTable`; Api only binds configuration and fails startup (allowed, like `AuthStartupGuard`) | ✅ |
| IV. Test-backed | Every table rule, the value object and the service have unit tests; startup paths have host tests | ✅ |
| V. Real engine | No PostgreSQL behavior and no MCP tool; host tests go through the real `Program` | ✅ N/A |
| VI. Data integrity | No money amounts, dates, deletes or destructive operations; no stored data changes (FR-012) | ✅ N/A |
| VII. Contracts & security | Operator contract in [contracts/](contracts/currency-configuration.md); no secrets; `BRL` default is a business default, not a secret or URL | ✅ |
| VIII. Simplicity | No new project or package; logs carry only setting paths, group names and codes | ✅ |

**Post-design re-check (after Phase 1)**: unchanged. No public MCP/REST contract change.

## Project Structure

### Documentation (this feature)

```text
specs/005-primary-currency/
├── spec.md · plan.md · research.md · data-model.md · quickstart.md
├── contracts/currency-configuration.md
├── checklists/requirements.md
└── tasks.md    (/speckit-tasks)
```

### Source Code (changes only)

```text
src/
  MymoneyMcp.Domain/
    Currencies/CurrencyCode.cs              # new value object: TryParse/Parse (R2)
    Currencies/Iso4217.cs                   # new: active List One codes, amendment noted
  MymoneyMcp.Application/
    Currencies/CurrencySettings.cs          # new: Default + Groups (bound from "Currency")
    Currencies/PrimaryCurrencyTable.cs      # new: Build rules, problems, Lookup (R3)
    Currencies/IPrimaryCurrency.cs          # new: GetForCurrentGroup()
    Currencies/PrimaryCurrencyService.cs    # new, scoped (R4)
    Common/Exceptions/PrimaryCurrencyMisconfiguredException.cs # new
    DependencyInjection.cs                  # + IPrimaryCurrency (scoped)
  MymoneyMcp.Api/
    Common/PrimaryCurrencyStartup.cs        # new: build/throw before Build, log after (R3)
    Program.cs                              # wire startup + register table singleton
    appsettings.json                        # new: "Currency": { "Default": "BRL" }
tests/
  MymoneyMcp.Application.Tests/Currencies/
    CurrencyCodeTests.cs                    # new
    PrimaryCurrencyTableTests.cs            # new
    PrimaryCurrencyServiceTests.cs          # new
  MymoneyMcp.Api.Tests/Currencies/
    PrimaryCurrencyStartupTests.cs          # new, real host
```

**Structure Decision**: the `CLAUDE.md` layout. Value object in Domain (no packages), rule and
service in Application, configuration binding in Api. No infrastructure project changes.

## Key Technical Decisions

| Topic | Decision | Research |
|---|---|---|
| Configuration | `Currency:Default` + `Currency:Groups[]` `{Group, Code}`; list, not dictionary | R1 |
| Valid codes | Embedded ISO 4217 List One; trim + upper-case; withdrawn codes rejected | R2 |
| Startup | Pure `Build` → table + problems; fatal throws before host build; others logged after | R3 |
| Reload | Read once with `Get<CurrencySettings>()`; no options monitor | R3 |
| Service | Scoped `IPrimaryCurrency` over a singleton table; misconfigured → exception | R4 |
| Caller mapping | Deferred to 006 (first consumer) | R5 |

## Startup Order

`AuthStartupGuard.Validate` → `PrimaryCurrencyStartup.Build` (throws on invalid default; registers
the table) → services → `builder.Build()` → `AuthStartupGuard.WarnIfDisabled` →
`PrimaryCurrencyStartup.LogProblems` → migrations → pipeline (unchanged).

## Testing Map

| Spec item | Test (project) |
|---|---|
| US1 sc. 1, FR-003 | No `Default` → `BRL` (table unit) |
| US1 sc. 2–3, FR-005 | `USD`, `usd`, ` usd ` → `USD` (value object + table unit) |
| FR-006, Assumptions | `EUR`, `XAU` accepted; `XYZ`, `REAL`, `HRK`, `""` rejected (value object unit) |
| US2 sc. 1–2, FR-003 | `A`=`EUR` → `EUR`; `B` → default (service unit) |
| US2 sc. 3, FR-010, SC-002 | Changing `A` never changes `B`; lookup uses only the context group (service unit) |
| US2 sc. 4, FR-004 | `HOME` entry, `home` call → default; ` Silva ` entry matches `Silva` (table unit) |
| US3 sc. 1, FR-007 | `Default`=`XYZ` or blank → host fails, message names `Currency:Default` (Api host) |
| US3 sc. 2–4, FR-014/015 | `A`=`REAL` → host starts, error log names `Currency:Groups:0` and `A`; service throws for `A`, `B`/`C` resolve (table/service unit + Api host) |
| US3 sc. 5, FR-018 | Two `Silva` entries (equal or not) → misconfigured, error logged (table unit + Api host) |
| US3 sc. 6, FR-017 | Blank `Group` → warning, entry ignored (table unit + Api host) |
| FR-016 | `whogroupami` on `/mcp/A` with `A` misconfigured → works (Api host) |
| FR-008 | Table built once; changing configuration after start has no effect (Api host: same instance) |
| R4 | No group in context → `InvalidOperationException` (service unit) |

## Closing Doc Tasks (Constitution I)

`tasks.md` MUST end with tasks that update:
- `specs/domain/glossary.md` — Currency, Primary currency, Default currency, `Currency:*`.
- `specs/domain/business-rules.md` — new "Primary currency (since 005)" section (defaults, matching,
  invalid default vs group, duplicates, blank names, read once, no stored data change).
- `specs/domain/mcp-tools.md` — note: no tool or route reads or changes the primary currency (FR-011).
- `specs/architecture.md` — startup order, `Currency` configuration, Domain `Currencies`.
- `specs/backlog.md` and spec `Status:` — 005 `Implemented`.
