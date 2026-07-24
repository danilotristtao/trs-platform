# ADR-0025 — Tenant-Safe Referential Integrity Standard

| Campo | Valor |
|---|---|
| **Status** | Aceito (corrige um bug real no ADR-0013, presente desde a Fase 1 original, mais lacunas equivalentes em ADR-0009, ADR-0018 e ADR-0024) |
| **Data** | 2026-07-20 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-004 (Concurrency and Transactions), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-TXN-002, AR-CHG-003 |
| **Fase do roadmap** | 1 |
| **Corrige** | ADR-0009 (`SalesOrder.customer_id`, `SalesOrderLine.sales_order_id`), ADR-0013 (`companies` — constraint ausente), ADR-0018 (`business_entities`, `Customer.business_entity_id`), ADR-0024 (`business_entity_identifiers.business_entity_id`) |
| **Depende de** | ADR-0007, ADR-0008, ADR-0009, ADR-0013, ADR-0018, ADR-0024 |

## Contexto

Quarta rodada de auditoria encontrou que `business_entity_identifiers.business_entity_id` (ADR-0024) usa FK simples para `business_entities(id)`, não composta com `tenant_id` — o mesmo modo de falha que o ADR-0013 já havia identificado e corrigido para `Company.parent_company_id` (uma FK simples permitiria referenciar uma linha de **outro Tenant**).

Investigando o precedente que deveria estar correto, encontrei um problema mais sério: **a própria correção do ADR-0013 nunca foi tecnicamente válida.** A constraint

```sql
CONSTRAINT companies_parent_same_tenant
    FOREIGN KEY (tenant_id, parent_company_id) REFERENCES companies(tenant_id, id)
```

referencia o par de colunas `(tenant_id, id)` em `companies`, mas a tabela só declara `PRIMARY KEY (id)` — nunca uma constraint `UNIQUE`/`PK` sobre exatamente `(tenant_id, id)`. Tanto PostgreSQL quanto SQL Server exigem que toda coluna (ou conjunto de colunas) referenciada por uma FK corresponda a uma constraint única existente sobre **esse conjunto exato** — `id` sozinho ser único (via `PRIMARY KEY`) não torna `(tenant_id, id)` automaticamente uma chave candidata válida para fins de FK. Essa migration, como documentada, falharia ao ser aplicada em qualquer um dos dois motores.

Rodando o checklist do `CLAUDE.md`:

1. **Lessons Learned:** LL-004 toca diretamente — é exatamente o tipo de garantia de integridade referencial sob concorrência/multi-tenant que a lição cobre. LL-007 toca por corrigir múltiplos ADRs já ratificados.
2. **Requisitos AR-\*:** AR-TXN-001/002 exigem que a fronteira de consistência do Aggregate seja garantida — incluindo pelo banco, não só pelo código, sempre que o banco puder expressar a garantia.
3. **Modo de falha específico:** sem esta correção, toda relação Tenant Scope → Tenant Scope que hoje usa FK simples permite, em tese, um bug de aplicação escrever uma referência cross-tenant que nem RLS nem FK impediriam — RLS protege leitura/escrita por sessão, mas não impede que uma FK simples aponte para a linha errada se a aplicação (por bug) construir o `INSERT` com o `id` de outro tenant.
4. **Pertence à fase atual?** Sim — nenhuma dessas tabelas tem dado real em produção; o custo de corrigir agora é reescrever `CREATE TABLE`, não migrar dado.

## Decisão

### Padrão transversal — Tenant-Safe Referential Integrity

Toda tabela Tenant Scope que pode ser referenciada por outra tabela Tenant Scope (como "pai" de uma relação) DEVE declarar, além de `PRIMARY KEY (id)`:

```sql
CONSTRAINT <tabela>_tenant_id_id_unique UNIQUE (tenant_id, id)
```

Toda tabela Tenant Scope que referencia outra tabela Tenant Scope (como "filha" da relação) DEVE usar FK composta, nunca FK simples:

```sql
CONSTRAINT <tabela>_<referencia>_same_tenant
    FOREIGN KEY (tenant_id, <referencia>_id) REFERENCES <tabela_pai>(tenant_id, id)
```

Isso vale para toda relação Tenant Scope → Tenant Scope, exceto quando o "pai" é o próprio `Tenant` (que não tem `tenant_id` — é a raiz da fronteira, ADR-0007; referenciar `tenants(id)` com FK simples continua correto, não há ambiguidade de qual tenant, já que só existe uma dimensão).

### Correções aplicadas às tabelas já ratificadas

