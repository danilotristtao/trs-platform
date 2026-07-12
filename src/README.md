# src/ — pendente de decisão de linguagem

Este diretório está deliberadamente sem código de aplicação.

A linguagem principal de backend ainda não foi escolhida — Foundation
v2 (VI.3, item 3) marca isso como "a definir via spike técnico", e
VI.1 deixa explícito que a plataforma deve evitar poliglotismo
prematuro (Rust+Go+TS simultâneos). Escrever Aggregates aqui em uma
linguagem específica antes dessa decisão vestiria uma escolha não
ratificada como se fosse fato consumado — o mesmo erro que o ADR-0009
evitou deliberadamente ao não modelar `Approval` antes da hora.

Quando o spike técnico terminar e a escolha virar ADR (`docs/adr/`),
os três módulos abaixo devem ser criados como pastas de código real,
cada um mapeado 1:1 a um Bounded Context (ADR-0006 — nenhum Module
atravessa mais de um Bounded Context):

- `tenancy/`  → Bounded Context Trust & Governance — Aggregate `Tenant`
- `identity/` → Bounded Context Trust & Governance — Aggregate `User`
- `sales/`    → Bounded Context Sales — Aggregates `SalesOrder`, `Customer`

Até lá, `migrations/0001_init.sql` já é código real (PostgreSQL é
decisão fechada, VI.1) e pode ser aplicado/testado independentemente
da linguagem de aplicação escolhida depois.
