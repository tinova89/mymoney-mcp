# MCP Tools and REST Endpoints

Current public contract. Updated whenever a feature creates or changes an MCP tool or REST route
(Constitution I). Headers and error rules for every call: [http-conventions](../001-system-foundation/contracts/http-conventions.md).

## MCP endpoint

| Item | Value |
|---|---|
| URL | `/mcp` (group from the header) or `/mcp/<group>` (group from the URL, since 002); Streamable HTTP, stateless |
| Server name | `mymoney-mcp` |
| Headers | `Authorization: Bearer <jwt>`; `X-Account-Group: <group>` unless the URL names the group |
| Errors | `400` missing group, blank URL group, or URL/header mismatch; `401` with `resource_metadata` when the external issuer is configured |
| Contracts | [001 mcp-endpoint.md](../001-system-foundation/contracts/mcp-endpoint.md), [002 mcp-endpoint.md](../002-claude-ai-connector/contracts/mcp-endpoint.md), [002 oauth-metadata.md](../002-claude-ai-connector/contracts/oauth-metadata.md) |

## Tools

| Tool | Since | Parameters | Behavior |
|---|---|---|---|
| `whogroupami` | 002 | none | Read-only diagnostic. Returns `{ accountGroup, groupSource (url/header/both), issuer (local/external/none), visibleAccounts }` for the call's group. Never returns the token or another group's data. |

## REST endpoints

| Method | Route | Since | Auth | Group header | Purpose |
|---|---|---|---|---|---|
| `POST` | `/auth/token` | 001 | anonymous | not required | Client credentials → access + refresh token |
| `POST` | `/auth/refresh` | 001 | anonymous | not required | Refresh token → new pair (old one revoked) |
| `GET` | `/health` | 001 | anonymous | not required | Readiness, includes the database |
| `GET` | `/alive` | 001 | anonymous | not required | Liveness only |
| `GET` | `/openapi/v1.json` | 001 | anonymous | not required | API description (Development only) |
| `GET` | `/.well-known/oauth-protected-resource/<mcp path>` | 002 | anonymous | not required | RFC 9728 metadata for that MCP URL; only when `Auth:External` is configured |

Request/response details: [auth-api.md](../001-system-foundation/contracts/auth-api.md).
