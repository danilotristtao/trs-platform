# ADR-0027 — Extension by Contract, Never by Modification (Princípio Fundacional de Customização)

| Campo | Valor |
|---|---|
| **Status** | Aceito (consolida AR-EXT-001 a 006, já ratificados na Fase 0, num princípio único e nomeado; adiciona a árvore de decisão de AR-EXT-006, antes não operacionalizada) |
| **Data** | 2026-07-23 |
| **Responsáveis** | Fundação TRS |
| **Lessons Learned relacionados** | LL-005 (Customization Debt), LL-001 (Parameter Explosion), LL-002 (Business Rule Fragmentation) |
| **Requisitos AR-\* relacionados** | AR-EXT-001, AR-EXT-002, AR-EXT-003, AR-EXT-004, AR-EXT-006, AR-RUL-001 |
| **Fase do roadmap** | 0 (princípio fundacional — materialização mecânica continua Fase 2 a 4, sem alteração) |
| **Depende de** | ADR-0006, ADR-0008 |

## Contexto

O problema mais recorrente em software de gestão empresarial (LL-005): não existe fronteira formal entre "o que é do produto" e "o que é do cliente". A saída mais rápida sob pressão comercial é alterar o core diretamente para um cliente — e essa alteração se perde ou gera conflito grave na próxima atualização de versão, porque nunca foi um artefato separado, versionado e declarado.

O TRS já havia decidido o princípio oposto desde a Fase 0, mas de forma espalhada: `Extension` como conceito de vocabulário (ADR-0006), seis requisitos formais `AR-EXT-001` a `006` (Foundation v2, Parte IV.5) e a fronteira física `Module.Contracts` (Architecture Definition, já parte do skeleton atual). Nenhum documento, porém, consolidava isso como um princípio único e nomeado, nem operacionalizava `AR-EXT-006` ("demanda exclusiva de cliente DEVE ser classificada como capacidade reutilizável, extensão temporária ou requisito rejeitado") numa árvore de decisão concreta.

Rodando o checklist do `CLAUDE.md`:

