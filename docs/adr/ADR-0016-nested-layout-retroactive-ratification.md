# ADR-0016 — Ratificação Retroativa da Adoção do Layout Aninhado

| Campo | Valor |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-07-19 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-CHG-003, AR-RUL-005 |
| **Fase do roadmap** | 0/1 (fecha lacuna de governança aberta desde a adoção do ADR-0014) |
| **Revisa** | ADR-0014 (o critério de ativação da migração, não a arquitetura-alvo em si) |
| **Depende de** | ADR-0014, ADR-0015 |

## Contexto

O ADR-0014 fixou um critério explícito e único para migrar `src/` do
layout plano para o layout aninhado (`TRS.BuildingBlocks`/`TRS.Kernel`/
`Modules`/`TRS.Infrastructure`): *"Revisitar quando um segundo módulo
de negócio (além de `sales`) entrar em implementação — decidir
explicitamente, naquele momento, se o layout aninhado... é adotado."*

Esse critério nunca foi satisfeito — nenhum segundo módulo de negócio
chegou a existir. Mesmo assim, na mesma sessão em que o ADR-0014 foi
ratificado, a migração física para o layout aninhado foi executada
(criação de `TRS.BuildingBlocks`, `TRS.Kernel/{Tenancy,Identity,Audit}`,
`TRS.Infrastructure/Database/{PostgreSQL,SqlServer}`,
`TRS.ApplicationHost`, `TRS.DatabaseMigrator`), a pedido explícito do
usuário, sem que nenhum ADR revisasse ou justificasse formalmente por
que o critério estava sendo antecipado. O ADR-0015, escrito
posteriormente na mesma sessão, registrou a remoção do código de
domínio (Aggregates, Repositories, migrations, testes), mas não tratou
dessa divergência específica — o ADR-0014 e o estado físico do
repositório ficaram desalinhados sem registro, exatamente o tipo de
lacuna que LL-007 (Change Governance) existe para prevenir.

Rodando o checklist do `CLAUDE.md` (Foundation v2, Parte IX.1):

1. **Lessons Learned:** LL-007 toca diretamente — um critério de
   ativação foi definido por escrito e depois ignorado na prática sem
   nenhum registro do porquê, o que é precisamente a ausência de
   governança de mudança que o LL nomeia.
2. **Requisitos AR-\*:** AR-CHG-003 (alteração gera nova versão
   registrada, nunca sobrescrita silenciosa) é satisfeito por este
   próprio ADR. AR-RUL-005 (centralização lógica não deve impor
   estrutura física prematura) é o requisito que o critério original do
   ADR-0014 protegia — este ADR avalia se essa proteção foi violada de
   fato.
3. **Modo de falha desta solução especificamente:** o risco que o
   ADR-0014 quis evitar era pagar custo de indireção (mais projetos,
   mais navegação, mais tempo de build) **antes** de existir código
   real que se beneficiasse da separação. Verificando o estado atual do
   repositório: não existe nenhum Aggregate implementado em nenhum dos
   dois módulos de Kernel nem em `Modules/` (ADR-0015 removeu toda a
   implementação de domínio). Ou seja, **o custo que o critério
   protegia contra nunca chegou a ser pago** — não há código real sendo
   movido ou fragmentado prematuramente, porque não há código real.
4. **Pertence à fase atual?** Sim — é uma correção de rastreabilidade
   dentro da própria Fase 1/0, não uma antecipação de fase futura.

## Decisão

Ratificar retroativamente a manutenção do layout aninhado como
estrutura física atual de `src/`, revisando o critério de ativação do
ADR-0014 nos seguintes termos:

- **O critério original do ADR-0014 permanece correto em princípio**
  (não migrar estrutura sem necessidade real) — o que este ADR corrige
  é que ele foi contornado sem registro, não que ele estivesse errado.
- **Não reverter ao layout plano agora.** Reverter exigiria recriar uma
  estrutura plana vazia só para, futuramente, migrar de novo para a
  mesma estrutura aninhada — puro trabalho repetido, sem nenhum código
  real protegido em nenhum dos dois cenários, já que hoje `src/` não
  contém nenhuma lógica de domínio (ADR-0015). O dano que o critério
  original evitava (mover código já escrito) não existe neste caso
  concreto.
- **O critério do ADR-0014 é considerado satisfeito de forma
  degenerada**: como não havia código de um segundo módulo (nem de um
  primeiro, depois do ADR-0015) para proteger, a pergunta "vale a pena
  pagar o custo de indireção" teve resposta trivial (custo zero,
  porque não há código a mover). Isso não estabelece precedente para
  ignorar o mesmo critério no futuro **quando já existir código real**
  em `Modules/` — a partir do primeiro módulo de negócio realmente
  implementado (provavelmente `sales`, quando os cadastros forem
  definidos), qualquer nova reestruturação física volta a exigir ADR
  próprio, com o mesmo rigor do ADR-0014 original.

## Consequências

- O critério de revisão futura do ADR-0014 é marcado como resolvido
  por este ADR — a divergência está fechada e rastreada, não mais em
  aberto silenciosamente.
- Da próxima vez que a estrutura física de `src/` for alterada depois
  que existir código de domínio real, isso exige ADR próprio,
  avaliando o custo de indireção contra o código real existente — não
  pode se apoiar neste ADR como precedente, porque a condição que o
  tornou aceitável aqui (zero código real) não se repete.

## Riscos

- Este ADR poderia ser lido, incorretamente, como "critérios de
  ativação podem ser ignorados se alguém decidir depois que não
  importou" — mitigado pelo parágrafo explícito acima negando esse
  precedente para quando houver código real.

## Alternativas Rejeitadas

- **Reverter `src/` ao layout plano até o critério do ADR-0014 ser
  satisfeito por um segundo módulo real** — rejeitada por gerar
  trabalho repetido sem proteger nenhum código real em nenhum dos dois
  cenários (hoje `src/` está vazio de domínio em ambos os layouts
  possíveis).
- **Não registrar nada, deixar a divergência sem ADR** — rejeitada por
  ser exatamente o risco que LL-007 e a auditoria documental deste
  projeto identificaram como lacuna real de governança.

## Critérios para Revisão Futura

- Revisar se, no futuro, um novo critério de ativação for contornado
  da mesma forma (decisão executada antes do gatilho declarado) — se
  isso se repetir, é sinal de que o processo de registrar ADR antes de
  executar mudança estrutural não está sendo seguido de fato, e
  precisa de correção de processo, não de mais ADRs retroativos.
