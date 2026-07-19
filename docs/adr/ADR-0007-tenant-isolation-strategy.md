# ADR-0007 — Tenant Isolation Strategy

| Campo | Valor |
|---|---|
| **Status** | Aceito, revisado parcialmente por **ADR-0011** e **ADR-0017**. ADR-0011: PostgreSQL deixou de ser o único motor suportado (SQL Server suportado desde a Fase 1, Oracle arquitetado para o futuro). ADR-0017: reconhece um terceiro nível de escopo (Platform Scope, Deployment Scope) além de Tenant Scope — RLS de **Tenant Scope continua obrigatório e sem exceção**, sem alteração; as duas exceções documentadas (raiz da fronteira = `Tenant`; conteúdo de referência = Platform Scope) e o padrão `EXISTS`-based para Deployment Scope estão detalhados no ADR-0017, não neste documento. (Revisão anterior a estas: versão original propunha modelo híbrido ambíguo.) |
| **Data** | 2026-07-12 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-004 (Concurrency and Transactions), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-TXN-002, AR-KNW-003, AR-EXP-005 |
| **Fase do roadmap** | 0 (bloqueia a criação da entidade `Tenant` na Fase 1) |

## Contexto

A estratégia de isolamento de dados entre tenants é, entre todas as
decisões técnicas da TRS, a mais cara de reverter depois que existem
dados reais em produção.

Uma revisão técnica desta decisão apontou um problema real na
formulação original: **Row-Level Security (RLS) compartilhado** e
**schema dedicado por tenant** não são dois níveis de isolamento da
mesma arquitetura — são dois modelos operacionais diferentes, com
implicações distintas para migrations, consultas, pooling de conexão,
backups, observabilidade, jobs em lote, extensões e relatórios globais.
Uma decisão que menciona os dois como parte de um "híbrido", sem
comprometer-se com qual é o padrão real do MVP, é ambígua o suficiente
para gerar implementações inconsistentes entre desenvolvedores.

## Decisão

**Row-Level Security (RLS) compartilhado é o único modelo de
persistência multi-tenant da Fase 1 à Fase 7 do roadmap.** Não há
schema dedicado nem database dedicado no MVP — nenhuma exceção.

- Toda tabela que armazene dado de negócio DEVE incluir `tenant_id` e
  uma política de RLS correspondente desde a primeira migração — isso é
  parte do template obrigatório de criação de qualquer tabela nova, não
  uma adição posterior.
- **Database dedicado por tenant é uma decisão explicitamente futura**,
  que exigirá um novo ADR próprio, só deve ser considerada quando um
  caso real (ex: exigência regulatória específica de um cliente
  enterprise, ou necessidade de isolamento de performance comprovada)
  justificar o custo operacional adicional — e mesmo então, será tratada
  como projeto de migração específico para aquele tenant, não como
  parte do modelo padrão da plataforma.
- Schema per tenant **não será suportado** em nenhuma fase — foi
  descartado como opção intermediária por combinar a complexidade
  operacional de gerenciar N estruturas com a maior parte das
  limitações de escala do RLS.

## Racional

RLS como único padrão mantém o custo operacional baixo enquanto a base
de clientes é pequena (coerente com a decisão de monólito modular), sem
ambiguidade sobre qual modelo os desenvolvedores devem implementar. A
arquitetura não promete um híbrido que ainda não foi demonstrado em
produção — a promessa de portabilidade futura para isolamento mais
forte é tratada como trabalho de migração específico, não como
abstração de persistência já pronta para isso.

## Consequências

- Testes automatizados DEVEM verificar, em CI, que toda tabela com dado
  de tenant tem política de RLS ativa — isso é um teste obrigatório de
  pipeline, não uma checagem manual (ver Fase 1, critério de saída no
  documento mestre).
- Qualquer solicitação comercial de isolamento mais forte antes da Fase
  8 deve ser tratada como exceção comercial a ser avaliada contra
  AR-EXC-004 (contratos não podem prometer o que a governança ainda não
  suporta), não implementada ad-hoc.

## Riscos

RLS mal configurado é uma fonte real de vazamento de dados entre
tenants caso uma política seja esquecida em uma tabela nova. Mitigação:
teste de pipeline obrigatório (ver Consequências) que falha o build se
qualquer tabela com `tenant_id` não tiver RLS correspondente.

## Alternativas Rejeitadas

- **Híbrido (RLS + schema dedicado) desde o início:** rejeitada por ser
  ambígua na prática — "híbrido" sem um modelo padrão claro tende a
  significar "cada desenvolvedor decide", que é o oposto de governança.
- **Database per tenant desde o início:** rejeitada para o MVP por
  custo operacional (N bancos para gerenciar, migrar e monitorar) sem
  benefício correspondente enquanto a base de clientes é pequena.
- **Schema per tenant:** rejeitada por combinar as desvantagens
  operacionais de múltiplas estruturas com a maior parte das limitações
  de escala do RLS, sem um benefício claro sobre nenhuma das outras
  duas opções.

## Critérios para Revisão Futura

Revisar esta decisão apenas quando houver um caso real e específico
(não hipotético) de cliente enterprise exigindo isolamento mais forte
que RLS — nesse momento, um novo ADR trata a migração daquele tenant
específico, sem alterar o modelo padrão dos demais.
