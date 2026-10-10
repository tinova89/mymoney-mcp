# Contract: Auth REST API

No `X-Account-Group` header and no bearer token required.

## POST /auth/token

Request:
```json
{ "clientId": "claude-code", "clientSecret": "<secret>" }
```

| Result | Status | Body |
|---|---|---|
| Valid credentials | `200` | `TokenResponse` |
| Unknown client or wrong secret | `401` | problem details (same for both) |
| Blank field | `400` | problem details with field errors |

## POST /auth/refresh

Request:
```json
{ "refreshToken": "<opaque>" }
```

| Result | Status | Body |
|---|---|---|
| Valid, unused, unexpired token | `200` | `TokenResponse`; the old refresh token is revoked |
| Unknown, used, or expired token | `401` | problem details |
| Blank field | `400` | problem details |

## TokenResponse

```json
{
  "accessToken": "<jwt>",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "refreshToken": "<opaque>"
}
```

JWT claims: `sub` (clientId), `iss`, `aud`, `iat`, `exp`. No group claim (spec FR-006).
