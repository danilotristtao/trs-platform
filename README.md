# TRS Platform

AI-Native Business Operating Platform. Ver `docs/foundation/TRS_Foundation_v2.md`
para a visão completa, `docs/adr/` para decisões ratificadas, e
`docs/lessons-learned/` para o histórico que motivou cada uma delas.

`CLAUDE.md`, na raiz, é lido automaticamente pelo Claude Code em toda
sessão neste repositório — comece por ele.

## Estado atual

- **Fase 0 (Fundação):** concluída — ADR-0006 a ADR-0011 ratificados.
- **Fase 1 (Vertical Zero):** em andamento.
  - `migrations/0001_init.sql`: schema inicial com RLS desde a primeira
    migração (ADR-0007) e invariantes estruturais do ADR-0009 (`tenants`,
    `users`, `customers`, `sales_orders`, `sales_order_lines`) — hoje
    cobre só PostgreSQL; migration equivalente em T-SQL para SQL Server
    ainda pendente (ADR-0011).
  - `tests/rls/check_rls_coverage.sql`: teste de CI que falha o build
    se uma tabela nova com `tenant_id` não tiver RLS no Postgres; teste
    equivalente para Security Policy do SQL Server ainda pendente
    (ADR-0011).
  - `src/`: linguagem de backend decidida (C#/.NET — ADR-0010); banco
    de dados suporta PostgreSQL e SQL Server desde a Fase 1, Oracle
    arquitetado para o futuro (ADR-0011); código de aplicação
    (`tenancy/`, `identity/`, `sales/` + infraestrutura por motor)
    ainda a implementar (ver `src/README.md`).

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
