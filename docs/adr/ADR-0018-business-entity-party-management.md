# ADR-0018 — BusinessEntity, Party Management e Relação com Customer e Company

| Campo | Valor |
|---|---|
| **Status** | Aceito (revisa parcialmente ADR-0006 — adiciona Bounded Context Party Management — e ADR-0009 — revisa a definição de `Customer`). **Corrigido por ADR-0021**: a definição física de `IdentifierType` como tabela única com escopo por linha (`scope`: `official`/`custom`) violava a regra de classificação por tabela do ADR-0017 — passa a ser duas tabelas físicas (`platform_identifier_types`, `deployment_identifier_types`); o conceito de domínio (`IdentifierType`) permanece o mesmo, só a realização física muda. |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-002 (Business Rule Fragmentation), LL-005 (Customization Debt), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-RUL-001, AR-EXP-005, AR-CHG-003 |
| **Fase do roadmap** | 1 (revisão de `Customer`, ratificado no ADR-0009; nenhum código existe ainda para migrar — ver ADR-0015) |
| **Revisa** | ADR-0006 (parcialmente — novo Bounded Context), ADR-0009 (parcialmente — redefine `Customer`) |
| **Depende de** | ADR-0006, ADR-0007, ADR-0009, ADR-0011, ADR-0013, ADR-0017 |

## Contexto

`docs/foundation/TRS_Cadastros_Consolidacao_Conceitual.md` propôs `BusinessEntity` como identidade central de pessoa/organização com quem o negócio se relaciona, com papéis (`Customer`, `Supplier`, `Partner`) desacoplados da identidade. Isso expôs uma tensão real com o modelo ratificado: o ADR-0009 já define `Customer` como Aggregate Root de Sales, com `name` e `tax_id` (`TaxId` Value Object) embutidos diretamente nele.

Rodando o checklist do `CLAUDE.md` (Foundation v2, Parte IX.1):

1. **Lessons Learned:** LL-002 toca diretamente — se `Customer` (Sales) mantém sua própria cópia de `tax_id`/`name`, e no futuro `Supplier` (Procurement, ainda não existe) precisar da mesma informação, cada módulo replicaria a mesma identidade com sua própria cópia, exatamente o padrão que LL-002 nomeia como origem de inconsistência. LL-005 toca porque, sem um lugar central pra identidade, cada cliente/domínio acaba inventando sua própria extensão pontual pra resolver isso. LL-007 toca porque isto revisa formalmente o ADR-0009.
2. **Requisitos AR-\*:** AR-RUL-001 é o requisito mais direto — decidir onde a identidade "mora" é literalmente uma decisão de local autoritativo de regra (ADR-0008), aplicada agora à modelagem de dado, não só a cálculo/política. AR-TXN-001 exige que `Customer` continue como unidade de consistência transacional válida mesmo referenciando `BusinessEntity` por ID (nunca embutido, mesma disciplina que `SalesOrder.customer_id` já usa).
3. **Modo de falha específico desta solução:** introduzir uma camada de indireção (`Customer` → `BusinessEntity`) sem nenhum segundo consumidor real (`Supplier`/Procurement não existe ainda) é over-engineering se não houver justificativa clara — o risco simétrico ao que motivou esta mudança. A justificativa aqui não é hipotética: a pendência de cadastros que pausou a retomada do módulo `sales` (ver memória de projeto) é exatamente sobre acertar esse modelo antes de reimplementar `Customer` pela segunda vez.
4. **Pertence à fase atual?** Sim — `Customer` é Aggregate ratificado da Fase 1 (ADR-0009); esta é uma correção de modelo antes da reimplementação, não uma antecipação de fase futura. Nenhum código existe hoje para `Customer` (ADR-0015), então o custo desta revisão é zero em termos de migração.

## Decisão

### Novo Bounded Context: Party Management

`BusinessEntity` não pertence a Trust & Governance (que já tem significado próprio — quem é o tenant, quem são os usuários da plataforma) nem a Sales (que é exatamente o contexto que duplicaria identidade se a possuísse). Cria-se o Bounded Context **Party Management**, com o Module `parties`, contendo os Aggregates abaixo.

