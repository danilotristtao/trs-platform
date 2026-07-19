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
formato dos ADR-0006 a 0013. Não implementar antes de ratificar.

## Regras que não podem ser esquecidas em nenhuma sessão

- **Vocabulário (ADR-0006):** usar exatamente Entity, Value Object,
  Aggregate, Aggregate Root, Capability, Module, Bounded Context,
  Extension, Policy Scope, Event Scope. Nenhum sinônimo informal.
  Nenhum `Command`, `EventStore`, `SagaCoordinator`, `ProcessManager`
  ou `SnapshotStore` sem caso de uso concreto do roadmap que os exija.
  Um `Module` NUNCA atravessa mais de um `Bounded Context`.
- **Isolamento de dados (ADR-0007, revisado parcialmente por
  ADR-0011):** PostgreSQL e SQL Server são motores de produção
  suportados desde a Fase 1 (Oracle arquitetado, não implementado).
  Quando o motor for PostgreSQL, Row-Level Security é obrigatório e sem
  exceção — toda tabela de dado de negócio DEVE ter `tenant_id` +
  política de RLS desde a primeira migração. Quando o motor for SQL
  Server, o equivalente obrigatório é Security Policy + predicate
  function via `SESSION_CONTEXT`. `Tenant` é a única tabela sem
  `tenant_id` próprio em qualquer motor (é a raiz da fronteira, não
  dado dentro dela). Aggregates e regras de negócio NUNCA acessam
  SQL/DbContext diretamente — só interfaces de Repository (ADR-0011),
  para que suportar um motor novo não exija tocar o domínio. CI DEVE
  falhar o build se uma tabela nova com dado de tenant não tiver a
  política de isolamento correspondente **em cada motor suportado**
  (ver `tests/rls/`).
- **Local autoritativo de regra (ADR-0008):** antes de escrever
  qualquer lógica nova, classificar em um dos 7 tipos da tabela do
  ADR-0008 (Invariante de domínio, Política de negócio,
  Processo/Workflow, Validação de UX, Transformação/Integração,
  Cálculo decisório, Derivação de leitura). Cálculo que influencia
  preço/imposto/desconto/limite/saldo é Aggregate/Domain Service, NUNCA
  Projection Layer. Authorization Layer ("pode tentar?") e Policy Layer
  ("é permitido neste contexto?") são camadas diferentes — não
  duplicar regra entre elas.
- **Modelo de domínio da Fase 1 (ADR-0009, revisado parcialmente por
  ADR-0013):** os Aggregates desta fase são `Tenant`, `User`,
  `SalesOrder`, `Customer` (ADR-0009) e `Company` (ADR-0013, Module
  `tenancy` — estrutura de matriz/filial dentro do Tenant, não um
  Tenant por filial). `role` em
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
- **Auditoria e logging (ADR-0012):** toda alteração relevante (não só
  criação) precisa gerar registro em `audit_events`/`audit_event_changes`
  (quem, quando, campo, valor antigo/novo) e eventos de segurança em
  `security_events` — nunca escrito direto pelos Modules, sempre via
  `IAuditService`; nunca via trigger de banco (a intenção de negócio só
  a aplicação sabe, o banco só vê que um valor mudou). Tabelas
  append-only (só `INSERT`), verificado por teste de CI. `business_events`
  (Event Store), `workflow_history` e `integration_events` continuam
  fora do escopo até os próprios ADRs/fases que os liberam (regra
  acima) — não reintroduzir informalmente dentro do mecanismo de
  auditoria.
- **Estrutura de Empresa/Filial (ADR-0013):** `Company` é Aggregate
  Root único para matriz e filial (auto-referência via
  `parent_company_id`, só dois níveis — nunca filial de filial), Module
  `tenancy`, nunca `sales`. FK composta `(tenant_id, parent_company_id)`
  é obrigatória — uma FK simples permitiria filial referenciar matriz de
  outro tenant. Código de negócio gerado via `config_code_sequences`
  (tabela portável, `SELECT ... FOR UPDATE` na mesma transação), nunca
  `SEQUENCE` nativa nem trigger. Nenhuma tabela de configuração por
  tabela do sistema inteiro (LL-001).

## Decisões técnicas já fechadas (além do checklist acima)

- **Linguagem principal de backend: C#/.NET** (ADR-0010). Todos os
  módulos do Kernel (`tenancy`, `identity`, `sales`, e futuramente
  `policy`/`workflow`) rodam como módulos internos do mesmo processo,
  na mesma linguagem — nenhum serviço físico separado antes da hora
  (AR-RUL-005). Frontend continua TypeScript (VI.1) — as duas camadas
  não compartilham linguagem, só o contrato REST/JSON+OpenAPI.

## Decisões técnicas ainda pendentes — não assumir
- Kubernetes, Kafka/NATS, GraphQL, múltiplas linguagens no backend,
  banco vetorial dedicado, marketplace público — todos explicitamente
  adiados (Foundation v2, VI.2). Não introduzir sem necessidade
  operacional comprovada e ADR correspondente.

## Estrutura do repositório

```
docs/adr/              → ADRs ratificados (fonte de verdade normativa)
docs/lessons-learned/   → LL-001 a LL-008 (contexto histórico e critérios derivados)
docs/foundation/        → TRS_Foundation_v2.md (requisitos AR-*, roadmap) e
                          TRS_Architecture_Definition.md (arquitetura-alvo de longo
                          prazo da solution — ADR-0014)
migrations/             → schema PostgreSQL, com RLS desde a primeira migração
src/tenancy/            → Module `tenancy` (Aggregates Tenant, Company — ADR-0013) — Bounded Context Trust & Governance
src/identity/           → Module `identity` (Aggregate User) — Bounded Context Trust & Governance
src/sales/              → Module `sales` (Aggregates SalesOrder, Customer) — Bounded Context Sales
tests/rls/              → teste de CI que falha o build se uma tabela com tenant_id não tiver RLS
```

**Layout atual vs. layout-alvo (ADR-0014):** a estrutura acima (`src/tenancy/`,
`src/identity/`, `src/sales/`, plana) é a implementação real e válida da Fase
1. `TRS_Architecture_Definition.md` define um layout aninhado de longo prazo
(`TRS.BuildingBlocks`/`TRS.Kernel`/`Modules`/`Processes`/`TRS.Infrastructure`)
como destino, não como obrigação imediata — a migração para ele só deve ser
decidida quando um segundo módulo de negócio real (além de `sales`) entrar em
implementação (ver Critérios para Revisão Futura do ADR-0014). Não
reestruturar `src/` antecipadamente com base só na arquitetura-alvo.

## Gate da Fase 1 (critério de saída, não lista de tarefas)

Nenhuma operação relevante ocorre sem: autorização registrada (role
`tenant_admin`/`member` verificado), autor identificado, e motivo
capturado (`reason_code`, com `human_statement` quando aplicável) — na
criação e em toda alteração posterior, via `audit_events`/
`audit_event_changes` (ADR-0012). Isso deve ser um teste automatizado,
não uma checagem manual.
