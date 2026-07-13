# ADR-0012 — Audit and Logging Strategy

| Campo | Valor |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-07-12 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-007 (Change Governance), LL-001 (Parameter Explosion), LL-008 (Exception-Driven Software) |
| **Requisitos AR-\* relacionados** | AR-CHG-005, AR-KNW-001, AR-TXN-001, AR-TXN-002, AR-KNW-003, AR-EXP-005, AR-TXN-006 (referenciado como limite, não implementado) |
| **Fase do roadmap** | 1 (`audit_events`, `audit_event_changes`, `security_events`); itens adiados explicitamente marcados por fase abaixo |
| **Depende de** | ADR-0006, ADR-0007, ADR-0008, ADR-0009, ADR-0010, ADR-0011 |

## Contexto

O Gate da Fase 1 (`CLAUDE.md`) exige que nenhuma operação relevante ocorra
sem autorização registrada, autor identificado e motivo capturado, como
teste automatizado — não como checagem manual. Hoje isso só existe para o
momento de **criação** de um registro (`reason_code`/`human_statement`/
`author` em `users`, `customers`, `sales_orders`, `companies`). Não existe
nenhum mecanismo para registrar **alterações** posteriores — quem mudou o
quê, quando, e por quê — nem para eventos de segurança (login, mudança de
permissão).

Uma primeira proposta considerou uma única tabela genérica de log para
tudo (auditoria de campo, evento de negócio, segurança, workflow,
integração, log técnico). Essa proposta foi corrigida durante a revisão:
misturar propósitos com volumes, consumidores e políticas de retenção tão
diferentes numa única tabela genérica demais deixa de ser útil para
qualquer um dos propósitos individualmente.

Uma pesquisa de mercado (SAP, Salesforce, Odoo, HubSpot, Microsoft
Dynamics) confirmou um padrão convergente: **separar por tipo de
propósito**, com auditoria de campo (SAP `CDHDR`/`CDPOS`, Odoo
`mail.tracking.value`) tipicamente centralizada, e histórico completo por
entidade (Salesforce `{Objeto}History`, HubSpot Property History) usado
apenas quando um objeto específico exige — não como padrão automático para
toda tabela nova.

## Decisão

### Tabelas da Fase 1

Auditoria de campo (padrão cabeçalho/detalhe, equivalente a `CDHDR`/
`CDPOS`):

```sql
CREATE TABLE audit_events (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    occurred_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    actor_id        UUID,                       -- nulo se ação de sistema/automação
    actor_type      TEXT NOT NULL CHECK (actor_type IN ('user', 'system', 'integration')),
    action          TEXT NOT NULL CHECK (action IN ('create', 'update', 'delete')),
    entity_type     TEXT NOT NULL,              -- nome da tabela/Aggregate afetado
    entity_id       UUID NOT NULL,
    module          TEXT,                        -- 'tenancy', 'identity', 'sales'
    reason_code     TEXT NOT NULL DEFAULT 'routine_update'
                        CHECK (reason_code IN ('routine_creation', 'routine_update', 'manual_override', 'exception_approval', 'correction')),
    human_statement TEXT,
    correlation_id  UUID,                        -- liga eventos da mesma operação lógica

    CONSTRAINT audit_events_human_statement_required
        CHECK (reason_code IN ('routine_creation', 'routine_update') OR human_statement IS NOT NULL)
);

CREATE TABLE audit_event_changes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    audit_event_id  UUID NOT NULL REFERENCES audit_events(id),
    field_name      TEXT NOT NULL,
    old_value       TEXT,                        -- serializado como texto, independente do tipo original
    new_value       TEXT,
    UNIQUE (audit_event_id, field_name)
);
```

Eventos de segurança (tabela separada — volume, consumidor e natureza
diferentes de auditoria de campo):

```sql
CREATE TABLE security_events (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    occurred_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    event_type      TEXT NOT NULL,               -- 'login_success', 'login_failed', 'permission_granted'...
    user_id         UUID,
    ip_address      TEXT,
    correlation_id  UUID
);
```

Todas com `tenant_id` e política de RLS (ADR-0007) — sem exceção, mesma
disciplina de qualquer outra tabela de dado de negócio. Todas existem de
forma idêntica nas migrations Postgres e SQL Server (ADR-0011) — nenhum
recurso nativo exclusivo de motor (nem `SEQUENCE`, nem Temporal Tables,
nem `COMMENT ON`/Extended Properties) é usado como mecanismo central,
pelo mesmo motivo já registrado em ADR-0011: dois motores dobram o custo
de manter qualquer recurso específico de um deles.

### Imutabilidade

`audit_events`, `audit_event_changes` e `security_events` só recebem
`INSERT`. Um teste de CI — no mesmo espírito de
`tests/rls/check_rls_coverage.sql` — falha o build se detectar qualquer
caminho de código ou privilégio de banco que permita `UPDATE`/`DELETE`
nessas tabelas.

### Quem escreve

Nenhum Module escreve diretamente nessas tabelas. Toda escrita passa por
um serviço de infraestrutura:

```csharp
public interface IAuditService
{
    Task RecordAsync(
        Guid tenantId,
        string entityType,
        Guid entityId,
        string action,
        IReadOnlyDictionary<string, (string? OldValue, string? NewValue)> changes,
        ReasonCode reasonCode,
        string? humanStatement,
        Guid? actorId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);
}
```

