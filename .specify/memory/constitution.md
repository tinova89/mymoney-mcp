<!--
Sync Impact Report
- Version change: (unversioned template) → 1.0.0
- Modified principles: all template placeholders replaced (initial ratification)
  - [PRINCIPLE_1_NAME] → I. Specs Are the Source of Truth
  - [PRINCIPLE_2_NAME] → II. Thin Edges, CQRS Core
  - [PRINCIPLE_3_NAME] → III. Business Rules Are Test-Backed (NON-NEGOTIABLE)
  - [PRINCIPLE_4_NAME] → IV. The Right Test for the Right Engine
  - [PRINCIPLE_5_NAME] → V. Financial Data Integrity
  - Added: VI. Explicit Contracts and Security Boundaries
  - Added: VII. Simplicity and Maintainability
- Added sections: Technology and Architecture Constraints; Development Workflow and Quality Gates
- Removed sections: none
- Templates reviewed (not modified by this command; they read the constitution at runtime):
  - .specify/templates/plan-template.md (Constitution Check gate) — compatible
  - .specify/templates/spec-template.md — compatible
  - .specify/templates/tasks-template.md — compatible (Principle III requires test tasks)
- Follow-up TODOs:
  - specs/ (business-rules.md, glossary.md, mcp-tools.md, architecture.md, backlog.md) is referenced
    by CLAUDE.md but does not exist yet; Principle I depends on it.
  - README.md still describes Portuguese entity names and an older tool list; reconcile with specs.
-->

# MymoneyMcp Constitution

## Core Principles

### I. Specs Are the Source of Truth

- Every feature MUST go through the SDD flow in order: `spec.md` → clarify → `plan.md` →
  `tasks.md` → implement. Code MUST NOT be written for behavior that no spec describes.
- When behavior changes, the domain docs (`specs/domain/business-rules.md`, `glossary.md`,
  `mcp-tools.md`, `specs/architecture.md`) MUST be updated **before or in the same change** as
  the code. Docs MUST never be older than the code.
- Known deviations between spec and code (bugs, quirks deliberately left unfixed) MUST be written
  down in the feature's spec or the domain docs and pinned by a test (see Principle III).

Rationale: the specs are what Claude and humans read first; a stale spec produces wrong code.

### II. Thin Edges, CQRS Core

- MCP tools and REST endpoints MUST only build a request and call `IMediator.Send`/`Publish`.
  No business rule may live in a tool, endpoint, or middleware (scope/header checks excepted).
- Reads are Queries and writes are Commands; each is an immutable `record`, with exactly one
  handler. Domain entities MUST NOT be reused as requests.
- Request, handler, validator, and response DTO MUST live together in one feature folder
  (`<Area>/<Action><Entity>/`), never in generic `Commands/`/`Handlers/` folders.
- Pure calculation rules (budget aggregation, billing cycle, next business day, installments,
  category parsing) MUST live in injectable domain/application services, not inline in handlers.
- The pipeline order is fixed: `LoggingBehavior` → `ValidationBehavior` → `TransactionBehavior`
  (writes only). Notifications MUST NOT carry logic the main flow depends on.

Rationale: thin edges make every use case testable without a transport, and keep rules in one place.

### III. Business Rules Are Test-Backed (NON-NEGOTIABLE)

- Every business rule, validator rule, and handler path (success, not-found, rejected) MUST have
  an automated test before the feature is marked `Implemented`.
- Every bug fix MUST add a regression test that fails without the fix.
- Pure rules MUST be unit-tested in isolation from handlers and tools.
- Tests MUST assert on observable behavior (DTO fields, persisted state, thrown exception type),
  not on private implementation details.
- A known, unfixed quirk MUST be locked in by a test that names it as such, so changing it is
  a visible decision rather than an accident.
- Money assertions MUST use exact `decimal` values; tolerances and `double` comparisons are
  forbidden.
- `dotnet test` MUST be green on `main` at all times; a skipped or disabled test MUST carry a
  reason and a linked backlog item.

Rationale: this system moves and reports family money; an untested rule is an unverified number.

### IV. The Right Test for the Right Engine

- Tests that depend on real transactions or EF Core global query filters MUST use SQLite
  `:memory:` (or real PostgreSQL). The EF Core `InMemory` provider MUST NOT be used for them.
- A green SQLite test is NOT sufficient evidence for:
  1. aggregations translated to SQL (`SumAsync`, `GroupBy` → `SUM(...)`), or
  2. behavior under concurrent writes, transaction isolation, or Npgsql retry.
  Either case MUST have a real-PostgreSQL integration test (`Testcontainers.PostgreSql`) before
  it ships.
- Money calculations SHOULD materialize rows and sum in C# `decimal`; pushing a money aggregation
  into SQL triggers rule 2 above.
- Exposed MCP tools MUST have integration coverage through `TestServer` plus a real MCP client
  (or equivalent) for the auth, header, and serialization path.
- Scope checks (e.g. `approval`) MUST be tested at the tool level, proving MediatR is never called
  when the scope is missing.

Rationale: each test engine proves only what it can actually run; we choose the engine on purpose.

