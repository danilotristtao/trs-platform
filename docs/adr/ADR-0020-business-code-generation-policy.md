# ADR-0020 — Business Code Generation Policy (Generalização de `config_code_sequences`)

| Campo | Valor |
|---|---|
| **Status** | Aceito (revisa parcialmente ADR-0013 — generaliza `config_code_sequences` de mecanismo específico de `Company` para política transversal da plataforma) |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-001 (Parameter Explosion), LL-005 (Customization Debt), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-TXN-002, AR-CHG-003 |
| **Fase do roadmap** | 1 |
| **Revisa** | ADR-0013 (parcialmente — generaliza `config_code_sequences`) |
| **Depende de** | ADR-0011, ADR-0013, ADR-0017, ADR-0018 |

## Contexto

O ADR-0013 introduziu `config_code_sequences` especificamente para gerar o `code` de `Company`. A tabela já foi desenhada de forma multi-entidade (`entity_type` faz parte da chave primária), mas nenhum ADR declarava isso como mecanismo obrigatório para qualquer entidade que precise de código — cada módulo futuro poderia reinventar sua própria geração, exatamente o risco que LL-001/LL-005 descrevem.

Rodando o checklist do `CLAUDE.md` (Foundation v2, Parte IX.1):

1. **Lessons Learned:** LL-001 e LL-005 tocam diretamente — sem uma política central declarada, geração de código vira um parâmetro reinventado por módulo, o mesmo padrão que motivou o ADR-0008. LL-007 toca porque isto revisa formalmente o ADR-0013.
2. **Requisitos AR-\*:** AR-TXN-002 (isolamento e integridade transacional) é o requisito central — geração de código concorrente sem serialização correta gera duplicata ou deadlock. AR-TXN-001 exige que a geração não vaze responsabilidade de Aggregate para fora do domínio.
3. **Modo de falha específico desta solução:** generalizar demais sem escopo declarado por domínio recria o antipadrão que o próprio ADR-0013 já rejeitou ("código de negócio configurável por tabela do sistema inteiro", LL-001). A mitigação já estava no ADR-0013 (escopo restrito a entidades que o usuário acessa) e permanece aqui.
4. **Pertence à fase atual?** Sim — é pré-requisito para qualquer Aggregate futuro (incluindo `Customer`/`BusinessEntity`, se decidirem ter `code`) que precise de geração de código, sem reinventar o mecanismo.

## Decisão

### Tabela generalizada (revisão de `config_code_sequences`, ADR-0013)

```sql
CREATE TABLE config_code_sequences (
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    entity_type     TEXT NOT NULL,
    scope_id        UUID NOT NULL,  -- ver "Escopo da sequência" abaixo
    is_automatic    BOOLEAN NOT NULL DEFAULT true,
    prefix          TEXT,
    padding         SMALLINT NOT NULL DEFAULT 4,
    next_number     INTEGER NOT NULL DEFAULT 1,
    strategy        TEXT NOT NULL DEFAULT 'high_performance'
                        CHECK (strategy IN ('high_performance', 'transactional_gapless', 'regulated')),
    token_template  TEXT,            -- ex.: 'SO-{YYYY}-{SEQ}', null quando prefix/padding bastam
    reset_policy    TEXT NOT NULL DEFAULT 'never'
                        CHECK (reset_policy IN ('never', 'yearly', 'monthly')),
    last_reset_at   TIMESTAMPTZ,

    PRIMARY KEY (tenant_id, entity_type, scope_id)
);
```

**Escopo da sequência (`scope_id`):** cada domínio declara explicitamente o escopo apropriado — não existe default silencioso. `scope_id` é sempre preenchido, nunca nulo (colunas de chave primária não aceitam nulo): quando o escopo é o próprio Tenant, `scope_id = tenant_id`; quando o escopo é `Company` (ex.: numeração de `SalesOrder` por filial), `scope_id = company_id`. Isso preserva uma única forma de tabela para os dois casos, sem exigir coluna nula na chave primária.

Exemplo:

```text
Customer  → escopo Tenant   → scope_id = tenant_id
SalesOrder → escopo Company → scope_id = company_id da filial
```

### Modos: Manual e Automatic

