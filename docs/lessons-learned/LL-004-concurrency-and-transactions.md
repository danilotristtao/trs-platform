# LL-004 — Concurrency and Transactions

**Status:** Ativo
**Categoria:** Consistência Transacional Distribuída
**Relacionado a:** Event Engine, Workflow Engine

---

## Contexto

Um único processo de negócio (por exemplo, um pedido de venda) toca
múltiplos módulos que tradicionalmente foram construídos como sistemas
separados, ou pelo menos como domínios separados dentro do mesmo
sistema: financeiro, estoque, faturamento, compras, produção. Cada um
desses módulos historicamente manipula o mesmo objeto de negócio de
forma direta e simultânea.

## Sintomas Observados

- Deadlocks entre módulos que competem pelo mesmo registro.
- Rollbacks parciais que deixam o sistema em estado inconsistente
  (ex: estoque reservado, mas financeiro não atualizado).
- Necessidade de rotinas manuais de reconciliação para "arrumar" dados
  divergentes entre módulos.
- Comportamento diferente sob carga alta versus carga baixa (problemas
  de concorrência que só aparecem em produção).

## Exemplo Real

```
Pedido aprovado
   ↓
Estoque reserva o produto
   ↓
Financeiro altera o limite de crédito do cliente
   ↓
CRM altera o status da oportunidade
   ↓
Integração externa altera o pedido no sistema do parceiro
```

Se qualquer uma dessas etapas falhar no meio do caminho, o sistema fica
em um estado que nenhuma dessas cinco partes, sozinha, sabe corrigir.

## Consequências

### Técnicas
- Inconsistência de dados entre módulos que deveriam estar sincronizados.
- Complexidade de debugging extremamente alta — o erro só aparece na
  interação entre módulos, não em nenhum deles isoladamente.

### Operacionais
- Equipes de suporte gastam tempo significativo em reconciliação manual
  de dados.
- Processos de "fechamento" (fiscal, financeiro) atrasam por causa de
  inconsistências acumuladas.

### Comerciais
- Perda de confiança do cliente na precisão dos dados do sistema.
- Custos de auditoria aumentam quando dados financeiros e operacionais
  divergem.

## Causa Raiz

Ausência de um modelo explícito de consistência transacional entre
domínios. Módulos foram construídos assumindo acesso direto e imediato
ao mesmo dado, sem um contrato claro de "quem é dono" de cada mudança e
em qual ordem ela deve acontecer.

## Critérios Arquiteturais Derivados

Toda decisão futura de arquitetura da TRS deve responder:

```
Esta funcionalidade permite que dois domínios diferentes
escrevam diretamente no mesmo dado, fora de uma sequência
de eventos controlada?
```

Se a resposta for **sim**, a decisão deve ser redesenhada em torno de
eventos e propriedade única de dado por domínio.

## Estratégia TRS

- **Event-driven architecture**: mudanças de estado se propagam como
  eventos, não como escrita direta cross-domínio.
- **Sagas**: processos de negócio multi-etapa são modelados como sagas
  compensáveis, com passos de rollback explícitos.
- **Outbox pattern**: garante que a publicação de eventos seja
  consistente com a transação local que a originou.
- **Idempotência**: todo consumidor de evento deve poder processar a
  mesma mensagem mais de uma vez sem gerar efeito duplicado.

## Riscos da Própria Solução

Arquitetura orientada a eventos, sagas e outbox é significativamente
mais difícil de depurar do que transações síncronas tradicionais.
Existe risco real de subestimar a curva de aprendizado e a
complexidade operacional (observabilidade, rastreamento distribuído)
necessárias para isso funcionar de forma confiável. Esse é um dos
pontos onde a TRS deve investir cedo em observabilidade (tracing
distribuído), sob pena de trocar um problema de inconsistência por um
problema de inconsistência mais difícil de diagnosticar.

## Métricas de Sucesso

- Número de incidentes de inconsistência de dados entre domínios por
  período.
- Tempo médio de reconciliação manual (meta: zero, no estado maduro).
- Percentual de processos de negócio multi-domínio modelados como sagas
  rastreáveis e reprocessáveis.

---

## Revisão Crítica e Lacunas (adicionada em revisão posterior)

Sagas, outbox, idempotência e arquitetura orientada a eventos não são
uma solução trivial — introduzem estados intermediários, compensações,
retries e um nível de dificuldade de debugging que a estratégia
original subestimava. Um MVP não deveria nascer distribuído por padrão.

**Decisão revisada:** priorizar monólito modular, PostgreSQL e outbox
transacional simples no início. Event sourcing como padrão global é uma
decisão que exige um ADR próprio — não deve ser assumida de largada
(ver AR-TXN-006, AR-TXN-007 em `TRS_Foundation_v2.md`, Parte IV.4).

**Complementos obrigatórios:** autoridade de escrita clara por
agregado; mapa explícito de quais operações exigem consistência forte;
idempotência e deduplicação em qualquer consumidor; fila de
reconciliação e dead-letter; caminho de intervenção humana; correlação
ponta a ponta; testes de concorrência e de falha.
