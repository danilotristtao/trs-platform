# ADR-0024 — IdentifierType: Correções de Precisão (Aggregate × Tabela, Isolamento da Disponibilidade, Unicidade)

| Campo | Valor |
|---|---|
| **Status** | Aceito (corrige ADR-0021 — terceira rodada de auditoria encontrou uma ambiguidade terminológica e duas lacunas reais de isolamento/integridade) |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-004 (Concurrency and Transactions), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-TXN-002, AR-CHG-003 |
| **Fase do roadmap** | 1 |
| **Corrige** | ADR-0021 |
| **Depende de** | ADR-0007, ADR-0008, ADR-0017, ADR-0018, ADR-0021 |

## Contexto

Uma terceira rodada de auditoria sobre o ADR-0021 (que já era, ele mesmo, uma correção) encontrou três problemas adicionais, dois deles reais e confirmados linha por linha, um deles (unicidade) confirmado ao mesmo tempo que uma quarta lacuna relacionada não apontada pela auditoria:

1. **Ambiguidade terminológica:** o ADR-0021 abre a Decisão com "separar em duas tabelas/**Aggregates**", mas a Seção "Conceito unificado permanece na modelagem" do mesmo documento já deixava claro que `IdentifierType` continua sendo **um** Aggregate conceitual, só com realização física em duas tabelas. As duas frases, lidas isoladamente, sugerem coisas diferentes.
2. **Tabela de disponibilidade sem escopo classificado:** `deployment_identifier_type_tenant_availability` (criada no ADR-0021) nunca recebeu classificação explícita de escopo nem política de RLS própria — o ADR-0017 exige que **toda tabela** seja classificada, sem exceção.
3. **Sem índice de unicidade após a divisão:** a invariante original do ADR-0018 (`(tenant_id, identifier_type_id, value)` único) não foi re-expressa para o modelo de duas FKs nuláveis do ADR-0021.
4. **(achado adicional, não apontado pela auditoria) `business_entity_identifiers` sem `tenant_id`/RLS própria:** toda outra tabela filha Tenant Scope do projeto (ex.: `sales_order_lines`, ADR-0009) carrega seu próprio `tenant_id` e não depende só do Aggregate pai — o schema do ADR-0021 para `business_entity_identifiers` não seguiu esse padrão.

Rodando o checklist do `CLAUDE.md`:

1. **Lessons Learned:** LL-004 toca diretamente — tabela sem isolamento classificado é risco de vazamento entre tenants, o tipo de falha de concorrência/consistência que a lição nomeia. LL-007 toca por ser mais uma correção de ADR recente.
2. **Requisitos AR-\*:** AR-TXN-001/002 exigem fronteira de consistência e isolamento inequívocas — os achados 2 e 4 são exatamente isso não estando garantido.
3. **Modo de falha específico:** sem RLS própria em `business_entity_identifiers` e na tabela de disponibilidade, um bug ou acesso direto ao banco (fora do caminho normal de aplicação) vazaria identificadores de outros Tenants — falha silenciosa, só percebida em auditoria ou incidente real.
4. **Pertence à fase atual?** Sim — nenhum código de `IdentifierType`/`BusinessEntityIdentifier` existe ainda; custo de correção é zero em termos de migração.

## Decisão

### 1. Terminologia: um Aggregate, duas tabelas

`IdentifierType` **é um Aggregate conceitual**, cuja realização física se divide em `platform_identifier_types` e `deployment_identifier_types` — nunca "dois Aggregates". A Fase 1 continua com sete Aggregates conceituais (`Tenant`, `User`, `Company`, `SalesOrder`, `Customer`, `BusinessEntity`, `IdentifierType`), consistente com o que `CLAUDE.md` e a Foundation já registram. O ADR-0021 é corrigido nesse ponto específico de redação.

### 2. `deployment_identifier_type_tenant_availability` é Tenant Scope

```sql
ALTER TABLE deployment_identifier_type_tenant_availability ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON deployment_identifier_type_tenant_availability
    USING (tenant_id = current_setting('app.tenant_id')::UUID);
```

Isso é seguro e não quebra o mecanismo `EXISTS` já definido no ADR-0021: a subquery da política de `deployment_identifier_types` já filtra por `tenant_id = current_setting(...)`, então aplicar a mesma restrição diretamente na tabela de disponibilidade apenas reforça (defesa em profundidade), nunca contradiz, o comportamento já especificado.

### 3. `business_entity_identifiers` recebe `tenant_id` próprio e índices de unicidade condicionais