**`companies` (ADR-0013)** — adiciona a constraint que faltava (a FK composta já existia, só não era válida sem isso):

```sql
ALTER TABLE companies ADD CONSTRAINT companies_tenant_id_id_unique UNIQUE (tenant_id, id);
```

**`customers` (ADR-0009)** — adiciona chave candidata para suportar as duas FKs compostas abaixo:

```sql
ALTER TABLE customers ADD CONSTRAINT customers_tenant_id_id_unique UNIQUE (tenant_id, id);
```

**`sales_orders.customer_id` (ADR-0009)** — upgrade de FK simples para composta:

```sql
-- antes: customer_id UUID NOT NULL REFERENCES customers(id)
-- agora:
CONSTRAINT sales_orders_customer_same_tenant
    FOREIGN KEY (tenant_id, customer_id) REFERENCES customers(tenant_id, id)
```

**`sales_order_lines.sales_order_id` (ADR-0009)** — mesmo padrão, defesa em profundidade além da invariante de Aggregate já existente ("tenant_id de toda linha idêntico ao do pedido"):

```sql
ALTER TABLE sales_orders ADD CONSTRAINT sales_orders_tenant_id_id_unique UNIQUE (tenant_id, id);

CONSTRAINT sales_order_lines_order_same_tenant
    FOREIGN KEY (tenant_id, sales_order_id) REFERENCES sales_orders(tenant_id, id)
```

**`business_entities` (ADR-0018)** — adiciona chave candidata:

```sql
ALTER TABLE business_entities ADD CONSTRAINT business_entities_tenant_id_id_unique UNIQUE (tenant_id, id);
```

Isso torna válidas as duas FKs compostas que já deveriam usar esse padrão:

- **`customers.business_entity_id` (ADR-0018)** — já especificado como "FK composta análoga ao ADR-0013" no texto original; agora tecnicamente correto com a constraint acima existindo.
- **`business_entity_identifiers.business_entity_id` (ADR-0024)** — upgrade de FK simples para composta (achado desta rodada):

```sql
-- antes: business_entity_id UUID NOT NULL REFERENCES business_entities(id)
-- agora:
CONSTRAINT business_entity_identifiers_entity_same_tenant
    FOREIGN KEY (tenant_id, business_entity_id) REFERENCES business_entities(tenant_id, id)
```

### O que não muda

`deployment_identifier_type_tenant_availability.tenant_id` e `security_events.user_id`/similares que referenciam `tenants(id)` diretamente (a raiz da fronteira) continuam com FK simples — não há ambiguidade a proteger nesse caso.

## Consequências

- ADR-0009, ADR-0013, ADR-0018 e ADR-0024 recebem nota de revisão no cabeçalho de status, apontando para este ADR.
- Todo Aggregate Tenant Scope referenciável por outro, a partir de agora, nasce com `UNIQUE (tenant_id, id)` desde a primeira migration — isso deveria fazer parte do template obrigatório de criação de tabela, no mesmo espírito da exigência de RLS do ADR-0007.
- `tests/rls/` (a recriar) passa a verificar também que toda FK entre tabelas Tenant Scope é composta com `tenant_id`, não só que RLS existe — são duas garantias diferentes e complementares.

## Riscos

- Índice único adicional (`UNIQUE (tenant_id, id)`) tem custo de armazenamento marginal — aceito, é pequeno comparado ao risco que mitiga.
- Se um Aggregate futuro esquecer de aplicar este padrão, nada além de revisão de código pega isso hoje — mitigado pela recriação do teste de CI mencionada acima.

## Alternativas Rejeitadas

- **Confiar só em RLS + validação de Aggregate, sem FK composta** — rejeitada pela mesma razão que o ADR-0013 original já rejeitou para `Company`: FK simples é um caminho de escrita que nem RLS nem validação de aplicação necessariamente cobrem (RLS protege o quê a sessão vê/altera; FK simples não impede a aplicação de montar um `INSERT` com `id` de outro tenant se houver bug na camada de negócio).
- **Corrigir só os casos apontados pela auditoria (`BusinessEntityIdentifier`), sem generalizar** — rejeitada por deixar o mesmo bug (já confirmado em `companies`) sem correção, e por deixar `SalesOrder.customer_id` no mesmo estado vulnerável sem necessidade de nova auditoria descobrir depois.

## Critérios para Revisão Futura

- Todo ADR de domínio futuro que declarar uma relação Tenant Scope → Tenant Scope deve aplicar este padrão desde a primeira versão, não como correção posterior.
