# ADR-0017 — Platform, Deployment e Tenant Scope (Hierarquia de Ownership de Dados)

| Campo | Valor |
|---|---|
| **Status** | Aceito (revisa parcialmente ADR-0007 e ADR-0006 — introduz um terceiro nível de escopo entre "raiz da fronteira" e "dado de Tenant") |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-001 (Parameter Explosion), LL-005 (Customization Debt), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-TXN-002, AR-KNW-003, AR-EXP-005, AR-EXT-001, AR-CHG-003 |
| **Fase do roadmap** | 0 (mesma categoria do ADR-0007 original — decisão de isolamento, cara de reverter, bloqueia os ADRs subsequentes de `docs/foundation/TRS_Cadastros_Consolidacao_Conceitual.md`) |
| **Revisa** | ADR-0007 (parcialmente — adiciona um nível de escopo, RLS de Tenant permanece intacto), ADR-0006 (parcialmente — adiciona vocabulário) |
| **Depende de** | ADR-0006, ADR-0007, ADR-0011 |

## Contexto

`docs/foundation/TRS_Cadastros_Consolidacao_Conceitual.md` identificou que o modelo de isolamento atual (ADR-0007/ADR-0011) só reconhece dois níveis: `Tenant` (dado de negócio, `tenant_id` + RLS/Security Policy obrigatórios) e a "raiz da fronteira" (só o próprio `Tenant`, sem `tenant_id`, por não ser dado dentro da fronteira). Isso não é suficiente para dois casos reais identificados:

1. **Definições oficiais governadas pela TRS** (ex.: `IdentifierType` como `BR_CNPJ`, `BR_CPF`) — não são dado de nenhum Tenant específico, mas também não são a raiz da fronteira. São conteúdo de referência, versionado pela TRS, que todo Tenant de um deployment enxerga igualmente.
2. **Definições criadas pelo cliente, compartilhadas entre alguns (não todos) os Tenants de um mesmo deployment** (ex.: um `CustomIdentifierType` disponível para os Tenants A e B, mas não para o C, todos na mesma base de dados). Isso não é dado de Tenant único (mais de um Tenant o compartilha) nem dado de Platform (a TRS não o governa).

Rodando o checklist do `CLAUDE.md` (Foundation v2, Parte IX.1):

1. **Lessons Learned:** LL-005 toca diretamente — dado compartilhável por deployment, sem governança explícita, é exatamente o tipo de customização que degenera em dívida técnica se cada cliente puder criar isso livremente sem regra. LL-001 toca porque, sem uma regra de "quando algo é Deployment Scope", esse nível vira válvula de escape para qualquer parâmetro que alguém não quis modelar direito. LL-007 toca porque este ADR revisa formalmente o ADR-0007, que é a decisão mais cara de reverter do projeto.
2. **Requisitos AR-\*:** AR-TXN-001/002 (autoridade de escrita, isolamento por agregado) precisam se aplicar aos três níveis, não só a Tenant. AR-KNW-003 (filtros tenant-aware) precisa de uma variante para o novo nível. AR-EXT-001 (extensões não alteram core) é relevante porque `CustomIdentifierType` é, na prática, uma forma controlada de customização por cliente — precisa das mesmas garantias de não vazar para o core.
3. **Modo de falha específico desta solução:** um terceiro nível de escopo mal implementado pode (a) virar uma segunda porta de vazamento de dado entre Tenants, além da que RLS já protege, se a política de "quais Tenants veem este registro" for implementada errado; ou (b) virar exatamente o parâmetro-de-exceção que LL-001 descreve, se "Deployment Scope" for usado como desculpa para não modelar direito uma regra de Tenant.
4. **Pertence à fase atual?** Sim, no sentido de que bloqueia decisões subsequentes já identificadas (BusinessEntity, Data Lifecycle Policy, Business Code Generation Policy dependem de saber em qual escopo cada dado vive) — mas não é urgência de implementação, é fundação conceitual, igual o ADR-0007 original foi antes de qualquer `SalesOrder` existir.