### V. Financial Data Integrity

- Money (`Amount`, `GoalAmount`, `ActualSpend`, `RemainingBudget`) MUST be `decimal` in C# and
  `numeric` in PostgreSQL. `double`/`float` are forbidden for money.
- Transaction dates MUST be explicit date types (`date`/`timestamptz`), never `text`.
- "Next business day" and `ParentCategory/Subcategory` parsing MUST each have exactly one
  implementation, reused everywhere.
- Deletes MUST be soft (`IsDeleted`/`DeletedAt`) and hidden by a global query filter; bypassing
  it (`IgnoreQueryFilters`) is allowed only in explicit administrative code.
- Destructive operations MUST require an explicit `confirm = true`, rejected by validation
  otherwise.
- Per-status timestamps MUST be stamped once on entering the status and never overwritten.
- Budget calculations MUST follow `specs/domain/business-rules.md` exactly; changing a rule means
  changing that doc first.

Rationale: these are the invariants that keep reported balances and budgets correct.

### VI. Explicit Contracts and Security Boundaries

- Domain entities MUST NOT cross the network; every MCP/REST/SignalR response is a DTO/record.
- Every write MUST be validated by FluentValidation (via `ValidationBehavior`) before persisting,
  including source-format checks on import (`;` separator, `dd/mm/yyyy`, dot-decimal).
- MCP tool contracts (parameter names, enum integer mappings, response shapes) are public API:
  a breaking change MUST be documented in `mcp-tools.md` and called out in the feature spec.
- Every request (REST and MCP) MUST carry `X-Account-Group`, except infra/docs routes.
  `Group` and `Kind` MUST be stamped server-side, never accepted from the caller.
- Auth is the custom JWT provider only. Write and approval operations MUST be gated by scope.
- Secrets, connection strings, ports, and URLs MUST NOT be hardcoded or committed; use
  user-secrets/environment configuration and Aspire service discovery.

Rationale: clear contracts let Claude call the tools safely; clear boundaries keep data isolated.

### VII. Simplicity and Maintainability

- Nullable reference types MUST be enabled; new code MUST NOT introduce compiler warnings.
- Code MUST follow the house style: file-scoped namespaces, `async`/`await` with `Async` suffix for
  I/O, PascalCase in C#, camelCase on the wire, English domain names.
- Build only what a spec asks for (YAGNI). Any added project, abstraction, pattern, or dependency
  beyond the standard layout MUST be justified in the plan's Complexity Tracking table.
- Each change SHOULD be small and focused on one feature/task; unrelated refactors go in their
  own change.
- Logging goes through `LoggingBehavior`/OpenTelemetry and MUST NOT include tokens, credentials,
  or full statement contents.
- Applied EF Core migrations MUST NOT be edited; create a new migration instead. Data remaps
  carry their own SQL steps.

Rationale: a small, consistent codebase stays cheap to change and easy for Claude to read.

## Technology and Architecture Constraints

- Stack: .NET 10, .NET Aspire (AppHost + ServiceDefaults), ASP.NET Core, MCP over the API host,
  PostgreSQL via EF Core + Npgsql, MediatR, FluentValidation, xUnit-style tests.
- Layering: `Api` → `Application` → `Domain`; `Infrastructure` implements `Application`
  interfaces. `Domain` MUST NOT reference EF Core, ASP.NET Core, or any infrastructure package.
- MediatR registration lives in `Application`, never in `Api`.
- Package versions are centralized in `Directory.Packages.props`; project files MUST NOT pin
  their own versions.
- Health (`/health`, `/alive`) and telemetry come from `ServiceDefaults`, referenced by every
  service project.

## Development Workflow and Quality Gates

A feature MAY be set to `Status: Implemented` only when all gates pass:

1. Every acceptance criterion in `spec.md` is checked and every task in `tasks.md` is done.
2. `dotnet build` succeeds with no new warnings, and `dotnet test` is fully green.
3. Each new rule, validator, and handler path has tests (Principle III), and Principle IV's
   real-PostgreSQL rule is satisfied where it applies.
4. The plan's Constitution Check passes, or each violation is justified in Complexity Tracking.
5. Domain docs are updated (Principle I) and `specs/backlog.md` reflects the new state.
6. Review (human or `/code-review`) confirms thin edges, DTO-only responses, and no secrets.

## Governance

- This constitution overrides any other practice or guide in the repo. `CLAUDE.md` and the specs
  provide runtime guidance and MUST NOT contradict it.
- Amendments are made through `/speckit-constitution`, with a Sync Impact Report, and are committed
  as a separate `docs:` change.
- Versioning follows semantic versioning: MAJOR for removing or redefining a principle, MINOR for
  a new principle/section or materially expanded guidance, PATCH for wording and clarifications.
- Every `plan.md` MUST include a Constitution Check; `/speckit-analyze` and reviews MUST flag
  violations. An unjustified violation blocks the feature.
- Compliance is reviewed whenever a feature is closed and at least on every MINOR/MAJOR amendment.

**Version**: 1.0.0 | **Ratified**: 2026-10-10 | **Last Amended**: 2026-10-10