```sql
CREATE TABLE business_entity_identifiers (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                       UUID NOT NULL REFERENCES tenants(id),
    business_entity_id             UUID NOT NULL REFERENCES business_entities(id),
    platform_identifier_type_id    UUID REFERENCES platform_identifier_types(id),
    deployment_identifier_type_id  UUID REFERENCES deployment_identifier_types(id),
    value                           TEXT NOT NULL,

    CONSTRAINT business_entity_identifiers_exactly_one_type
        CHECK (
            (platform_identifier_type_id IS NOT NULL AND deployment_identifier_type_id IS NULL)
            OR
            (platform_identifier_type_id IS NULL AND deployment_identifier_type_id IS NOT NULL)
        )
);

ALTER TABLE business_entity_identifiers ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON business_entity_identifiers
    USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE UNIQUE INDEX business_entity_identifiers_platform_unique
    ON business_entity_identifiers (tenant_id, platform_identifier_type_id, value)
    WHERE platform_identifier_type_id IS NOT NULL;

CREATE UNIQUE INDEX business_entity_identifiers_deployment_unique
    ON business_entity_identifiers (tenant_id, deployment_identifier_type_id, value)
    WHERE deployment_identifier_type_id IS NOT NULL;
```

`tenant_id` é redundante em relação a `business_entity_id → business_entities.tenant_id`, mas segue o mesmo padrão já estabelecido para toda tabela filha Tenant Scope do projeto (`SalesOrderLine`, ADR-0009: *"tenant_id de toda linha idêntico ao do pedido"*) — RLS não deve depender de join para ser aplicada, e a invariante "`tenant_id` da linha filha = `tenant_id` do Aggregate pai" é responsabilidade do próprio Aggregate `BusinessEntity` ao criar o identificador, não do banco.

Os dois índices únicos condicionais (`WHERE ... IS NOT NULL`) substituem a invariante original de coluna única do ADR-0018, agora expressa separadamente por variante de tipo — suportado tanto por PostgreSQL (índice parcial) quanto por SQL Server (índice filtrado), preservando paridade (ADR-0011).

### 4. Invariante de disponibilidade é responsabilidade do Aggregate, não do banco

FK de banco garante que `deployment_identifier_type_id` referencia uma linha que **existe**, mas validação de FK não respeita RLS — uma linha pode existir fisicamente em `deployment_identifier_types` sem estar disponível para o Tenant que está tentando usá-la, e a FK não perceberia isso. Portanto, antes de persistir um `BusinessEntityIdentifier` referenciando um `deployment_identifier_type`, o Aggregate `BusinessEntity` DEVE verificar explicitamente que aquele tipo está disponível para seu próprio `tenant_id` (consulta a `deployment_identifier_type_tenant_availability`) — essa é uma invariante de código (ADR-0008: nunca confiada só a constraint de banco quando a constraint não consegue expressá-la).

## Consequências

- `docs/adr/ADR-0021-identifier-type-scope-split.md` recebe nota de revisão no cabeçalho de status, apontando para este ADR.
- Toda consulta/escrita de `business_entity_identifiers` e `deployment_identifier_type_tenant_availability` passa a respeitar RLS diretamente, sem depender de join com a tabela pai para isolamento.
- O teste de CI do ADR-0017 (a recriar) cobre agora também essas duas tabelas, antes ausentes da classificação.

## Riscos

- Denormalizar `tenant_id` em `business_entity_identifiers` exige que o Aggregate `BusinessEntity` mantenha essa cópia sincronizada com o `tenant_id` do próprio Aggregate — mesmo risco já aceito e mitigado (teste de invariante obrigatório) para `SalesOrderLine` desde o ADR-0009.

## Alternativas Rejeitadas

- **Deixar `business_entity_identifiers` sem `tenant_id` próprio, confiando em join com `business_entities`** — rejeitada por quebrar o padrão já estabelecido (`SalesOrderLine`) e exigir que toda política de RLS dependa de subquery/join, mais cara e mais fácil de esquecer numa tabela nova futura.
- **Não classificar a tabela de disponibilidade, tratando-a como "detalhe de implementação" do Deployment Scope** — rejeitada porque o próprio ADR-0017 não admite exceção "por implicação" — toda tabela precisa de classificação explícita.

## Critérios para Revisão Futura

- Nenhum — este ADR fecha os achados da terceira rodada de auditoria sobre o ADR-0021. Se uma quarta rodada encontrar algo novo no mesmo desenho, revisar o processo de escrita de ADR (não só o conteúdo), já que três correções sucessivas sobre a mesma decisão indicam que o desenho inicial (ADR-0018/0021) foi escrito rápido demais para o nível de detalhe físico que exigia.
