# ADR-0010 — Linguagem Principal de Backend

| Campo | Valor |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-07-12 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-002, LL-004, LL-006 |
| **Requisitos AR-\* relacionados** | AR-TXN-007, AR-RUL-005, AR-EXP-002 |
| **Fase do roadmap** | 0→1 (resolve o item pendente de VI.3.3; desbloqueia início de código real em `src/`) |
| **Depende de** | ADR-0006, ADR-0007, ADR-0008, ADR-0009 |

## Contexto

Foundation v2 (VI.3, item 3) deixou a linguagem principal de backend
explicitamente em aberto — "a definir via spike técnico" — para não
vestir uma escolha não ratificada como fato consumado antes da hora
(`src/README.md` documentava essa pendência). VI.1 já fixava duas
restrições para esse spike: uma única linguagem principal (sem
poliglotismo prematuro) e arquitetura de monólito modular (AR-TXN-007).

O spike consistiu em levantar candidatos realistas dentro dessas
restrições, avaliar trade-offs de cada um contra as necessidades já
travadas do projeto (PostgreSQL com RLS — ADR-0007; modelagem de
Aggregate/Value Object — ADR-0006; cálculo decisório e evidência
estruturada para o futuro Decision Envelope — ADR-0008, AR-EXP-002), e
decidir.

Quatro candidatos foram avaliados: **TypeScript/Node.js**, **Go**,
**Kotlin/JVM** e **C#/.NET**. Os quatro atendem, de forma equivalente,
as restrições técnicas centrais (driver Postgres maduro, suporte a
`SET app.tenant_id` por sessão, tipagem estática suficiente para
proteger invariante de Aggregate, observabilidade via OpenTelemetry).
Nenhum se destacou tecnicamente a ponto de decidir sozinho — a
diferença real entre eles, para este projeto, é marginal neste estágio.

**O fator que efetivamente decidiu não foi comparação técnica, e isso
precisa ficar registrado com honestidade, não maquiado:** nenhuma
pessoa envolvida na implementação da Fase 1 tinha experiência prévia
real de produção em nenhum dos quatro candidatos — com uma exceção:
experiência pessoal anterior do responsável pela decisão especificamente
com C#/.NET. Diante de um empate técnico genuíno entre as opções,
experiência real prévia foi tratada como o critério de maior peso para
reduzir risco de execução da Fase 1 — mais concretamente do que a
vantagem teórica teórica de unificar linguagem com o frontend
(TypeScript, já fixado em VI.1), que foi o principal argumento a favor
de Node.js e que este ADR conscientemente abre mão.

## Decisão

Adotar **C#/.NET** como linguagem principal única do backend do
monólito modular da TRS. Todos os módulos do Kernel — `tenancy`,
`identity`, `sales` (Fase 1) e, futuramente, `policy` e `workflow`
(Fases 2-3) — rodam como módulos internos do mesmo processo/deployment,
na mesma linguagem, conforme AR-RUL-005 (nenhum serviço físico
prematuro) e AR-TXN-007 (monólito modular no MVP).

Esta decisão **não unifica linguagem com o frontend** — TypeScript
permanece no frontend (VI.1), C#/.NET no backend. Essa divisão é aceita
conscientemente, não é uma lacuna: o contrato REST/JSON + OpenAPI (já
decidido em VI.1) é o único mecanismo de sincronização entre as duas
camadas, sem tipos compartilhados nativamente.

### Diretrizes de implementação decorrentes do vocabulário já ratificado

- **Value Objects** (`Money`, `EmailAddress`, `TaxId` — ADR-0009) devem
  usar `record` types do C#, que dão imutabilidade e igualdade por
  valor nativamente, sem código boilerplate adicional.
- **Invariantes de Aggregate** (ADR-0008) são protegidos em métodos e
  construtores da classe do Aggregate Root — nunca delegados a
  validação de ORM ou de camada de persistência.
- Acesso a PostgreSQL via **Npgsql**, com `SET app.tenant_id` executado
  centralmente na abertura de conexão (ver nota operacional do
  `README.md` sobre RLS) — nunca ad-hoc por query individual.
- Observabilidade via OpenTelemetry nativo do .NET, conforme já
  decidido em VI.1.

## Consequências

- `src/tenancy/`, `src/identity/`, `src/sales/` deixam de ser pastas
  pendentes (`src/README.md`) e passam a ser código C#/.NET real,
  cada um mapeado 1:1 a um Bounded Context, conforme ADR-0006.
- O contrato entre frontend (TypeScript) e backend (C#) precisa de
  geração de schema (OpenAPI a partir do backend como fonte de
  verdade, consumido via codegen no frontend) para não divergir
  silenciosamente — isso deveria ser automatizado em CI, não mantido
  manualmente.
- Contratação futura (Fase 7-8) depende de encontrar ou formar
  familiaridade com C#/.NET no time — diferente de uma escolha como
  Node, que teria aproveitado a familiaridade já existente com
  TypeScript do frontend.

## Riscos

- **Esta decisão foi tomada sem afinidade prévia real do time com
  nenhum candidato, exceto a experiência pessoal do decisor com
  C#/.NET.** Isso não é uma comparação técnica profunda decidindo por
  C# — é uma redução pragmática de risco de execução da Fase 1. Se essa
  premissa mudar (ex: entrada de novas pessoas no time sem nenhuma
  familiaridade com C#/.NET, e sem tempo hábil para adquiri-la), o
  argumento que sustenta esta decisão deixa de se sustentar.
- Divergência entre o modelo de tipos do frontend (TypeScript) e do
  backend (C#) é um ponto real de atrito que não existiria com Node —
  mitigado apenas pelo pipeline de geração de contrato OpenAPI, que
  precisa existir de fato, não como intenção.
- Mercado de contratação para C#/.NET, embora maduro, não é o mesmo
  que o de TypeScript — repor ou expandir o time depende de achar (ou
  formar) esse perfil especificamente.

## Alternativas Rejeitadas

- **TypeScript/Node.js** — rejeitada apesar de unificar linguagem com o
  frontend (seu principal ponto a favor), porque nenhuma pessoa
  envolvida, incluindo o decisor, tinha experiência prática prévia real
  nela equivalente à que existe com C#/.NET. A economia teórica de "uma
  linguagem só do banco à tela" foi julgada mais fraca, neste momento,
  do que a redução de risco de execução vinda de experiência real.
- **Go** — tecnicamente adequada (simplicidade, tipagem estática,
  concorrência nativa útil para outbox/workflow futuros), mas rejeitada
  pelo mesmo motivo: ausência de experiência prévia real de qualquer
  pessoa envolvida.
- **Kotlin/JVM** — ecossistema de DDD maduro e comparável a C#/.NET,
  rejeitada pelo mesmo motivo de ausência de experiência prévia real.

## Critérios para Revisão Futura

- Revisar esta decisão se o time crescer com pessoas sem nenhuma
  familiaridade com C#/.NET e sem conseguir formar experiência real
  dentro de um prazo razoável (ex.: primeiro trimestre da Fase 1) — o
  argumento central desta decisão (experiência prévia reduz risco de
  execução) deixaria de se sustentar.
- Revisar o pipeline de sincronização de contrato TypeScript↔C# se o
  custo de mantê-lo se mostrar, na prática, mais caro do que o
  esperado — a resposta não seria automaticamente unificar linguagem
  (frontend já está fechado por VI.1), e sim revisar como o contrato é
  gerado e consumido.