## Decisão

### Vocabulário (revisão do ADR-0006)

Adotar três termos de escopo, como classificação de **ownership de dado**, não como Aggregate nem Module:

| Termo | Definição | É Aggregate/tabela em Fase 1? |
|---|---|---|
| **Platform Scope** | Dado/definição governado pela TRS, distribuído a todo deployment, idêntico para todos os Tenants que o enxergam. | Não — é uma classificação aplicada a tabelas específicas (ex.: `identifier_types` oficiais), não um Aggregate `Platform` próprio. |
| **Deployment Scope** | Dado criado pelo cliente/administrador do deployment, compartilhável entre um subconjunto explícito de Tenants do mesmo deployment (banco de dados). | Não — é uma classificação aplicada a tabelas específicas, não um Aggregate `Deployment` próprio. |
| **Tenant Scope** | Dado de negócio pertencente a exatamente um Tenant — o modelo já existente, inalterado (ADR-0007/0011). | Não se aplica — é o modelo atual. |

`Platform` e `Deployment` **não viram Aggregates nem tabelas de gestão** nesta fase — não existe caso de uso concreto do roadmap (ADR-0006, meta-regra de introdução de conceitos) que exija uma tabela `deployments` ou um console de gestão multi-deployment. Continuam sendo rótulos de ownership até que apareça esse caso de uso real.

### Realização física (revisão do ADR-0007)

Cada nível de escopo tem uma estratégia de isolamento própria — nenhuma tabela nova é "tenant-scoped por padrão silencioso" (a classificação deve ser explícita, por tabela):

**Platform Scope:**
- Tabela sem `tenant_id`.
- **Sem RLS/Security Policy** — não há nada tenant-específico a proteger, já que todo Tenant do deployment enxerga o mesmo conteúdo por design.
- Escrita restrita ao processo de distribuição/migração da TRS (não exposta como Capability de escrita para usuário de Tenant) — equivalente, em espírito, a como `audit_events` só é escrito via `IAuditService` (ADR-0012): mecanismo central, não acesso direto.
- Isso é uma classificação **diferente** da exceção de `Tenant` no ADR-0007 (que é "raiz da fronteira, não dado dentro dela") — aqui é "dado de referência global dentro da fronteira, não específico de nenhum Tenant". As duas exceções a `tenant_id`+RLS coexistem, com motivos diferentes, ambos documentados explicitamente.

**Deployment Scope:**
- Tabela principal sem `tenant_id` fixo (o registro não pertence a um único Tenant).
- Tabela de disponibilidade (`*_tenant_availability` ou equivalente) com `(entity_id, tenant_id)`, listando explicitamente quais Tenants enxergam aquele registro.
- RLS/Security Policy usa `EXISTS` contra a tabela de disponibilidade, em vez de igualdade direta de `tenant_id` — **continua obrigatório, fail-closed** (ausência de linha na tabela de disponibilidade para o tenant atual = zero linhas retornadas), só muda o predicado de `tenant_id = current_setting(...)` para `EXISTS (SELECT 1 FROM ..._tenant_availability WHERE tenant_id = current_setting(...) AND entity_id = ...id)`.
- Escrita/gestão da disponibilidade restrita a uma autoridade administrativa de nível de deployment (não um `member`/`tenant_admin` comum de um Tenant individual — nenhum Tenant altera unilateralmente a definição compartilhada).

**Tenant Scope:** inalterado — `tenant_id` + RLS/Security Policy exatamente como ADR-0007/0011 já definem.

### Teste de CI (consequência para ADR-0007/0011)

O teste de cobertura de isolamento (`tests/rls/`, removido em 2026-07-19 mas a ser recriado) passa a classificar toda tabela em exatamente um dos três escopos, e falhar o build se:
- uma tabela Tenant Scope não tiver `tenant_id` + RLS;
- uma tabela Deployment Scope não tiver a tabela de disponibilidade + política `EXISTS`-based;
- uma tabela não estiver classificada em nenhum dos três (ambiguidade não é permitida).