### Aggregate `BusinessEntity` — Module `parties`, Bounded Context Party Management

- Tenant Scope (ADR-0017): `id`, `tenant_id`, `name`, `status` (`active`\|`inactive`).
- Invariante: pertence a exatamente um Tenant.
- Entity interna: `BusinessEntityIdentifier[]` — `identifier_type_id`, `value` (canônico, sem máscara — ex.: `12345678000190`, nunca `12.345.678/0001-90`).
- Invariante: `(tenant_id, identifier_type_id, value)` único — nenhuma duplicata do mesmo identificador oficial entre `BusinessEntity` do mesmo Tenant.
- Sem Golden Record cross-tenant — `BusinessEntity` do Tenant A é inteiramente independente da do Tenant B, mesmo que representem a mesma empresa do mundo real.
- Eventos: `BusinessEntityCreated`, `BusinessEntityUpdated`. Decision Envelope: não (Fase 2). Audit Record + Rationale conforme padrão do ADR-0009/0012.

### Aggregate `IdentifierType` — Module `parties`

- **Platform Scope** quando oficial (`BR_CNPJ`, `BR_CPF`, `US_EIN`, `US_SSN`...) — sem `tenant_id`, sem RLS, governado e versionado pela TRS (ADR-0017).
- **Deployment Scope** quando personalizado (`CUSTOM_*`) — namespace reservado `CUSTOM_*` obrigatório, disponibilidade controlada por Tenant via tabela de disponibilidade (ADR-0017), gerido só por autoridade administrativa de deployment.
- Campos: `id`, `technical_key` (estável, não gerado por Business Code Generation Policy — ver ADR-0006 vocabulário, `technical_key` é Stable Technical Key, distinto de `code`), `name`, `scope` (`official`\|`custom`), `country_code`, `value_type`, `format_mask`, `validator_key`, `definition_version`, `status` (`active`\|`deprecated`\|`inactive`).
- `definition_version` representa evolução da definição; `status`, estado operacional — conceitos diferentes, não conflar.
- Histórico físico de `definition_version` (Audit Record vs. tabela própria de versões): não decidido nesta fase — ver Critérios para Revisão Futura.

### `EconomicGroup` — reconhecido, não implementado

- Tenant Scope, Module `parties`. Forma conceitual reservada: `id`, `tenant_id`, `name`; `BusinessEntity.economic_group_id` (nullable) quando implementado.
- **Não é criado como Aggregate/tabela nesta fase** — pela meta-regra do ADR-0006, não há caso comercial concreto que exija implementação completa de grupo econômico agora (o próprio documento consolidado já reconhecia isso, Seção 5.3). Reconhecido arquiteturalmente para que sua futura introdução não exija redesenho, sem autorizar código agora.

### `Customer` (ADR-0009) — revisão

`Customer` deixa de ser dono direto de identidade (`name`, `tax_id`) e passa a referenciar `BusinessEntity` por ID — mesma disciplina que `SalesOrder.customer_id` já usa (referência, nunca embutido, AR-TXN-001):

- Campos revisados: `id`, `tenant_id`, `business_entity_id` (referência, `BusinessEntity` do mesmo `tenant_id` — FK composta análoga à do ADR-0013), `status` (`active`\|`inactive` — **status operacional do papel Customer em Sales**, independente do `status` da `BusinessEntity` subjacente), Rationale (`reason_code`, `human_statement`, `author`, `created_at` — inalterado do ADR-0009).
- **Removidos:** `name`, `tax_id: TaxId`. Passam a viver em `BusinessEntity`/`BusinessEntityIdentifier`.
- **Invariante superada:** "`tax_id` único dentro do `tenant_id`" (ADR-0009) é substituída pela invariante de `BusinessEntityIdentifier` (`(tenant_id, identifier_type_id, value)` único) — não é mais responsabilidade de `Customer`.
- `SalesOrder.customer_id` continua referenciando `Customer` (não `BusinessEntity` diretamente) — Sales preserva ownership do que "Customer" significa operacionalmente no seu próprio contexto (ADR-0006: "Cliente" em Sales pode ter atributos diferentes de "Cliente" em outro contexto), podendo adicionar campos Sales-específicos (limite de crédito, tabela de preço) no futuro sem tocar `BusinessEntity`.

