# ADR-0013 — Estrutura de Empresa/Filial (Company)

| Campo | Valor |
|---|---|
| **Status** | Aceito (revisa parcialmente ADR-0009 — adiciona um quinto Aggregate à Fase 1). Revisado parcialmente por **ADR-0020**: `config_code_sequences` deixa de ser mecanismo específico de `Company` e passa a ser política transversal (`scope_id` generalizado, estratégias `high_performance`/`transactional_gapless`/`regulated`) — o schema e o comportamento para `Company.code` permanecem inalterados. **Corrigido por ADR-0025**: a constraint `companies_parent_same_tenant` (FK composta) nunca foi tecnicamente válida — referenciava `(tenant_id, id)` sem que essa tabela tivesse uma constraint `UNIQUE`/`PK` sobre exatamente esse par; `UNIQUE (tenant_id, id)` adicionada. |
| **Data** | 2026-07-12 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-001 (Parameter Explosion), LL-008 (Exception-Driven Software), LL-005 (Customization Debt), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | Nenhum requisito específico de hierarquia organizacional existe no Foundation v2 (achado do checklist, ver Contexto). Toca AR-TXN-001, AR-TXN-002, AR-KNW-003, AR-EXP-005, AR-EXT-006 |
| **Fase do roadmap** | 1 (revisão parcial de ADR-0009 — decisão de negócio explícita, não antecipação do roadmap original) |
| **Revisa** | ADR-0009 (parcialmente — adiciona `Company` aos quatro Aggregates originais da Fase 1) |
| **Depende de** | ADR-0006, ADR-0007, ADR-0008, ADR-0009, ADR-0010, ADR-0011 |

## Contexto

O ADR-0009 fechou o modelo de domínio da Fase 1 em exatamente quatro
Aggregates (`Tenant`, `User`, `SalesOrder`, `Customer`). Clientes reais da
TRS, no entanto, frequentemente são estruturados como grupo econômico —
uma matriz com uma ou mais filiais, cada uma com CNPJ próprio (a
Receita Federal já estrutura o CNPJ como raiz + filial: `XX.XXX.XXX/
YYYY-ZZ`, onde `YYYY` identifica a filial). Hoje o `Tenant` não tem campo
nenhum de identidade fiscal/societária (só `name`, `status`), e não existe
lugar para representar mais de uma entidade legal por conta.

**Rodando o checklist do `CLAUDE.md` (Foundation v2, Parte IX.1):**

1. *Lessons Learned:* LL-001 e LL-008 tocam diretamente — os dois citam
   "filial" nominalmente como exemplo do antipadrão de parâmetro/exceção
   descontrolada (`"Se a filial for internacional..."`,
   `"if filial = Y → comportamento B"`). Isso não invalida a decisão, mas
   exige que `Company` seja modelada como conceito formal desde o início,
   não como campo condicional solto. LL-005 e LL-007 tocam de forma
   secundária (governança de mudança, dívida de customização).
2. *Requisitos AR-\*:* busca no Foundation v2 não encontrou **nenhuma**
   menção a filial, matriz (no sentido societário), subsidiária ou grupo
   econômico. Não existe requisito que esta decisão esteja atendendo por
   antecipação do roadmap — é extensão nova.
3. *Modo de falha desta solução especificamente:* a auto-referência
   (`parent_company_id` apontando para `companies.id`) tem dois riscos
   técnicos próprios, distintos do problema geral de "representar grupo
   econômico": (a) nada impede ciclo ou profundidade de hierarquia
   arbitrária sem uma invariante explícita; (b) uma FK simples para
   `companies(id)` não impede que o `parent_company_id` de uma filial
   aponte para uma `Company` de **outro tenant** — uma forma sutil de
   vazamento de isolamento que uma FK comum não pega.
