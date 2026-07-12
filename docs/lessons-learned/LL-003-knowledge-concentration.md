# LL-003 — Knowledge Concentration

**Status:** Ativo
**Categoria:** Continuidade Organizacional do Conhecimento
**Relacionado a:** Knowledge Engine, Audit Engine

---

## Contexto

Em implantações longas de ERP, decisões de configuração são tomadas por
pessoas específicas em momentos específicos, geralmente sob pressão de
prazo. O motivo da decisão raramente é documentado — fica apenas na
memória de quem a tomou.

## Sintomas Observados

- Configurações críticas cujo motivo de existir ninguém mais sabe.
- Dependência de indivíduos específicos ("só o João sabe mexer nisso").
- Perda de conhecimento organizacional quando a pessoa sai da empresa.
- Medo generalizado de remover qualquer configuração antiga, porque
  ninguém sabe se ela ainda é necessária.

## Exemplo Real

```
João configurou isso, em algum momento.

João saiu da empresa.

Agora ninguém sabe:
- por que aquilo existe;
- quem pediu;
- qual cliente ou processo depende disso;
- se pode ser removido com segurança.
```

## Consequências

### Técnicas
- Acúmulo de configurações "mortas" que ninguém ousa remover, por medo
  de quebrar algo desconhecido.
- Crescimento constante da superfície de manutenção, sem nunca reduzir.

### Operacionais
- Onboarding de novos técnicos e consultores se torna lento e caro,
  porque o conhecimento não está documentado — está em pessoas.
- Risco de continuidade de negócio ligado à rotatividade de
  funcionários específicos.

### Comerciais
- Cliente fica refém do consultor ou integrador original, o que gera
  dependência comercial não saudável e custos permanentes.

## Causa Raiz

Ausência de um mecanismo estrutural que capture, no momento da decisão,
o "porquê" e não apenas o "o quê". O sistema registra a configuração
final, mas não registra a intenção, o contexto de negócio e a
justificativa por trás dela.

## Critérios Arquiteturais Derivados

Toda decisão futura de arquitetura da TRS deve responder:

```
Esta configuração/decisão pode ser criada sem um registro
explícito de motivo, autor e contexto de negócio?
```

Se a resposta for **sim**, o mecanismo está incompleto. Toda entidade
de decisão (política, workflow, permissão) deve nascer com metadado de
justificativa obrigatório, não opcional.

## Estratégia TRS

- **Knowledge Engine**: camada dedicada a capturar e indexar o "porquê"
  de cada decisão relevante da plataforma, tornando-o pesquisável.
- **Audit Engine**: todo evento de criação, alteração ou desativação de
  política/workflow gera um registro imutável com autor, motivo e data.
- Uso de IA para sumarizar e explicar decisões antigas a partir dos
  registros de auditoria, reduzindo a dependência de memória humana.

## Riscos da Própria Solução

Exigir justificativa obrigatória para toda decisão pode gerar atrito e
incentivar preenchimento genérico ("ajuste solicitado pelo cliente") que
não agrega valor real. É preciso desenhar o processo para que capturar
o motivo seja mais rápido do que não capturá-lo.

## Métricas de Sucesso

- Percentual de políticas/configurações ativas com justificativa e
  autor registrados.
- Tempo médio para responder "por que esta regra existe?" usando apenas
  a plataforma, sem depender de pessoas específicas.
- Redução no número de configurações "órfãs" (sem justificativa
  rastreável) ao longo do tempo.

---

## Revisão Crítica e Lacunas (adicionada em revisão posterior)

Logs não se transformam automaticamente em conhecimento. É necessário
capturar intenção estruturada — não apenas metadado técnico (quem,
quando), mas o "porquê" de negócio, com propriedade (owner), vigência e
proveniência. O futuro Knowledge Engine não pode ser apenas um
repositório de textos ou resumos gerados por IA sem validação humana —
isso apenas trocaria "conhecimento preso em pessoas" por "conhecimento
inventado por um modelo", que é potencialmente pior.

**Complementos obrigatórios:** exigência mínima de justificativa
(rationale); classificação de conhecimento em normativo, operacional,
histórico e inferido; owner obrigatório; ciclo de revisão; política de
retenção; busca respeitando autorização de acesso; validação humana
obrigatória de qualquer conteúdo sugerido por IA; detecção de
conhecimento desatualizado.

**Achado transversal importante:** pesquisa de mercado (Salesforce,
Odoo, ServiceNow, Bitrix24) mostrou que esta é a única lição sem
resposta madura em nenhum dos quatro concorrentes pesquisados. Isso
elevou a prioridade desta capacidade no roadmap — a captura mínima de
rationale (AR-KNW-001 a AR-KNW-006) passou da Fase 6 original para a
Fase 1 ("Vertical Zero"). A estrutura mínima de Rationale (category,
reason_code, human_statement, source_reference, author, created_at,
validity, confidentiality_level) está definida em `TRS_Foundation_v2.md`,
Parte IV.3.
