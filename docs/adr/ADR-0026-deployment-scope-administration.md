# ADR-0026 — Deployment Scope Administration na Fase 1

| Campo | Valor |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-07-20 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-001 (Parameter Explosion), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-RUL-001, AR-EXT-001 |
| **Fase do roadmap** | 1 (posiciona sem implementar mecanismo completo) |
| **Depende de** | ADR-0009, ADR-0017, ADR-0021, ADR-0024 |

## Contexto

O ADR-0017 exige que a gestão de dado Deployment Scope (ex.: `CustomIdentifierType`) seja "restrita a uma autoridade administrativa de nível de deployment... nenhum Tenant altera unilateralmente a definição compartilhada" — mas o modelo de identidade da Fase 1 (ADR-0009) só define dois papéis, `tenant_admin` e `member`, ambos escopados a **um** Tenant, e instrui explicitamente a não expandir esse enum informalmente antes da Fase 2. Não existe, hoje, nenhum ator no modelo de `User` capaz de exercer essa autoridade — ela é, por definição, cross-tenant dentro do mesmo deployment, e `tenant_admin` nunca é.

Um problema relacionado: a RLS de `deployment_identifier_type_tenant_availability` (ADR-0024, Tenant Scope, `tenant_id = current_setting(...)`) protege corretamente o acesso operacional normal — mas a própria operação administrativa de "tornar `CUSTOM_X` disponível para os Tenants A e B, não para o C" é, por natureza, uma escrita que precisa enxergar/tocar múltiplos `tenant_id` ao mesmo tempo. RLS padrão de Tenant Scope não permite isso a nenhuma sessão comum.

Rodando o checklist do `CLAUDE.md`:

1. **Lessons Learned:** LL-001 toca porque inventar um papel novo só para resolver isso, sem necessidade concreta imediata, seria o mesmo antipadrão de parâmetro/papel adicionado por conveniência que o projeto evita desde o ADR-0006. LL-007 toca por ser uma decisão de governança de acesso que precisa registro formal antes de qualquer implementação de `CustomIdentifierType`.
2. **Requisitos AR-\*:** AR-RUL-001 exige que a decisão de "quem pode fazer isso" seja explícita, não implícita por ausência. AR-EXT-001 é relevante porque `CustomIdentifierType` é, na prática, uma forma de customização controlada — a mesma disciplina de não deixar o core ser alterado por acesso irrestrito se aplica à sua administração.
3. **Modo de falha específico:** duas soluções ruins são simétricas aqui — (a) inventar `deployment_admin` como terceiro valor de `User.role` agora, antecipando RBAC/ABAC sem necessidade (viola a instrução explícita do ADR-0009); (b) deixar sem nenhuma definição, na esperança de que "alguém" resolva quando `CustomIdentifierType` for implementado, o que na prática significaria alguém improvisando um acesso privilegiado ad-hoc sem revisão.
4. **Pertence à fase atual?** Como posicionamento conceitual, sim (evita ambiguidade antes de `CustomIdentifierType` ser implementado). Como mecanismo completo, não — não há caso de uso comercial concreto de `CustomIdentifierType` implementado ainda.

## Decisão

### Fase 1: administração de Deployment Scope é operação interna da TRS, não papel de Tenant

Deployment Scope Administration **não é** um `User.role` do Tenant, **não é** exposta na UI normal de nenhum Tenant, e **não** entra no modelo de `User`/`role` do ADR-0009 nesta fase. É tratada como operação de control plane/administração privilegiada da própria TRS — equivalente, em espírito, a como a distribuição de `platform_identifier_types` (ADR-0017/0021) já é "processo de distribuição da TRS", não uma Capability exposta a usuário de Tenant.

Isso preserva integralmente a decisão do ADR-0009 de não expandir `role` informalmente — nenhum terceiro valor é adicionado ao enum.

### Acesso privilegiado à tabela de disponibilidade

A RLS padrão de Tenant Scope em `deployment_identifier_type_tenant_availability` (ADR-0024) continua protegendo todo acesso operacional comum, sem exceção. A operação administrativa (gerir disponibilidade cross-tenant) usa um **caminho privilegiado e auditado, separado do caminho de aplicação normal de qualquer Tenant** — não uma sessão de `tenant_admin` com bypass, e não uma política de RLS mais permissiva aplicada a todos.

O mecanismo concreto (papel de banco separado, processo administrativo fora da aplicação principal, ou control plane dedicado) **não é decidido por este ADR** — só o princípio: RLS Tenant Scope protege acesso operacional; administração de Deployment Scope usa via distinta, nunca a mesma sessão/role que atende requisição comum de Tenant.

### Pré-condição de implementação

`CustomIdentifierType` (ou qualquer outro dado Deployment Scope real) não deve ser exposto/implementado antes de existir uma decisão concreta do mecanismo de acesso administrativo — este ADR posiciona o conceito, não libera implementação (mesma disciplina de phase-gating do ADR-0006/Regra 24-25 da `TRS_Architecture_Definition.md`).

## Consequências

- `ADR-0017` e `ADR-0018` continuam válidos sem alteração — este ADR só remove a ambiguidade sobre "quem" exerce a autoridade que eles já exigiam.
- Nenhuma implementação de `CustomIdentifierType` deve começar até que o mecanismo de acesso privilegiado seja decidido em ADR próprio, quando houver caso de uso comercial concreto.
- `User.role` permanece com exatamente dois valores (ADR-0009), sem expansão.

## Riscos

- Adiar a decisão do mecanismo concreto significa que, quando `CustomIdentifierType` for realmente necessário, ainda faltará trabalho de design antes de codificar — aceito, é melhor que antecipar um mecanismo sem caso de uso real para validá-lo.
- "Operação interna da TRS" sem nenhum detalhe de implementação pode ser lido como uma escotilha não auditada se não for lembrado, no futuro ADR que definir o mecanismo, que auditoria (ADR-0012) é obrigatória para essa operação como para qualquer alteração relevante.

## Alternativas Rejeitadas

- **Adicionar `deployment_admin` como terceiro valor de `User.role` agora** — rejeitada por violar diretamente a instrução do ADR-0009 de não expandir o enum antes da Fase 2, sem nenhum caso de uso implementado que o exija.
- **Deixar `tenant_admin` de qualquer Tenant do deployment gerenciar disponibilidade cross-tenant** — rejeitada porque o próprio ADR-0017 já exclui isso explicitamente ("nenhum Tenant altera unilateralmente a definição compartilhada") — um `tenant_admin` é, por definição, de um único Tenant.
- **Política de RLS permissiva especial para administração** (ex.: uma claim/flag que desliga o filtro de tenant) — rejeitada por criar um caminho de bypass de RLS genérico, exatamente o tipo de exceção de segurança ampla que o ADR-0007 evita.

## Critérios para Revisão Futura

- Definir o mecanismo concreto de acesso privilegiado quando `CustomIdentifierType` (ou outro dado Deployment Scope real) tiver caso de uso comercial concreto — não antecipar.
- Revisar se, nesse momento, uma identidade administrativa própria (distinta de `User` de Tenant) precisa ser formalizada como novo conceito — decisão da Fase 2, não desta.
