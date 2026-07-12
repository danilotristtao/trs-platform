# LL-005 — Customization Debt

**Status:** Ativo
**Categoria:** Governança de Extensibilidade
**Relacionado a:** Extension Engine, Constitution

---

## Contexto

Talvez o problema mais recorrente em três décadas de história de ERP.
Cada cliente tem uma necessidade legítima e específica. A forma mais
rápida de atendê-la, sob pressão comercial e de prazo, é alterar
diretamente o código-fonte ou o comportamento central do produto para
aquele cliente.

## Sintomas Observados

- Cada cliente relevante acaba rodando, na prática, uma versão
  ligeiramente diferente do produto.
- Upgrades de versão se tornam projetos de risco alto, em vez de
  rotina.
- Manutenção cresce de forma proporcional ao número de clientes, em vez
  de proporcional à complexidade real do produto.
- Inovação no produto central desacelera, porque qualquer mudança pode
  quebrar alguma das centenas de variações existentes.

## Exemplo Real

```
Cliente A precisa de uma alteração.
Cliente B precisa de outra.
Cliente C precisa de outra.

Depois de alguns anos:
existem 200 versões do "mesmo" produto.

Resultado:
- upgrade se torna impraticável;
- manutenção multiplica;
- inovação central para.
```

## Consequências

### Técnicas
- Divergência progressiva do código-fonte entre instalações (fork
  silencioso e não intencional).
- Testes de regressão deixam de cobrir a realidade de cada cliente.

### Operacionais
- Cada atualização de versão exige validação individual por cliente.
- Equipe de engenharia gasta proporção crescente do tempo mantendo
  variações, não evoluindo o produto.

### Comerciais
- Custo de suporte por cliente cresce ao longo do tempo, em vez de
  cair com a maturidade da plataforma.
- Vantagem competitiva de "plataforma", que deveria vir de escala,
  desaparece.

## Causa Raiz

Ausência de um mecanismo de extensibilidade que separe claramente o
núcleo do produto das necessidades específicas de cada cliente. Quando
a única ferramenta disponível é "alterar o código", toda necessidade
específica vira uma alteração ao núcleo.

## Critérios Arquiteturais Derivados

Toda decisão futura de arquitetura da TRS deve responder:

```
Esta necessidade específica de um cliente pode ser resolvida
sem tocar no código-fonte do núcleo?
```

Se a resposta for **não**, a plataforma ainda não oferece o ponto de
extensão necessário — e a solução correta é criar esse ponto de
extensão, não alterar o núcleo pontualmente.

## Estratégia TRS

- **Extension Engine**: pontos de extensão formais (plugins, hooks,
  webhooks) para lógica específica de cliente.
- **Policies e Workflows** como primeira linha de customização —
  cobrem a maioria dos casos sem exigir código.
- **Core imutável por contrato**: o código-fonte do núcleo não é
  alterado para atender a um cliente específico; se uma necessidade
  recorrente aparece em múltiplos clientes, ela é avaliada para virar
  parte do núcleo — de forma deliberada, não acidental.

## Riscos da Própria Solução

Um Extension Engine mal desenhado pode simplesmente mover o problema:
em vez de 200 forks do núcleo, passamos a ter 200 extensões
incompatíveis entre si, com o mesmo efeito de fragmentação. É essencial
que extensões sejam versionadas, testadas e sigam contratos de API
estáveis — do contrário, o problema apenas migra de camada.

## Métricas de Sucesso

- Percentual de necessidades específicas de cliente resolvidas via
  política/workflow/extensão, sem alteração do núcleo.
- Número de forks reais do código-fonte do núcleo (meta: zero).
- Tempo médio de upgrade de versão por cliente.

---

## Revisão Crítica e Lacunas (adicionada em revisão posterior)

"Core imutável" precisa significar que clientes e extensões não
modificam os internals do core — não que o produto para de evoluir. Ao
mesmo tempo, extensão sem limites pode ser tão destrutiva quanto um
fork: se qualquer extensão pode fazer qualquer coisa, a TRS recria o
problema de LL-005 dentro do próprio mecanismo criado para resolvê-lo.

**Complementos obrigatórios:** níveis formais de extensão (o que cada
nível pode e não pode tocar); sandbox de teste; manifesto de
capacidades declarado por extensão; versionamento semântico;
certificação para extensões privilegiadas; verificação automática de
compatibilidade em cada upgrade; gestão de dependências entre
extensões; processo de depreciação; limites de volume de dados/eventos
por extensão; definição clara de responsabilidade de suporte.

Esta lacuna foi formalizada nos requisitos AR-EXT-001 a AR-EXT-006 em
`TRS_Foundation_v2.md`, Parte IV.5, e depende do vocabulário de
`Capability` e `Extension` definido em ADR-0006.
