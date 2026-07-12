# LL-008 — Exception-Driven Software

**Status:** Ativo
**Categoria:** Erosão Arquitetural de Longo Prazo
**Relacionado a:** Constitution, Policy Engine, Extension Engine

---

## Contexto

Esta lição funciona como síntese das sete anteriores (LL-001 a LL-007).
Depois de anos acumulando parâmetros (LL-001), regras fragmentadas
(LL-002), conhecimento perdido (LL-003), inconsistências transacionais
(LL-004), customizações destrutivas (LL-005), falta de explicabilidade
(LL-006) e ausência de governança de mudança (LL-007), o produto original
deixa de existir como uma coisa coesa. O que resta é uma coleção de
exceções empilhadas umas sobre as outras.

## Sintomas Observados

- O código-fonte e as configurações passam a ser dominados por
  condicionais específicos de cliente, filial, país ou segmento.
- Ninguém consegue mais descrever, de forma simples, "o que o produto
  faz" — só é possível descrever "o que o produto faz para o cliente X".
- Cada exceção nova aumenta a probabilidade de a próxima mudança quebrar
  alguma exceção anterior.

## Exemplo Real

```
if cliente = X → comportamento A
if filial = Y → comportamento B
if país = Z → comportamento C
if segmento = indústria → comportamento D
if usa integração XPTO → comportamento E
```

Depois de anos empilhando exceções desse tipo, o produto deixa de
existir como conceito unificado. Existe apenas uma coleção gigante de
exceções coexistindo, cada uma com sua própria história e justificativa
perdida no tempo.

## Consequências

### Técnicas
- Complexidade ciclomática do sistema cresce sem limite superior
  natural.
- Qualquer mudança tem probabilidade crescente de efeito colateral
  não previsto.

### Operacionais
- Suporte, QA e engenharia gastam proporção cada vez maior do tempo
  lidando com exceções específicas, não com o produto central.
- Onboarding de novos profissionais se torna extremamente difícil, pois
  não há mais um "modelo mental" simples do sistema para ensinar.

### Comerciais
- O produto perde a capacidade de escalar comercialmente, porque cada
  novo cliente potencialmente introduz mais exceções, em vez de se
  encaixar em um modelo já existente.
- Diferenciação competitiva desaparece: o produto vira, na prática, uma
  coleção de projetos únicos vendidos sob a mesma marca.

## Causa Raiz

Esta lição não tem uma causa raiz isolada — ela é a consequência
acumulada de LL-001 a LL-007 quando nenhuma delas é endereçada com
rigor. É o estado final de um sistema que resolveu, repetidamente, cada
problema individual da forma mais rápida possível, sem nunca revisitar
o modelo central do produto.

## Critérios Arquiteturais Derivados

Toda decisão futura de arquitetura da TRS deve responder, em conjunto:

```
Esta decisão introduz uma exceção que só se aplica a um
cliente, filial, país ou segmento específico — fora dos
mecanismos formais de política e extensão?
```

Se a resposta for **sim**, esta lição funciona como o alarme final:
mesmo que a decisão pareça pequena isoladamente, ela é exatamente o tipo
de escolha que, repetida centenas de vezes, produz o colapso descrito
aqui.

## Estratégia TRS

- Tratar LL-001 a LL-007 como um **conjunto**, não como itens isolados
  — a checagem arquitetural de uma decisão relevante deve, idealmente,
  passar por todas elas.
- Estabelecer um processo formal (equivalente a um Architecture Review
  Board leve) onde decisões de maior impacto são avaliadas contra este
  checklist antes de serem implementadas.
- Tratar o crescimento do número de exceções ativas como uma métrica de
  saúde arquitetural de primeira classe, monitorada continuamente — não
  apenas descoberta anos depois.

## Riscos da Própria Solução

Existe o risco de esta lição ser tratada como "óbvia demais para agir
sobre ela na prática" — todo mundo concorda em teoria que exceções são
ruins, mas sob pressão comercial de um cliente específico, a exceção
pontual sempre parece a decisão certa no curto prazo. A mitigação real
depende de processo (revisão obrigatória), não apenas de boa intenção.

## Métricas de Sucesso

- Número total de exceções ativas na plataforma (por cliente, filial,
  país, segmento), monitorado como métrica contínua.
- Taxa de crescimento dessa métrica ao longo do tempo (meta: estável ou
  decrescente, nunca crescente sem limite).
- Percentual de decisões arquiteturais relevantes que passaram
  explicitamente pelo checklist de LL-001 a LL-007 antes de serem
  implementadas.

---

## Revisão Crítica e Lacunas (adicionada em revisão posterior)

Nem toda variação por país, setor ou tenant é uma "exceção ruim" —
variabilidade planejada (ex: regras fiscais diferentes por país) é
diferente de exceção privada oportunista (ex: "só para o cliente X,
porque ele pediu"). A documentação original tratava as duas como a
mesma coisa. Além disso, contar exceções isoladamente como métrica pode
ter um efeito colateral perverso: incentivar a criação de "políticas
gigantes" que escondem múltiplas exceções dentro de uma única política
aparentemente simples, só para não aparecer na contagem.

**Complementos obrigatórios:** classificação formal de variação em
global, regulatória, regional, setorial, tenant ou temporária; registro
formal de "dívida de exceção" com owner e prazo de expiração; processo
definido para promover uma demanda de cliente a capacidade de produto
reutilizável; métricas de sobreposição e complexidade (não apenas
contagem simples); governança comercial (contratos não podem prometer o
que a governança da plataforma não permite entregar).

Esta lacuna foi formalizada nos requisitos AR-EXC-001 a AR-EXC-004 em
`TRS_Foundation_v2.md`, Parte IV.8, e no anti-padrão adicional de
"vocabulário arquitetural antecipado sem caso de uso real" (Parte VIII),
que é a mesma categoria de erro em direção oposta: excesso de rigor
prematuro em vez de exceção descontrolada.
