# src/ — skeleton da arquitetura-alvo (Fase 1, ainda sem implementação)

Linguagem decidida: **C#/.NET** (ADR-0010, `docs/adr/ADR-0010-backend-language.md`).
Target framework: `net8.0` (LTS).

**Banco de dados: PostgreSQL e SQL Server suportados desde a Fase 1,
Oracle arquitetado para o futuro** (ADR-0011, revisão parcial de
ADR-0007 — RLS continua obrigatório sem exceção quando o motor for
PostgreSQL; Security Policy/`SESSION_CONTEXT` é o equivalente no SQL
Server).

## Estado atual — só skeleton, sem domínio

Nenhum Aggregate está implementado. O que existe é o esqueleto físico
da arquitetura-alvo definida em
`docs/foundation/TRS_Architecture_Definition.md` (ratificada pelo
ADR-0014), criado em 2026-07-19:

```
TRS.BuildingBlocks/                          → vazio (Domain, Application,
                                                 Contracts/{Messaging,Versioning},
                                                 Persistence, Observability)
TRS.Kernel/
  ├── Tenancy/    → Trs.Kernel.Tenancy.csproj    (vazio, ref. BuildingBlocks)
  ├── Identity/   → Trs.Kernel.Identity.csproj   (vazio, ref. BuildingBlocks)
  └── Audit/      → Trs.Kernel.Audit.csproj      (vazio, ref. BuildingBlocks)
TRS.Infrastructure/Database/
  ├── PostgreSQL/ → Trs.Infrastructure.Database.PostgreSQL.csproj
  │                 (vazio; pacote Npgsql.EntityFrameworkCore.PostgreSQL já referenciado)
  └── SqlServer/  → Trs.Infrastructure.Database.SqlServer.csproj
                    (vazio; pacote Microsoft.EntityFrameworkCore.SqlServer já referenciado)
TRS.ApplicationHost/                         → console stub (Program.cs padrão)
TRS.DatabaseMigrator/                        → console stub (Program.cs padrão)
Modules/                                     → vazio (Sales entra aqui quando retomar)
```

Todos os `.csproj` estão registrados em `TrsPlatform.sln` e o build
passa limpo (`dotnet build TrsPlatform.sln`). Nenhum arquivo `.cs` tem
lógica além do `Program.cs` gerado por template.

## O que foi removido em 2026-07-19

A implementação anterior — Aggregates `Tenant`, `User`, `Customer`,
`SalesOrder` (ADR-0009/0013), Repository interfaces, infraestrutura
Postgres (EF Core + Npgsql) e SQL Server, `migrations/` dos dois
motores e os testes unitários/CI de RLS — foi apagada por decisão do
usuário, para retomar a fase de documentação/planejamento antes de
reescrever o código. Isso **não** reverte nenhum ADR: os Aggregates e
invariantes continuam ratificados em ADR-0009/0011/0013, só ainda não
têm código correspondente.

## Deliberadamente fora do skeleton (phase-gating, ADR-0006 Regra 24/25)

`Kernel/Authorization`, `Kernel/Metadata`, `Kernel/Configuration`,
`Kernel/Extensibility`, `Processes/`,
`TRS.Infrastructure/Messaging/{Outbox,Inbox,EventBus}` — nenhum tem
caso de uso concreto da Fase 1. Não criar nem como pasta vazia sem
decisão explícita.

## Antes de escrever código de domínio

Dois tópicos ainda sem ADR, a discutir antes de reescrever qualquer
Aggregate:

1. **Cadastros** a definir antes do módulo `sales` retomar.
2. **Estratégia de exclusão de dados** (lógica vs. física, performance).

## Fluxo de trabalho

A implementação é escrita manualmente pelo usuário no VS Code. Claude
Code gera código sugerido na conversa quando solicitado, mas não
escreve diretamente arquivos `.cs` de domínio/aplicação neste projeto.
