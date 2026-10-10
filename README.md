# MymoneyMcp

Solução .NET 10 / Aspire para o **MCP Server financeiro** descrito em `CLAUDE.md`. Este README documenta como abrir, restaurar e rodar a solução; `CLAUDE.md` continua sendo a fonte de verdade para arquitetura e regras de negócio.

## Pré-requisitos

- .NET 10 SDK (ver `global.json`) com suporte a `.slnx` (formato nativo a partir do SDK 9.0.200+).
- Docker (ou outro provedor de containers compatível com Aspire) — usado pelo `AddPostgres` no `AppHost` para subir o Postgres localmente.
- Workload do Aspire: `dotnet workload install aspire` (se ainda não instalado).

## Abrir a solução

```bash
dotnet restore MymoneyMcp.slnx
```

> Este scaffold foi gerado sem acesso à internet (ambiente sandbox sem acesso a `nuget.org`), portanto **nenhum pacote foi restaurado nem o build foi validado aqui**. As versões em `Directory.Packages.props` são um ponto de partida — rode `dotnet restore` e ajuste para as versões estáveis mais recentes de Aspire 9.x / EF Core 9.x / `ModelContextProtocol.AspNetCore` disponíveis no seu ambiente.

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
dotnet ef migrations add InitialCreate --project src/MymoneyMcp.Infrastructure.Persistence --startup-project src/MymoneyMcp.Api
dotnet ef database update --project src/MymoneyMcp.Infrastructure --startup-project src/MymoneyMcp.Api
```

## Rodar testes

```bash
dotnet test MymoneyMcp.slnx
```

## SDD

╭───────────────────────────────────────────────────── Next Steps ─────────────────────────────────────────────────────╮
│                                                                                                                      │
│  1. Go to the project folder: cd mymoney-mcp                                                                         │
│  2. Start Claude in this project directory; spec-kit skills were installed to .claude/skills                         │
│  3. Start using skills with your coding agent:                                                                       │
│     3.1 /speckit-constitution - Establish project principles                                                         │
│     3.2 /speckit-specify - Create baseline specification                                                             │
│     3.3 /speckit-plan - Create implementation plan                                                                   │
│     3.4 /speckit-tasks - Generate actionable tasks                                                                   │
│     3.5 /speckit-implement - Execute implementation                                                                  │
│     3.6 /speckit-converge - Assess the codebase and append remaining work as tasks                                   │
│                                                                                                                      │
╰──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╯

╭───────────────────────────────────────────────── Enhancement Skills ─────────────────────────────────────────────────╮
│                                                                                                                      │
│  Optional skills that you can use for your specs (improve quality & confidence)                                      │
│                                                                                                                      │
│  ○ /speckit-clarify (optional) - Ask structured questions to de-risk ambiguous areas before planning (run before     │
│  /speckit-plan if used)                                                                                              │
│  ○ /speckit-analyze (optional) - Cross-artifact consistency & alignment report (after /speckit-tasks, before         │
│  /speckit-implement)                                                                                                 │
│  ○ /speckit-checklist (optional) - Generate quality checklists to validate requirements completeness, clarity, and   │
│  consistency (after /speckit-plan)                                                                                   │
│                                                                                                                      │
╰──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╯


## Validação feita neste ambiente

Este scaffold foi gerado em um sandbox sem acesso a `nuget.org` (só domínios como `npm`/`pypi`/`github`/`apt` estão liberados), então o restore completo da solução não foi possível aqui. Ainda assim, com o SDK do .NET 10 instalado localmente no sandbox, foi possível validar de verdade:

- **`MymoneyMcp.slnx` carrega corretamente** via `dotnet sln list` — a primeira versão gerada tinha um `<Folder Name="/">` aninhado inválido (conflito com a pasta raiz implícita do `.slnx`), que foi **encontrado pelo parser real do SDK e corrigido**.
- **`MymoneyMcp.Domain` compila 100% limpo** (`dotnet build`, 0 erros/0 warnings) — é o único projeto sem dependências de pacote externo, então pôde ser restaurado e buildado offline de ponta a ponta.
- Revisão manual dos demais projetos identificou e corrigiu 3 problemas reais de referência que só apareceriam no build completo:
  1. `MymoneyMcp.Application.csproj` usava `DbSet<T>`/`DbContext` (em `IApplicationDbContext`, `TransactionBehavior` e vários handlers) sem referenciar o pacote `Microsoft.EntityFrameworkCore` — adicionado.
  2. `MymoneyMcp.Infrastructure.csproj` usava `IHttpContextAccessor`/`HttpContext` (`CurrentUserService`) sem `FrameworkReference` para `Microsoft.AspNetCore.App` (necessário em class libraries que não usam o SDK Web) — adicionado.
  3. `MymoneyMcp.ServiceDefaults.csproj` usava `Microsoft.AspNetCore.Builder.WebApplication` (`MapDefaultEndpoints`) pelo mesmo motivo — `FrameworkReference` adicionado.

O que **ainda não foi validado** por falta de acesso ao NuGet real: restore completo de `Application`/`Infrastructure`/`Api`/`AppHost`/testes, e portanto a compilação semântica completa desses projetos (uso correto das APIs de MediatR, FluentValidation, EF Core/Npgsql, `ModelContextProtocol.AspNetCore` e Aspire). Assim que você rodar `dotnet restore && dotnet build` com acesso normal à internet, é esperado que apareçam pequenos ajustes de API (principalmente em `ModelContextProtocol.AspNetCore`, que é uma biblioteca ainda em preview e muda com frequência) — mas a arquitetura, os nomes de tipos e a lógica de negócio já foram revisados a fundo.

## O que já está implementado neste scaffold

- Estrutura completa da solução (`.slnx` + todos os projetos de `CLAUDE.md`), com `Directory.Build.props`/`Directory.Packages.props` centralizando target framework e versões de pacote.
- `AppHost` provisionando Postgres e referenciando a Api (`WithReference`/`WaitFor`).
- `ServiceDefaults` com OpenTelemetry, health checks (`/health`, `/alive`) e resiliência HTTP padrão.
- `Domain`: entidades (`Transacao`, `Conta`, `Cartao`, `MetaOrcamento`, `RefreshToken`), enums e os value objects `Categoria` e `MesAno` centralizando as regras de parsing/agregação.
- `Application`: pipeline MediatR completo (Logging → Validation → Transaction), e as 9 features CQRS correspondentes 1:1 às tools MCP do `CLAUDE.md` (`list_transactions`, `get_transaction`, `create_transaction`, `update_transaction`, `delete_transaction`, `confirm_transaction`, `list_categories`, `get_budget_status`, `get_balance_projection`, `import_statement`), incluindo a notification de confirmação.
- `Infrastructure`: `ApplicationDbContext` com soft delete global, mapeamentos EF Core (`numeric`, `date`/`timestamptz`, índices), provedor JWT customizado (emissão, validação, refresh com rotação) e o parser CSV do formato de extrato.
- `Api`: `Program.cs` unificando Aspire + Postgres + Auth JWT + MediatR + host MCP (`WithHttpTransport`), tools MCP como wrappers finos sobre `IMediator`, endpoints `/auth/token` e `/auth/refresh`, e middleware de tratamento de erros mapeando `ValidationException`/`NotFoundException` para respostas HTTP/MCP.
- Testes: exemplos unitários para `Categoria` e `BusinessDayHelper` (Application.Tests) e um teste de integração esqueleto via `WebApplicationFactory` (Api.Tests).

## O que ainda precisa de atenção antes de produção

- Validação real de usuário/senha em `POST /auth/token` (hoje é um placeholder).
- Migration inicial do EF Core (não gerada aqui — requer o SDK instalado).
- Ajuste fino de versões de pacote em `Directory.Packages.props` contra o feed de NuGet real.
