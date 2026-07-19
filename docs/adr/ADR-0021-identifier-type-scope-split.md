# ADR-0021 — Correção: IdentifierType Separado por Escopo (Platform vs. Deployment)

| Campo | Valor |
|---|---|
| **Status** | Aceito (corrige ADR-0018 — `IdentifierType` violava a regra de classificação por tabela do ADR-0017). **Corrigido por ADR-0024**: terminologia "duas tabelas/Aggregates" precisada para "um Aggregate conceitual, duas tabelas físicas"; `deployment_identifier_type_tenant_availability` e `business_entity_identifiers` receberam classificação de escopo (Tenant Scope) e RLS próprias, antes ausentes; índices de unicidade condicionais adicionados. |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-CHG-003 |
| **Fase do roadmap** | 1 |
| **Corrige** | ADR-0018 (definição física de `IdentifierType`) |
| **Depende de** | ADR-0017, ADR-0018 |

## Contexto

Auditoria de consistência encontrou uma contradição real entre dois ADRs escritos na mesma sessão: o ADR-0017 exige que "toda tabela nova... seja classificada em exatamente um dos três escopos" e que o teste de CI "falhe o build se uma tabela não estiver classificada em nenhum dos três" — a unidade de classificação é **a tabela**, não a linha. O ADR-0018, porém, definiu um único Aggregate `IdentifierType` com um campo `scope` (`official`\|`custom`) que classifica **por linha**: registros oficiais (`BR_CNPJ`) seriam Platform Scope, registros personalizados (`CUSTOM_XYZ`) seriam Deployment Scope, na mesma tabela física.

Isso é uma contradição real, não interpretação — a mesma tabela não pode estar em dois escopos ao mesmo tempo segundo a regra que o próprio ADR-0017 estabeleceu.

Rodando o checklist do `CLAUDE.md`:

1. **Lessons Learned:** LL-007 toca porque isto corrige um ADR recém-ratificado antes de qualquer código existir — o tipo de correção que deveria ser rara, mas que a governança do projeto exige registrar quando acontece, não silenciar.
2. **Requisitos AR-\*:** AR-TXN-001 exige que a fronteira de consistência (aqui, a fronteira de isolamento) seja inequívoca por Aggregate/tabela — o desenho anterior quebrava isso.
3. **Modo de falha específico:** manter o desenho anterior tornaria o teste de CI do ADR-0017 inaplicável a `IdentifierType` (nem "sempre Platform" nem "sempre Deployment" descreve a tabela real) — o teste automatizado que deveria proteger contra vazamento de isolamento ficaria sem como classificar essa tabela específica.
4. **Pertence à fase atual?** Sim — corrige definição da Fase 1 antes de qualquer implementação (nenhum código de `IdentifierType` existe).

## Decisão

Separar em duas tabelas/Aggregates, cada uma com escopo único (Opção A da auditoria — preferida por manter a regra de isolamento simples e verificável, em vez de generalizar o ADR-0017 para permitir tabelas multi-escopo):

### `platform_identifier_types` — Platform Scope

```sql
CREATE TABLE platform_identifier_types (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    technical_key     TEXT NOT NULL UNIQUE,   -- ex.: BR_CNPJ, US_EIN — namespace oficial, sem CUSTOM_*
    name              TEXT NOT NULL,
    country_code      TEXT,
    value_type        TEXT NOT NULL,
    format_mask       TEXT,
    validator_key     TEXT,
    definition_version INTEGER NOT NULL DEFAULT 1,
    status            TEXT NOT NULL CHECK (status IN ('active', 'deprecated', 'inactive'))
);
```

Sem `tenant_id`, sem RLS (ADR-0017) — conteúdo de referência governado e versionado pela TRS, escrita restrita ao processo de distribuição, nunca exposta como Capability de escrita a usuário de Tenant.

### `deployment_identifier_types` — Deployment Scope

