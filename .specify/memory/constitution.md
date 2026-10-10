<!--
Sync Impact Report
- Version change: 2.1.0 → 2.2.0 (MINOR: materially expanded guidance)
- Modified sections:
  - Technology and Architecture Constraints — infrastructure is split into one
    `MymoneyMcp.Infrastructure.<Concern>` project per concern (e.g. `.Data`, `.Auth`, future
    `.Import`); infrastructure projects do not reference each other.
- Principles: unchanged. A new `Infrastructure.<Concern>` project follows the convention, so it
  does not need a Complexity Tracking entry (Principle VIII) unless it breaks the convention.
- Templates: not modified.
- Follow-up TODOs: none.
-->

# MymoneyMcp Constitution

## Core Principles

### I. Specs Are the Source of Truth

- Every feature MUST go through the SDD flow in order: `spec.md` → clarify → `plan.md` →
  `tasks.md` → implement. Code MUST NOT be written for behavior that no spec describes.
- Domain docs MUST be updated as the closing step of every implementation, in the same change
  as the code. Docs MUST never be older than the code:
  - `specs/domain/business-rules.md` — whenever the feature creates or changes a business rule.
  - `specs/domain/mcp-tools.md` — whenever the feature creates or changes an MCP tool (name,
    parameters, behavior, response, or the REST endpoint that mirrors it).
  - `specs/domain/glossary.md` and `specs/architecture.md` — whenever a domain term or an
    architecture decision changes.
- `tasks.md` MUST end with one task per doc that needs updating. If a doc does not exist yet,
  that task creates it.
- Domain names (entities, fields, enums) are defined once, in `specs/domain/glossary.md`, and
  used the same way everywhere else.
- Known deviations between spec and code (bugs or quirks deliberately left unfixed) MUST be
  written down in the spec or domain docs and pinned by a test (see Principle IV).
- Specs MUST be concise:
  - One idea per bullet; one sentence per requirement or acceptance criterion.
  - Prefer tables and lists over prose. No background essays, no history of the decision.
  - Link to domain docs and this constitution instead of repeating them.
  - `spec.md` says what and why only; how goes in `plan.md`; steps go in `tasks.md`.
    Nothing is written twice across the three files.
  - Remove template sections that do not apply instead of filling them with "N/A".
  - Target size: `spec.md` ≤ 150 lines, `plan.md` ≤ 200 lines. A larger file is a sign the
    feature MUST be split; if it cannot be split, say why at the top of the file.

Rationale: the specs are what Claude and humans read first; a stale or bloated spec produces
wrong code.

### II. Accounts Are the Root of All Data

- The account is the top of the data hierarchy. Transactions, categories, and budgets exist only
  under accounts, and a request can see them only through an account it is allowed to see.
- The set of visible accounts is defined by the request's `X-Account-Group` header: a request
  sees only the accounts of that group, and only the data below them:
  - transactions of those accounts;
  - categories used by those transactions;
  - budgets of that group.
- A budget belongs to a group and a category, not to a single account. Its spent amount adds up
  the category's transactions across **all** accounts of that group, and never includes
  transactions from another group.
- Isolation MUST be enforced in one central place in the persistence layer (e.g. an EF Core
  global query filter), and MUST NOT depend only on filters written in each handler.
- A write that references an account outside the current group MUST be rejected as not found,
  so the existence of other groups' data is never revealed.
- The scoping path MUST be fast: every table under the account MUST be filterable by group
  through an indexed column. Copying the group onto child tables is allowed and MUST be decided
  and justified in the feature's `plan.md`.
- Every query and command feature MUST include a test proving that data from another group is
  invisible (reads) or rejected (writes).

Rationale: one family must never see another family's money; scoping at the root keeps every
feature isolated without each one having to remember it.

### III. Thin Edges, CQRS Core

- MCP tools and REST endpoints MUST only build a request and send it through MediatR. No business
  rule may live in a tool, endpoint, or middleware (header and auth checks excepted).
- Reads are queries and writes are commands; each request is immutable and has exactly one
  handler. Domain entities MUST NOT be reused as requests.
- Each feature keeps its request, handler, validator, and response DTO together in one folder
  in `MymoneyMcp.Application`.
- Calculation rules MUST live in domain or application services injected into handlers, not
  written inline in handlers.
- Validation MUST run before the handler, as a MediatR pipeline step, not inside the handler.

Rationale: thin edges make every use case testable without a transport, and keep rules in one place.

### IV. Behavior Is Test-Backed (NON-NEGOTIABLE)

- Every business rule, validator rule, and handler path (success, not found, rejected) MUST have
  an automated test before the feature is marked `Implemented`.
- Every bug fix MUST add a regression test that fails without the fix.
- Calculation rules MUST be unit-tested in isolation from handlers and tools.
- Tests MUST assert on observable behavior (returned DTOs, saved data, error returned), not on
  private implementation details.
- A known, unfixed quirk MUST be locked in by a test that names it as such, so changing it is a
  visible decision rather than an accident.
- Money assertions MUST compare exact `decimal` values; tolerances and floating-point
  comparisons are forbidden.
- `dotnet test` MUST be green on `main` at all times; a skipped test MUST carry a reason and a
  linked backlog item.

Rationale: this system reports family money; an untested rule is an unverified number.

### V. Test Against the Real Engine

- A test double or in-memory database proves application logic only. Behavior that depends on
  PostgreSQL — query filters, transactions, SQL aggregation, concurrency — MUST be covered by an
  integration test against a real PostgreSQL instance.
