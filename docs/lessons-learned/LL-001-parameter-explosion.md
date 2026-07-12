# LL-001 — Parameter Explosion

**Status:** Ativo
**Categoria:** Configuração e Governança de Comportamento
**Relacionado a:** Policy Engine, Constitution

---

## Contexto

Como os ERPs tradicionalmente resolveram o problema de atender múltiplos
clientes, países, filiais e segmentos: adicionando um novo parâmetro a
cada exceção encontrada. Cada cliente que pedia um comportamento
diferente resultava em mais um flag, mais uma tabela de configuração,
mais uma combinação possível.

Isso funcionou nos primeiros anos de qualquer produto. O problema
aparece na escala: depois de uma década de operação, o número de
parâmetros deixa de ser gerenciável por qualquer ser humano.

## Sintomas Observados

- Milhares de parâmetros ativos em uma única instalação.
- Dependências ocultas entre parâmetros não documentadas em nenhum lugar.
- Comportamento do sistema imprevisível mesmo para quem o configurou.
- Combinações de parâmetros nunca testadas, porque o espaço combinatório
  é grande demais.
- Necessidade de um especialista para prever o efeito de qualquer
  mudança.

## Exemplo Real

```
Parâmetro A habilita o comportamento X.

Mas se o parâmetro B estiver ativo,
o comportamento muda.

Se a filial for internacional,
executa outro fluxo.

Se o cliente possuir uma customização Z,
executa outra regra.

Se o país for Brasil,
uma procedure fiscal específica é chamada.

Se for Estados Unidos,
outra procedure é chamada.
```

Ninguém consegue mais prever o comportamento do sistema apenas lendo a
tela de configuração. É preciso testar, ou perguntar a quem já viu esse
comportamento antes.

## Consequências

### Técnicas
- Aumento exponencial da complexidade de teste (combinatória de flags).
- Código repleto de condicionais aninhados (`if` dentro de `if`).
- Dificuldade real de prever efeitos colaterais de qualquer mudança.

### Operacionais
- Dependência estrutural de consultores especializados.
- Tempo de implantação longo, porque cada projeto precisa "descobrir"
  a combinação certa de parâmetros.
- Suporte lento, porque o diagnóstico de um problema exige entender a
  combinação específica ativa naquele cliente.

### Comerciais
- Upgrades se tornam arriscados ou inviáveis (medo de quebrar uma
  combinação específica).
- Custo total de propriedade (TCO) cresce ao longo do tempo, em vez de
  cair com a maturidade do produto.

## Causa Raiz

Ausência de um mecanismo explícito de governança de decisões
empresariais. O parâmetro nasceu como atalho técnico para representar
uma regra de negócio, mas nunca existiu um lugar formal para expressar
*por que* aquela regra existe, quem a aprovou e sob quais condições ela
deveria deixar de existir.

## Critérios Arquiteturais Derivados

Toda decisão futura de arquitetura da TRS deve responder a esta
pergunta:

```
Essa solução cria um novo parâmetro implícito de comportamento?
```

Se a resposta for **sim**, a decisão deve ser reavaliada antes de
prosseguir. Uma regra de negócio na TRS deve nascer como política
declarativa, não como flag de configuração.

## Estratégia TRS

- **Policy Engine** como substituto formal de parametrização dispersa.
- **Policy Simulation**: capacidade de simular o efeito de uma política
  antes de ativá-la em produção.
- **Policy Governance**: toda política tem criador, aprovador, versão e
  data de vigência (ver TRS Policy Engine v1, seção 4).
- **Policy Explainability**: toda decisão do sistema deve poder ser
  explicada citando a política e a versão responsável.

## Riscos da Própria Solução

O Policy Engine pode, se mal governado, se tornar apenas um
**"Parameter Engine 2.0"** — ou seja, uma nova camada de configuração
que reintroduz a mesma explosão combinatória, apenas com uma sintaxe
mais bonita. A diferença real só existe se houver simulação,
versionamento e limites explícitos de quantas políticas podem se
sobrepor em um mesmo contexto.

## Métricas de Sucesso

- Número total de políticas ativas por contexto (deve ser auditável e
  limitado).
- Número médio de dependências entre políticas.
- Percentual de decisões do sistema que podem ser explicadas
  automaticamente, sem intervenção humana.
- Tempo médio necessário para prever o efeito de uma nova política antes
  de ativá-la.

---

## Revisão Crítica e Lacunas (adicionada em revisão posterior)

O diagnóstico original está correto — o problema não é a existência de
parâmetros, mas a opacidade das combinações. Porém, a estratégia
original ("Policy Engine resolve isso") é ampla demais sem uma
**taxonomia**. Sem distinguir configuração técnica, preferência de
interface, dado mestre, metadado de aplicação e política empresarial,
qualquer coisa pode acabar dentro do Policy Engine — recriando o mesmo
problema em uma camada nova. Também faltam regras formais de
precedência, escopo, temporalidade, herança e exclusão mútua entre
políticas — sem isso, "política" pode se tornar tão imprevisível quanto
"parâmetro" já era.

**Complementos obrigatórios:** taxonomia explícita de tipos de
configuração; simulação antes/depois; análise de impacto; expiração;
revisão periódica; limites de complexidade; grafo de dependências entre
políticas; plano de migração de parâmetros legados.

Esta lacuna foi formalizada como **ADR-0008 — Rule Placement
Standard**, que define os sete tipos de lógica e seus respectivos locais
autoritativos (ver `/docs/adr/ADR-0008-rule-placement-standard.md`).
