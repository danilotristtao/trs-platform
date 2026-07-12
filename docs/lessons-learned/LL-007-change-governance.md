# LL-007 — Change Governance

**Status:** Ativo
**Categoria:** Governança de Mudanças
**Relacionado a:** Audit Engine, Policy Engine

---

## Contexto

Configurações e regras de negócio, em ERPs tradicionais, mudam ao
longo do tempo por múltiplas mãos — consultores, administradores
internos, integradores terceiros — frequentemente sem nenhum processo
formal de aprovação ou registro.

## Sintomas Observados

- Mudanças de configuração acontecem sem registro de quem, quando e
  por quê.
- Ausência de processo de aprovação prévia para mudanças de alto
  impacto.
- Impossibilidade de reconstruir, meses depois, a sequência de mudanças
  que levou o sistema ao estado atual.

## Exemplo Real

```
Uma configuração foi alterada.

Ninguém sabe:
- quem mudou;
- quando mudou;
- por que mudou;
- quem aprovou a mudança.
```

## Consequências

### Técnicas
- Impossibilidade de correlacionar um incidente de produção com a
  mudança que o causou.
- Ausência de trilha de auditoria confiável para reverter mudanças
  problemáticas.

### Operacionais
- Investigação de incidentes se torna lenta, porque a causa raiz pode
  estar em uma mudança não registrada.
- Times de operação perdem confiança na estabilidade do ambiente.

### Comerciais
- Exposição a riscos de compliance e auditoria externa (SOX, fiscal,
  ISO, entre outros), especialmente em setores regulados.
- Dificuldade de comprovar, para reguladores, quem tinha autoridade
  para autorizar determinada mudança.

## Causa Raiz

Ausência de um processo de governança de mudança embutido na própria
plataforma. A capacidade técnica de alterar uma configuração nunca foi
acoplada à obrigação de registrar e, quando aplicável, aprovar essa
mudança antes que ela produza efeito.

## Critérios Arquiteturais Derivados

Toda decisão futura de arquitetura da TRS deve responder:

```
É possível alterar o comportamento do sistema sem gerar
um registro imutável de autor, motivo e (quando aplicável)
aprovação prévia?
```

Se a resposta for **sim**, o mecanismo de mudança está incompleto.

## Estratégia TRS

- **Audit Engine**: toda mudança relevante gera um evento imutável
  (`PolicyChanged`, `PermissionChanged`, etc.), incluindo autor, motivo
  e timestamp.
- **Aprovação por política**: mudanças de alto impacto (definidas pela
  própria plataforma, via política) exigem aprovação formal antes de
  entrar em vigor — a própria governança de mudança é, em si, modelada
  como uma política.
- **Versionamento obrigatório**: nenhuma política ou configuração
  crítica é sobrescrita; toda alteração gera uma nova versão, mantendo
  histórico completo e possibilidade de rollback (ver TRS Policy Engine
  v1, seção 6).

## Riscos da Própria Solução

Processos de aprovação mal calibrados podem se tornar burocracia que
atrasa mudanças legítimas e urgentes. É necessário diferenciar o nível
de governança exigido conforme o impacto real da mudança — nem toda
alteração precisa do mesmo rigor que uma mudança em política fiscal ou
financeira.

## Métricas de Sucesso

- Percentual de mudanças relevantes com autor, motivo e timestamp
  registrados automaticamente.
- Tempo médio para reconstruir o histórico completo de mudanças de uma
  configuração específica.
- Número de mudanças de alto impacto realizadas sem aprovação formal
  (meta: zero).

---

## Revisão Crítica e Lacunas (adicionada em revisão posterior)

Versionamento isolado (guardar histórico) não basta como governança. É
preciso um ciclo de vida completo: draft, teste, revisão, aprovação,
promoção, publicação, observação e — quando necessário — rollback. A
governança deve ser **proporcional ao risco** da mudança, e deve existir
um caminho de emergência ("break-glass") controlado e auditado para
quando o processo formal seria tarde demais.

**Complementos obrigatórios:** segregação de funções (quem propõe não
pode ser o único a aprovar); ambientes formais (dev/test/produção);
"change sets" rastreáveis; assinatura/aprovação registrada; análise de
impacto antes da promoção; feature flags governadas; processo de
emergência com revisão obrigatória posterior; evidência de teste
vinculada à versão publicada.

Esta lacuna foi formalizada nos requisitos AR-CHG-001 a AR-CHG-005 em
`TRS_Foundation_v2.md`, Parte IV.7, com gates explícitos por fase do
roadmap (Parte VII) — inclusive definindo o critério de saída da Fase 5
como demonstração real do ciclo completo em ambiente controlado.