### `Company` (ADR-0013) — sem relação com `BusinessEntity`

`Company` representa a estrutura legal **interna** do próprio Tenant (matriz/filial — "quem somos"); `BusinessEntity` representa terceiros com quem o Tenant se relaciona (clientes, fornecedores — "com quem nos relacionamos"). São propósitos semanticamente diferentes. Esta decisão: **não** unificar nem cruzar referência entre os dois nesta fase — `Company` mantém exatamente o schema já ratificado no ADR-0013 (`tax_id` como campo próprio, não via `BusinessEntityIdentifier`). Uma eventual necessidade real de tratar uma `Company` como contraparte de outra (ex.: transação intercompany) é decisão futura, fora do escopo deste ADR.

## Consequências

- `docs/adr/ADR-0009-domain-model-fase1.md` precisa de nota de revisão em seu cabeçalho de status, apontando este ADR, análoga à que o ADR-0013 já recebeu.
- Nenhum código precisa migrar — `Customer` nunca foi reimplementado desde a reversão do ADR-0015; esta é a definição a implementar quando `sales` retomar.
- A Fase 1 passa a ter um Bounded Context a mais (Party Management) e dois Aggregates a mais (`BusinessEntity`, `IdentifierType`) além dos cinco já ratificados (`Tenant`, `Company`, `User`, `SalesOrder`, `Customer`).
- Todo consumidor futuro de identidade de terceiros (ex.: um futuro módulo `procurement` com `Supplier`) referencia `BusinessEntity` por ID, do mesmo jeito que `Customer` passa a fazer agora — não duplica identidade.

## Riscos

- Camada de indireção (`Customer` → `BusinessEntity`) sem segundo consumidor real ainda implementado (`Supplier` não existe) — aceito como aposta informada, não hipotética: a pendência de cadastros que pausou `sales` é precisamente sobre isso.
- `IdentifierType` com `definition_version` sem histórico físico decidido é uma lacuna real, não crítica — mitigada por não bloquear a Fase 1 (não há caso de mudança de definição oficial ainda).
- `EconomicGroup` "reconhecido mas não implementado" corre o risco de nunca ser revisitado se o critério de revisão não for de fato monitorado — mesmo risco que ADR-0013 já assume para holding-de-holding.

## Alternativas Rejeitadas

- **Manter `Customer` como está no ADR-0009** (identidade embutida) — rejeitada por ser exatamente o padrão que originou a pendência de cadastros e o risco de LL-002 se um segundo contexto precisar da mesma identidade.
- **Unificar `Company` e `BusinessEntity` em um único conceito** — rejeitada por misturarem propósitos de ownership diferentes ("quem somos" vs. "com quem nos relacionamos"), o que quebraria as invariantes de hierarquia matriz/filial já ratificadas no ADR-0013 sem benefício correspondente.
- **Implementar `EconomicGroup` completo agora, já que o modelo já prevê o campo** — rejeitada pela meta-regra do ADR-0006: nenhum caso de uso comercial concreto o exige ainda.

## Critérios para Revisão Futura

- Revisar quando um segundo Bounded Context consumidor de `BusinessEntity` for implementado (ex.: `Supplier` em um futuro módulo `procurement`) — confirmar que a referência por ID se sustenta sem necessidade de campos adicionais em `BusinessEntity` específicos daquele contexto.
- Revisar `EconomicGroup` quando aparecer caso real de agrupamento econômico (não hipotético).
- Definir a forma física do histórico de `definition_version` de `IdentifierType` antes da primeira mudança real de definição oficial acontecer.
- Revisar a separação `Company`/`BusinessEntity` se aparecer caso real de transação intercompany que precise tratar uma `Company` como contraparte.
