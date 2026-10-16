# Contract: MCP Endpoint (002)

Extends [001 mcp-endpoint](../../001-system-foundation/contracts/mcp-endpoint.md) and
[001 http-conventions](../../001-system-foundation/contracts/http-conventions.md); REST is unchanged.

## URLs

| URL | Group from | Typical client |
|---|---|---|
| `/mcp` (or `/mcp/`) | `X-Account-Group` header | Claude Code |
| `/mcp/<group>` | URL segment, optionally also the header | claude.ai connector (one per group) |

- `<group>` is one path segment, percent-encoded (`Fam%C3%ADlia`, `a%2Fb` → `a/b`), decoded once,
  then trimmed. `/mcp/A/B` → `404`.
- Transport: Streamable HTTP, stateless (no `Mcp-Session-Id`).

## Check order and errors

| Order | Check | Failure |
|---|---|---|
| 1 | Token valid (when enabled) | `401`; on MCP URLs with the external issuer configured, `WWW-Authenticate: Bearer resource_metadata="<origin>/.well-known/oauth-protected-resource/<mcp path>"` |
| 2 | Group present | `400`, `title: "Missing X-Account-Group header"` (001 title kept) |
| 2 | URL group not blank | `400`, `title: "Blank account group in MCP URL"` |
| 2 | URL group equals header | `400`, `title: "Account group mismatch"` |

All `400`s are `application/problem+json` and never echo the header or URL value.

## Tool `whogroupami`

| Item | Value |
|---|---|
| Parameters | none |
| Annotations | `readOnlyHint: true` |
| Result | JSON text content, below |

```json
{ "accountGroup": "Silva", "groupSource": "url", "issuer": "external", "visibleAccounts": 2 }
```

| Field | Values |
|---|---|
| `groupSource` | `url`, `header`, `both` |
| `issuer` | `local`, `external`, `none` (token checking off) |
| `visibleAccounts` | accounts of this group only; `0` for an unknown group |

`tools/list` now returns exactly `[whogroupami]` (was `[]` in 001).

## Client examples

```bash
# Claude Code, unchanged
claude mcp add --transport http mymoney http://localhost:<port>/mcp \
  --header "Authorization: Bearer <jwt>" --header "X-Account-Group: HOME"
```

claude.ai: Settings → Connectors → Add custom connector → URL `https://<host>/mcp/HOME`; no
headers, no token; sign-in starts from the `401` ([oauth-metadata.md](oauth-metadata.md)).
