# Glossary

Domain names are defined here once and used the same way everywhere (Constitution I).

| Term | Code name | Meaning |
|---|---|---|
| Account | `Account` | A financial account owned by one account group. The root of all financial data: transactions, categories and budgets (later features) exist only under accounts. Fields: `Id`, `DisplayName`, `Group`, `CreatedAt`. |
| Account group | `Account.Group`, header `X-Account-Group`, URL `/mcp/<group>` | Free text (e.g. a family name) that groups accounts and defines what a call can see. Exact, case-sensitive. Comes from the request header or, for MCP, the URL group — never from the token or the body. |
| URL group | `/mcp/<group>` | The account group named in the MCP URL path, for clients that cannot send headers (claude.ai connectors). One percent-encoded segment. Never stored. |
| Group source | `AccountGroupSource` | Where the call's group came from: `url`, `header`, or `both` (same value in each). |
| Token issuer | `TokenIssuer` | Which issuer's token authenticated the call: `local` (this system), `external`, or `none` (token checking off). |
| Client | `ClientCredential` (`Auth:Clients`) | A machine caller (e.g. Claude Code) identified by `ClientId` + `ClientSecret`, configured on the server. Becomes the token's `sub`. |
| Access token | JWT | Short-lived bearer token (`sub`, `iss`, `aud`, `iat`, `exp`). Issued by this system or a trusted external issuer. Carries no group. |
| Refresh token | `RefreshToken` | Opaque, single-use token exchanged for a new token pair. Stored only as a SHA-256 hash. Not account-owned. |
| External issuer | `Auth:External` | A trusted identity provider whose tokens are also accepted (OIDC discovery). claude.ai signs in through it. |
| Authorization details | protected resource metadata (RFC 9728) | Public document at `/.well-known/oauth-protected-resource/<mcp path>` telling a client which issuer to sign in with, for which MCP URL. |
| Token checking | `Auth:Enabled` | Whether a bearer token is required. Can be off only outside Production. |
