# ADR-0011 — Suporte a Múltiplos Bancos de Dados (Revisão Parcial de ADR-0007)

| Campo | Valor |
|---|---|
| **Status** | Aceito (revisa parcialmente ADR-0007 — mantém RLS obrigatório quando o motor for PostgreSQL, remove a exclusividade de PostgreSQL como único motor suportado) |
| **Data** | 2026-07-12 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-004, LL-007, LL-008 |
| **Requisitos AR-\* relacionados** | AR-TXN-001, AR-TXN-002, AR-KNW-003, AR-EXP-005, AR-CHG-003, AR-EXC-004 |
| **Fase do roadmap** | 1 (revisão ocorre dentro da própria Fase 1, antes de qualquer código de aplicação escrito em `src/`) |
| **Revisa** | ADR-0007 (parcialmente — ver Status) |

## Contexto

ADR-0007 estabeleceu Row-Level Security compartilhado como único modelo
de persistência multi-tenant "da Fase 1 à Fase 7 do roadmap... sem
exceção", justamente por ser a decisão técnica mais cara de reverter
do projeto. O próprio ADR-0007 também previa que "qualquer solicitação
comercial de isolamento mais forte antes da Fase 8 deve ser tratada
como exceção comercial a ser avaliada... não implementada ad-hoc"
(AR-EXC-004).

Durante a definição da linguagem de backend (ADR-0010) e da camada de
acesso a dado, surgiu uma decisão comercial explícita, não amarrada a
um cliente nomeado: a plataforma deve permitir que o cliente escolha o
motor de banco de dados mais adequado ao cenário dele — começando com
**PostgreSQL e SQL Server suportados de fato desde a Fase 1**, e
**Oracle arquitetado para ser suportado no futuro**, sem exigir
reescrita da camada de domínio quando isso acontecer.

**O motivo registrado é comercial, não técnico nem regulatório
específico:** captar clientes que já têm infraestrutura e investimento
em SQL Server (e futuramente Oracle) sem exigir migração de banco como
pré-condição de venda. Isso é registrado aqui com a mesma honestidade
que o ADR-0010 registrou "experiência prévia" como motivo real da
escolha de linguagem — este ADR não finge que houve uma reavaliação
técnica que mudou a conclusão original de ADR-0007; o que mudou foi uma
decisão de negócio explícita, tomada com conhecimento do custo técnico
envolvido (discutido e registrado durante o processo que levou a este
ADR).

Diferente de uma exceção comercial implementada ad-hoc (o que o
ADR-0007 original alertava contra), esta decisão está sendo formalizada
como ADR próprio, com o custo técnico nomeado explicitamente — que é a
forma correta de tratar isso segundo a própria governança do projeto
(AR-CHG-003: alteração gera nova versão registrada, não sobrescrita
silenciosa).

## Decisão

**PostgreSQL deixa de ser o único motor de banco de dados suportado.**
A partir deste ADR:

- **PostgreSQL** e **SQL Server** são motores de produção plenamente
  suportados desde a Fase 1.
- **Oracle** não é implementado agora — a arquitetura deve garantir que
  adicionar suporte a ele no futuro seja um projeto delimitado (nova
  implementação de infraestrutura + migrations + política de
  isolamento daquele motor), não uma reescrita do domínio.
- **RLS continua obrigatório e não-negociável quando o motor for
  PostgreSQL** — ADR-0007 permanece válido nesse ponto específico, sem
  exceção. O que este ADR revisa é exclusivamente a exclusividade de
  PostgreSQL como único motor da plataforma.
- Para **SQL Server**, o mecanismo equivalente de isolamento por tenant
  é **Security Policies + predicate functions**, usando
  `SESSION_CONTEXT` como equivalente funcional a
  `current_setting('app.tenant_id')` do Postgres — com a mesma garantia
  de falha fechada: uma sessão sem contexto de tenant definido não deve
  retornar nenhuma linha.

### Diretrizes de implementação

- **Repository pattern obrigatório, sem exceção.** Aggregates e regras
  de negócio (ADR-0006, ADR-0008, ADR-0009) só enxergam interfaces de
  Repository (`ISalesOrderRepository`, `ICustomerRepository`, etc.) —
  nunca `DbContext` do EF Core ou SQL específico de motor diretamente.
  Esta é a única garantia real de que adicionar Oracle depois não exige
  tocar o domínio.
- Cada motor suportado tem sua **própria implementação de
  infraestrutura** (ex.: módulo/projeto `infrastructure/postgres/`,
  `infrastructure/sqlserver/`), implementando os mesmos contratos de
  Repository definidos no domínio.
