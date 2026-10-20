# Data Model: Primary currency

No database change and no migration: everything here lives in memory, built once at startup.
Decisions: [research.md](research.md).

## CurrencyCode (Domain value object)

| Field | Type | Rules |
|---|---|---|
| `Value` | string | 3 upper-case letters on ISO 4217 List One (R2) |

- `TryParse(string?, out CurrencyCode)`: trims, upper-cases (invariant), accepts only codes in `Iso4217`.
- `Parse(string)`: same, throws `ArgumentException` when invalid.
- Equality by `Value`; `ToString()` returns `Value`.
- Reused by 006 for an account's currency.

## CurrencySettings (Application, bound from `Currency`)

| Field | Type | Meaning |
|---|---|---|
| `Default` | string? | `null` = not configured (→ `BRL`); otherwise must parse |
| `Groups` | list of `GroupCurrencySetting` | Optional per-group entries, in configuration order |

`GroupCurrencySetting`: `Group` (string?), `Code` (string?). Raw values; validated by the table.

## PrimaryCurrencyTable (Application, singleton)

| Member | Meaning |
|---|---|
| `Default` | `CurrencyCode` for groups without their own entry |
| per-group state | `Configured(CurrencyCode)` or `Misconfigured` |
| `Lookup(group)` | the group's state, or `Default` when the group has no entry |

`Build(CurrencySettings)` → table + problems. Problem kinds (R3):

| Kind | Severity | Effect |
|---|---|---|
| `InvalidDefault` (blank or not ISO 4217) | fatal | host does not start |
| `BlankGroupName` | warning | entry ignored |
| `DuplicateGroup` | error | group misconfigured |
| `InvalidGroupCode` (blank or not ISO 4217) | error | group misconfigured |

Each problem carries the setting path (`Currency:Default` or `Currency:Groups:<index>`) and, for
group problems, the trimmed group name.

## State per group

```text
no entry ──────────────► Default
one valid entry ───────► Configured(code)
invalid or duplicate ──► Misconfigured  (IPrimaryCurrency throws; restart after a fix)
```