Isso vale para os dois motores (Postgres RLS e SQL Server Security Policy/`SESSION_CONTEXT`), conforme paridade exigida pelo ADR-0011.

## Consequências

- ADR-0007 passa a ter uma terceira exceção documentada (`Platform Scope`), além da já existente (`Tenant` como raiz da fronteira), e um novo padrão de isolamento (`Deployment Scope`, via tabela de disponibilidade) — nenhuma delas enfraquece a regra original para Tenant Scope, que continua sem exceção.
- Toda tabela nova, a partir de agora, precisa declarar explicitamente seu escopo (Platform/Deployment/Tenant) antes de ser criada — não há mais só uma pergunta binária ("tem `tenant_id`?"), são três categorias.
- `IdentifierType` oficial (Seção 9 do documento consolidado) é Platform Scope; `IdentifierType` personalizado (Seção 10) é Deployment Scope; `BusinessEntity`, `EconomicGroup` e os Aggregates já ratificados (`Tenant`, `User`, `SalesOrder`, `Customer`, `Company`) continuam Tenant Scope.
- O teste de CI de isolamento precisa ser reescrito (será recriado quando a implementação retomar) para cobrir os três escopos, não só dois.

## Riscos

- **Deployment Scope pode virar válvula de escape** para dado que deveria ser Tenant Scope (alguém classifica errado pra "simplificar") — mitigado pela regra explícita já herdada do documento consolidado: "nenhum dado deve ser classificado automaticamente como Deployment-Scoped", exige justificativa por escrito por tabela.
- **Padrão `EXISTS`-based é mais caro de auditar que igualdade simples** — uma política RLS com subquery é mais fácil de errar do que `tenant_id = current_setting(...)`. Mitigado pelo teste de CI obrigatório cobrindo especificamente esse padrão, não só seu resultado agregado.
- **Nenhum caso de uso comercial concreto ainda exige `Deployment Scope` na prática** (o exemplo `CustomIdentifierType` é hipotético/ilustrativo até agora) — aceito como fundação antecipada, mas justificado pela mesma honestidade do ADR-0011/0013 (decisão que resolve um caso realista identificado, não hipotético no sentido de "nunca vai acontecer").

## Alternativas Rejeitadas

- **Tratar Deployment Scope como um Tenant "guarda-chuva" fictício** (um pseudo-Tenant que englobaria os Tenants reais) — rejeitada por contaminar o modelo de Tenant real com um conceito que não é, de fato, um Tenant; um relatório consolidado ou uma auditoria que tratasse esse pseudo-Tenant como Tenant real quebraria a garantia de isolamento que RLS foi desenhado para dar.
- **Platform Scope com RLS "sempre permite tudo"** (manter RLS habilitado, mas com política permissiva) — rejeitada por adicionar custo de avaliação de política sem nenhum ganho de segurança real, já que não há nada tenant-específico a filtrar.
- **Deployment Scope via `tenant_id` nulo + fallback na aplicação** (em vez de tabela de disponibilidade + RLS) — rejeitada pelo mesmo motivo que ADR-0011 já rejeitou "filtro só na aplicação" para Tenant Scope: abre mão da garantia de falha fechada no banco.

## Critérios para Revisão Futura

- Revisar se `Deployment` precisar virar Aggregate/tabela real (ex.: um console de gestão multi-deployment for necessário) — não antecipar.
- Revisar quando o primeiro caso real de `Deployment Scope` for implementado (provavelmente `IdentifierType` personalizado) — confirmar que o padrão `EXISTS`-based funciona conforme especificado nos dois motores.
- Revisar esta decisão junto com o ADR de `BusinessEntity` (próximo da sequência), que depende desta hierarquia de escopo estar correta.