4. *Pertence à fase atual?* Não, honestamente. O ADR-0009 fechou a Fase 1
   em quatro Aggregates. Esta decisão é uma extensão explícita motivada
   por necessidade real de negócio — a mesma honestidade que o ADR-0011
   já registrou para SQL Server ("o motivo registrado é comercial, não
   antecipação técnica do roadmap").

**Pesquisa de mercado** (NetSuite Subsidiary, SAP Company Code, Odoo
`res.company`) confirma o padrão: a hierarquia de entidades legais vive
**dentro** da fronteira de isolamento da plataforma (`Tenant`), nunca
como um `Tenant` por filial — senão relatório consolidado entre filiais
vira consulta cruzando tenant, o que o RLS foi desenhado para impedir.

## Decisão

### Aggregate `Company` — Module `tenancy`, Bounded Context Trust & Governance

Correção em relação a discussões anteriores da mesma sessão: `Company`
não pertence ao módulo `sales` — é estrutura organizacional do próprio
`Tenant` (mais próxima de `Tenant`/`User` do que de `Customer`, que é
conceito de Vendas). Vive em `src/tenancy/`, ao lado de `Tenant`.

Uma única tabela, com auto-referência, representando matriz e filial
como o mesmo tipo:

```sql
CREATE TABLE companies (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               UUID NOT NULL REFERENCES tenants(id),
    parent_company_id       UUID,                  -- nulo na matriz

    code                    TEXT NOT NULL,          -- gerado via config_code_sequences ou manual
    legal_name              TEXT NOT NULL,
    trade_name              TEXT,
    tax_id                  TEXT NOT NULL,
    state_registration      TEXT,
    municipal_registration  TEXT,
    legal_nature            TEXT,
    founded_at              TIMESTAMPTZ,

    address_street          TEXT,
    address_number          TEXT,
    address_complement      TEXT,
    address_neighborhood    TEXT,
    address_city            TEXT,
    address_postal_code     TEXT,

    phone                   TEXT,
    email                   TEXT,
    website                 TEXT,

    status                  TEXT NOT NULL CHECK (status IN ('active', 'inactive')),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now(),

    reason_code             TEXT NOT NULL DEFAULT 'routine_creation'
                                CHECK (reason_code IN ('routine_creation', 'manual_override', 'exception_approval', 'correction')),
    human_statement         TEXT,
    author                  UUID,

    CONSTRAINT companies_code_unique_per_tenant UNIQUE (tenant_id, code),
    CONSTRAINT companies_human_statement_required
        CHECK (reason_code = 'routine_creation' OR human_statement IS NOT NULL),

    -- Mitigação do modo de falha (b) do item 3: a FK composta garante
    -- que uma filial só pode apontar para uma matriz do MESMO tenant —
    -- uma FK simples para companies(id) não garantiria isso.
    CONSTRAINT companies_parent_same_tenant
        FOREIGN KEY (tenant_id, parent_company_id) REFERENCES companies(tenant_id, id)
);

ALTER TABLE companies ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON companies
    USING (tenant_id = current_setting('app.tenant_id')::UUID);
```

Mitigação do modo de falha (a) do item 3 — profundidade/ciclo de
hierarquia — fica no Aggregate (C#), não no banco: `Company.Create(...)`
só aceita `parentCompanyId` que aponte para uma `Company` cujo próprio
`ParentCompanyId` seja nulo (só dois níveis: matriz → filial, nunca
filial de filial). Isso é invariante de domínio (ADR-0008), não
constraint de banco, porque depende de consultar outra linha — mesma
categoria do "ao menos uma linha" de `SalesOrder` (ADR-0009).

### Código de negócio

Tabela de configuração central e portável (Postgres/SQL Server, sem
`SEQUENCE` nem recurso nativo específico de motor):

```sql
CREATE TABLE config_code_sequences (
    tenant_id      UUID NOT NULL REFERENCES tenants(id),
    entity_type    TEXT NOT NULL,
    is_automatic   BOOLEAN NOT NULL DEFAULT true,
    prefix         TEXT,
    padding        SMALLINT NOT NULL DEFAULT 4,
    next_number    INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY (tenant_id, entity_type)
);
```

Geração via `SELECT ... FOR UPDATE` sobre essa linha, na **mesma
transação** que cria a `Company` — nunca via `SEQUENCE` nativa (não
transacional, gera buraco em rollback) nem trigger (não sabe decidir
`is_automatic`, que é decisão de configuração, não de banco). Escopo
restrito às entidades que o usuário acessa — não uma linha de config por
tabela do sistema inteiro (rejeitado abaixo).

## Consequências

- A Fase 1 passa a ter **cinco** Aggregates, não quatro — `ICompanyRepository`
  segue o mesmo padrão de Repository das demais (ADR-0011).
- `migrations/0001_init.sql` (Postgres) e o equivalente T-SQL (pendente,
  ADR-0011) precisam incluir `companies` e `config_code_sequences`.
- `audit_events`/`audit_event_changes` (ADR-0012) cobrem `companies`
  desde a primeira migração — é tabela de `cadastro`, portanto auditada
  por padrão.

## Riscos

- **Aposta de negócio sem cliente nomeado hoje** — mesmo risco que o
  ADR-0011 já assumiu para SQL Server. Se a necessidade de multi-empresa
  não se confirmar na prática, o custo de manter esse Aggregate a mais
  continua sendo pago sem retorno correspondente.
- Hierarquia limitada a dois níveis (matriz/filial) pode não bastar para
  um grupo econômico com holding de holding — aceito como limite
  conhecido, não como caso de uso real hoje.

## Alternativas Rejeitadas

- **Duas tabelas separadas (`companies` + `branches`)** — rejeitada por
  duplicar estrutura para o mesmo tipo de dado (matriz e filial têm
  exatamente os mesmos campos).
- **Cada filial como `Tenant` separado** — rejeitada porque quebra
  relatório consolidado entre filiais e o próprio conceito de "mesma
  conta, mesmo grupo".
- **Código de negócio configurável por tabela do sistema inteiro**
  (padrão legado tipo `CONFIG_COD`) — rejeitada, LL-001; escopo
  restrito só às entidades que o usuário acessa.
- **`COMMENT ON`/Extended Properties ou `SEQUENCE` nativa para geração
  de código** — rejeitada por não ser portável entre Postgres e SQL
  Server (ADR-0011).

## Critérios para Revisão Futura

- Revisar limite de dois níveis de hierarquia se um caso real (não
  hipotético) de holding-de-holding aparecer.
- Revisar geração de código em lote (padrão Hi/Lo) se importação em
  massa de filiais virar necessidade medida, não hipotética.
- Revisar esta decisão se, como o ADR-0011 já prevê para SQL Server,
  nenhum cliente real adotar estrutura multi-empresa dentro de um
  horizonte razoável.
