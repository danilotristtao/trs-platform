# TRS Platform

AI-Native Business Operating Platform. Ver `docs/foundation/TRS_Foundation_v2.md`
para a visão completa, `docs/adr/` para decisões ratificadas, e
`docs/lessons-learned/` para o histórico que motivou cada uma delas.

`CLAUDE.md`, na raiz, é lido automaticamente pelo Claude Code em toda
sessão neste repositório — comece por ele.

## Estado atual

- **Fase 0 (Fundação):** concluída — ADR-0006 a ADR-0009 ratificados.
- **Fase 1 (Vertical Zero):** em andamento.
  - `migrations/0001_init.sql`: schema inicial com RLS desde a primeira
    migração (ADR-0007) e invariantes estruturais do ADR-0009 (`tenants`,
    `users`, `customers`, `sales_orders`, `sales_order_lines`).
  - `tests/rls/check_rls_coverage.sql`: teste de CI que falha o build
    se uma tabela nova com `tenant_id` não tiver RLS.
  - `src/`: pendente — linguagem de backend ainda não decidida (ver
    `src/README.md`).

## Antes de rodar `psql` contra o schema

Este projeto usa RLS com `current_setting('app.tenant_id')`. Toda
conexão de aplicação precisa executar, no início da sessão:

```sql
SET app.tenant_id = '<uuid-do-tenant-autenticado>';
```

Sem isso, as políticas de RLS não têm o que comparar e as consultas
retornam vazio (comportamento esperado e seguro — falha fechada, não
aberta).
