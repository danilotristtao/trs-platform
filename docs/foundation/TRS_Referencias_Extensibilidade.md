# TRS — Referências de Mercado para Extensibilidade e Customização

**Status:** Lista de pesquisa, não normativa — não é ADR, não autoriza
nenhuma implementação. Ligada ao ADR-0027 (Extension by Contract,
Never by Modification).
**Quando usar:** ao iniciar o desenho real de Metadata (Fase 2),
Policy/Workflow (Fase 2-3) ou Extension (Fase 4) — não antes.
**Data:** 2026-07-23

---

## Objetivo

Registrar, antes de esquecer, quais produtos de mercado valem estudo
direto quando a TRS Platform desenhar de fato os mecanismos que
operacionalizam o princípio do ADR-0027 (Metadata, Policy, Workflow,
Extension). A pergunta que orienta o estudo não é "como o produto X
faz", é "como cada um resolve o mesmo problema do LL-005, com que
vantagens e limitações — o que a TRS adota, adapta ou evita
deliberadamente".

Números de mercado (nota G2, estrelas GitHub) citados abaixo foram
verificados via busca em 2026-07-23, quando possível — não repetir
número não conferido.

## Prioridade alta — estudar primeiro quando a fase relevante começar

| Produto | O que estudar para a TRS | Dado de mercado verificado |
|---|---|---|
| **ServiceNow** | Scoped Applications — isolamento de extensão, Application Repository, comportamento de upgrade | Taxa de renovação de cliente 97-98% (2026, dados de investidor) |
| **Microsoft Power Platform / Dataverse** | Solutions, Managed vs. Unmanaged, Publishers/namespaces, transporte entre ambientes — customização como artefato governado e transportável | — |
| **ABP Framework** | Arquitetura modular em C#/.NET, DDD, multi-tenancy — referência técnica de implementação, não de produto. **Ressalva:** é maximalista (Event Bus distribuído, Background Jobs, Permission Management embutidos) — estudar os padrões, não adotar o framework inteiro nem seu vocabulário sem caso de uso (ADR-0006) | ~14 mil estrelas no GitHub |
| **SAP Clean Core** | É o mesmo princípio do ADR-0027 ("nunca customizar o core, sempre via extensibilidade in-app ou side-by-side"), testado na escala mais extrema que existe — décadas de clientes presos em upgrade por terem customizado o core ABAP direto. Catálogo de erro real, não teoria | — |
| **OutSystems** | Modules com dependências versionadas, LifeTime (gestão de ciclo de vida entre ambientes), Forge (componentes reutilizáveis) — outro exemplo de customização como artefato transportável, forte em governança de deploy | Líder no G2 Grid Winter 2026 (Low-Code); 1.117 reviews, 98% com 4-5 estrelas, satisfação 99/100 na categoria |

## Prioridade média — referência secundária, estudar para contraste

| Produto | O que estudar para a TRS | Dado de mercado verificado |
|---|---|---|
| **Frappe Framework / ERPNext** | Metadata-driven (DocType) — onde metadata resolve bem e onde começa a virar dívida, contraste direto com a preferência da TRS por modelo de domínio fortemente tipado (ADR-0006/0008) | ~10,5 mil estrelas no GitHub (`frappe/frappe`) |
| **Salesforce** | Packages (Managed/Unmanaged) — mesma família conceitual de Dataverse Solutions, em escala de marketplace (AppExchange) — mais relevante pra Fase 8-9 (SDK/Marketplace) que pra Fase 4 | G2: 4,4/5 — **atenção:** contagem de reviews citada anteriormente (~95 mil) não foi confirmada; a busca real mostrou ~25.415 reviews (Salesforce Sales Cloud) |
| **Odoo** | Estudo de caso do que evitar — dívida de customização e dor de upgrade real e documentada em módulos customizados | G2: métricas de Facilidade de Uso (8,4-8,5/10) e Customização (8,5/10) confirmadas; nota geral de 5 estrelas não confirmada |
| **Orchard Core** | Separação framework-primeiro → produto-depois (Orchard Core Framework → Orchard CMS) — paralelo estrutural com Kernel → CRM/Finance/HCM, menos relevante para o problema de customização em si | — |

## Prioridade baixa — mencionar, não aprofundar sem sinal real

Shopify (apps/extensões versionadas via contrato), nopCommerce
(plugins reais em .NET) — comparáveis, mas nenhum tem urgência de
estudo até haver sinal comercial concreto de necessidade.

## Como isso se conecta às fases do roadmap

```text
ADR-0027 (princípio, Fase 0)
        │
        ▼
Fase 2 — Metadata / Policy Runtime
        │   └── estudar: Power Platform (Solutions), Frappe (DocType, como contraste)
        ▼
Fase 3 — Workflow
        │   └── estudar: Power Platform (processos), OutSystems (LifeTime)
        ▼
Fase 4 — Extension
            └── estudar: ServiceNow (Scoped Apps), SAP Clean Core,
                ABP (padrões técnicos), OutSystems (Modules/Forge)
```

Nenhuma dessas entradas autoriza começar a construir o mecanismo
correspondente antes da fase indicada — é lista de leitura, não plano
de implementação.
