# ADR-0008 — Rule Placement Standard

| Campo | Valor |
|---|---|
| **Status** | Aceito (revisado — versão anterior tratava todo cálculo como Projection Layer, e a fronteira Authorization/Policy era implícita) |
| **Data** | 2026-07-12 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-001 (Parameter Explosion), LL-002 (Business Rule Fragmentation) |
| **Requisitos AR-\* relacionados** | AR-CFG-001, AR-RUL-001, AR-RUL-002 |
| **Fase do roadmap** | 0 (referenciado desde a Fase 1) |

## Contexto

Sem um padrão explícito, existe uma tendência natural e recorrente
(observada tanto nos concorrentes pesquisados quanto na experiência que
originou os Lessons Learned) de empurrar toda e qualquer lógica para
dentro do Policy Engine — preço, aprovação, layout, integração,
validação de campo, workflow — até que o Policy Engine se torne, na
prática, um "ERP configurado em YAML", que é exatamente o problema que
a TRS nasceu para evitar (LL-001, LL-002).

## Decisão

Toda lógica do sistema deve ser classificada em um dos sete tipos
abaixo, cada um com um local autoritativo único:

| Tipo de lógica | Local autoritativo | Exemplo |
|---|---|---|
| **Invariante de domínio** | Código do Aggregate (nunca configurável) | "o saldo calculado deve corresponder integralmente aos movimentos registrados" |
| **Política de negócio** | Policy Engine | "pedidos acima de R$ 10.000 exigem aprovação financeira"; "todo movimento que resulte em estoque negativo deve ter autorização válida, rastreável e vigente" |
| **Processo/Workflow** | Workflow Engine | "sequência de aprovação: gerente → diretor → financeiro" |
| **Validação de interface (UX)** | Frontend, apenas validação cosmética/de usabilidade, nunca de negócio | "campo obrigatório antes de habilitar o botão de envio" |
| **Transformação/Integração** | Adapter dedicado, fora do core | "converter formato de data para o sistema do parceiro X" |
| **Cálculo decisório ou transacional** | Aggregate ou Domain Service autoritativo | preço final, imposto, desconto, juros, limite de crédito, custo, rateio contábil — qualquer cálculo que possa influenciar uma decisão de negócio ou aparecer em uma invariante |
| **Derivação para leitura** | Projection Layer | um total pré-calculado exibido em um dashboard analítico, sem relevância financeira ou decisória própria |

### Correção em relação à versão anterior deste ADR

A versão anterior classificava todo "Derivação/Cálculo" como
responsabilidade da Projection Layer. Isso era impreciso: **nem todo
cálculo é projeção de leitura**. Cálculos que podem influenciar uma
decisão de negócio ou aparecer em uma invariante (preço, imposto,
desconto, juros, limite de crédito, saldo, rateio) devem ocorrer no
Aggregate ou em um Domain Service autoritativo — devem existir como
resultado versionável e reproduzível, não apenas como valor calculado
para exibição. Somente cálculos sem relevância decisória ou financeira
própria (ex: um total pré-agregado para um dashboard, que pode ser
recalculado sem afetar nenhuma decisão) pertencem à Projection Layer.

### Segurança/Autorização — fronteira com Política

**Regra de fronteira:** a **Authorization Layer** decide **se o ator
pode tentar executar a Capability** (identidade + papel + permissão
básica). A **Policy Layer** decide **se a operação é permitida pelas
regras de negócio no contexto atual** (valor, unidade, estado do
recurso). Em muitos casos, ambas precisam aprovar, e isso não é
duplicação — são perguntas diferentes:

> **Authorization:** "Danilo pode executar `sales.order.approve`?"
> **Policy:** "Danilo pode aprovar este pedido específico de R$ 80.000
> nesta unidade?"

Regras que misturam identidade/papel com valor financeiro ou contexto
de recurso (ex: "gerente pode aprovar até R$ 10 mil; diretor até R$ 100
mil") DEVEM ser decompostas: a Authorization Layer resolve "este papel
pode executar esta Capability", e a Policy Layer resolve o limite de
valor específico — a composição das duas é o que determina o resultado
final, mas cada uma vive em sua própria camada, para evitar duplicação
de regra entre elas.

## Regra de Decisão Prática

Diante de qualquer nova lógica: "isto muda uma obrigação, autorização,
risco, preço, limite ou resultado financeiro?" (AR-CFG-002). Se sim, é
Política, Invariante ou Cálculo Decisório — nunca Frontend ou Adapter.
Se a lógica só afeta a experiência de uso da tela, é Frontend. Se só
traduz formato de dado para um sistema externo, é Adapter. Se calcula
um valor que pode aparecer em uma decisão ou invariante, é Cálculo
Decisório — não Derivação. Se decide "quem pode tentar fazer algo"
(sem considerar o contexto específico do recurso), é
Autorização — não Política.

## Consequências

Este ADR é o critério objetivo por trás de AR-RUL-001 e AR-RUL-002 —
deixam de ser requisitos abstratos e passam a ter uma tabela de decisão
verificável em revisão de código e em design review.

## Alternativas Rejeitadas

Tratar todo cálculo como Projection Layer por padrão (versão anterior
deste ADR) — rejeitada por esconder cálculos financeiramente relevantes
atrás de uma camada pensada para leitura, sem versionamento nem
reprodutibilidade.

## Critérios para Revisão Futura

Se surgir um caso onde a fronteira entre Cálculo Decisório e Derivação
for genuinamente ambígua na prática (não hipotética), revisar este ADR
com o caso concreto documentado.
