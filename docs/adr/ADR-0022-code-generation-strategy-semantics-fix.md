# ADR-0022 — Correção: Semântica de HighPerformance vs. TransactionalGapless

| Campo | Valor |
|---|---|
| **Status** | Aceito (corrige ADR-0020 — o mecanismo físico descrito não correspondia ao comportamento declarado para `high_performance`) |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-004 (Concurrency and Transactions), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-TXN-002, AR-CHG-003 |
| **Fase do roadmap** | 1 |
| **Corrige** | ADR-0020 (mecanismo de `high_performance`/`transactional_gapless`) |
| **Depende de** | ADR-0011, ADR-0020 |

## Contexto

Auditoria de consistência encontrou uma contradição técnica real no ADR-0020: a estratégia `high_performance` foi descrita como permitindo gaps em caso de rollback ("aceito, prioriza concorrência"), mas o mecanismo físico especificado — `SELECT ... FOR UPDATE`/`UPDLOCK` sobre a linha de sequência, **na mesma transação que persiste o registro** — na verdade produz o comportamento oposto: se o incremento de `next_number` ocorre dentro da mesma transação que a operação de negócio, um `ROLLBACK` desfaz os dois juntos, e o número correspondente nunca é de fato consumido (nenhum gap). O texto descrevia as propriedades de um mecanismo (reserva fora da transação principal, maior throughput, gaps possíveis) mas especificava a implementação de outro (lock mantido pela transação inteira, sem gaps por rollback).

Rodando o checklist do `CLAUDE.md`:

1. **Lessons Learned:** LL-004 toca diretamente — é exatamente sobre isso que a lição trata: comportamento transacional mal especificado entre módulos/operações concorrentes. LL-007 toca porque corrige um ADR recém-ratificado.
2. **Requisitos AR-\*:** AR-TXN-002 exige que o comportamento sob concorrência seja especificado sem ambiguidade — a versão anterior não permitia implementar `high_performance` corretamente porque descrição e mecanismo se contradiziam.
3. **Modo de falha específico:** implementar literalmente o que estava escrito produziria, na prática, o comportamento de `transactional_gapless` rotulado como `high_performance` — nome e comportamento discordantes, o que confundiria qualquer decisão futura de qual estratégia usar (a "estratégia de alta performance" real, com lock breve, nunca teria sido implementada).
4. **Pertence à fase atual?** Sim — corrige definição antes de qualquer código de geração de código de negócio existir.

## Decisão

As duas estratégias diferem na **duração do lock/transação sobre a linha de sequência**, não em qual cálculo é usado — as duas usam o mesmo contador `next_number` de `config_code_sequences` (ADR-0020); a proposta anterior de `MAX(número já persistido) + 1` para `transactional_gapless` é abandonada (adicionava um segundo mecanismo sem necessidade).

| Estratégia | Mecanismo | Efeito em rollback | Concorrência |
|---|---|---|---|
| **`high_performance`** (default) | Incremento de `next_number` ocorre em transação **própria, curta, commitada imediatamente** — separada da transação de negócio que efetivamente persiste o registro (ex.: uma chamada a um serviço de geração de código que abre/commita sua própria transação, antes ou independente da transação que insere o `Customer`/`SalesOrder`) | Se a transação de negócio falhar depois, o número já foi commitado e consumido — **gap possível**, aceito | Lock mantido só pelo instante da geração, não pela duração da operação de negócio inteira — maior throughput |
| **`transactional_gapless`** | Incremento de `next_number` ocorre **dentro da mesma transação** que persiste o registro de negócio, lock mantido até o fim dessa transação | Rollback da transação de negócio desfaz o incremento junto — **sem gap** | Lock mantido pela duração inteira da transação de negócio — menor throughput sob concorrência alta |

Ambas usam `SELECT ... FOR UPDATE` (PostgreSQL) / `SELECT ... WITH (UPDLOCK, ROWLOCK)` (SQL Server) sobre a linha `(tenant_id, entity_type, scope_id)` — o que muda é **quando essa transação de lock é aberta e fechada em relação à transação de negócio**, não a instrução em si.

### Implicação de implementação

`high_performance` exige que o componente de geração de código seja capaz de abrir e commitar sua própria transação/conexão, independente da transação ambiente da operação de negócio (ex.: um serviço dedicado, não uma chamada dentro do mesmo `DbContext.SaveChangesAsync()` da operação principal). `transactional_gapless` exige o oposto: geração de código **precisa** participar da mesma transação/`DbContext` da operação de negócio — chamá-lo fora dela quebraria a garantia de gapless.

## Consequências

- `docs/adr/ADR-0020-business-code-generation-policy.md` precisa de nota de revisão no cabeçalho de status, apontando para este ADR.
- Qualquer implementação futura de geração de código precisa decidir, por chamada, se está participando da transação ambiente (`transactional_gapless`) ou abrindo a sua própria (`high_performance`) — isso é uma decisão de código explícita, não um detalhe de configuração que o banco resolve sozinho.
- Testes de concorrência (quando a implementação retomar) precisam verificar especificamente que `high_performance` de fato permite gap sob rollback simulado, e que `transactional_gapless` de fato não permite — não basta testar o caminho feliz.

## Riscos

- `high_performance` com transação própria exige disciplina de implementação (não vazar a chamada para dentro da transação ambiente por engano) — sem teste automatizado específico, um desenvolvedor poderia implementar as duas estratégias de forma idêntica sem perceber a diferença.

## Alternativas Rejeitadas

- **Manter `MAX(número já persistido) + 1` como mecanismo de `transactional_gapless`** — rejeitada por introduzir um segundo mecanismo de cálculo sem necessidade, quando manter o mesmo contador com transação de duração diferente resolve com menos superfície de código.
- **Unificar as duas estratégias em uma só, sempre com transação própria e curta** — rejeitada por eliminar a garantia gapless que `transactional_gapless` existe para oferecer a domínios que precisam de continuidade (ex.: numeração regulada, ainda que `regulated` seja estratégia própria — alguns casos não regulados também podem precisar de continuidade simples).

## Critérios para Revisão Futura

- Revisar quando a primeira implementação real de geração de código (Fase 1, quando `sales`/`parties` retomarem) expuser dificuldade prática em abrir transação própria dentro do padrão de Repository/DbContext já adotado (ADR-0011) — pode exigir um mecanismo de infraestrutura dedicado, não só uma decisão de código por chamada.
