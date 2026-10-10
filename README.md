# MymoneyMcp

Solução .NET 10 / Aspire para o **MCP Server financeiro** familiar. Este README documenta como abrir, restaurar e rodar a solução.

O projeto segue **Spec-Driven Development (SDD)**: a fonte de verdade para arquitetura e regras de negócio são a [constituição](.specify/memory/constitution.md) e as specs em [`specs/`](specs/). O [`CLAUDE.md`](CLAUDE.md) é o guia de navegação para o Claude Code.

## Pré-requisitos

- .NET 10 SDK (ver `global.json`) com suporte a `.slnx` (formato nativo a partir do SDK 9.0.200+).
- Docker (ou outro provedor de containers compatível com Aspire) — usado pelo `AddPostgres` no `AppHost` para subir o Postgres localmente.
- Workload do Aspire: `dotnet workload install aspire` (se ainda não instalado).

## Abrir a solução

```bash
dotnet restore MymoneyMcp.slnx
```

## Rodar tudo (backend + Aspire dashboard)

```bash
dotnet run --project src/MymoneyMcp.AppHost
```

O AppHost provisiona o Postgres (`mymoneymcp-postgres` / banco `mymoneymcp-db`), injeta a connection string na API por service discovery, e abre o Aspire dashboard.

## Configurar o segredo do JWT (obrigatório antes do primeiro `dotnet run`)

```bash
cd src/MymoneyMcp.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<uma-chave-aleatoria-de-pelo-menos-32-bytes>"
```

Sem esse segredo, `AddJwtBearer` sobe com uma chave vazia (apenas para não quebrar o boot local) — **nunca** rodar assim fora do ambiente de desenvolvimento.

## Migration inicial do EF Core

```bash
dotnet ef migrations add InitialCreate --project src/MymoneyMcp.Infrastructure --startup-project src/MymoneyMcp.Api
dotnet ef database update --project src/MymoneyMcp.Infrastructure --startup-project src/MymoneyMcp.Api
```

## Rodar testes

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
