# ADR-0019 — Data Lifecycle Policy (Estratégia de Exclusão de Dados)

| Campo | Valor |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-004 (Concurrency and Transactions), LL-007 (Change Governance), LL-008 (Exception-Driven Software) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-KNW-001, AR-CHG-005 |
| **Fase do roadmap** | 1 |
| **Depende de** | ADR-0007, ADR-0009, ADR-0012, ADR-0013, ADR-0017, ADR-0018 |

## Contexto

"Estratégia de exclusão de dados (lógica vs. física, olhando performance)" era uma pendência registrada desde antes da consolidação de cadastros — nenhum ADR existente decide isso. `docs/foundation/TRS_Cadastros_Consolidacao_Conceitual.md` (Seção 17) propôs quatro conceitos distintos que são frequentemente confundidos em sistemas empresariais. Este ADR formaliza essa proposta e resolve as decisões físicas concretas que o documento consolidado deixou em aberto.

Rodando o checklist do `CLAUDE.md` (Foundation v2, Parte IX.1):

1. **Lessons Learned:** LL-004 toca diretamente — excluir um registro referenciado por outro Aggregate sem regra clara de integridade é fonte de inconsistência transacional. LL-008 toca porque, sem uma política central, cada módulo inventaria sua própria regra de "quando algo pode sumir", reproduzindo exceção sobre exceção. LL-007 toca porque isto é uma decisão de governança de dado que precisa registro formal, não implementação ad-hoc por Aggregate.
2. **Requisitos AR-\*:** AR-KNW-001 (toda criação/alteração tem `reason_code`, autor, timestamp) se estende naturalmente a exclusão lógica e física — não é uma operação especial isenta de rationale. AR-TXN-001 exige que exclusão respeite a fronteira de consistência do Aggregate, nunca decidida por FK/trigger de banco isoladamente.
3. **Modo de falha específico desta solução:** obrigar todo Aggregate a suportar as quatro capacidades (Operational Status, Logical Delete, Archive, Physical Delete) geraria campos mortos (`IsActive`/`IsDeleted`/`Archived` em tabelas que nunca usam metade deles) — o oposto do problema que se quer resolver. O risco simétrico é permitir Physical Delete sem nenhuma restrição, quebrando o próprio `audit_events`/`audit_event_changes` (ADR-0012), que dependem do registro ainda existir para correlacionar auditoria.
4. **Pertence à fase atual?** Sim — é pré-requisito explícito para retomar a implementação de `Customer`/`BusinessEntity` (ADR-0018), que precisam declarar quais capacidades de lifecycle suportam antes de existir em código.

## Decisão

### Os quatro conceitos, sem sobreposição

| Conceito | Responde | Mecanismo físico |
|---|---|---|
| **Operational Status** | O registro pode participar de novas operações? | Campo `status` próprio do Aggregate (já usado por `Tenant`, `User`, `Company`, `Customer` — `active`/`inactive`/`suspended` conforme o domínio). Não é novidade desta ADR, só a formalização do que já existe. |
| **Logical Delete** | O registro deve sair da visão operacional, mas continuar existindo fisicamente? | Coluna `deleted_at TIMESTAMPTZ NULL` — nulo = não excluído; preenchido = excluído logicamente, com o timestamp de quando. Não é booleano: o timestamp já responde "quando", sem campo adicional. |
| **Archive** | O registro deve sair das estruturas operacionais de uso frequente por volume/retenção/performance? | Mecanismo físico **não decidido nesta fase** — reconhecido arquiteturalmente (mesmo tratamento que ADR-0012 já deu a particionamento de `audit_events`: revisar quando volume real justificar, não antecipar). |
| **Physical Delete** | O registro deve ser removido definitivamente? | `DELETE` real, só permitido quando o domínio declarar suporte explícito, sem dependências impeditivas (FK), sem obrigação de retenção — e **sempre** precedido de um registro em `audit_events` com `action = 'delete'` (ADR-0012 já prevê esse valor; este ADR confirma que ele é obrigatório antes do `DELETE` executar, não depois). |

`Operational Status` e `Logical Delete` **não são a mesma coisa**: um `Customer` pode estar `Inactive` (não participa de nova venda) sem estar logicamente excluído (continua aparecendo em relatórios/listagens operacionais normais); e pode estar logicamente excluído independentemente do valor de `status`.

### Regra de aplicação: cada Aggregate declara o que suporta

