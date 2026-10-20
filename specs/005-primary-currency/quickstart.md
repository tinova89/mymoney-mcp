# Quickstart: Primary currency

Validates the feature. Configuration contract: [contracts/currency-configuration.md](contracts/currency-configuration.md).
Base setup (secrets, AppHost): [001 quickstart](../001-system-foundation/quickstart.md).

## Automated

```bash
dotnet test
```

Covers every scenario below (plan Testing Map). Host tests need Docker (Testcontainers).

## Manual (startup behavior)

Set each value with `dotnet user-secrets set "<key>" "<value>"` in `src/MymoneyMcp.Api`, then run
`dotnet run --project src/MymoneyMcp.AppHost` from the root. Remove it afterwards
(`dotnet user-secrets remove "<key>"`).

| # | Set | Expect | Spec |
|---|---|---|---|
| 1 | nothing | API starts | US1 sc. 1 |
| 2 | `Currency:Default` = `XYZ` | API does not start; error names `Currency:Default` | US3 sc. 1 |
| 3 | `Currency:Default` = `usd` | API starts | US1 sc. 3 |
| 4 | `Currency:Groups:0:Group` = `Silva`, `...:Code` = `REAL` | API starts; error log names `Currency:Groups:0` and `Silva` | US3 sc. 2 |
| 5 | as 4, then `whogroupami` on `/mcp/Silva` | still works | FR-016 |
| 6 | two entries for `Silva` (`EUR`, `USD`) | API starts; error log names `Silva` | US3 sc. 5 |
| 7 | `Currency:Groups:0:Group` = `"  "` | API starts; warning names `Currency:Groups:0` | US3 sc. 6 |

The primary-currency value itself is not visible to callers until 006 (Spec Assumptions); its
resolution per group is proven by the automated tests.
