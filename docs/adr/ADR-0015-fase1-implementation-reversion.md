# ADR-0015 — Reversão da Implementação da Fase 1 para Retomar Planejamento

| Campo | Valor |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-003 (Knowledge Concentration), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-CHG-003, AR-CHG-005 |
| **Fase do roadmap** | 1 (retomada de planejamento — nenhum ADR de domínio é revertido) |
| **Depende de** | ADR-0009, ADR-0011, ADR-0012, ADR-0013, ADR-0014 |

## Contexto

Uma primeira implementação da Fase 1 chegou a existir em C#/.NET:
Aggregates `Tenant`, `User`, `Customer`, `SalesOrder` (ADR-0009), Value
Objects (`EmailAddress`, `Money`, `TaxId`), Repository interfaces,
infraestrutura Postgres e SQL Server (EF Core + Npgsql/SqlClient,
`TenantContextInterceptor` via `SET app.tenant_id`/`SESSION_CONTEXT`),
migrations dos dois motores (`migrations/0001_init.sql`,
`migrations/sqlserver/0001_init.sql`) e testes unitários com cobertura
dos invariantes (`Trs.Tenancy.Tests`, `Trs.Identity.Tests`,
`Trs.Sales.Tests`), além do teste de CI de cobertura de RLS
(`tests/rls/check_rls_coverage.sql`).

Em 2026-07-19 o usuário decidiu apagar toda essa implementação e voltar
à fase de documentação/planejamento, por dois motivos declarados na
mesma sessão:

1. Existem **cadastros** (entidades de dado mestre/registro) ainda não
   discutidos nem definidos, que precisam ser fechados antes de
   retomar o módulo `sales` — continuar implementando em cima do
   modelo atual arriscava construir sobre uma base que ainda vai
   mudar.
2. A **estratégia de exclusão de dados** (lógica vs. física, com
   atenção a performance) ainda não tem decisão nem ADR — outra
   pergunta em aberto que toca diretamente o schema já implementado.

Rodando o checklist do `CLAUDE.md` (Foundation v2, Parte IX.1):

1. **Lessons Learned:** LL-003 toca diretamente — sem este registro,
   alguém revisando o repositório meses depois encontraria um
   `git log` com implementação real seguido de um commit de remoção em
   massa, sem nenhum documento explicando o motivo; o motivo ficaria
   só na memória de quem tomou a decisão, exatamente o risco que LL-003
   nomeia. LL-007 toca porque isso é uma mudança de estado
   arquitetural significativa (não uma correção pontual) e precisa de
   registro formal, não só mensagem de commit.
2. **Requisitos AR-\*:** AR-CHG-003 (alteração gera nova versão
   registrada, nunca sobrescrita silenciosa) é o motivo direto deste
   ADR existir — a reversão já aconteceu via commits de código; este
   documento é o registro formal que os acompanha. AR-CHG-005
   (rastreabilidade entre incidente/mudança e sua causa) é satisfeito
   por este ADR nomear exatamente o que foi removido e por quê.
3. **Modo de falha desta solução especificamente:** o risco não é a
   reversão em si (reverter código não testado em produção é barato),
   é a **falta de registro formal** dela — sem este ADR, o histórico
   de decisão fica só nos commits e na conversa, que não é o local
   normativo definido pelo próprio `CLAUDE.md` ("registre a resposta
   por escrito no ADR correspondente, não em um comentário de
   código").
4. **Pertence à fase atual?** Sim — isto é literalmente uma decisão
   sobre o estado da Fase 1, tomada dentro da própria Fase 1.

## Decisão

Registrar formalmente que, em 2026-07-19, a implementação de código da
Fase 1 foi removida por decisão do usuário, mantendo:

- **Os ADRs de domínio permanecem válidos e ratificados** — ADR-0009
  (`Tenant`, `User`, `SalesOrder`, `Customer`), ADR-0011 (dual-engine),
  ADR-0012 (auditoria), ADR-0013 (`Company`) **não são revertidos por
  este ADR.** Continuam sendo a especificação correta para quando a
  implementação for retomada.
- **O que foi removido** (commits `c8e66b0` e antecessores, branch
  `main`): `src/tenancy/`, `src/identity/`, `src/sales/`,
  `src/infrastructure/{postgres,sqlserver}/` (Aggregates, Value
  Objects, Repository interfaces e implementações), `migrations/`
  (Postgres e SQL Server), `tests/Trs.Tenancy.Tests/`,
  `tests/Trs.Identity.Tests/`, `tests/Trs.Sales.Tests/`,
  `tests/rls/check_rls_coverage.sql`.
- **O que permanece:** o skeleton vazio da arquitetura-alvo
  (`TRS.BuildingBlocks`, `TRS.Kernel/{Tenancy,Identity,Audit}`,
  `TRS.Infrastructure/Database/{PostgreSQL,SqlServer}`,
  `TRS.ApplicationHost`, `TRS.DatabaseMigrator`, `Modules/` vazio),
  criado conforme `TRS_Architecture_Definition.md`/ADR-0014, sem
  nenhuma lógica de domínio.
- **Pré-condição para retomar implementação:** os dois tópicos abertos
  (cadastros; estratégia de exclusão de dados) devem ser discutidos e,
  se resultarem em decisão cara de reverter, registrados em ADR próprio
  antes — não durante — a reimplementação dos Aggregates.

## Consequências

- `README.md` e `src/README.md` foram atualizados para refletir o
  estado real (skeleton vazio, Fase 1 não implementada) — eles não
  prevalecem sobre este ADR nem sobre os ADRs de domínio, mas não
  devem contradizê-los.
- Quando a implementação for retomada, ela deve reconstruir exatamente
  os Aggregates/invariantes já ratificados em ADR-0009/0011/0012/0013
  — este ADR não abre espaço para reabrir esse modelo de domínio, só
  para adicionar os cadastros que ainda serão definidos.
- Trabalho de implementação a partir de agora é escrito manualmente
  pelo usuário no VS Code, não por edição direta de arquivo por parte
  do Claude Code (acordado na mesma sessão) — Claude gera código como
  texto na conversa.

## Riscos

- Reimplementar do zero tem custo real de tempo, mesmo com a
  especificação (ADRs) já pronta — mitigado por o modelo de domínio em
  si não estar sendo redesenhado, só recriado em código.
- Se os dois tópicos pendentes (cadastros, exclusão de dados) não
  forem de fato fechados antes de retomar, esta reversão não cumpre o
  motivo que a justificou — vira só trabalho perdido sem ganho de
  clareza correspondente.

## Alternativas Rejeitadas

- **Não registrar a reversão formalmente, deixar só nos commits** —
  rejeitada por reproduzir exatamente o risco de LL-003 (conhecimento
  perdido, motivo só na memória de quem decidiu).
- **Reverter também os ADRs de domínio (0009/0011/0012/0013)** —
  rejeitada porque a decisão foi sobre o código, não sobre a validade
  do modelo de domínio já ratificado; misturar os dois obrigaria
  redecidir coisas que já têm consenso registrado.

## Critérios para Revisão Futura

- Revisar quando os cadastros forem definidos e a estratégia de
  exclusão de dados for decidida — nesse momento, a reimplementação
  dos Aggregates pode começar sem essas duas pendências em aberto.
- Se esta sequência (implementar → reverter → discutir → reimplementar)
  se repetir, é sinal de que falta uma etapa de validação de domínio
  antes de começar a escrever código — revisar o processo, não só
  repetir a reversão.