Nenhum Aggregate é obrigado a suportar as quatro capacidades. Tabela de aplicação para os Aggregates já ratificados:

| Aggregate | Operational Status | Logical Delete | Archive | Physical Delete |
|---|---|---|---|---|
| `Tenant` (ADR-0009) | Sim (`active`/`suspended`) | Não | Não decidido | Não |
| `User` (ADR-0009) | Sim (via `UserDeactivated`) | Não | Não decidido | Não |
| `Company` (ADR-0013) | Sim (`active`/`inactive`) | Não | Não decidido | Não |
| `SalesOrder` (ADR-0009) | Sim (`draft`/`active`) | Não | Não decidido | Não |
| `Customer` (ADR-0018) | Sim (`active`/`inactive`) | **Sim** | Não decidido | Não |
| `BusinessEntity` (ADR-0018) | Sim (`active`/`inactive`) | **Sim** | Não decidido | Não |
| `IdentifierType` (ADR-0018) | Usa enum próprio (`active`/`deprecated`/`inactive` — não o par Operational Status/Logical Delete genérico, porque `deprecated` carrega significado específico de versionamento, ADR-0018) | — | Não decidido | Não |

Nenhum Aggregate ratificado suporta `Physical Delete` na Fase 1 — reservado para casos futuros (ex.: direito ao esquecimento regulatório), não implementado sem caso concreto.

### Filtragem de Logical Delete não é isolamento de tenant

`deleted_at` é filtro de aplicação/Repository (exclui por padrão registros logicamente excluídos das consultas operacionais), **não** um mecanismo de segurança e **não** deve ser confundido com RLS/Security Policy (ADR-0007/0017) — a fronteira de tenant continua sendo a única garantia de segurança no banco. `deleted_at` é sobre visibilidade operacional dentro do próprio Tenant, não sobre isolamento entre Tenants.

### Autorização e auditoria

Toda transição de `Logical Delete` ou execução de `Physical Delete` exige `reason_code` (`manual_override`, `exception_approval` ou `correction` — nunca `routine_creation`, já que remover algo não é rotina de criação) e gera `audit_events`/`audit_event_changes` (ADR-0012), com a mesma disciplina de qualquer alteração relevante.

## Consequências

- `Customer` e `BusinessEntity` (ADR-0018) precisam de coluna `deleted_at` desde a primeira migração, quando implementados.
- `tests/rls/` (a ser recriado) não precisa cobrir `deleted_at` — é responsabilidade de teste de Repository/Application, não de teste de isolamento.
- Archive e Physical Delete permanecem conceitos reconhecidos, sem mecanismo físico obrigatório — revisão futura quando caso real justificar (mesmo padrão do ADR-0012 para particionamento de auditoria).

## Riscos

- Tabela de aplicação por Aggregate pode ficar desatualizada se um Aggregate futuro não a atualizar explicitamente — mitigação: qualquer novo ADR de domínio (como o ADR-0018 já fez) deve declarar explicitamente quais capacidades de lifecycle o Aggregate suporta, não herdar por omissão.
- Sem uma decisão física de Archive, tabelas de alto volume (ex.: `audit_events`, que ADR-0012 já identifica como candidata) não têm estratégia de redução de volume operacional — aceito como risco conhecido, mesma mitigação do ADR-0012 (revisar quando volume real justificar).

## Alternativas Rejeitadas

- **Coluna booleana `is_deleted` em vez de `deleted_at`** — rejeitada por perder a informação de "quando", que `audit_events` já registraria de qualquer forma mas que vale ter também diretamente na linha, sem exigir join para saber a data de exclusão.
- **Obrigar todo Aggregate a suportar as quatro capacidades por padrão** — rejeitada por gerar campo morto em tabelas que nunca usam metade deles (o antipadrão que este ADR busca evitar).
- **Tratar Logical Delete como parte do mecanismo de RLS/isolamento** — rejeitada por misturar duas preocupações diferentes (segurança entre tenants vs. visibilidade operacional dentro de um tenant), que precisam permanecer conceitualmente e fisicamente separadas.

## Critérios para Revisão Futura

- Definir mecanismo físico de Archive quando volume real de alguma tabela justificar (ex.: `audit_events` ou `sales_orders` em produção real).
- Revisar `Physical Delete` quando houver exigência regulatória real (ex.: direito ao esquecimento) — não antecipar.
- Revisar a tabela de aplicação por Aggregate sempre que um novo Aggregate for ratificado.
