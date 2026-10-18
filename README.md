# MymoneyMcp

Solução .NET 10 / Aspire para o **MCP Server financeiro** familiar. Este README documenta como abrir, restaurar e rodar a solução.

O projeto segue **Spec-Driven Development (SDD)**: a fonte de verdade para arquitetura e regras de negócio são a [constituição](.specify/memory/constitution.md) e as specs em [`specs/`](specs/). O [`CLAUDE.md`](CLAUDE.md) é o guia de navegação para o Claude Code.

## Pré-requisitos

- .NET 10 SDK (ver `global.json`) com suporte a `.slnx` (formato nativo a partir do SDK 9.0.200+).
- Docker (ou outro provedor de containers compatível com Aspire) — usado pelo `AddPostgres` no `AppHost` para subir o Postgres localmente.
- Aspire 13 vem como pacote NuGet (`Aspire.AppHost.Sdk`); não é preciso instalar workload.

## Abrir a solução

```bash
dotnet restore MymoneyMcp.slnx
```

## Rodar tudo (backend + Aspire dashboard)

```bash
dotnet run --project src/MymoneyMcp.AppHost
```

O AppHost provisiona o Postgres (`mymoneymcp-postgres` / banco `mymoneymcp-db`), injeta a connection string na API por service discovery, e abre o Aspire dashboard.

## Configurar segredos e clients (obrigatório antes do primeiro `dotnet run`)

O projeto `MymoneyMcp.Api` já tem `UserSecretsId`; não é preciso rodar `user-secrets init`.

```bash
cd src/MymoneyMcp.Api
dotnet user-secrets set "Jwt:SigningKey" "<uma-chave-aleatoria-de-pelo-menos-32-bytes>"
dotnet user-secrets set "Auth:Clients:0:ClientId" "claude-code"
dotnet user-secrets set "Auth:Clients:0:ClientSecret" "<um-secret-forte>"
```

Com a verificação de token ligada (padrão), a API **não sobe** sem `Jwt:SigningKey` de pelo menos 32 bytes.

## Configurações de autenticação

| Chave | Padrão | Uso |
|---|---|---|
| `Jwt:SigningKey` | — | Chave HMAC dos tokens emitidos pelo sistema (segredo). |
| `Jwt:Issuer` / `Jwt:Audience` | `mymoney-mcp` | `iss`/`aud` dos tokens próprios. |
| `Jwt:AccessTokenMinutes` / `Jwt:RefreshTokenDays` | `60` / `14` | Validade dos tokens. |
| `Auth:Clients:N:ClientId` / `ClientSecret` | — | Clients aceitos em `POST /auth/token` (segredo). |
| `Auth:Enabled` | `true` | `false` desliga a verificação de token. **Proibido em Production**: a API se recusa a subir. O header `X-Account-Group` continua obrigatório. |
| `Auth:External:Authority` / `Audience` / `Issuer` | — | Emissor externo confiável (OIDC). Desligado se `Authority` estiver vazio. |

Toda chamada MCP/REST precisa de `Authorization: Bearer <token>` e de um grupo: no REST, pelo header `X-Account-Group: <grupo>`; no MCP, pelo header ou pela URL `/mcp/<grupo>`. Isentas: `/health`, `/alive`, `/openapi` (só em Development), `/auth/token`, `/auth/refresh` e `/.well-known/*` (contratos: [`001`](specs/001-system-foundation/contracts/), [`002`](specs/002-claude-ai-connector/contracts/)).

## Chamar o MCP localmente

Com a API rodando (`dotnet run --project src/MymoneyMcp.AppHost`, ou o perfil `http` da Api em `http://localhost:5180`) e o client configurado acima:

### 1. Pegar um token

```bash
curl -s -X POST http://localhost:5180/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId":"claude-code","clientSecret":"<um-secret-forte>"}'
```

A resposta traz `accessToken` (vale 60 min) e `refreshToken`. Para renovar sem o secret:

```bash
curl -s -X POST http://localhost:5180/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<refreshToken>"}'
```

Cada refresh token vale uma vez só; a resposta traz um par novo.

### 2. Conectar o Claude Code

O grupo pode ir na URL (um conector por grupo) ou no header:

```bash
# grupo na URL
claude mcp add --transport http mymoney-home http://localhost:5180/mcp/HOME \
  --header "Authorization: Bearer <accessToken>"

# grupo no header
claude mcp add --transport http mymoney http://localhost:5180/mcp \
  --header "Authorization: Bearer <accessToken>" --header "X-Account-Group: HOME"
```

Depois peça ao Claude para rodar a tool `whogroupami`: ela mostra o grupo recebido, de onde veio (`url`, `header` ou `both`), o emissor do token e quantas contas o grupo enxerga.

### 3. Chamar com `curl`

O transporte é Streamable HTTP sem sessão: cada `POST` é independente e não precisa de `initialize` antes. O `Accept` precisa aceitar JSON e `text/event-stream`; a resposta vem como evento SSE (`event: message` / `data: {...}`).

```bash
# listar as tools
curl -s -X POST http://localhost:5180/mcp/HOME \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

# chamar whogroupami
curl -s -X POST http://localhost:5180/mcp/HOME \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"whogroupami","arguments":{}}}'
```

