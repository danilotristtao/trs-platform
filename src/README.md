# src/ — linguagem decidida, código de aplicação pendente

Este diretório está deliberadamente sem código de aplicação ainda.

A linguagem principal de backend foi decidida: **C#/.NET** (ADR-0010,
`docs/adr/ADR-0010-backend-language.md`). O spike técnico previsto em
Foundation v2 (VI.3, item 3) avaliou quatro candidatos (TypeScript/
Node.js, Go, Kotlin/JVM, C#/.NET) — tecnicamente equivalentes para as
restrições já travadas do projeto (PostgreSQL+RLS, monólito modular,
modelagem de Aggregate/Value Object) — e decidiu por experiência prévia
real com a stack, não por vantagem técnica isolada de um candidato
sobre o outro (ver ADR-0010, Contexto e Riscos, para a rationale
completa e honesta).

**Banco de dados: PostgreSQL e SQL Server suportados desde a Fase 1,
Oracle arquitetado para o futuro** (ADR-0011, revisão parcial de
ADR-0007 — RLS continua obrigatório sem exceção quando o motor for
PostgreSQL).

Os três módulos de domínio abaixo devem ser criados como pastas de
código C#/.NET real, cada um mapeado 1:1 a um Bounded Context
(ADR-0006 — nenhum Module atravessa mais de um Bounded Context):

- `tenancy/`  → Bounded Context Trust & Governance — Aggregate `Tenant`
- `identity/` → Bounded Context Trust & Governance — Aggregate `User`
- `sales/`    → Bounded Context Sales — Aggregates `SalesOrder`, `Customer`

Nenhum deles acessa banco diretamente — só interfaces de Repository
(ADR-0011). A implementação de cada motor vive em módulos de
infraestrutura separados, ao lado dos módulos de domínio:

- `infrastructure/postgres/`  → Npgsql + EF Core, `SET app.tenant_id`
  centralizado na abertura de conexão (ADR-0007).
- `infrastructure/sqlserver/` → EF Core (provider SqlServer),
  `SESSION_CONTEXT` como equivalente funcional, Security Policy +
  predicate function por tabela (ADR-0011).

Diretrizes de implementação (ver ADR-0010 e ADR-0011): Value Objects
como `record` types; invariantes de Aggregate protegidos em métodos/
construtores da própria classe, nunca em validação de ORM ou de
Repository; nenhuma sintaxe específica de motor vazando para fora dos
módulos de `infrastructure/`.

`migrations/0001_init.sql` já é código real (PostgreSQL, ADR-0007) e
continua podendo ser aplicado/testado independentemente do progresso do
código de aplicação. A migration equivalente em T-SQL para SQL Server
ainda não existe — é pendência real, não apenas teórica, antes de
qualquer tabela rodar nesse motor (ADR-0011).
