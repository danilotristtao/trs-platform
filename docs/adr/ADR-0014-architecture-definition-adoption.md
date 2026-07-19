# ADR-0014 — Adoção da Architecture Definition como Referência de Longo Prazo

| Campo | Valor |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-07-18 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-002 (Business Rule Fragmentation), LL-005 (Customization Debt), LL-007 (Change Governance) |
| **Requisitos AR-\* relacionados** | AR-TXN-007, AR-RUL-005, AR-EXT-001, AR-EXT-002, AR-CHG-003 |
| **Fase do roadmap** | 0 (documento de referência; não força reestruturação de código na Fase 1) |
| **Depende de** | ADR-0006, ADR-0007, ADR-0008, ADR-0009, ADR-0010, ADR-0011, ADR-0012, ADR-0013 |

## Contexto

Durante a definição de uma proposta de "TRS — Definição da Arquitetura de Software e Estrutura da Solution", uma primeira versão foi reconciliada contra os ADRs ratificados (0006–0013) e apresentou conflitos reais, não apenas de forma:

1. Tratava filtro de aplicação/EF Core como o mecanismo de isolamento de tenant, quando o ADR-0011 rejeita explicitamente essa alternativa em favor de RLS/Security Policy no banco como garantia primária.
2. Colocava `User` dentro de um `KernelDbContext` não-tenant-scoped, contradizendo o ADR-0009 (`User` tem `tenant_id` obrigatório e RLS como qualquer outra entidade de negócio — só `Tenant` é exceção, por ser a raiz da fronteira).
3. Apresentava `Command`, `Outbox`/`Inbox`, `Saga`/`ProcessManager` e `AuthorizationEngine` como estrutura presente, sem distinguir "onde a capacidade pertence" de "se já pode ser implementada" — em tensão direta com a meta-regra do ADR-0006 (que nomeia `CommandBus`, `SagaCoordinator`, `ProcessManager` como vocabulário que não entra sem caso de uso concreto do roadmap) e com o ADR-0012 (que adia explicitamente `integration_events`/outbox para a Fase 3).
4. Assumia PostgreSQL como único motor de persistência (schemas `kernel`/`crm`/`finance`/`hcm`), sem engajar com a decisão comercial do ADR-0011 de suportar PostgreSQL e SQL Server em paridade desde a Fase 1.

Uma segunda versão do documento corrigiu os quatro pontos: separou formalmente "Architecture Definition" (onde uma capacidade pertence, caso exista) de "ADR + Roadmap" (se e quando ela é implementada); corrigiu o escopo de tenancy de `Identity`/`User`; tornou explícito que filtros de ORM são proteção adicional, nunca a garantia primária de isolamento (defesa em profundidade, fail-closed); e tratou cada motor de banco suportado como decisão estratégica com custo real, sujeita a ADR próprio. A segunda reconciliação não encontrou nenhum conflito adicional contra os ADRs 0006–0013.

Rodando o checklist do `CLAUDE.md` (Foundation v2, Parte IX.1):

1. **Lessons Learned:** LL-002 e LL-005 tocam diretamente — um monólito sem alvo estrutural explícito para crescimento de módulos tende a repetir o antipadrão de fragmentação de regra e dívida de customização assim que um segundo ou terceiro módulo de negócio aparece sem acordo prévio de fronteiras. LL-007 toca a hierarquia de governança formalizada na Seção 3 do documento (nenhuma mudança estrutural substitui ADR ratificado silenciosamente).
2. **Requisitos AR-\*:** AR-TXN-007 (MVP deveria usar monólito modular) e AR-RUL-005 (centralização lógica não deve impor serviço físico prematuro) são atendidos pela disciplina de extração seletiva (Seção 46) e pela regra de que Kernel/BuildingBlocks nunca conhecem Modules. AR-EXT-001/002 (extensões não alteram core, manifesto formal) são referenciados pela posição arquitetural de `Kernel/Extensibility`, sem autorizar implementação antecipada. AR-CHG-003 (alteração gera nova versão registrada, não sobrescrita silenciosa) é o próprio fundamento da Seção 3.
3. **Modo de falha desta solução especificamente:** o risco não é o conteúdo do documento, é o **status** dele — um documento de arquitetura de longo prazo pode ser lido, por engano, como autorização de implementação imediata, ou pode divergir silenciosamente de um ADR já ratificado sem que ninguém marque o conflito. Este ADR existe para eliminar exatamente esse modo de falha: fixando o documento na hierarquia de decisão (abaixo de ADRs ratificados, acima de Solution Structure) e deixando registrado, por escrito, o que ele não autoriza.
4. **Pertence à fase atual?** Como documento de referência, sim — Fase 0 (fundação), sem bloquear nenhuma implementação da Fase 1. Como estrutura de código a ser adotada agora, não — os cinco Aggregates ratificados (`Tenant`, `Company`, `User`, `SalesOrder`, `Customer`) e os três Modules (`tenancy`, `identity`, `sales`) continuam implementados no layout plano atual (`src/tenancy/`, `src/identity/`, `src/sales/`, `src/infrastructure/{postgres,sqlserver}/`).

