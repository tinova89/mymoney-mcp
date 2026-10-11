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

Toda chamada MCP/REST precisa de `Authorization: Bearer <token>` e `X-Account-Group: <grupo>`. Isentas: `/health`, `/alive`, `/openapi` (só em Development) e `/auth/token`, `/auth/refresh` (contrato: [`specs/001-system-foundation/contracts/`](specs/001-system-foundation/contracts/)).

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
