# MCP Tools and REST Endpoints

Current public contract. Updated whenever a feature creates or changes an MCP tool or REST route
(Constitution I). Headers and error rules for every call: [http-conventions](../001-system-foundation/contracts/http-conventions.md).

## MCP endpoint

| Item | Value |
|---|---|
| URL | `/mcp` (Streamable HTTP) |
| Server name | `mymoney-mcp` |
| Headers | `Authorization: Bearer <jwt>`, `X-Account-Group: <group>` |
| Contract | [mcp-endpoint.md](../001-system-foundation/contracts/mcp-endpoint.md) |

## Tools

| Tool | Since | Parameters | Behavior |
|---|---|---|---|
| — | — | — | No tools yet. `tools/list` returns `[]`. |

## REST endpoints

| Method | Route | Since | Auth | Group header | Purpose |
|---|---|---|---|---|---|
| `POST` | `/auth/token` | 001 | anonymous | not required | Client credentials → access + refresh token |
| `POST` | `/auth/refresh` | 001 | anonymous | not required | Refresh token → new pair (old one revoked) |
| `GET` | `/health` | 001 | anonymous | not required | Readiness, includes the database |
| `GET` | `/alive` | 001 | anonymous | not required | Liveness only |
| `GET` | `/openapi/v1.json` | 001 | anonymous | not required | API description (Development only) |

Request/response details: [auth-api.md](../001-system-foundation/contracts/auth-api.md).
