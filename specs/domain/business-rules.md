# Business Rules

Current behavior of the system. Updated at the end of every feature (Constitution I).
Terms: [glossary.md](glossary.md).

## Account-group isolation (since 001)

| # | Rule |
|---|---|
| BR-01 | Every account belongs to exactly one account group. |
| BR-02 | A call sees only the accounts of its group — from the `X-Account-Group` header or, for MCP, the MCP URL (since 002) — and only data under those accounts. |
| BR-03 | Isolation is enforced once, centrally (EF Core named query filter `AccountGroup`); features never filter by group themselves. |
| BR-04 | An account of another group behaves exactly like an id that does not exist ("not found"); its existence is never revealed. |
| BR-05 | Group matching is exact and case-sensitive after trimming surrounding spaces (`HOME` ≠ `home`). |
| BR-06 | A group with no accounts reads as empty, not as an error. |
| BR-07 | Any authenticated caller may use any group. Restricting groups per caller is a backlog item. |

## Group header and MCP URL (since 001, URL since 002)

| # | Rule |
|---|---|
| BR-10 | Every call must name a non-blank group, or it is rejected with `400` before any data is read: REST in the `X-Account-Group` header; MCP in the header, the URL `/mcp/<group>`, or both (since 002). |
| BR-11 | Exempt: `/health`, `/alive`, `/openapi/*`, `/auth/token`, `/auth/refresh`, `/.well-known/*` (since 002). Any other `/auth/*` route needs the header. |
| BR-12 | The token is checked before the group: an anonymous call with a missing, blank or mismatched group gets `401`, not `400`. |
| BR-13 | The URL group is one path segment, percent-decoded once (`%2F` → `/`), then trimmed and matched like the header (BR-05). `/mcp/` has no URL group; `/mcp/A/B` is not an MCP route. (since 002) |
| BR-14 | A URL group that is blank after decoding and trimming is rejected with `400` (`Blank account group in MCP URL`). (since 002) |
| BR-15 | A URL group and a header that differ after trimming are rejected with `400` (`Account group mismatch`). A blank header counts as absent. (since 002) |
| BR-16 | The group is never remembered between calls: the MCP transport is stateless and each call uses only its own URL or header. (since 002) |
| BR-17 | `400` problem details never echo the header or URL value. (since 002) |

## Authentication (since 001)

| # | Rule |
|---|---|
| BR-20 | Every MCP and REST call except the exempt routes needs a valid, unexpired bearer token. |
| BR-21 | Tokens are accepted from this system and from one trusted external issuer configured on the server. |
| BR-22 | Tokens never carry or depend on an account group. |
| BR-23 | A refresh token works once: using it revokes it and returns a new pair. Concurrent uses of the same token succeed at most once. |
| BR-24 | Token checking is on by default. It may be turned off only by server configuration, never by a caller, and never in Production (the API refuses to start). While off, a startup warning is logged and the group header is still required. |
| BR-25 | With token checking on, the API refuses to start without a signing key of at least 32 bytes. |
| BR-26 | Secrets (client secret, refresh token, signing key) never appear in logs or responses. |
| BR-27 | A token's audience is the whole server: one valid token works on every MCP URL, whatever the group (since 002). |
| BR-28 | With the external issuer configured, the server publishes RFC 9728 metadata at `/.well-known/oauth-protected-resource/<mcp path>` naming it as the authorization server, and MCP `401`s carry `resource_metadata` pointing there. REST `401`s are unchanged. Without it, nothing is published. (since 002) |

## Availability (since 001)

| # | Rule |
|---|---|
| BR-30 | Pending database migrations are applied at startup. If the database is unreachable or a migration fails, the API stops (fail fast); PostgreSQL rolls the failed migration back. |
| BR-31 | If the database is lost after startup, `/health` reports unhealthy (traffic should stop), `/alive` stays OK, and the API keeps running. |
