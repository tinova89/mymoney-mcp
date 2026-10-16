# Quickstart: Connect from claude.ai

Validates the feature end to end. Contracts: [contracts/](contracts/). Data: [data-model.md](data-model.md).
Base setup (secrets, AppHost): [001 quickstart](../001-system-foundation/quickstart.md).

## Prerequisites

- 001 prerequisites, and two accounts in `HOME`, one in `WORK` (test data).
- For scenarios 7–9: an external issuer (IdP still to choose, spec Assumptions) that:
  - claude.ai can sign in with (supports its OAuth client registration);
  - issues tokens whose `aud` is `Auth:External:Audience` for **every** `/mcp/<group>` URL;
- and the API reachable from claude.ai over HTTPS (e.g. a tunnel).

```bash
cd src/MymoneyMcp.Api
dotnet user-secrets set "Auth:External:Authority" "<issuer URL>"
dotnet user-secrets set "Auth:External:Audience" "<audience>"
```

## Scenarios

| # | Do | Expect | Spec |
|---|---|---|---|
| 1 | `whogroupami` on `/mcp/HOME`, local token, no header | `HOME`, `url`, `local`, `2` | US1, US3 |
| 2 | `whogroupami` on `/mcp/WORK`, same client | `WORK`, `url`, `1` | US1 sc. 2 |
| 3 | `whogroupami` on `/mcp` with header `HOME` | `header` | US2 sc. 3, US3 |
| 4 | `/mcp/HOME` with header ` HOME ` | `both` | US2 sc. 2 |
| 5 | `/mcp/HOME` with header `WORK`; `/mcp/%20` | `400` mismatch; `400` blank | US2, FR-004/005 |
| 6 | `POST /mcp/HOME` without a token | `401` + `resource_metadata` | US4, FR-012 |
| 7 | `GET /.well-known/oauth-protected-resource/mcp/HOME` | `resource` = that URL, issuer listed | FR-011 |
| 8 | claude.ai: add connector `https://<host>/mcp/HOME`, sign in, ask Claude to run `whogroupami` | `HOME`, `url`, `external`, under 5 min | SC-001 |
| 9 | Same token on `/mcp/WORK` | accepted | FR-016 |
| 10 | `dotnet test` | all green | SC-002–004 |

Scenarios 1–7, 9 and 10 are automated; scenario 8 is manual and recorded below when run.
