# Backlog

| # | Feature | Status | Notes |
|---|---|---|---|
| 001 | [System foundation](001-system-foundation/spec.md) | Draft | Solution, database, account-group isolation, auth, empty MCP host |
| — | Restrict account groups per caller | Planned | Next after 001. Today any authenticated caller may use any group (001 FR-009): a leaked token exposes every family's data. Store which groups each caller may use and reject the rest. |