Exemplo de `data:` do `whogroupami` (o resultado vem como texto JSON dentro de `content`):

```json
{"accountGroup":"HOME","groupSource":"url","issuer":"local","visibleAccounts":0}
```

Para um grupo com espaço ou acento, codifique o segmento na URL (`/mcp/Fam%C3%ADlia`).

| Resposta | Causa |
|---|---|
| `401` | Sem token, token expirado, ou nenhum `Auth:Clients` configurado para gerar um |
| `400` `Missing X-Account-Group header` | `/mcp` (ou `/mcp/`) sem o header |
| `400` `Blank account group in MCP URL` | Grupo em branco na URL (ex.: `/mcp/%20`) |
| `400` `Account group mismatch` | Grupo da URL diferente do header |

### 4. Usar pelo Docker MCP Toolkit (opcional)

O [Docker MCP Toolkit](https://docs.docker.com/ai/mcp-catalog-and-toolkit/toolkit/) (plugin `docker mcp` do Docker Desktop) pode agregar o MymoneyMcp a outros MCP servers num gateway só. O MymoneyMcp entra como servidor **remoto**; nada roda em container. Exemplos prontos em [`tools/docker-mcp/`](tools/docker-mcp/):

| Arquivo | Grupo |
|---|---|
| [`mymoney-home-url.yaml`](tools/docker-mcp/mymoney-home-url.yaml) | Na URL (`/mcp/HOME`), um servidor por grupo |
| [`mymoney-home-header.yaml`](tools/docker-mcp/mymoney-home-header.yaml) | No header `X-Account-Group` (`/mcp`) |

Não coloque grupo na URL **e** um header diferente: a API recusa com `400` e o gateway sobe sem as tools do MymoneyMcp (o log mostra `Can't start ... Bad Request`).

1. Pegue um token (passo 1 acima) e grave no keychain do Toolkit:

   ```bash
   docker mcp secret set mymoney.token=<accessToken>
   ```

   O comando fica no histórico do shell; o token expira em 60 min, mas apague a linha do histórico se preferir. Quando o token expirar, as chamadas voltam `401`: repita este passo.

2. Crie um profile com um dos exemplos e teste sem cliente nenhum:

   ```bash
   docker mcp profile create --name mymoney-poc --server file://./tools/docker-mcp/mymoney-home-url.yaml
   docker mcp tools call --gateway-arg=--profile=mymoney_poc whogroupami
   ```

   O Toolkit troca `-` por `_` no ID do profile (`mymoney-poc` → `mymoney_poc`); os demais comandos usam o ID (`docker mcp profile list`). O aviso `Dynamic OAuth discovery failed ... 401` ao criar o profile é esperado sem emissor externo configurado.

3. Conecte o Claude Code ao gateway (`--global` evita escrever a configuração dentro do repositório):

   ```bash
   docker mcp client connect claude-code --profile mymoney_poc --global
   ```

- A URL é `localhost`: o gateway roda no host e a API escuta só no loopback, então `host.docker.internal` não funciona com o perfil `http`.
- Para diagnosticar, `docker mcp tools ls --verbose --gateway-arg=--profile=mymoney_poc` mostra o tamanho do secret (`len=0` = vazio) e o erro de conexão com a API.
- Desfazer: `docker mcp profile remove mymoney_poc` e `docker mcp secret rm mymoney.token`.

## Migrations do EF Core

A API aplica as migrations pendentes ao subir; se o banco estiver inacessível ou uma migration falhar, ela para com o erro no log. Para criar uma migration nova:

```bash
dotnet tool restore
dotnet ef migrations add <Nome> --project src/MymoneyMcp.Infrastructure.Data --startup-project src/MymoneyMcp.Api
```

## Rodar testes

Os testes de integração sobem PostgreSQL real com Testcontainers, então o **Docker precisa estar rodando**.

```bash
dotnet test MymoneyMcp.slnx
```

## SDD

Cada feature segue o fluxo abaixo, com as skills do spec-kit instaladas em `.claude/skills`:

| Ordem | Comando | O que faz |
|---|---|---|
| 1 | `/speckit-constitution` | Cria ou altera os princípios do projeto (também pode editar o arquivo direto) |
| 2 | `/speckit-specify` | Escreve o `spec.md` da feature (o quê e por quê) |
| 3 | `/speckit-clarify` *(opcional)* | Responde as dúvidas do spec antes do plano |
| 4 | `/speckit-plan` | Escreve o `plan.md` (como implementar) |
| 5 | `/speckit-checklist` *(opcional)* | Gera checklists de qualidade dos requisitos |
| 6 | `/speckit-tasks` | Quebra o plano em `tasks.md` |
| 7 | `/speckit-analyze` *(opcional)* | Verifica a consistência entre spec, plan e tasks |
| 8 | `/speckit-implement` | Executa as tasks |
| 9 | `/speckit-converge` *(opcional)* | Compara o código com o spec e adiciona as tasks que faltam |

`/speckit-taskstoissues` converte as tasks em issues do GitHub.