- **Migrations mantidas em paralelo, para os dois motores, a partir de
  agora e para sempre** — `migrations/` (Postgres, SQL puro) precisa de
  um conjunto equivalente em T-SQL para SQL Server, incluindo
  invariantes hoje implementadas via trigger (ex.: moeda única por
  `SalesOrder`, ADR-0009).
- **Teste de isolamento obrigatório por motor.**
  `tests/rls/check_rls_coverage.sql` cobre hoje só Postgres; precisa de
  um equivalente que falhe o build se uma tabela nova no SQL Server não
  tiver Security Policy correspondente. Nenhuma tabela é considerada
  "coberta" sem os dois testes passando.
- EF Core (ADR-0010) passa a ser usado de fato em seu modo
  multi-provider (`Npgsql.EntityFrameworkCore.PostgreSQL` +
  `Microsoft.EntityFrameworkCore.SqlServer`) — uma das poucas vantagens
  genuínas dessa escolha de ORM que se tornam relevantes só a partir
  desta decisão.

## Consequências

- Custo de manutenção contínuo e real, não pontual: toda migration nova
  e toda mudança de schema exige duas implementações e dois testes de
  isolamento, indefinidamente (ou até decisão em contrário).
- O tempo de entrega do gate da Fase 1 (autorização + autor + motivo em
  toda operação) é diretamente afetado, já que agora inclui construir e
  testar duas implementações de infraestrutura em vez de uma.
- `src/` deve refletir a separação desde a primeira pasta criada:
  módulos de domínio (`tenancy/`, `identity/`, `sales/`) separados de
  módulos de infraestrutura por motor.

## Riscos

- **Esta é uma aposta comercial antecipada, sem cliente nomeado
  exigindo isso agora.** O próprio ADR-0007 original já enquadrava esse
  tipo de solicitação como exceção a ser avaliada, não implementada
  livremente (AR-EXC-004). Formalizar como ADR não elimina o risco de
  mercado — só garante que a decisão seja rastreável e revisável, não
  silenciosa. Se a aposta não se confirmar, o custo de manutenção
  duplicada continua sendo pago sem retorno correspondente.
- Duplicar a camada de isolamento **dobra** o risco que o próprio
  ADR-0007 já nomeia: "RLS mal configurado é fonte real de vazamento de
  dado entre tenants". Agora existem dois lugares onde esquecer a
  política de isolamento em uma tabela nova vaza dado entre tenants, não
  um. Mitigação: os dois testes de CI descritos acima são obrigatórios,
  não opcionais.
- Sem disciplina real de Repository pattern desde o primeiro Aggregate
  implementado, a arquitetura degrada rapidamente para SQL específico
  de motor vazando para o domínio — a promessa de "Oracle sem
  reescrita" depende inteiramente dessa disciplina sendo mantida desde
  a primeira linha de código, não apenas da intenção registrada aqui.

## Alternativas Rejeitadas

- **Manter PostgreSQL único (ADR-0007 sem revisão)** — era a
  recomendação técnica default até esta decisão comercial explícita
  mudar o contexto; rejeitada porque a decisão de atender clientes com
  infraestrutura SQL Server já instalada foi julgada, pelo responsável
  pela decisão, como mais importante do que o custo de manutenção
  duplicada.
- **Suportar os três motores (Postgres, SQL Server, Oracle) já na Fase
  1** — rejeitada por triplicar o custo de manutenção da camada de
  isolamento sem necessidade imediata; Oracle fica arquitetado, não
  implementado, até haver necessidade real.
- **Abandonar isolamento em nível de banco (RLS/Security Policy) em
  favor de filtro só na aplicação/EF Core (global query filters) para
  simplificar o suporte multi-motor** — rejeitada por eliminar a
  garantia de falha fechada que é a proteção mais forte contra
  vazamento de dado entre tenants; a defesa em profundidade continua
  exigida, não confiança exclusiva na camada de aplicação.

## Critérios para Revisão Futura

- Revisar se a aposta comercial não se confirmar (ex.: nenhum cliente
  real adotar o suporte a SQL Server dentro de um horizonte razoável,
  como os primeiros 2-3 trimestres após o piloto comercial da Fase 7) —
  nesse caso, avaliar descontinuar a manutenção paralela.
- Revisar quando Oracle se tornar necessidade real e concreta (não
  hipotética): nesse momento, a arquitetura de Repository já deveria
  permitir a implementação sem tocar o domínio. Se isso não se
  confirmar na prática, é sinal de que a disciplina arquitetural
  descrita aqui não foi mantida, e precisa correção antes de prosseguir.
- Revisar o teste de CI de isolamento equivalente para SQL Server antes
  de qualquer tabela de produção rodar nesse motor.