A detecção de **o que mudou** (`old_value`/`new_value` por campo) é
automática, via interceptor no `SaveChanges` do EF Core (mesmo padrão do
`TenantContextInterceptor` já existente em `infrastructure/postgres/`) —
mecânico, não é decisão de negócio, então pode viver na infraestrutura
sem violar ADR-0008. O **motivo** (`reason_code`/`human_statement`/
`correlation_id`) vem da operação de negócio específica que está sendo
executada, porque só ela sabe a intenção — nunca inferido pelo banco ou
pelo interceptor.

### Explicitamente adiado (não faz parte desta decisão)

- **`business_events`** (log de fatos de domínio com `causation_id`,
  `event_version`, `payload` genérico) — isso é, na prática, um Event
  Store. **AR-TXN-006** exige ADR específico próprio antes de adotar
  event sourcing como padrão — não entra aqui "de carona".
- **`workflow_history`** — não há workflow para registrar ainda.
  ADR-0009 já registra que `SalesOrder` não tem estados de aprovação na
  Fase 1 ("sem `Submitted`/`Approved`/`Rejected` — pressupõem workflow,
  Fase 3").
- **`integration_events`** — é o padrão outbox. Já registrado como Fase
  3 (AR-TXN-003).
- **Histórico completo por entidade** (`customer_history`,
  `contract_history`, `price_history`, `tax_rule_history`,
  `accounting_rule_history`, no estilo Salesforce `{Objeto}History`) —
  nenhum Aggregate da Fase 1 tem caso de uso concreto que exija
  versionamento completo (distinto de auditoria de campo). Pela
  meta-regra do ADR-0006, não entra sem esse caso de uso real.
- **Classificação LGPD/GDPR de campo, criptografia de campo sensível,
  hash chain (`event_hash`/`previous_hash`), Time Travel, auditoria de
  agente de IA** — capacidades reais, mas de fases avançadas do roadmap,
  sem necessidade operacional comprovada hoje.

## Consequências

- Toda migration nova (Postgres e T-SQL) que crie uma tabela de
  `cadastro`/`movimentação` precisa, a partir de agora, também produzir
  registros em `audit_events`/`audit_event_changes` através do
  `IAuditService` — verificado por CI, não por convenção manual.
- `security_events` passa a ser o destino de eventos de autenticação e
  permissão assim que o módulo `identity` implementar login/MFA (ainda
  não implementado — só a tabela e o contrato existem a partir desta
  decisão).
- Logs técnicos (`INFO`/`WARN`/`ERROR`/`DEBUG`/`TRACE`) permanecem fora
  do banco transacional principal, em ferramenta de observabilidade —
  qual ferramenta específica (Loki, Datadog, OpenSearch etc.) é decisão
  técnica separada, não coberta por este ADR.

## Riscos

- `audit_events`/`audit_event_changes` crescem proporcionalmente a toda
  escrita do sistema — estratégia de arquivamento/particionamento não é
  definida aqui, e deve ser revisada quando o volume real justificar
  (não antecipar).
- Sem disciplina real de chamar `IAuditService` em toda operação de
  escrita desde o primeiro Aggregate, a garantia de cobertura depende
  inteiramente do teste de CI proposto ser mantido rigorosamente — o
  mesmo risco que ADR-0007 já registra para RLS.
- Adiar `business_events`/`workflow_history`/`integration_events`
  explicitamente aqui reduz, mas não elimina, o risco de alguém
  reintroduzi-los informalmente mais tarde sem passar pelo ADR
  específico que AR-TXN-006 exige.

## Alternativas Rejeitadas

- **Tabela única genérica para todo tipo de log** — rejeitada por
  misturar volumes, consumidores e políticas de retenção incompatíveis
  numa estrutura só, dificultando análise e evolução futura.
- **Histórico por objeto como padrão automático** (todo Aggregate novo
  ganha sua própria tabela `_history`) — rejeitada como padrão default
  porque exige criar uma migration/tabela nova manualmente a cada
  Aggregate, o que é esquecível e não é verificável por CI da mesma
  forma que uma tabela central compartilhada; mantida como padrão
  disponível só para os casos futuros que justificarem versionamento
  completo (ver "Explicitamente adiado").
- **Trigger de banco para captura automática de mudança** — rejeitada
  pela mesma razão de ADR-0008/ADR-0010: um trigger vê que um valor
  mudou, nunca vê a intenção de negócio por trás — a captura mecânica
  pode ser automática (via interceptor do EF Core), mas nunca a decisão
  de por quê.

## Critérios para Revisão Futura

- Revisar `business_events` quando houver caso de uso concreto e real
  que exija replay ou reconstrução de estado a partir de eventos — nesse
  momento, tratar como proposta de ADR específico para AR-TXN-006, não
  como extensão informal deste ADR.
- Revisar `workflow_history` quando a Fase 3 (Workflow Engine) começar
  formalmente.
- Revisar `integration_events` quando a Fase 3 (Outbox, AR-TXN-003)
  começar formalmente.
- Revisar necessidade de histórico completo por entidade quando um
  Aggregate real e específico (ex: um futuro Aggregate de Contrato, ou
  regra fiscal) demonstrar essa necessidade concreta — não antes.
