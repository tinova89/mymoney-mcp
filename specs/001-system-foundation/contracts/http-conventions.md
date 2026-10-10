# Contract: HTTP Conventions (all routes)

## Headers

| Header | Required on | Notes |
|---|---|---|
| `Authorization: Bearer <jwt>` | everything except exempt routes | Not required when `Auth:Enabled=false` (non-production only). |
| `X-Account-Group: <group>` | everything except exempt routes | Trimmed; case-sensitive. |

Exempt routes: `/health`, `/alive`, `/openapi/*`, `/auth/token`, `/auth/refresh`.

## Check order and errors

| Order | Check | Failure |
|---|---|---|
| 1 | Token valid (when enabled) | `401` + `WWW-Authenticate: Bearer` |
| 2 | Group header present and not blank | `400` `application/problem+json`, `title: "Missing X-Account-Group header"` |
| 3 | Resource visible in the group | `404` (same body as a missing id) |

Errors use RFC 9457 problem details and never reveal other groups' data.
