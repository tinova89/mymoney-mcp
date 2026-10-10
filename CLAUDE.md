# CLAUDE.md

A family financial management **MCP Server** (.NET 10 + Aspire, PostgreSQL/EF Core, custom JWT).

This project uses **Spec-Driven Development (SDD)**: the specs in [`/specs`](specs/) are the source of truth. We write the code from them.

## Always load

@.specify/memory/constitution.md

## Where to find things

| What you need | Read |
|---|---|
| Rules you must always follow (money, dates, soft delete, DTOs, CQRS, `X-Account-Group`) | [`.specify/memory/constitution.md`](.specify/memory/constitution.md) |
| Business rules | [`specs/domain/business-rules.md`](specs/domain/business-rules.md) |
| Domain terms and enum values | [`specs/domain/glossary.md`](specs/domain/glossary.md) |
| MCP tools (parameters, behavior, scopes) and REST endpoints | [`specs/domain/mcp-tools.md`](specs/domain/mcp-tools.md) |
| Aspire, persistence, auth, account group | [`specs/architecture.md`](specs/architecture.md) |
| A feature (what / how / steps) | `specs/<NNN-FT-name>/spec.md` · `plan.md` · `tasks.md` |
| What comes next | [`specs/backlog.md`](specs/backlog.md) |

## Commands

```bash
dotnet run --project src/MymoneyMcp.AppHost   # backend + Aspire dashboard
dotnet build
dotnet test
dotnet tool restore                              # pins dotnet-ef to EF Core 10
dotnet ef migrations add <Name> --project src/MymoneyMcp.Infrastructure.Data --startup-project src/MymoneyMcp.Api
```

## Solution layout

```
src/
  MymoneyMcp.AppHost          Aspire orchestration (entry point)
  MymoneyMcp.ServiceDefaults  health checks, telemetry, resilience
  MymoneyMcp.Api              REST + MCP host (tools in Mcp/Tools)
  MymoneyMcp.Application      MediatR handlers, validators, services (one folder for each feature)
  MymoneyMcp.Domain           entities, value objects, enums
  MymoneyMcp.Infrastructure.*  one project per infrastructure concern:
    .Data                     EF Core, DbContext, migrations
    .Auth                     JWT issuance, client credentials
    (future, e.g. .Import)    CSV import, external integrations
tests/
  MymoneyMcp.Api.Tests
  MymoneyMcp.Application.Tests
```

## SDD workflow (for each feature)

1. **Specify** → `spec.md` (what and why, user stories, acceptance criteria, no implementation details).
2. **Clarify** → answer the "Open questions" in `spec.md` before you plan.
3. **Plan** → `plan.md` (migrations, handlers, validators, contracts, technical approach, data changes, components).
4. **Tasks** → `tasks.md` (small steps, in order, that you can check).
5. **Implement** → read `spec.md` + `plan.md` + the constitution, then do the steps in `tasks.md`.

Templates: [`.specify/templates/`](.specify/templates/).

## Before you finish a feature

- Every acceptance criterion in `spec.md` is checked, and every task in `tasks.md` is done.
- `dotnet build` and `dotnet test` pass.
- Last step of the implementation: update `specs/domain/business-rules.md` if a rule was created or changed, and `specs/domain/mcp-tools.md` if an MCP tool was created or changed (create the file if it does not exist). The docs must never be older than the code.
- Set the feature `Status:` to `Implemented` and update `specs/backlog.md`.
- Do not add details about one feature here. They go in the feature folder.
