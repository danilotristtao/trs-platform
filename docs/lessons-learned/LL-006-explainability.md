# LL-006 — Explainability

**Status:** Ativo
**Categoria:** Transparência de Decisões
**Relacionado a:** Policy Engine, Audit Engine, AI Governance

---

## Contexto

ERPs tradicionais tomam decisões de negócio constantemente — aprovar,
bloquear, calcular, encaminhar — mas raramente conseguem justificar
essas decisões de forma acessível a um usuário de negócio. A lógica
está enterrada em código, procedures ou configurações que exigem
conhecimento técnico para interpretar.

Esta é, entre todas as lições, provavelmente a que mais se conecta
diretamente com a era de inteligência artificial: sistemas que decidem
sem explicar se tornam ainda mais opacos quando parte da decisão passa
a ser tomada por modelos de IA.

## Sintomas Observados

- Usuários de negócio não conseguem entender por que uma decisão
  automática foi tomada.
- Equipe de suporte não consegue responder perguntas básicas sobre o
  comportamento do sistema sem escalar para engenharia.
- Auditores e reguladores exigem justificativas que o sistema não
  consegue produzir nativamente.

## Exemplo Real

```
Por que esse usuário conseguiu aprovar este pedido?

Por que esse pedido foi rejeitado?

Por que esse workflow seguiu por este caminho
e não por outro?
```

Em ERPs tradicionais, a resposta normalmente exige investigação manual
de código ou configuração — não existe uma resposta direta e imediata.

## Consequências

### Técnicas
- Decisões de sistema não são rastreáveis até sua causa de forma
  automática.
- Dificuldade de depurar comportamento inesperado sem acesso a código.

### Operacionais
- Tempo de suporte aumenta significativamente para qualquer questão
  sobre "por que o sistema fez isso".
- Escalonamento constante de dúvidas de negócio para times técnicos.

### Comerciais
- Auditorias externas (fiscais, regulatórias, de compliance) se tornam
  mais lentas e caras.
- Perda de confiança de usuários de negócio na plataforma quando decisões
  parecem arbitrárias.

## Causa Raiz

Ausência de um contrato formal de que toda decisão do sistema deve
carregar consigo a evidência de sua própria justificativa: qual regra
foi aplicada, em qual versão, e sob qual contexto.

## Critérios Arquiteturais Derivados

Toda decisão futura de arquitetura da TRS deve responder:

```
Esta decisão pode ser tomada pelo sistema sem produzir,
no mesmo instante, uma explicação legível por um usuário
de negócio (não apenas por um engenheiro)?
```

Se a resposta for **sim**, o mecanismo de decisão está incompleto.

## Estratégia TRS

- **Explicabilidade por padrão**: toda execução de política registra
  qual política, versão e condição levaram à decisão (ver TRS Policy
  Engine v1, seção 9).
- **Linguagem natural**: explicações devem ser produzidas em linguagem
  compreensível a um usuário de negócio, não apenas em log técnico.
- **Governança de IA**: quando parte da decisão envolve modelos de IA,
  a explicação deve deixar claro qual foi a contribuição do modelo e
  qual foi a regra determinística aplicada — a IA nunca deve ser a
  única fonte de justificativa para uma decisão de negócio crítica.

## Riscos da Própria Solução

Explicações geradas automaticamente podem se tornar genéricas ou
verbosas a ponto de perder utilidade real ("a política X foi aplicada"
sem contexto suficiente). É necessário desenhar explicações centradas
no que o usuário de negócio realmente precisa saber, não apenas no que é
tecnicamente correto expor.

## Métricas de Sucesso

- Percentual de decisões do sistema com explicação automática disponível
  no momento em que ocorrem.
- Tempo médio de resposta a perguntas de "por que isso aconteceu"
  (meta: instantâneo, sem escalonamento para engenharia).
- Satisfação de usuários de negócio e auditores com a clareza das
  explicações fornecidas.

---

## Revisão Crítica e Lacunas (adicionada em revisão posterior)

A explicação de uma decisão precisa ser determinística e ancorada em
evidência real, não gerada. A IA pode **traduzir** a evidência para
linguagem natural, mas não pode **criar** a causa — se a explicação for
gerada por um modelo sem lastro em evidência estruturada, ela deixa de
ser explicação e passa a ser justificativa pós-hoc, exatamente o oposto
do que esta lição pede. Também é preciso reconhecer que públicos
diferentes (usuário operacional, técnico, auditor, regulador) precisam
de explicações com nível de detalhe diferente sobre o mesmo evento.

**Complementos obrigatórios:** um "Decision Envelope" — estrutura de
dados padronizada que acompanha toda decisão, com entradas, regra,
versão, resultado, ator e correlação; códigos de razão padronizados;
capacidade de reprodução histórica de decisões passadas; explicação de
por que uma regra **não** foi aplicada; tratamento explícito de
incerteza; autorização e mascaramento de dados sensíveis; testes de
fidelidade.

**Correção importante:** nem toda alteração de estado deve gerar um
Decision Envelope completo — apenas as decorrentes de decisão de
negócio, autorização, política, workflow ou intervenção humana
relevante. Alterações puramente técnicas (cache, contadores, jobs)
geram telemetria/auditoria técnica, não um envelope completo (ver
AR-EXP-001, revisado em `TRS_Foundation_v2.md`, Parte IV.6, e a tabela
de distinção entre Decision Envelope, Audit Record, Domain Event,
Telemetry e Rationale na Parte IV.3).
