# Contract: OAuth Protected Resource Metadata (RFC 9728)

Published only when `Auth:External:Authority` is configured; anonymous, no group header.

| Method | Route | Response |
|---|---|---|
| `GET` | `/.well-known/oauth-protected-resource/mcp` | metadata for `/mcp` |
| `GET` | `/.well-known/oauth-protected-resource/mcp/<group>` | metadata for `/mcp/<group>` |

```json
{
  "resource": "https://<host>/mcp/HOME",
  "authorization_servers": ["<Auth:External:Authority>"],
  "bearer_methods_supported": ["header"]
}
```

- `resource` is the absolute URL of the requested MCP path, with the group segment kept
  percent-encoded exactly as the client sent it (claude.ai compares it with the connector URL as a
  string): `/mcp/Fam%C3%ADlia`, never `/mcp/Família`.
- The token audience is not per group: every MCP URL accepts `Auth:External:Audience` (spec FR-016).

## Sign-in flow (claude.ai)

1. claude.ai calls `/mcp/HOME` without a token → `401` with `resource_metadata`.
2. It reads the metadata, then the external issuer's own discovery document.
3. The user signs in at the issuer; claude.ai gets a token with the configured audience.
4. claude.ai calls `/mcp/HOME` with `Authorization: Bearer <token>` → accepted (BR-21).
