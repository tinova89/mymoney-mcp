# Contract: MCP Endpoint

- **URL**: `/mcp` (Streamable HTTP transport).
- **Headers**: bearer token and `X-Account-Group`, as in [http-conventions.md](http-conventions.md).
- **Server info**: name `mymoney-mcp`.
- **Capabilities**: `tools` advertised; `tools/list` returns an empty list in this feature
  (spec FR-010).

| Call | Expected |
|---|---|
| `initialize` with token + header | success, server info returned |
| `tools/list` | `{ "tools": [] }` |
| any call without token (auth on) | `401` |
| any call without header | `400` |

Client example (Claude Code):
```bash
claude mcp add --transport http mymoney http://localhost:<port>/mcp \
  --header "Authorization: Bearer <jwt>" --header "X-Account-Group: HOME"
```