## Decisão

Adotar o documento como `docs/foundation/TRS_Architecture_Definition.md` — a arquitetura de referência de longo prazo para a estrutura de solution do TRS (Modular Monolith + Microkernel-Inspired Kernel + DDD + Vertical Slice Architecture + Clean Architecture + Event-Driven quando aplicável + Selective Microservices quando justificável).

Este ADR explicitamente **não** faz duas coisas:

- **Não autoriza a implementação de nenhuma capacidade hoje bloqueada por outro ADR** — `Outbox`, `Inbox`, `Process Manager`/`Saga`, `Command`, `Authorization`/`Policy Engine`, `Metadata`, `Extensibility` continuam sujeitas às suas próprias fases e ADRs futuros (Regra 24 e 25 do documento). A posição arquitetural definida não é liberação de implementação.
- **Não exige reestruturação imediata de `src/`.** O layout plano atual (`src/tenancy/`, `src/identity/`, `src/sales/`, `src/infrastructure/{postgres,sqlserver}/`) permanece a implementação válida da Fase 1. A estrutura aninhada (`TRS.BuildingBlocks`/`TRS.Kernel`/`Modules`/`Processes`/`TRS.Infrastructure`) é o destino de longo prazo, não o ponto de partida obrigatório — migrar para ela antes de existir um segundo módulo de negócio real pagaria o custo de indireção (mais projetos, mais navegação, mais tempo de build) sem nenhum benefício correspondente ainda.

O que este ADR estabelece de fato: quando uma nova decisão estrutural for tomada (novo módulo, novo mecanismo cross-cutting, extração de código compartilhado), ela deve ser avaliada contra esta Architecture Definition como forma-alvo — não desenhada ad hoc. Se uma decisão da Fase 1 divergir do documento (permanecer no layout plano em vez de adotar o agrupamento `TRS.Kernel`), isso é aceito como adiamento deliberado, não como substituição do alvo.

## Consequências

- A seção "Estrutura do repositório" do `CLAUDE.md` passa a referenciar `docs/foundation/TRS_Architecture_Definition.md` como o layout-alvo de longo prazo, distinto do layout efetivamente implementado na Fase 1.
- ADRs estruturais futuros devem ser verificados quanto à consistência com este documento — e, pela própria Regra 25 do documento, um ADR prevalece sobre a arquitetura-alvo em caso de conflito, nunca o contrário.
- O documento passa a ser referência citável, não rascunho — qualquer revisão material de sua estrutura (renomear conteúdo do Kernel, adicionar nova capacidade) deve ser registrada como revisão deste ADR ou de um ADR sucessor, seguindo a mesma disciplina de AR-CHG-003.

## Riscos

- O documento pode se tornar obsoleto se não for revisitado quando código real começar a colidir com ele (ex.: o primeiro módulo de negócio além de `sales` pode revelar que a separação Kernel/Modules proposta não se sustenta em algum ponto específico) — mitigação: os Critérios para Revisão Futura abaixo funcionam como checklist vivo, não como aprovação única.
- Por permitir explicitamente que o código atual divirja do alvo (layout plano hoje, layout aninhado como destino), existe risco do destino nunca ser adotado na prática e virar ficção aspiracional — mitigação: o ADR que eventualmente motivar o segundo módulo de negócio deve decidir explicitamente se adota o layout aninhado naquele momento, não adiar indefinidamente sem decisão.
- Um documento de 48 seções é mais difícil de manter sincronizado com os ADRs do que um ADR individual — mitigação: qualquer alteração deve continuar sendo registrada como revisão deste ADR, nunca como edição silenciosa do arquivo em `docs/foundation/`.

## Alternativas Rejeitadas

- **Adotar a primeira versão do documento sem a camada de governança da Seção 2/3** — rejeitada na primeira reconciliação pelos quatro conflitos descritos no Contexto.
- **Rejeitar o documento inteiramente e manter só o layout plano de `src/` como única declaração arquitetural** — rejeitada porque um monólito sem alvo explícito para crescimento de módulos tende a repetir LL-002 assim que um segundo ou terceiro módulo for adicionado sem acordo prévio de fronteiras.
- **Adotar o documento e migrar `src/` para o layout aninhado imediatamente** — rejeitada por antecipar uma reestruturação sem nenhum código de um segundo módulo que a justifique; o custo de indireção seria pago sem benefício correspondente na Fase 1.

## Critérios para Revisão Futura

- Revisitar quando um segundo módulo de negócio (além de `sales`) entrar em implementação — decidir explicitamente, naquele momento, se o layout aninhado `TRS.Kernel`/`Modules` é adotado.
- Revisitar sempre que um dos ADRs referenciados (0006–0013) for revisado de forma que altere vocabulário ou phase-gating assumido por este documento.
- Revisitar quando o suporte a Oracle (ADR-0011, arquitetado mas não implementado) se tornar real — confirmar se a separação `TRS.Infrastructure.Database` por provider se sustenta para um terceiro motor.
