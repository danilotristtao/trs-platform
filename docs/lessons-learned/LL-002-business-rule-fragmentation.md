# LL-002 — Business Rule Fragmentation

**Status:** Ativo
**Categoria:** Localização de Regras de Negócio
**Relacionado a:** Policy Engine, Workflow Engine

---

## Contexto

Em plataformas de gestão tradicionais, uma única regra de negócio
raramente vive em um só lugar. Ao longo de anos de evolução, partes da
mesma regra vão sendo implementadas onde for mais rápido resolver o
problema imediato — sem nenhuma governança sobre onde as regras
deveriam, de fato, residir.

## Sintomas Observados

Uma mesma regra de negócio termina distribuída entre:

- procedures de banco de dados;
- triggers;
- código de backend;
- código de frontend (validações de tela);
- rotinas de ETL;
- relatórios;
- integrações com terceiros;
- planilhas de Excel do próprio cliente;
- conhecimento tácito de consultores.

## Exemplo Real

```
Pergunta do usuário:
"Por que esse pedido foi bloqueado?"

Resposta típica da equipe de suporte:
"Não sabemos."

Ou, com mais honestidade:
"Precisamos falar com o consultor que implantou
isso em 2017."
```

Nenhuma pessoa viva na organização consegue apontar, com certeza, todos
os lugares onde uma regra específica está implementada.

## Consequências

### Técnicas
- Impossibilidade de mapear o "grafo completo" de uma regra de negócio.
- Alterar uma regra exige caçar implementações em múltiplas camadas,
  linguagens e sistemas.
- Testes de regressão incompletos, porque ninguém sabe todos os lugares
  afetados por uma mudança.

### Operacionais
- Toda mudança de regra vira um projeto de investigação antes de virar
  um projeto de implementação.
- Conhecimento técnico se torna tribal, concentrado em poucas pessoas.

### Comerciais
- Tempo de resposta a mudanças regulatórias ou de mercado aumenta.
- Auditorias externas (fiscais, financeiras) se tornam mais caras e
  demoradas, porque a evidência da regra está espalhada.

## Causa Raiz

Ausência de um local único e autoritativo onde regras de negócio devam
obrigatoriamente residir. Quando não existe um lugar "certo" para uma
regra, ela vai para o lugar mais conveniente no momento em que foi
escrita — que quase nunca é o mesmo lugar da regra anterior.

## Critérios Arquiteturais Derivados

Toda decisão futura de arquitetura da TRS deve responder:

```
Essa regra de negócio pode ser implementada fora do Policy Engine
ou do Workflow Engine?
```

Se a resposta for **sim, e for conveniente fazer isso**, essa é
precisamente a situação que precisa ser bloqueada por design — por
exemplo, impedindo que camadas de frontend ou integrações implementem
lógica de decisão de negócio por conta própria.

## Estratégia TRS

- **Policy Engine centralizado**: toda condição/decisão de negócio deve
  passar por ele, independentemente da camada que a originou.
- **Workflow Engine** como único responsável por orquestrar processos
  que dependem de decisões de política.
- Frontend, relatórios e integrações devem ser **consumidores** de
  decisões, nunca **produtores** de regras de negócio.

## Riscos da Própria Solução

Existe o risco de o Policy Engine se tornar um gargalo de performance
ou de produtividade se toda regra, por menor que seja, precisar passar
por ele com o mesmo nível de burocracia. É necessário distinguir regras
de negócio (que merecem governança plena) de validações puramente
técnicas (formato de campo, por exemplo), que não precisam desse rigor.

## Métricas de Sucesso

- Percentual de regras de negócio ativas que residem exclusivamente no
  Policy Engine / Workflow Engine.
- Tempo médio para localizar todas as implementações de uma regra
  específica (meta: instantâneo, via busca no Policy Engine).
- Número de incidentes de suporte cuja causa raiz foi "regra
  implementada fora do lugar esperado".

---

## Revisão Crítica e Lacunas (adicionada em revisão posterior)

A centralização lógica está correta em espírito, mas "toda regra no
Policy/Workflow Engine" é uma generalização perigosa. Invariantes de
domínio — equilíbrio contábil, integridade estrutural, regras que jamais
podem ser desativadas por decisão administrativa — devem permanecer no
código do domínio, não virar política configurável. Confundir os dois é
um risco arquitetural real: uma regra que garante que "débito = crédito"
não deveria poder ser desligada por um administrador de política, mesmo
que acidentalmente.

Também é preciso distinguir: políticas configuráveis, regras de
processo, validações de UX, transformações de dado e regras de
integração — nem tudo tem a mesma natureza. E há um risco de
infraestrutura: um único serviço físico de políticas pode virar gargalo
e ponto único de falha. A TRS deve centralizar **contratos e
autoridade** sobre onde uma regra vive — não necessariamente o
deployment físico dela.

**Complementos obrigatórios:** um "Rule Placement Standard" (padrão que
define, para cada tipo de regra, onde ela deve residir); contrato de
decisão formal; estratégia síncrona/assíncrona; cache com degradação
segura; linhagem requisito → regra → teste → evento; mecanismos que
impeçam duplicação de lógica em frontend e integrações.

Esta lacuna foi formalizada como **ADR-0006 — Core Domain Meta Model**
(definição de Aggregate e invariante) e **ADR-0008 — Rule Placement
Standard** (tabela de local autoritativo por tipo de lógica, incluindo
a fronteira entre Cálculo Decisório e Derivação, e entre Authorization
Layer e Policy Layer). Ver `/docs/adr/`.
