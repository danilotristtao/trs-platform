# TRS Platform

AI-Native Business Operating Platform. Ver `docs/foundation/TRS_Foundation_v2.md`
para a visão completa, `docs/adr/` para decisões ratificadas, e
`docs/lessons-learned/` para o histórico que motivou cada uma delas.

`CLAUDE.md`, na raiz, é lido automaticamente pelo Claude Code em toda
sessão neste repositório — comece por ele.

## Estado atual

- **Fase 0 (Fundação):** concluída — ADR-0006 a ADR-0011 ratificados.
- **Fase 1 (Vertical Zero):** em andamento.
  - `docs/adr/ADR-0012-audit-and-logging-strategy.md`: estratégia de
    auditoria/logging ratificada (`audit_events`/`audit_event_changes`
    para alteração de campo, `security_events` para autenticação/
    permissão, escrita só via `IAuditService`, nunca trigger) —
    `business_events`, `workflow_history` e `integration_events`
    explicitamente adiados para fases futuras. Ainda **só o ADR**:
    migration, `IAuditService`, o interceptor do EF Core e o teste de
    CI de imutabilidade ainda não foram implementados.
  - `docs/adr/ADR-0013-company-branch-structure.md`: revisa
    parcialmente o ADR-0009 — adiciona `Company` como quinto Aggregate
    da Fase 1 (Module `tenancy`), representando matriz/filial dentro do
    mesmo `Tenant` (auto-referência via `parent_company_id`, só dois
    níveis), com geração de código de negócio portável
    (`config_code_sequences`). Ainda **só o ADR**: código C#, migration
    e Repository ainda não foram implementados.
  - `migrations/0001_init.sql`: schema inicial com RLS desde a primeira
    migração (ADR-0007) e invariantes estruturais do ADR-0009 (`tenants`,
    `users`, `customers`, `sales_orders`, `sales_order_lines`) — hoje
    cobre só PostgreSQL; migration equivalente em T-SQL para SQL Server
    ainda pendente (ADR-0011).
  - `tests/rls/check_rls_coverage.sql`: teste de CI que falha o build
    se uma tabela nova com `tenant_id` não tiver RLS no Postgres; teste
    equivalente para Security Policy do SQL Server ainda pendente
    (ADR-0011).
  - `src/`: scaffolding C#/.NET real (`TrsPlatform.sln`, .NET 8 LTS) —
    Aggregates `Tenant`, `User`, `Customer`, `SalesOrder` com os
    invariantes do ADR-0009 em código, Repository interfaces (ADR-0011)
    e infraestrutura Postgres (EF Core + Npgsql) com `SET app.tenant_id`
    centralizado na abertura de conexão (ADR-0007). Infraestrutura SQL
    Server ainda pendente (`src/infrastructure/sqlserver/README.md`).
    Sem host/API ainda, sem teste de integração contra Postgres real,
    sem o teste automatizado do gate da Fase 1 (próximos passos).

## Antes de rodar contra qualquer motor de banco

RLS (PostgreSQL) e Security Policy (SQL Server) são obrigatórios e
equivalentes em garantia — nenhum dos dois é opcional dependendo do
motor escolhido pelo cliente (ADR-0011).

**PostgreSQL:** usa `current_setting('app.tenant_id')`. Toda conexão de
aplicação precisa executar, no início da sessão:

```sql
SET app.tenant_id = '<uuid-do-tenant-autenticado>';
```

**SQL Server:** usa `SESSION_CONTEXT` como equivalente funcional —
migration e Security Policy ainda pendentes de implementação
(ADR-0011).

Sem o contexto de tenant definido, a consulta deve retornar vazio em
qualquer um dos dois motores (comportamento esperado e seguro — falha
fechada, não aberta).