- **Manual:** usuário informa o `code`. A plataforma valida unicidade, formato, tamanho e caracteres permitidos — essa validação é responsabilidade do domínio (Aggregate), não desta tabela.
- **Automatic** (`is_automatic = true`): geração via `prefix` + `padding` + `next_number`, ou via `token_template` quando o formato exigir mais que prefixo simples (ex.: `SO-{YYYY}-{SEQ}`, `PO-{COMPANY_CODE}-{YYYY}-{SEQ}`). Tokens são uma lista fechada e oficial — nunca script ou expressão arbitrária (evita reintroduzir o antipadrão de "motor de regra genérico" que o ADR-0008 já rejeitou para cálculo decisório).

### Estratégias de sequência

| Estratégia | Quando usar | Comportamento em rollback |
|---|---|---|
| **`high_performance`** (default) | Geração automática padrão — a esmagadora maioria dos casos | Número pode ficar com buraco (gap) se a transação que o gerou fizer rollback — aceito, prioriza concorrência |
| **`transactional_gapless`** | Só quando continuidade for requisito explícito do domínio | Próximo número é calculado como `MAX(número já persistido) + 1` dentro da mesma transação travada, nunca via contador pré-incrementado — rollback não consome o número, porque ele nunca foi "reservado" antes de a linha existir de fato |
| **`regulated`** | Numeração fiscal/legal (ex.: nota fiscal) | Regras de atribuição, cancelamento, inutilização e gaps são do domínio e da jurisdição — esta tabela só guarda o próximo número disponível; a lógica de cancelamento/inutilização vive no Aggregate, não aqui |

### Concorrência

Geração via `SELECT ... FOR UPDATE` (PostgreSQL) / `SELECT ... WITH (UPDLOCK, ROWLOCK)` (SQL Server) sobre a linha `(tenant_id, entity_type, scope_id)`, na mesma transação que persiste o registro — nunca `SEQUENCE` nativa (não transacional, gera buraco em rollback mesmo na estratégia `high_performance`, que aceita buraco só por concorrência, não por decisão técnica arbitrária) nem trigger (não decide `is_automatic`/estratégia, que são decisão de configuração). Serialização ocorre só sobre a sequência específica — `Tenant A + Customer` nunca bloqueia `Tenant B + Customer`, nem `Company X + SalesOrder` bloqueia `Company Y + SalesOrder`.

### Reset

`reset_policy` (`never`/`yearly`/`monthly`) controla se `next_number` volta a `1` (respeitando `prefix`/token de ano-mês) periodicamente. `last_reset_at` registra a última vez que o reset ocorreu, para o processo de geração decidir se precisa resetar antes de gerar o próximo número.

## Consequências

- `Company.code` (ADR-0013) continua funcionando sem alteração de comportamento — `scope_id = tenant_id` para essa linha específica preserva compatibilidade total com o schema original.
- Este ADR **não adiciona** `code` a `Customer` ou `SalesOrder` — isso é decisão de quem detalhar esses Aggregates quando `sales` retomar implementação; se decidirem por `code`, usam este mecanismo, não inventam um novo.
- Testes de concorrência (quando a implementação retomar) precisam cobrir os dois motores (Postgres/SQL Server) e as três estratégias, não só o caminho feliz de `high_performance`.

## Riscos

- `transactional_gapless` tem custo de concorrência real (mais contenção que um contador simples) — aceito só quando explicitamente escolhido, nunca como default.
- Tokens em lista fechada podem não cobrir um caso real de formato ainda não previsto — mitigado por revisão quando aparecer, não por permitir expressão arbitrária "só para não esperar".

## Alternativas Rejeitadas

- **Coluna `scope_id` nula quando escopo é Tenant** — rejeitada porque colunas de chave primária não podem ser nulas; a convenção `scope_id = tenant_id` resolve sem exigir uma segunda chave (`scope_type` + `scope_id` nulável) mais complexa.
- **Permitir template de token como expressão livre (ex.: JavaScript embutido)** — rejeitada pelo mesmo motivo que o ADR-0008 rejeita motor de regra genérico incontrolado: token livre reabriria a porta para lógica de negócio arbitrária dentro de configuração.
- **`SEQUENCE` nativa do motor para `high_performance`** — rejeitada por não ser portável entre Postgres e SQL Server (ADR-0011), mesmo aceitando gaps por concorrência.

## Critérios para Revisão Futura

- Revisar `token_template` quando aparecer um formato real não coberto pela lista de tokens oficiais.
- Revisar `regulated` com um caso fiscal real (ex.: NF-e brasileira) antes de qualquer implementação em produção que dependa dele.
- Revisar se `Customer`/`SalesOrder` precisam de `code` quando `sales` retomar implementação — não decidido por este ADR.
