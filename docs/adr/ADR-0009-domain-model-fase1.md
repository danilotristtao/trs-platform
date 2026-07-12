# ADR-0009 — Domain Model Fase 1 (Vertical Zero)

| Campo | Valor |
|---|---|
| **Status** | Aceito (incorpora revisão técnica sobre a versão Proposta anterior — três ajustes descritos na seção "Correção em relação à versão anterior") |
| **Data** | 2026-07-12 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-001, LL-002, LL-003, LL-004, LL-006, LL-007 |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-TXN-007, AR-RUL-001 a 003, AR-KNW-001, AR-KNW-006, AR-EXP-001 (parcial) |
| **Fase do roadmap** | 1 (bloqueia o início de implementação de `Tenant`, `User`, `SalesOrder`, `Customer`) |
| **Depende de** | ADR-0006, ADR-0007, ADR-0008 |

## Contexto

Uma versão "Proposta" deste modelo (`Domain-Model-v2-Fase1.md`) foi
revisada tecnicamente antes da ratificação. A revisão confirmou o
raciocínio central — em particular, a recusa de modelar `Approval`
como Aggregate nesta fase (ver Seção "Approval — deferido") — mas
identificou três pontos subespecificados que precisavam de decisão
explícita antes de virar base de código. Este ADR substitui a versão
Proposta como fonte de verdade.

## Decisão

Adotar os quatro Aggregates abaixo para a Fase 1, com o vocabulário do
ADR-0006 e os três ajustes descritos a seguir.

### Correção em relação à versão anterior (os três ajustes)

**1. Rationale obrigatório não significa `human_statement` obrigatório
em toda criação.** A versão anterior exigia o campo de texto livre em
todo `SalesOrder` criado, inclusive nos casos rotineiros — isso
recriaria, já na Fase 1, o próprio ruído que o LL-003 nomeia como
risco ("preenchimento genérico que não agrega valor real"). Correção:
`reason_code` é sempre obrigatório e estruturado (conforme AR-KNW-006),
mas passa a existir um valor padrão `routine_creation` que **dispensa**
`human_statement`. `human_statement` só é obrigatório quando
`reason_code` indica desvio do fluxo padrão (ex.: `manual_override`,
`exception_approval`, `correction`). Isso mantém AR-KNW-001 satisfeito
(toda criação tem `reason_code`, autor e timestamp) sem forçar texto
humano sem informação real.

**2. Autorização mínima da Fase 1 precisa de uma frase explícita, não
apenas "Permission mínimo".** O gate da Fase 1 exige que nenhuma
operação relevante ocorra sem autorização registrada — uma associação
`User ↔ Role` sem nenhum papel definido não responde "quem pode criar
um `SalesOrder`". Decisão: a Fase 1 define exatamente dois papéis
implícitos, sem motor de regras por trás deles (isto é Authorization
Layer, não Policy Layer — a distinção do ADR-0008 se aplica aqui
também, só que entre dois papéis fixos, não entre condições de valor):

| Role | Pode |
|---|---|
| `tenant_admin` | Criar/desativar `User` dentro do próprio tenant; tudo que `member` pode |
| `member` | Criar/ler/atualizar `SalesOrder` e `Customer` dentro do próprio tenant |

Isso não é RBAC/ABAC contextual (que continua Fase 2, AR-RUL-001,
AR-EXP-005) — é a resposta mínima e fixa de "este papel pode tentar
executar esta Capability", sem nenhuma condição de valor/contexto de
recurso ainda.

**3. Invariante de moeda única em `SalesOrder`.** A invariante "total =
soma dos totais de linha" pressupõe implicitamente uma moeda comum.
Adiciono uma quarta invariante explícita: todas as `SalesOrderLine` de
um mesmo `SalesOrder` DEVEM compartilhar a mesma moeda; uma tentativa
de adicionar linha com moeda diferente é rejeitada pelo próprio
Aggregate — não é validação de UX (frontend), é integridade estrutural
do dado (`Money` como Value Object só é somável dentro da mesma
moeda).

### Modelo completo (com os ajustes já incorporados)

#### Tenant — Trust & Governance / Module `tenancy`
- Aggregate Root `Tenant`. Sem `tenant_id` próprio (é a raiz da
  fronteira de isolamento, não dado de negócio dentro dela — leitura
  correta do ADR-0007, não uma exceção a ele).
- Campos: `id`, `name`, `status` (`active`\|`suspended`), `created_at`.
- Invariante: transição de estado válida; identidade única.
- Eventos: `TenantCreated`, `TenantStatusChanged`. Event Scope: Trust &
  Governance, consumidor = Audit. Sem outbox (Fase 3).
- Decision Envelope: não (Fase 2). `TenantCreated` gera Audit Record +
  Rationale (`reason_code` obrigatório; `routine_creation` aplicável a
  onboarding padrão).

#### User — Trust & Governance / Module `identity`
- Aggregate Root `User`. VO interno: `EmailAddress`.
- Campos: `id`, `tenant_id` (obrigatório, ADR-0007), `external_identity_reference`,
  `email: EmailAddress`, `name`, `status`, **`role`** (`tenant_admin` \|
  `member` — ver ajuste 2 acima).
- Invariante: pertence a exatamente um Tenant; `EmailAddress` com
  formato estruturalmente válido.
- Eventos: `UserCreated`, `UserDeactivated`. Event Scope: Trust &
  Governance, consumidor = Audit.
- Decision Envelope: não. Audit Record + Rationale (`reason_code`
  padrão aplicável).

