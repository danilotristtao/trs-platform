# CLAUDE.md — TRS Platform

Este arquivo é lido automaticamente pelo Claude Code no início de toda
sessão neste repositório. Ele é um **resumo operacional**, não um
substituto dos documentos completos em `docs/`. Em caso de dúvida ou
conflito aparente, os arquivos em `docs/adr/` e `docs/foundation/`
são a fonte de verdade — este arquivo nunca prevalece sobre eles.

## Antes de qualquer decisão de arquitetura

Rode este checklist (Foundation v2, Parte IX.1) e registre a resposta
por escrito no ADR correspondente, não em um comentário de código:

1. Quais Lessons Learned (LL-001 a LL-008, em `docs/lessons-learned/`)
   esta decisão toca?
2. Quais requisitos AR-* (`docs/foundation/TRS_Foundation_v2.md`,
   Parte IV) ela atende, e quais poderia violar?
3. Qual é o modo de falha específico *desta solução* (não do problema
   que ela resolve)?
4. Esta decisão pertence à fase atual do roadmap, ou está antecipando
   algo que depende de uma fase anterior ainda incompleta?

Se a decisão for cara de reverter (afeta persistência, tenancy,
vocabulário do domínio, ou introduz conceito novo no Kernel), ela
precisa de um ADR próprio em `docs/adr/ADR-00XX-nome.md`, seguindo o
formato dos ADR-0006 a 0009. Não implementar antes de ratificar.

## Regras que não podem ser esquecidas em nenhuma sessão

- **Vocabulário (ADR-0006):** usar exatamente Entity, Value Object,
  Aggregate, Aggregate Root, Capability, Module, Bounded Context,
  Extension, Policy Scope, Event Scope. Nenhum sinônimo informal.
  Nenhum `Command`, `EventStore`, `SagaCoordinator`, `ProcessManager`
  ou `SnapshotStore` sem caso de uso concreto do roadmap que os exija.
  Um `Module` NUNCA atravessa mais de um `Bounded Context`.
- **Isolamento de dados (ADR-0007):** Row-Level Security compartilhado
  é o único modelo de persistência multi-tenant até a Fase 7. Toda
  tabela de dado de negócio DEVE ter `tenant_id` + política de RLS
  desde a primeira migração — sem exceção, sem schema/database
  dedicado. `Tenant` é a única tabela sem `tenant_id` próprio (é a
  raiz da fronteira, não dado dentro dela). CI DEVE falhar o build se
  uma tabela nova com dado de tenant não tiver RLS correspondente (ver
  `tests/rls/`).
- **Local autoritativo de regra (ADR-0008):** antes de escrever
  qualquer lógica nova, classificar em um dos 7 tipos da tabela do
  ADR-0008 (Invariante de domínio, Política de negócio,
  Processo/Workflow, Validação de UX, Transformação/Integração,
  Cálculo decisório, Derivação de leitura). Cálculo que influencia
  preço/imposto/desconto/limite/saldo é Aggregate/Domain Service, NUNCA
  Projection Layer. Authorization Layer ("pode tentar?") e Policy Layer
  ("é permitido neste contexto?") são camadas diferentes — não
  duplicar regra entre elas.
- **Modelo de domínio da Fase 1 (ADR-0009):** os quatro Aggregates
  desta fase são `Tenant`, `User`, `SalesOrder`, `Customer`. `role` em
  `User` tem exatamente dois valores fixos (`tenant_admin`, `member`) —
  não expandir informalmente antes da Fase 2. `reason_code` de
  Rationale tem vocabulário controlado (`routine_creation` como
  default, dispensa `human_statement`; `manual_override`,
  `exception_approval`, `correction` exigem `human_statement`). Toda
  `SalesOrderLine` de um mesmo `SalesOrder` DEVE compartilhar a mesma
  moeda — rejeitado pelo próprio Aggregate, nunca pela UI.
- **`Approval` não existe como Aggregate nesta fase.** Não criar
  tabela, classe ou conceito `Approval` até a Fase 2 (Policy) e Fase 3
  (Workflow) começarem formalmente. `sales.approval.threshold` é
  exemplo ilustrativo no ADR-0008, não autorização para adiantar.
- **Sem Decision Envelope completo, sem outbox transacional, sem
  event sourcing nesta fase.** Fase 1 gera apenas Audit Record +
  Rationale mínimo. Decision Envelope (AR-EXP-001/002) é Fase 2.
  Outbox (AR-TXN-003) é Fase 3. Event sourcing como padrão global
  exige ADR próprio (AR-TXN-006) — não assumir.

## Decisões técnicas ainda pendentes — não assumir

- **Linguagem principal de backend:** ainda não escolhida (Foundation
  v2, VI.3, item 3 — "a definir via spike técnico"). Não escrever
  código de aplicação em `src/` assumindo uma linguagem até essa
  decisão virar ADR. `migrations/` já pode usar SQL puro (PostgreSQL
  já é decisão fechada, VI.1).
- Kubernetes, Kafka/NATS, GraphQL, múltiplas linguagens no backend,
  banco vetorial dedicado, marketplace público — todos explicitamente
  adiados (Foundation v2, VI.2). Não introduzir sem necessidade
  operacional comprovada e ADR correspondente.

## Estrutura do repositório

```
docs/adr/              → ADRs ratificados (fonte de verdade normativa)
docs/lessons-learned/   → LL-001 a LL-008 (contexto histórico e critérios derivados)
docs/foundation/        → TRS_Foundation_v2.md (requisitos AR-*, roadmap, arquitetura de referência)
migrations/             → schema PostgreSQL, com RLS desde a primeira migração
src/tenancy/            → Module `tenancy` (Aggregate Tenant) — Bounded Context Trust & Governance
src/identity/           → Module `identity` (Aggregate User) — Bounded Context Trust & Governance
src/sales/              → Module `sales` (Aggregates SalesOrder, Customer) — Bounded Context Sales
tests/rls/              → teste de CI que falha o build se uma tabela com tenant_id não tiver RLS
```

## Gate da Fase 1 (critério de saída, não lista de tarefas)

Nenhuma operação relevante ocorre sem: autorização registrada (role
`tenant_admin`/`member` verificado), autor identificado, e motivo
capturado (`reason_code`, com `human_statement` quando aplicável).
Isso deve ser um teste automatizado, não uma checagem manual.
