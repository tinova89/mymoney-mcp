# Glossary

Domain names are defined here once and used the same way everywhere (Constitution I).

| Term | Code name | Meaning |
|---|---|---|
| Account | `Account` | A financial account owned by one account group. The root of all financial data: transactions, categories and budgets (later features) exist only under accounts. Fields: `Id`, `DisplayName`, `Group`, `CreatedAt`. |
| Account group | `Account.Group`, header `X-Account-Group` | Free text (e.g. a family name) that groups accounts and defines what a call can see. Exact, case-sensitive. Comes only from the request header, never from the token or the body. |
| Client | `ClientCredential` (`Auth:Clients`) | A machine caller (e.g. Claude Code) identified by `ClientId` + `ClientSecret`, configured on the server. Becomes the token's `sub`. |
| Access token | JWT | Short-lived bearer token (`sub`, `iss`, `aud`, `iat`, `exp`). Issued by this system or a trusted external issuer. Carries no group. |
| Refresh token | `RefreshToken` | Opaque, single-use token exchanged for a new token pair. Stored only as a SHA-256 hash. Not account-owned. |
| External issuer | `Auth:External` | A trusted identity provider whose tokens are also accepted (OIDC discovery). |
| Token checking | `Auth:Enabled` | Whether a bearer token is required. Can be off only outside Production. |