#### SalesOrder — Sales / Module `sales`
- Aggregate Root `SalesOrder`. Entity interna: `SalesOrderLine`. VO
  interno: `Money` (amount + currency).
- Campos: `id`, `tenant_id` (obrigatório), `customer_id` (referência
  por ID, nunca embutido — AR-TXN-001), `lines: SalesOrderLine[]`,
  `total: Money` (calculado), `status` (`draft`\|`active` — sem estados
  de aprovação, ver Seção "Approval — deferido"), e Rationale de
  criação: `category`, `reason_code` (default `routine_creation`),
  `human_statement` (condicional — ver ajuste 1), `source_reference`,
  `author`, `created_at`, `validity` (tipicamente nulo), `confidentiality_level`.
- Invariantes:
  1. Ao menos uma `SalesOrderLine` para ser válido.
  2. `total` = soma dos totais de linha — cálculo decisório no
     Aggregate (ADR-0008), não Projection Layer.
  3. `tenant_id` de toda linha idêntico ao do pedido.
  4. **(novo)** Todas as linhas compartilham a mesma moeda.
- **Explicitamente não invariante:** limiar de aprovação
  (`sales.approval.threshold`) é Política (ADR-0008), não código do
  Aggregate.
- Autorização mínima: `Capability` "criar SalesOrder" exige role
  `member` ou `tenant_admin` no `tenant_id` do próprio recurso — sem
  condição de valor (isso é Policy, Fase 2).
- Eventos: `SalesOrderCreated`, `SalesOrderUpdated`. Sem
  `Submitted`/`Approved`/`Rejected` (pressupõem workflow, Fase 3). Sem
  outbox (Fase 3).
- Decision Envelope: não (Fase 2). Audit Record + Rationale conforme
  ajuste 1.

#### Customer — Sales / Module `sales`
- Aggregate Root `Customer`, escopado ao Bounded Context de Sales —
  não é cliente universal cross-context (ver ADR-0006, exemplo
  "Cliente" em Vendas vs. Cobrança).
- Campos: `id`, `tenant_id` (obrigatório), `name`, `tax_id: TaxId`,
  `status`.
- Invariante: `tax_id` único dentro do `tenant_id`.
- Autorização mínima: mesma regra de `SalesOrder` (`member` ou
  `tenant_admin`).
- Eventos: `CustomerCreated`, `CustomerUpdated`. Decision Envelope: não.

### Approval — deferido (mantido da versão anterior, sem alteração)

Não modelado como Aggregate nesta fase. `sales.approval.threshold` é
exemplo ilustrativo da fronteira Política/Invariante no ADR-0008, não
autorização para adiantar o Aggregate. Nasce como Policy na Fase 2
(consultando `SalesOrder.total`, já corretamente calculado desde esta
fase) e como Workflow na Fase 3.

## Consequências

- `reason_code` passa a ter um vocabulário controlado mínimo desde a
  Fase 1 (`routine_creation`, `manual_override`, `exception_approval`,
  `correction`, extensível) — isso deveria ser registrado como parte
  do schema de Audit Record, não inventado ad-hoc por desenvolvedor.
- `role` em `User` é um campo fixo de dois valores, não uma tabela de
  papéis configurável — qualquer necessidade de papel adicional antes
  da Fase 2 deve ser tratada como sinal de que a Policy Engine
  precisa chegar mais cedo, não como motivo para expandir este enum
  informalmente.
- Toda implementação de `SalesOrder` DEVE rejeitar, no próprio
  Aggregate, uma tentativa de adicionar linha com moeda diferente das
  demais — teste de invariante obrigatório em CI, no mesmo espírito do
  teste de RLS do ADR-0007.

## Riscos

- Vocabulário fechado de `reason_code` pode ficar insuficiente rápido
  demais (ex.: primeiro caso real de desconto manual que não se encaixa
  em nenhum código existente) — mitigação: tratar isso como sinal
  esperado de entrada na Fase 2, não como falha do modelo da Fase 1.
- Os dois `role`s fixos não têm nenhuma verificação de contexto de
  recurso (ex.: um `member` de um tenant não pode, por definição,
  acessar dado de outro tenant — isso já é garantido por RLS/ADR-0007,
  não pelo `role` em si; o `role` só decide "pode tentar", nunca
  "pode tentar sobre qual recurso").

## Alternativas Rejeitadas

- Exigir `human_statement` em toda criação, sem exceção — rejeitada
  por recriar o ruído que LL-003 identifica como risco da própria
  solução.
- Modelar RBAC/ABAC contextual completo já na Fase 1 — rejeitada por
  antecipar Policy Engine (Fase 2) sem caso de uso executável ainda.
- Permitir `SalesOrderLine` com moedas distintas e resolver conversão
  na camada de leitura — rejeitada por esconder um cálculo
  potencialmente decisório (conversão de moeda) na Projection Layer,
  o mesmo erro que o ADR-0008 já corrigiu para preço/total.

## Critérios para Revisão Futura

- Revisar o vocabulário de `reason_code` quando aparecer o primeiro
  caso real não coberto por `routine_creation`/`manual_override`/
  `exception_approval`/`correction`.
- Revisar os dois `role`s fixos quando a Fase 2 (Policy Engine) tornar
  necessária uma terceira distinção real de autorização — não antes.
- Revisar a decisão de não modelar `Approval` apenas quando a Fase 2
  ou 3 do roadmap forem formalmente iniciadas (não antes).