- Money totals SHOULD be summed in C# `decimal`; when a total is computed in SQL, it MUST have
  a real-PostgreSQL test.
- Each MCP tool MUST have an integration test that goes through the real host: authentication,
  the `X-Account-Group` header, and request/response serialization.

Rationale: each test engine proves only what it actually runs; money and isolation rules must be
proven on the database that runs in production.

### VI. Financial Data Integrity

- Money MUST be `decimal` in C# and `numeric` in PostgreSQL. `double`/`float` are forbidden for
  money.
- Dates MUST be stored as date/time types, never as text.
- Deletes MUST be soft and hidden by default from every query; reading deleted data is allowed
  only in explicit administrative code.
- Destructive operations follow a two-step `needsConfirm` flow:
  - The tool, not the caller, decides whether an operation is destructive.
  - A call without explicit confirmation MUST NOT change any data. It returns
    `needsConfirm: true` with a short summary of what would happen.
  - Only a second call carrying the explicit confirmation executes the operation.
  - The caller (e.g. Claude) MUST get the user's approval between the two calls.
- Budget and balance calculations MUST follow `specs/domain/business-rules.md` exactly; changing
  a rule means changing that doc first.

Rationale: these are the invariants that keep reported balances and budgets correct.

### VII. Explicit Contracts and Security Boundaries

- Domain entities MUST NOT cross the network; every MCP and REST response is a DTO.
- Every write MUST be validated before anything is saved, including CSV imports.
- MCP tool contracts (parameter names, value types, response shapes) are public API: a breaking
  change MUST be documented in `mcp-tools.md` and called out in the feature spec.
- Every MCP and REST call MUST carry `X-Account-Group`. It defines which accounts — and therefore
  which data — the call can see (Principle II). A missing or blank header MUST be rejected with
  `400` before any handler runs. Exempt routes: infrastructure (health checks, API docs) and
  authentication (`/auth/token`, `/auth/refresh`).
- Authentication uses the custom JWT provider. Tokens MAY be issued by another system that knows
  nothing about groups, so the JWT MUST NOT carry or depend on a group.
- Secrets (such as the JWT signing key), connection strings, ports, and URLs MUST NOT be
  hardcoded or committed; use user-secrets locally, environment configuration in production, and
  Aspire service discovery between services.

Rationale: clear contracts let Claude call the tools safely; clear boundaries keep data isolated.

### VIII. Simplicity and Maintainability

- New code MUST NOT introduce compiler warnings.
- Build only what a spec asks for (YAGNI). Any added project, abstraction, pattern, or dependency
  beyond the solution layout in `CLAUDE.md` MUST be justified in the plan's Complexity Tracking
  table.
- Each change SHOULD be small and focused on one feature or task; unrelated refactors go in
  their own change.
- Logs MUST NOT include tokens, credentials, or full statement contents.
- EF Core migrations already applied to a shared database MUST NOT be edited; create a new
  migration instead.

Rationale: a small, consistent codebase stays cheap to change and easy for Claude to read.

## Technology and Architecture Constraints

- Stack: .NET 10, .NET Aspire, PostgreSQL via EF Core, MediatR, custom JWT, MCP and REST served
  by the same API host.
- Layering follows the solution layout in `CLAUDE.md`: `Api` → `Application` → `Domain`;
  infrastructure projects implement what `Application` needs. `Domain` MUST NOT reference EF Core,
  ASP.NET Core, or any infrastructure package.
- Infrastructure is split by concern: one `MymoneyMcp.Infrastructure.<Concern>` project each
  (e.g. `.Data` for persistence, `.Auth` for token issuance, `.Import` for CSV). Infrastructure
  projects MUST NOT reference each other; they share data only through `Application` interfaces.
  A new project that follows this convention needs no Complexity Tracking entry.
- Health checks, telemetry, and resilience come from `MymoneyMcp.ServiceDefaults`, referenced by
  every service project.

## Development Workflow and Quality Gates

A feature MAY be set to `Status: Implemented` only when all gates pass:

1. Every acceptance criterion in `spec.md` is checked and every task in `tasks.md` is done.
2. `dotnet build` succeeds with no new warnings, and `dotnet test` is fully green.
3. Each new rule, validator, and handler path has tests (Principle IV), with real-PostgreSQL
   tests where Principle V requires them.
4. Each new query or command has a cross-group isolation test (Principle II).
5. The plan's Constitution Check passes, or each violation is justified in Complexity Tracking.
6. `business-rules.md` and `mcp-tools.md` (and any other affected domain doc) reflect the
   feature (Principle I), and `specs/backlog.md` reflects the new state.
7. Review (human or `/code-review`) confirms thin edges, DTO-only responses, and no secrets.

## Governance

- This constitution overrides any other practice or guide in the repo. `CLAUDE.md` and the specs
  provide runtime guidance and MUST NOT contradict it.
- Amendments MAY be made by editing this file directly or through `/speckit-constitution`.
  Either way, each amendment MUST bump the version, set **Last Amended**, add a Sync Impact
  Report, and be committed as a separate `docs:` change.
- Versioning follows semantic versioning: MAJOR for removing or redefining a principle, MINOR for
  a new principle/section or materially expanded guidance, PATCH for wording and clarifications.
- Every `plan.md` MUST include a Constitution Check; `/speckit-analyze` and reviews MUST flag
  violations. An unjustified violation blocks the feature.
- Compliance is reviewed whenever a feature is closed and at least on every MINOR/MAJOR amendment.

**Version**: 2.2.0 | **Ratified**: 2026-10-10 | **Last Amended**: 2026-10-10
