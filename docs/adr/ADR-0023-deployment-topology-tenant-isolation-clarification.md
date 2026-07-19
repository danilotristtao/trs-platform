# ADR-0023 — Esclarecimento: Topologia de Deployment vs. Estratégia de Isolamento de Tenant

| Campo | Valor |
|---|---|
| **Status** | Aceito (esclarece ADR-0007 — não reverte RLS obrigatório, distingue topologia de infraestrutura de mecanismo de isolamento) |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-004 (Concurrency and Transactions), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-TXN-002, AR-CHG-003 |
| **Fase do roadmap** | 0/1 (esclarece decisão fundacional) |
| **Esclarece** | ADR-0007 (sem reverter a obrigatoriedade de RLS) |
| **Depende de** | ADR-0007, ADR-0011, ADR-0017 |

## Contexto

Auditoria de consistência apontou uma tensão real entre dois documentos: o ADR-0007 afirma, em termos absolutos, "não há database dedicado no MVP — nenhuma exceção", e rejeita explicitamente "database per tenant desde o início" como alternativa. O `TRS_Cadastros_Consolidacao_Conceitual.md`, por outro lado, descreve o cenário comercial inicial mais provável como `1 Cliente TRS → 1 Deployment próprio → 1 Banco de dados próprio → Tenant` — ou seja, cada cliente tipicamente recebe seu próprio banco de dados, dentro do qual normalmente existe um único Tenant no início.

Na prática, isso significa que a topologia comercial mais comum tende a resultar, fisicamente, em "um banco por cliente" — o mesmo resultado físico que "database per tenant" produziria, ainda que por um motivo conceitualmente diferente (fronteira de deployment, não de tenant).

Rodando o checklist do `CLAUDE.md`:

1. **Lessons Learned:** LL-007 toca porque a linguagem absoluta do ADR-0007 ("nenhuma exceção") precisa de esclarecimento formal, não reinterpretação informal por quem ler os dois documentos e tirar conclusões diferentes.
2. **Requisitos AR-\*:** AR-TXN-001/002 continuam exigindo que o isolamento seja garantido no banco (RLS), independentemente de quantos Tenants existem fisicamente num dado banco — este ADR confirma isso, não afrouxa.
3. **Modo de falha específico:** o risco real não é a topologia em si (deployment com banco dedicado é uma decisão operacional legítima), é alguém interpretar "este cliente já tem banco próprio" como desculpa para **não implementar RLS** ("total, só tem um Tenant aqui mesmo") — isso violaria o ADR-0007 na prática, mesmo sem violar a letra.
4. **Pertence à fase atual?** Sim — esclarece a base de isolamento antes de qualquer implementação de banco retomar.

## Decisão

Duas perguntas diferentes, que o ADR-0007 original não separou com clareza suficiente:

```text
"Quantos bancos de dados físicos existem, e como são distribuídos entre clientes?"
→ Topologia de infraestrutura/deployment (ADR-0017, Deployment Scope)
→ Decisão operacional/comercial, pode variar por cliente

"Dentro de um banco de dados físico que contém um ou mais Tenants,
como a plataforma garante que um Tenant nunca vê dado de outro?"
→ Estratégia de isolamento (ADR-0007)
→ RLS/Security Policy, sempre, sem exceção, independentemente de
  quantos Tenants aquele banco específico contém hoje
```

O ADR-0007 nunca teve autoridade sobre a primeira pergunta — "database dedicado no MVP" ali significa **a plataforma não provê nem depende de separação física por tenant como seu mecanismo de isolamento**, não que a operação/comercial da TRS esteja proibida de provisionar um banco dedicado a um deployment por qualquer motivo (performance, contrato, regulação, ou simplesmente porque o cenário comercial inicial mais comum resulta nisso, como o ADR-0017 já reconhece).

**Reformulação explícita, substituindo a leitura absoluta anterior:**

> A unidade física de deployment (ADR-0017) pode possuir banco de dados dedicado — isso é decisão de topologia de infraestrutura, não de arquitetura de isolamento. Dentro de qualquer banco, independentemente de conter um ou vários Tenants hoje, Tenant Scope permanece obrigatoriamente protegido por `tenant_id` + RLS/Security Policy, sem exceção — a mesma garantia de falha fechada do ADR-0007 original, agora explicitamente independente da topologia de deployment.

O que continua proibido, sem exceção, exatamente como o ADR-0007 original: usar a separação física (banco dedicado) **como substituto** de RLS dentro daquele banco — "só tem um Tenant aqui, não precisa de política de isolamento" é uma violação, mesmo que hoje seja tecnicamente verdade que só existe um Tenant naquele banco, porque o mesmo deployment pode receber um segundo Tenant no futuro sem migração de schema.

## Consequências

- `docs/adr/ADR-0007-tenant-isolation-strategy.md` recebe mais uma nota de revisão no cabeçalho de status, apontando para este ADR.
- Nenhuma mudança de comportamento técnico — RLS continua obrigatório em toda tabela Tenant Scope, em todo banco, sempre. A mudança é de precisão de linguagem, não de garantia.
- Decisões comerciais/operacionais sobre quantos deployments/bancos provisionar por cliente ficam explicitamente fora do escopo do ADR-0007 — pertencem à camada de infraestrutura/ops, informada pelo modelo do ADR-0017.

## Riscos

- Esclarecer que "banco dedicado por deployment é permitido" pode ser mal-lido, sem o parágrafo de proibição explícita acima, como "então RLS não é tão necessário assim" — mitigado por deixar a frase de proibição no mesmo parágrafo da permissão, não em seção separada.

## Alternativas Rejeitadas

- **Não esclarecer nada, deixar a tensão como está** — rejeitada porque um documento (ADR-0007) em termos absolutos e outro (documento consolidado, base do ADR-0017) descrevendo o cenário oposto como comum, sem reconciliação, é exatamente o tipo de inconsistência documental que este projeto tem investido em eliminar.
- **Reverter a obrigatoriedade de RLS quando o deployment tiver banco dedicado com um único Tenant** — rejeitada porque abre uma exceção real de segurança baseada em uma condição que muda (quantos Tenants existem hoje), a mesma falha de design que motivou a rejeição de "database per tenant" no ADR-0007 original.

## Critérios para Revisão Futura

- Revisar se, na prática comercial real (Fase 7, piloto), a maioria dos deployments realmente operar com banco dedicado e um único Tenant — nesse caso, confirmar que a disciplina de RLS não afrouxou informalmente ao longo do tempo.
