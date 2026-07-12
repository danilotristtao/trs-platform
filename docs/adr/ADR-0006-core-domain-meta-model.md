# ADR-0006 — Core Domain Meta Model

| Campo | Valor |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-07-12 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-002 (Business Rule Fragmentation), LL-005 (Customization Debt) |
| **Requisitos AR-\* relacionados** | AR-RUL-001, AR-EXT-002, AR-TXN-001, AR-EXP-001 |
| **Fase do roadmap** | 0 (bloqueia o início da Fase 1) |

## Contexto

Os documentos da TRS usam termos como "entidade", "capability", "módulo",
"bounded context" e "extensão" de forma intercambiável e sem definição
formal. Sem uma gramática comum, cada componente do Kernel corre o risco
de inventar sua própria ontologia — seria LL-002 (fragmentação de
regras) se repetindo na camada de modelagem, antes mesmo de qualquer
regra de negócio existir.

## Decisão

Adotar as seguintes definições como vocabulário obrigatório de todo o
Kernel, sem sinônimos informais:

| Termo | Definição |
|---|---|
| **Entity** | Um objeto de negócio com identidade própria e ciclo de vida (ex: `SalesOrder`, `Customer`). Possui um ID estável e pode mudar de estado ao longo do tempo. |
| **Value Object** | Um objeto sem identidade própria, definido inteiramente pelo valor de seus atributos (ex: um `Endereço` ou um `Intervalo de Datas` dentro de um pedido). Dois Value Objects com os mesmos atributos são intercambiáveis; não têm ciclo de vida próprio e não são rastreados por ID — vivem sempre dentro de uma Entity ou Aggregate. |
| **Aggregate** | Um conjunto de Entities e Value Objects tratados como uma unidade de consistência transacional, com uma única Entity raiz (Aggregate Root) responsável por proteger os invariantes internos (ver AR-TXN-001). |
| **Capability** | Um caso de uso de negócio exposto pela plataforma — nomeado, independente e observável de fora (ex: "Aprovação de Pedido", "Cálculo de Frete"). É o que efetivamente aparece em um manifesto de extensão (AR-EXT-002) e o que um cliente ou parceiro reconheceria como "uma coisa que a TRS faz". Pode ser composto de políticas, workflows e comportamento de Aggregate, mas sua fronteira é definida pelo caso de uso, não pela implementação técnica por trás dele. |
| **Module** | Uma unidade de deployment e organização de código dentro do monólito modular — um agrupamento físico de Capabilities relacionadas (ex: módulo `sales`, módulo `finance`). Não tem significado de negócio próprio, é uma fronteira técnica. **Um Module NÃO DEVE atravessar múltiplos Bounded Contexts.** Se uma Capability parece exigir isso (ex: um módulo `sales` que mistura precificação, aprovação e crédito), é sinal de que o módulo está mal recortado e deveria ser dividido ao longo das fronteiras semânticas reais — não que a regra deva ser flexibilizada. |
| **Bounded Context** | Uma fronteira semântica onde um termo de negócio tem um único significado consistente (ex: "Cliente" no contexto de Vendas pode ter atributos diferentes de "Cliente" no contexto de Cobrança). Um Bounded Context pode conter múltiplos Modules; a relação inversa é proibida pela regra acima. |
| **Extension** | Um pacote de comportamento adicional, declarado via manifesto (AR-EXT-002), que consome Capabilities existentes ou registra novas, sem alterar o código do core (AR-EXT-001). |
| **Policy Scope** | O contexto de aplicação de uma política — a combinação de Bounded Context + Aggregate/Entity + condição que define onde uma política é avaliada. |
| **Event Scope** | O domínio de origem e o conjunto de consumidores autorizados de um evento — necessário para que AR-TXN-001 (autoridade de escrita por agregado) seja verificável na prática. |

### Regra adicional — Aggregate Root e Decision Envelope

Toda alteração de estado **decorrente de uma decisão de negócio,
autorização, política, workflow ou intervenção humana relevante** DEVE
produzir um Decision Envelope. Alterações puramente técnicas (ex:
atualizar `last_seen_at`, renovar cache, marcar evento como processado,
correção de dado por job técnico) DEVEM produzir telemetria ou registro
de auditoria técnica conforme sua natureza — não um Decision Envelope
completo.

> Esta regra foi revisada em relação à formulação original ("toda
> alteração de estado gera Decision Envelope"), que era ampla demais e
> geraria volume excessivo, ruído e rationales artificiais para
> operações puramente técnicas. Ver a tabela de distinção de artefatos
> no documento mestre (Parte IV.6) para a fronteira exata entre Decision
> Envelope, Audit Record, Domain Event, Telemetry e Rationale.

### Meta-regra de introdução de conceitos

Nenhum conceito arquitetural novo (além dos definidos nesta tabela)
entra no vocabulário do Kernel sem que exista, no momento da proposta,
pelo menos um caso de uso concreto do roadmap que o exija. Esta regra
existe para conter o risco simétrico ao que motivou este ADR: acumular
vocabulário de framework DDD acadêmico (`CommandBus`, `EventStore`,
`SagaCoordinator`, `ProcessManager`, `SnapshotStore`,
`ProjectionManager`) antes de precisar de qualquer um deles na prática.

**Aplicação desta regra a si mesma:** o conceito de `Command` (como
entidade formal distinta de uma chamada direta a um método do
Aggregate) não é adicionado nesta versão. Não existe, na Fase 1 do
roadmap, caso de uso que exija distinguir "intenção" de "execução" como
objetos separados. Se, ao longo das Fases 2-3, surgir necessidade real
(fila de comandos assíncrona, replay de intenção antes da execução),
este ADR deve ser revisado para incorporá-lo formalmente — não antes.

## Consequências

Todo documento futuro (Domain Model v2, especificações de Capability)
deve usar esses termos exatamente como definidos aqui. Divergência de
vocabulário em qualquer novo documento é, por si só, motivo de revisão.

## Riscos

- Vocabulário insuficiente para casos reais que apareçam na Fase 1-3
  (mitigado pela meta-regra: revisão deste ADR é o mecanismo formal de
  correção, não extensão informal do vocabulário).
- Ambiguidade residual entre Capability e Aggregate para casos de uso
  simples — aceito como risco conhecido; ver item pendente "Capability
  Contract" (não priorizado nesta versão, ver documento mestre Parte
  IX).

## Alternativas Rejeitadas

Adotar terminologia de um framework DDD existente "tal qual" (ex: os
termos do livro de Eric Evans, sem adaptação). Rejeitada porque alguns
conceitos da TRS (Capability como unidade de extensão comercial, Policy
Scope) não têm equivalente direto em DDD clássico e precisavam de
definição própria.

## Critérios para Revisão Futura

- Se um Module precisar legitimamente atravessar Bounded Contexts em
  mais de um caso real (não hipotético), revisar a regra de fronteira.
- Se surgir necessidade real de fila assíncrona ou replay de intenção,
  revisar a decisão de não incluir `Command`.
- Definir o "Capability Contract" (especificação mínima de entradas,
  pré-condições, eventos emitidos, políticas consultadas) antes do
  início da Fase 3, não antes.
