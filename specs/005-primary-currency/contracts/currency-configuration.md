# Contract: Currency configuration

Operator-facing interface of 005 (no MCP tool, no REST route; Spec FR-011). Read once at startup.

## Shape

```json
"Currency": {
  "Default": "BRL",
  "Groups": [
    { "Group": "Silva", "Code": "EUR" },
    { "Group": "HOME",  "Code": "usd" }
  ]
}
```

| Key | Required | Value |
|---|---|---|
| `Currency:Default` | no | ISO 4217 code, any case; absent → `BRL` |
| `Currency:Groups:<i>:Group` | per entry | Account group name; trimmed, exact and case-sensitive (BR-05) |
| `Currency:Groups:<i>:Code` | per entry | ISO 4217 code, any case |

## Where to set it

| Environment | Source | Example |
|---|---|---|
| Base | `src/MymoneyMcp.Api/appsettings.json` | `"Currency": { "Default": "BRL" }` |
| Local | user-secrets or `appsettings.Development.json` | `dotnet user-secrets set "Currency:Groups:0:Group" "Silva"` |
| Production | environment variables | `Currency__Default=BRL`, `Currency__Groups__0__Group=Silva`, `Currency__Groups__0__Code=EUR` |

## Outcomes at startup

| Configuration | Startup | Log | Primary currency |
|---|---|---|---|
| No `Default` | starts | — | `BRL` for groups without an entry |
| `Default` blank or invalid | **fails** | exception names `Currency:Default` | — |
| Entry with a valid code | starts | — | that code for that group |
| Entry with a blank or invalid code | starts | error: `Currency:Groups:<i>`, group | group misconfigured |
| Two or more entries for one group | starts | error: each path, group | group misconfigured |
| Entry with a blank `Group` | starts | warning: `Currency:Groups:<i>` | entry ignored |

A misconfigured group gets an error whenever a feature asks for its primary currency (never the
default); its other calls work. Fix the configuration and restart.
