# infrastructure/sqlserver/ — pendente

Este diretório é deliberadamente um placeholder, no mesmo espírito do
`src/README.md` original antes do scaffolding de `tenancy/`, `identity/`
e `sales/`.

**Pendência real, não apenas teórica** (ADR-0011): SQL Server é motor de
produção suportado desde a Fase 1, mas ainda faltam, antes de qualquer
tabela rodar nesse motor:

- Migration T-SQL equivalente a `migrations/0001_init.sql`, incluindo a
  invariante de moeda única por `SalesOrder` (hoje implementada via
  trigger no Postgres).
- Security Policy + predicate function por tabela, usando
  `SESSION_CONTEXT` como equivalente funcional a
  `current_setting('app.tenant_id')` — mesma garantia de falha fechada
  do RLS do Postgres.
- Teste de CI equivalente a `tests/rls/check_rls_coverage.sql`, que
  falhe o build se uma tabela nova não tiver Security Policy
  correspondente.
- Implementação de `ITenantRepository`, `IUserRepository`,
  `ICustomerRepository`, `ISalesOrderRepository` (mesmos contratos já
  definidos em `tenancy/`, `identity/`, `sales/`) usando
  `Microsoft.EntityFrameworkCore.SqlServer`.

Nenhuma tabela é considerada "coberta" (ADR-0011) sem os dois motores —
Postgres (`infrastructure/postgres/`, já implementado) e SQL Server
(este diretório) — com seus respectivos testes de isolamento passando.
