# TRS Platform

AI-Native Business Operating Platform. Ver `docs/foundation/TRS_Foundation_v2.md`
para a visão completa, `docs/adr/` para decisões ratificadas, e
`docs/lessons-learned/` para o histórico que motivou cada uma delas.

`CLAUDE.md`, na raiz, é lido automaticamente pelo Claude Code em toda
sessão neste repositório — comece por ele.

## Estado atual

- **Fase 0 (Fundação):** concluída — ADR-0006 a ADR-0011 ratificados,
  além do ADR-0014 (adoção da arquitetura-alvo de longo prazo,
  `docs/foundation/TRS_Architecture_Definition.md`). ADR-0015 (reversão
  da implementação de Fase 1) e ADR-0016 (correção de governança sobre
  o layout de `src/`) são decisões posteriores sobre o estado atual do
  projeto, não pertencem à Fase 0 em si — ver `docs/adr/` para o índice
  completo e sempre atualizado.
- **Fase 1 (Vertical Zero):** em planejamento/documentação — **nenhum
  Aggregate está implementado no momento.** Uma primeira implementação
  (`Tenant`, `User`, `Customer`, `SalesOrder`, repositories Postgres/SQL
  Server, migrations e testes) chegou a existir e foi deliberadamente
  removida em 2026-07-19 para retomar a fase de planejamento antes de
  reescrever o código — decisão do usuário, não reversão de nenhum ADR.
  - `src/`: só o skeleton vazio da arquitetura-alvo definida em
    `TRS_Architecture_Definition.md` (ver `src/README.md` para detalhe).
    Sem migrations, sem testes, sem lógica de domínio.
  - Dois tópicos aguardando discussão antes de reescrever código:
    1. **Cadastros** a definir antes do módulo `sales` (o que são, onde
       pertencem no domínio).
    2. **Estratégia de exclusão de dados** (lógica vs. física,
       considerando performance) — ainda sem ADR.
  - Implementação de código é feita manualmente pelo usuário no VS
    Code; Claude Code participa gerando código sugerido na conversa,
    não escrevendo diretamente nos arquivos do projeto.

## Isolamento de tenant (para quando a implementação retomar)

RLS (PostgreSQL) e Security Policy (SQL Server) serão obrigatórios e
equivalentes em garantia desde a primeira migração de cada motor —
nenhum dos dois é opcional (ADR-0007, ADR-0011). Falha de contexto de
tenant deve sempre falhar fechada (nenhuma linha retornada), nunca
aberta, em qualquer um dos dois motores — ver `TRS_Architecture_Definition.md`,
Regra 11/12.