1. **Lessons Learned:** LL-005 é o motivo direto deste ADR existir. LL-001 toca porque, sem uma árvore de decisão explícita, "extensão" viraria a resposta padrão pra qualquer pedido de cliente — o mesmo padrão de parâmetro-por-exceção, só que travestido de mecanismo formal (o que o texto de origem chamou corretamente de "Extension Debt"). LL-002 toca porque a mesma regra de negócio poderia acabar implementada tanto como Policy quanto como Extension, sem critério, se a árvore não existir.
2. **Requisitos AR-\*:** este ADR não cria requisito novo — consolida AR-EXT-001 a 006 já ratificados, e adiciona o mecanismo de classificação que AR-EXT-006 exige mas não detalhava. AR-RUL-001 (local autoritativo de regra) é o mesmo princípio do ADR-0008, aplicado agora à origem "pedido de cliente" em vez de "nova lógica em geral".
3. **Modo de falha específico:** o risco simétrico ao problema original — se este princípio virar desculpa para começar a construir Metadata Engine, Policy Engine e Extension Manifest agora, isso é antecipação sem caso de uso concreto (o mesmo antipadrão #10 do Foundation, Parte VIII). O ADR precisa fixar o princípio sem autorizar a implementação.
4. **Pertence à fase atual?** O princípio, sim — já pertence à Fase 0 (é consolidação de decisões já ratificadas lá). A árvore de decisão, sim, como conceito — não como ferramenta. A implementação de qualquer um dos mecanismos (Metadata, Policy, Workflow, Extension) continua Fase 2-4, sem mudança.

## Decisão

### O princípio

> **Extension by Contract, Never by Modification.** Toda necessidade específica de um cliente é atendida por um mecanismo suportado pela plataforma — nunca por alteração direta do código, schema ou comportamento central do core. O core evolui através de contratos versionados e deliberados (`Module.Contracts`, additive-first — Architecture Definition, Regra 17); ele nunca é editado por ou para um cliente específico.

Isso não é princípio novo — é a consolidação nomeada de `AR-EXT-001` (extensões não alteram core), do conceito `Extension` do ADR-0006, e da fronteira física `Module.Contracts` já presente no skeleton atual (`TRS.BuildingBlocks`/`TRS.Kernel`/`Modules`).

### A árvore de decisão (operacionaliza AR-EXT-006)

Toda demanda específica de cliente passa por esta classificação, nesta ordem — a primeira categoria que resolver a necessidade é a usada, nunca a mais poderosa disponível:

```text
Necessidade específica de cliente
        │
        ▼
Um campo/atributo adicional resolve?
        │ sim → Metadata (Kernel/Metadata, Fase 2+)
        │ não
        ▼
Uma regra de negócio parametrizável resolve?
   (ex.: limite de aprovação, condição de desconto)
        │ sim → Policy (Policy Engine, Fase 2, ADR-0008)
        │ não
        ▼
Uma sequência de aprovação/processo resolve?
        │ sim → Workflow (Workflow Engine, Fase 3)
        │ não
        ▼
Troca de dado com sistema externo resolve?
        │ sim → Integration (Adapter dedicado, ADR-0008)
        │ não
        ▼
Comportamento novo, real, que consome Capability existente?
        │ sim → Extension (Fase 4, manifesto AR-EXT-002)
        │ não
        ▼
Alteração direta do core
        │
        ▼
NUNCA — vira requisito rejeitado ou candidato a
nova Capability oficial do produto (AR-EXT-006)
```

Cada nó da árvore já tem seu mecanismo e sua fase definidos por ADR próprio (ADR-0008 para Policy/Integration; roadmap para Metadata/Workflow/Extension) — este ADR não inventa mecanismo novo, só formaliza a ordem de preferência e o destino final ("nunca alteração direta").

### O que isso já exige do código escrito hoje, mesmo sem nenhum mecanismo implementado

- Todo módulo expõe comportamento só via `Module.Contracts` (já regra do skeleton atual) — isso já é, por construção, a mesma disciplina que uma futura Extension usaria para consumir Capability, sem precisar refatoração pesada depois.
- Toda regra que hoje seria "resolvida rápido" com um campo condicional ou um `if` específico de cliente deve, em vez disso, ser nomeada explicitamente como candidata a um dos nós da árvore acima — mesmo que o mecanismo real (Policy, Extension) só exista em fase futura. Isso evita a alternativa real, que é o campo condicional entrar no código agora "provisoriamente" e nunca sair.

### O que continua explicitamente fora de escopo (Fase 2-4, sem mudança)

Formato do manifesto (`extension.yaml`/`.json`), SemVer vs. outra política de versionamento, desenho do Compatibility Analyzer, sandbox, certificação (AR-EXT-005) — tudo isso é implementação, decidida quando a fase correspondente começar formalmente, exatamente como já valia antes deste ADR.

### Correção de precisão no Gate da Fase 4 (Foundation v2, Parte VII)

O Gate atual — *"um upgrade de versão do core ocorre sem exigir patch manual em nenhuma extensão instalada"* — é impreciso: uma breaking change deliberada de contrato legitimamente pode exigir ajuste em uma Extension que dependia da versão antiga. Substituído por dois critérios distintos:

> **Upgrade compatível:** uma atualização do core que preserva compatibilidade dos contratos públicos usados por uma Extension nunca exige modificação manual dela.
> **Breaking change deliberada:** toda incompatibilidade entre uma nova versão do core e uma Extension instalada deve ser detectável automaticamente **antes** da atualização ser aplicada (AR-EXT-003) — nunca descoberta em produção depois.

## Consequências

- `docs/foundation/TRS_Foundation_v2.md` (Parte VII, Gate da Fase 4) é corrigido com os dois critérios acima, substituindo a promessa absoluta original.
- `CLAUDE.md` recebe uma regra curta lembrando o princípio e a árvore, para que nenhuma sessão futura resolva uma demanda de cliente com alteração direta de core "só dessa vez".
- Nenhuma mudança em código — o skeleton atual (`Module.Contracts`) já respeita o princípio por construção.

## Riscos

- A árvore de decisão pode ser lida como licença para forçar toda demanda em Policy/Workflow mesmo quando a resposta certa seria "requisito rejeitado" — mitigado pelo próprio AR-EXT-006, que inclui essa saída explicitamente como resultado legítimo.
- Nomear o princípio agora, sem nenhum mecanismo implementado, corre o risco de virar "só discurso" se ninguém aplicar a árvore de fato quando a primeira demanda de cliente real aparecer — mitigado por já valer, desde já, como critério de code review (a última seção da Decisão).

## Alternativas Rejeitadas

- **Não consolidar nada, deixar AR-EXT-001 a 006 espalhados** — rejeitada porque um princípio disperso em seis requisitos e um ADR de vocabulário é mais fácil de esquecer ou aplicar parcialmente do que um princípio nomeado e citável.
- **Começar a implementar Metadata/Policy/Extension agora, já que o princípio está claro** — rejeitada por violar o próprio phase-gating que este projeto aplica em toda outra decisão (ADR-0006 meta-regra, Regra 24/25 da Architecture Definition) — princípio decidido não é autorização de mecanismo construído.

## Critérios para Revisão Futura

- Revisar a árvore de decisão quando a primeira demanda real de cliente passar por ela — confirmar que as categorias fazem sentido na prática, não só na teoria.
- Revisar quando a Fase 2 (Policy) começar formalmente — a árvore referencia Policy Engine como nó, sem definir seus detalhes; a Fase 2 é quem define isso.
- Revisar quando a Fase 4 (Extension) começar formalmente — mesmo raciocínio, para o nó de Extension.
- `docs/foundation/TRS_Referencias_Extensibilidade.md` registra produtos de mercado (ServiceNow, Power Platform/Dataverse, ABP Framework, SAP Clean Core, OutSystems, Frappe, Salesforce, Odoo, Orchard Core) a estudar quando Metadata/Policy/Workflow/Extension forem desenhados de fato — lista de pesquisa, não normativa, não revisar como se fosse decisão.