```sql
CREATE TABLE deployment_identifier_types (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    technical_key     TEXT NOT NULL UNIQUE,   -- namespace CUSTOM_*, obrigatório (ADR-0018)
    name              TEXT NOT NULL,
    country_code      TEXT,
    value_type        TEXT NOT NULL,
    format_mask       TEXT,
    validator_key     TEXT,
    definition_version INTEGER NOT NULL DEFAULT 1,
    status            TEXT NOT NULL CHECK (status IN ('active', 'deprecated', 'inactive')),

    CONSTRAINT deployment_identifier_types_custom_namespace
        CHECK (technical_key LIKE 'CUSTOM_%')
);

CREATE TABLE deployment_identifier_type_tenant_availability (
    deployment_identifier_type_id UUID NOT NULL REFERENCES deployment_identifier_types(id),
    tenant_id                     UUID NOT NULL REFERENCES tenants(id),
    PRIMARY KEY (deployment_identifier_type_id, tenant_id)
);

ALTER TABLE deployment_identifier_types ENABLE ROW LEVEL SECURITY;
CREATE POLICY deployment_scope_availability ON deployment_identifier_types
    USING (EXISTS (
        SELECT 1 FROM deployment_identifier_type_tenant_availability a
        WHERE a.deployment_identifier_type_id = id
          AND a.tenant_id = current_setting('app.tenant_id')::UUID
    ));
```

Isolamento via tabela de disponibilidade + RLS `EXISTS`-based, exatamente como o ADR-0017 especifica para Deployment Scope — fail-closed, gestão restrita a autoridade administrativa de deployment (ADR-0018).

### `BusinessEntityIdentifier` — referência polimórfica

Como as duas tabelas de `IdentifierType` são fisicamente distintas, `BusinessEntityIdentifier` precisa referenciar uma ou outra, nunca ambas ao mesmo tempo:

```sql
CREATE TABLE business_entity_identifiers (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    business_entity_id             UUID NOT NULL REFERENCES business_entities(id),
    platform_identifier_type_id    UUID REFERENCES platform_identifier_types(id),
    deployment_identifier_type_id  UUID REFERENCES deployment_identifier_types(id),
    value                           TEXT NOT NULL,   -- canônico, sem máscara (ADR-0018)

    CONSTRAINT business_entity_identifiers_exactly_one_type
        CHECK (
            (platform_identifier_type_id IS NOT NULL AND deployment_identifier_type_id IS NULL)
            OR
            (platform_identifier_type_id IS NULL AND deployment_identifier_type_id IS NOT NULL)
        )
);
```

Duas colunas de FK nulável, com `CHECK` garantindo exatamente uma preenchida — preserva integridade referencial real (FK de banco) para os dois casos, em vez de um discriminador sem FK.

### Conceito unificado permanece na modelagem, não no schema

`IdentifierType` continua existindo como **conceito** (a distinção Technical Identity/Stable Technical Key/Business Code do ADR-0018 vocabulário não muda) — só a realização física é que se divide em duas tabelas. Camada de aplicação pode expor uma abstração única de leitura (ex.: uma view ou um tipo C# `IdentifierTypeRef` que resolve para uma das duas tabelas) sem violar a separação física exigida pelo ADR-0017.

## Consequências

- `docs/adr/ADR-0018-business-entity-party-management.md` precisa de nota de revisão no cabeçalho de status, apontando para este ADR.
- O teste de CI do ADR-0017 agora classifica `platform_identifier_types` e `deployment_identifier_types` sem ambiguidade, cada uma em exatamente um escopo.
- Consulta de identificadores de uma `BusinessEntity` precisa fazer `LEFT JOIN` com as duas tabelas (ou usar a view de abstração), nunca assumir uma só.

## Riscos

- Duas tabelas fisicamente separadas para o "mesmo conceito" adiciona alguma duplicação de schema (mesmos campos em ambas) — aceito porque a alternativa (Opção B: permitir tabela multi-escopo, revisando o ADR-0017 para uma política mais complexa) enfraquece a verificabilidade do teste de CI, que é a proteção mais forte contra vazamento de isolamento no projeto.
- Referência polimórfica (duas FKs nuláveis + CHECK) é mais complexa de consultar que uma FK simples — mitigado pela view/abstração de leitura mencionada acima.

## Alternativas Rejeitadas

- **Opção B — permitir tabela multi-escopo com política combinada** (`scope = PLATFORM OR (scope = DEPLOYMENT AND EXISTS tenant_availability)`) — rejeitada por exigir revisão do próprio ADR-0017 para uma regra de isolamento mais complexa e menos verificável por CI, trocando simplicidade por economia marginal de schema.
- **Discriminador sem FK real** (`identifier_type_scope` + `identifier_type_id` genérico, sem constraint de banco) — rejeitada por abrir mão de integridade referencial garantida pelo banco, dependendo só de disciplina de aplicação.

## Critérios para Revisão Futura

- Se o número de tabelas "por escopo" crescer além de `IdentifierType` (outro conceito que precise das duas variantes), avaliar se vale um padrão de código-gerado (mesmo schema, dois nomes de tabela) em vez de escrever cada par manualmente.
