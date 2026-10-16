# Data Model: Connect from claude.ai

No tables, columns, or migrations: every value below lives only for one request (spec FR-007).
Persistent model: [001 data-model](../001-system-foundation/data-model.md).

## Account group resolution (per request)

| Field | Type | Rule |
|---|---|---|
| `Group` | `string` | Trimmed; empty when an error is set. Feeds the `AccountGroup` query filter (BR-03). |
| `Source` | `AccountGroupSource` | `None`, `Url`, `Header`, `Both`. |
| `Error` | enum | `None`, `Missing`, `BlankUrlGroup`, `Mismatch` → `400` ([contract](contracts/mcp-endpoint.md)). |

| URL group | Header | Result |
|---|---|---|
| none (`/mcp`, `/mcp/`, REST) | absent or blank | `Missing` |
| none | `A` | `A`, `Header` |
| blank after decode + trim | any | `BlankUrlGroup` |
| `A` | absent or blank | `A`, `Url` |
| `A` | `A` (after trim) | `A`, `Both` |
| `A` | `B` | `Mismatch` |

## Enums (Application)

| Enum | Values | Contract value |
|---|---|---|
| `AccountGroupSource` | `None`, `Url`, `Header`, `Both` | `url`, `header`, `both` (`None` never reaches a handler) |
| `TokenIssuer` | `None`, `Local`, `External` | `none`, `local`, `external` |

## `WhoGroupAmIResponse` (DTO)

| Field | Type | Source |
|---|---|---|
| `accountGroup` | string | `IAccountGroupContext.Group` |
| `groupSource` | string | `IAccountGroupContext.Source` |
| `issuer` | string | `ICallerContext.Issuer` |
| `visibleAccounts` | int | Count of `Accounts` under the group filter |
