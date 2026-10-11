# Business Rules

Current behavior of the system. Updated at the end of every feature (Constitution I).
Terms: [glossary.md](glossary.md).

## Account-group isolation (since 001)

| # | Rule |
|---|---|
| BR-01 | Every account belongs to exactly one account group. |
| BR-02 | A call sees only the accounts of the group named in its `X-Account-Group` header, and only data under those accounts. |
| BR-03 | Isolation is enforced once, centrally (EF Core named query filter `AccountGroup`); features never filter by group themselves. |
| BR-04 | An account of another group behaves exactly like an id that does not exist ("not found"); its existence is never revealed. |
| BR-05 | Group matching is exact and case-sensitive after trimming surrounding spaces (`HOME` ≠ `home`). |
| BR-06 | A group with no accounts reads as empty, not as an error. |
| BR-07 | Any authenticated caller may use any group. Restricting groups per caller is a backlog item. |

## Group header (since 001)

| # | Rule |
|---|---|
| BR-10 | Every MCP and REST call must carry a non-blank `X-Account-Group` header, or it is rejected with `400` before any data is read. |
| BR-11 | Exempt: `/health`, `/alive`, `/openapi/*`, `/auth/token`, `/auth/refresh`. Any other `/auth/*` route needs the header. |
| BR-12 | The token is checked before the header: an anonymous call without the header gets `401`, not `400`. |

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

## Availability (since 001)

| # | Rule |
|---|---|
| BR-30 | Pending database migrations are applied at startup. If the database is unreachable or a migration fails, the API stops (fail fast); PostgreSQL rolls the failed migration back. |
| BR-31 | If the database is lost after startup, `/health` reports unhealthy (traffic should stop), `/alive` stays OK, and the API keeps running. |
