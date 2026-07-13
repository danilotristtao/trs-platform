# src/ — scaffolding C#/.NET (Fase 1)

Linguagem decidida: **C#/.NET** (ADR-0010, `docs/adr/ADR-0010-backend-language.md`).
Target framework: `net8.0` (LTS, único SDK instalado no ambiente de
desenvolvimento atual).

**Banco de dados: PostgreSQL e SQL Server suportados desde a Fase 1,
Oracle arquitetado para o futuro** (ADR-0011, revisão parcial de
ADR-0007 — RLS continua obrigatório sem exceção quando o motor for
PostgreSQL).

## Estado atual

Os três módulos de domínio existem como código C#/.NET real, cada um
mapeado 1:1 a um Bounded Context (ADR-0006 — nenhum Module atravessa
mais de um Bounded Context):

- `tenancy/`  → Bounded Context Trust & Governance — Aggregate `Tenant`
- `identity/` → Bounded Context Trust & Governance — Aggregate `User`
  (+ Value Object `EmailAddress`)
- `sales/`    → Bounded Context Sales — Aggregates `SalesOrder`,
  `Customer` (+ Value Objects `Money`, `TaxId`)

Nenhum deles acessa banco diretamente — só interfaces de Repository
(`ITenantRepository`, `IUserRepository`, `ICustomerRepository`,
`ISalesOrderRepository`), definidas junto ao domínio, conforme
ADR-0011. A implementação de cada motor vive em módulos de
infraestrutura separados:

- `infrastructure/postgres/` → **implementado**. `TrsDbContext` (EF
  Core + Npgsql), Repositories para os quatro Aggregates,
  `TenantContextInterceptor` centralizando `SET app.tenant_id` na
  abertura de conexão (ADR-0007).
- `infrastructure/sqlserver/` → **pendente** (ver README próprio nesse
  diretório) — migration T-SQL, Security Policy/`SESSION_CONTEXT` e
  implementação dos mesmos Repository interfaces ainda faltam.

Invariantes de Aggregate protegidos em métodos/construtores da própria
classe (nunca em validação de ORM ou de Repository); Value Objects como
`record` types. Testes unitários em `tests/Trs.Tenancy.Tests/`,
`tests/Trs.Identity.Tests/`, `tests/Trs.Sales.Tests/` cobrem os
invariantes de cada Aggregate — em especial a rejeição de
`SalesOrderLine` com moeda diferente das demais (ADR-0009, ajuste 3) e
a exigência de `human_statement` quando `reason_code` não é
`routine_creation` (ADR-0009, ajuste 1).

`migrations/0001_init.sql` já é código real (PostgreSQL, ADR-0007) e
continua podendo ser aplicado/testado independentemente do progresso do
código de aplicação. A migration equivalente em T-SQL para SQL Server
ainda não existe — é pendência real, não apenas teórica, antes de
qualquer tabela rodar nesse motor (ADR-0011).

## Pendências conhecidas (não incluídas neste passo)

- Infraestrutura SQL Server completa (`infrastructure/sqlserver/`).
- Teste de integração contra Postgres real (RLS end-to-end).
- Teste automatizado do gate da Fase 1 (autorização + autor + motivo em
  toda operação relevante — ver `CLAUDE.md`, "Gate da Fase 1").
- Host/API (contrato REST/JSON + OpenAPI com o frontend, ADR-0010) —
  ainda não iniciado.
- `Approval` não existe como Aggregate nesta fase (ADR-0009) — não
  antecipar.
