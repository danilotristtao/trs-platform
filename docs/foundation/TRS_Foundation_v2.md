# TRS Foundation v2

## AI-Native Business Operating Platform — Documento Mestre de Consolidação

**Versão:** 2.0
**Status:** Fusão consolidada — substitui a v1.0 e o documento paralelo de revisão técnica

---

### Sobre esta versão

Este documento funde dois documentos mestre produzidos em paralelo sobre
a TRS: um com foco em **validação de mercado e evidência externa**
(pesquisa real sobre Salesforce, Odoo, ServiceNow e Bitrix24) e outro com
foco em **rigor de formalização arquitetural** (requisitos normativos,
roadmap com critérios de saída, riscos sistêmicos, arquitetura de
referência). Nenhum dos dois, isoladamente, respondia às duas perguntas
que um projeto desta ambição precisa responder ao mesmo tempo:

> **Por que a TRS precisa existir?** (mercado, evidência, lacuna real)
> **Como impedir que a TRS morra tentando existir?** (rigor, sequência,
> modos de falha)

Este documento tenta responder as duas.

---

## Parte I — Posicionamento e Visão

> TRS é uma **AI-Native Business Operating Platform**: uma fundação
> compartilhada de metadados, identidade, autorização, decisões,
> workflows, eventos, auditoria, conhecimento, extensões e inteligência.
> CRM, ERP, Service Management, Projects, Procurement, Finance e
> soluções setoriais são aplicações construídas sobre essa fundação —
> não produtos isolados.
>
> Ao contrário dos ERPs tradicionais, que evoluíram por acumulação de
> parametrizações, customizações e regras fragmentadas, a TRS nasce
> orientada por políticas, eventos, workflows e governança explícita.

**Sobre a analogia com o Linux:** ela continua útil como narrativa de
plataforma e ecossistema, mas não deve ser lida como equivalência
técnica. Não existe um "POSIX empresarial" — um contrato universal e
estável que todo domínio de negócio já concorda em seguir, como existe
para chamadas de sistema operacional. Por isso, a TRS não deve tentar
padronizar todas as entidades e processos de negócio possíveis; deve
padronizar **como** capacidades são modeladas, governadas, executadas,
explicadas, auditadas e estendidas. Essa é uma ambição mais estreita e
mais alcançável do que "ser o Linux do ERP" soa à primeira vista — e é
a versão que este documento assume daqui em diante.

**Nota de rastreabilidade:** esta definição substitui oficialmente a
seção 1 (Executive Vision) do TRS Architecture Handbook v0.1, por ser
mais precisa e mais tangível — decisão já formalizada na v1.0 deste
documento.

---

## Parte II — Lessons Learned: Síntese e Revisão Crítica

Os 8 documentos originais (LL-001 a LL-008) permanecem como referência
completa (contexto, sintomas, exemplo real, consequências, causa raiz,
critérios arquiteturais, estratégia TRS, riscos da própria solução,
métricas). Esta seção adiciona uma segunda camada de revisão — mais
técnica e mais cética — que identifica onde a estratégia original de
cada LL era boa em intenção, mas incompleta em execução.

### LL-001 — Parameter Explosion

**Revisão crítica:** o diagnóstico original está correto — o problema
não é a existência de parâmetros, mas a opacidade das combinações.
Porém, a estratégia original ("Policy Engine resolve isso") é ampla
demais sem uma **taxonomia**. Sem distinguir configuração técnica,
preferência de interface, dado mestre, metadado de aplicação e política
empresarial, qualquer coisa pode acabar dentro do Policy Engine —
recriando o mesmo problema em uma camada nova. Também faltam regras
formais de precedência, escopo, temporalidade, herança e exclusão mútua
entre políticas — sem isso, "política" pode se tornar tão imprevisível
quanto "parâmetro" já era.

**Complementos obrigatórios:** taxonomia explícita de tipos de
configuração; simulação antes/depois; análise de impacto; expiração;
revisão periódica; limites de complexidade; grafo de dependências entre
políticas; plano de migração de parâmetros legados.

### LL-002 — Business Rule Fragmentation

**Revisão crítica:** a centralização lógica está correta em espírito,
mas "toda regra no Policy/Workflow Engine" é uma generalização perigosa.
Invariantes de domínio — equilíbrio contábil, integridade estrutural,
regras que jamais podem ser desativadas por decisão administrativa —
devem permanecer no código do domínio, não virar política configurável.
Confundir os dois é um risco arquitetural real: uma regra que garante
que "débito = crédito" não deveria poder ser desligada por um
administrador de política, mesmo que acidentalmente.

Também é preciso distinguir: políticas configuráveis, regras de
processo, validações de UX, transformações de dado e regras de
integração — nem tudo tem a mesma natureza. E há um risco de
infraestrutura: um único serviço físico de políticas pode virar
gargalo e ponto único de falha. A TRS deve centralizar **contratos e
autoridade** sobre onde uma regra vive — não necessariamente o
deployment físico dela.

**Complementos obrigatórios:** um "Rule Placement Standard" (padrão que
define, para cada tipo de regra, onde ela deve residir); contrato de
decisão formal; estratégia síncrona/assíncrona; cache com degradação
seguro; linhagem requisito → regra → teste → evento; mecanismos que
impeçam duplicação de lógica em frontend e integrações.

### LL-003 — Knowledge Concentration

**Revisão crítica:** logs não se transformam automaticamente em
conhecimento. É necessário capturar intenção estruturada — não apenas
metadado técnico (quem, quando), mas o "porquê" de negócio, com
propriedade (owner), vigência e proveniência. O futuro Knowledge Engine
não pode ser apenas um repositório de textos ou resumos gerados por IA
sem validação humana — isso apenas trocaria "conhecimento preso em
pessoas" por "conhecimento inventado por um modelo", que é
potencialmente pior.

**Complementos obrigatórios:** exigência mínima de justificativa
(rationale); classificação de conhecimento em normativo, operacional,
histórico e inferido; owner obrigatório; ciclo de revisão; política de
retenção; busca respeitando autorização de acesso; validação humana
obrigatória de qualquer conteúdo sugerido por IA; detecção de
conhecimento desatualizado.

> **Achado transversal importante** (ver Parte III): esta é a única
> lição sem resposta madura em nenhum dos quatro concorrentes
> pesquisados. Isso muda sua prioridade no roadmap (ver Parte VII).

### LL-004 — Concurrency and Transactions

**Revisão crítica:** sagas, outbox, idempotência e arquitetura orientada
a eventos não são uma solução trivial — introduzem estados
intermediários, compensações, retries e um nível de dificuldade de
debugging que a estratégia original subestimava. Um MVP não deveria
nascer distribuído por padrão. A recomendação revisada é: **priorizar
monólito modular, PostgreSQL e outbox transacional simples** no início.
Event sourcing como padrão global é uma decisão que exige um ADR
próprio — não deve ser assumida de largada.

**Complementos obrigatórios:** autoridade de escrita clara por
agregado; mapa explícito de quais operações exigem consistência forte;
idempotência e deduplicação em qualquer consumidor; fila de
reconciliação e dead-letter; caminho de intervenção humana; correlação
ponta a ponta; testes de concorrência e de falha.

### LL-005 — Customization Debt

**Revisão crítica:** "core imutável" precisa significar que clientes e
extensões não modificam os internals do core — não que o produto para
de evoluir. Ao mesmo tempo, extensão sem limites pode ser tão
destrutiva quanto um fork: se qualquer extensão pode fazer qualquer
coisa, a TRS recria o problema de LL-005 dentro do próprio mecanismo
criado para resolvê-lo.

**Complementos obrigatórios:** níveis formais de extensão (o que cada
nível pode e não pode tocar); sandbox de teste; manifesto de
capacidades declarado por extensão; versionamento semântico;
certificação para extensões privilegiadas; verificação automática de
compatibilidade em cada upgrade; gestão de dependências entre
extensões; processo de depreciação; limites de volume de dados/eventos
por extensão; definição clara de responsabilidade de suporte.

### LL-006 — Explainability

**Revisão crítica:** a explicação de uma decisão precisa ser
determinística e ancorada em evidência real, não gerada. A IA pode
**traduzir** a evidência para linguagem natural, mas não pode **criar**
a causa — se a explicação for gerada por um modelo sem lastro em
evidência estruturada, ela deixa de ser explicação e passa a ser
justificativa pós-hoc, que é exatamente o oposto do que LL-006 pede.
Também é preciso reconhecer que públicos diferentes (usuário
operacional, técnico, auditor, regulador) precisam de explicações com
nível de detalhe diferente sobre o mesmo evento.

**Complementos obrigatórios:** um "Decision Envelope" — estrutura de
dados padronizada que acompanha toda decisão, com entradas, regra,
versão, resultado, ator e correlação; códigos de razão padronizados;
capacidade de reprodução histórica de decisões passadas; explicação de
por que uma regra **não** foi aplicada (não apenas por que foi);
tratamento explícito de incerteza; autorização e mascaramento de dados
sensíveis na explicação; testes de fidelidade (a explicação realmente
corresponde ao que aconteceu?).

### LL-007 — Change Governance

**Revisão crítica:** versionamento isolado (guardar histórico) não
basta como governança. É preciso um ciclo de vida completo: draft,
teste, revisão, aprovação, promoção, publicação, observação e — quando
necessário — rollback. A governança deve ser **proporcional ao risco**
da mudança (uma mudança de cor de botão não precisa do mesmo processo
que uma mudança de política fiscal), e deve existir um caminho de
emergência ("break-glass") controlado e auditado para quando o processo
formal seria tarde demais.

**Complementos obrigatórios:** segregação de funções (quem propõe não
pode ser o único a aprovar); ambientes formais (dev/test/produção);
"change sets" rastreáveis; assinatura/aprovação registrada; análise de
impacto antes da promoção; feature flags governadas (não soltas);
processo de emergência com revisão obrigatória posterior; evidência de
teste vinculada à versão publicada.

### LL-008 — Exception-Driven Software (Síntese)

**Revisão crítica:** nem toda variação por país, setor ou tenant é uma
"exceção ruim" — variabilidade planejada (ex: regras fiscais diferentes
por país) é diferente de exceção privada oportunista (ex: "só para o
cliente X, porque ele pediu"). A documentação original tratava as duas
como a mesma coisa. Além disso, contar exceções isoladamente como
métrica pode ter um efeito colateral perverso: incentivar a criação de
"políticas gigantes" que escondem múltiplas exceções dentro de uma
única política aparentemente simples, só para não aparecer na contagem.

**Complementos obrigatórios:** classificação formal de variação em
global, regulatória, regional, setorial, tenant ou temporária; registro
formal de "dívida de exceção" com owner e prazo de expiração; processo
definido para promover uma demanda de cliente a capacidade de produto
reutilizável (em vez de deixá-la como exceção permanente); métricas de
sobreposição e complexidade (não apenas contagem simples); governança
comercial (contratos não podem prometer o que a governança da
plataforma não permite entregar).

---

## Parte III — Pesquisa de Mercado e Validação Externa

Esta seção é baseada em pesquisa web realizada em julho de 2026, não em
conhecimento genérico sobre essas plataformas — várias mudaram
significativamente nos últimos meses (ex: Salesforce encerrou Process
Builder e Workflow Rules em dezembro de 2025). O objetivo aqui não é
apenas descrever os concorrentes, mas **transformar cada observação em
evidência verificável**, para que a tese da TRS seja uma hipótese
testada, não uma opinião.

### LL-001 — Parameter Explosion

Salesforce trata isso parcialmente com o **Business Rules Engine**, que
<cite index="4-1">centraliza regras que antes viviam em planilhas, com versionamento embutido e trilhas de auditoria para cada decisão automatizada</cite>. Ao mesmo tempo, <cite index="7-1">Process Builder e Workflow Rules — as ferramentas declarativas antigas — encerraram o suporte oficial em 31 de dezembro de 2025</cite>, precisamente por terem acumulado complexidade demais. Ou seja: mesmo a Salesforce já passou por um ciclo de "explosão de automação legada" e precisou consolidar tudo em uma ferramenta única.

ServiceNow recomenda <cite index="28-1">priorizar configuração sobre customização sempre que possível, para manter compatibilidade com upgrades e reduzir dívida técnica</cite>. A Odoo formaliza a mesma ideia como distinção de custo: <cite index="16-1">configuração usa opções nativas e avança automaticamente a cada versão, com custo de manutenção zero, enquanto customização exige código revisado e frequentemente reescrito a cada versão maior</cite>.

Bitrix24 não apresenta, nos materiais disponíveis, mecanismo equivalente de simulação, versionamento ou limite de complexidade.

### LL-002 — Business Rule Fragmentation

Salesforce está consolidando onde a lógica deve viver: <cite index="5-1">aproximadamente 70% dos requisitos devem ser resolvidos em Flow, e os 30% restantes exigem Apex por razões técnicas</cite>. A Odoo recomenda disciplina equivalente: <cite index="23-1">customizações devem usar o mecanismo de herança de modelos, views e controllers, e o código customizado deve ficar sempre separado do código-fonte principal</cite>.

ServiceNow tem múltiplos tipos de Business Rules (before, after, async,
display) executando em pontos diferentes do ciclo de vida do registro, o
que pode gerar loops de recursão quando regras em tabelas relacionadas se
atualizam mutuamente. Bitrix24 permite <cite index="35-1">registrar ações e regras de automação customizadas via API, reutilizáveis tanto como regras de automação quanto como ações de workflow</cite> — flexível, mas sem catálogo central que impeça duplicação.

### LL-003 — Knowledge Concentration

Este foi o problema com **menor cobertura direta** nos resultados de
pesquisa entre as quatro plataformas. Nenhuma anuncia um "Knowledge
Engine" dedicado a capturar o motivo de negócio por trás de uma decisão
— o mais próximo são campos de descrição livre, sem estrutura ou
obrigatoriedade.

**Este é o achado mais importante desta seção.** Se nenhum concorrente
relevante resolveu isso de forma madura, o diferencial real da TRS pode
não estar no Policy Engine (onde a Salesforce já tem um produto
comparável e nomeado) — pode estar em transformar conhecimento
organizacional em ativo operacional governado. Isso é discutido com
mais profundidade na Parte VII (Roadmap).

### LL-004 — Concurrency and Transactions

Salesforce impõe limites rígidos por transação e recomenda ativamente
evitar escrita dentro de loops, já que <cite index="5-1">o motivo número um de falhas em produção é quebrar a bulkificação automática colocando operações de escrita dentro de loops</cite>. ServiceNow oferece regras assíncronas para processamento pesado sem travar a sessão do usuário. Nem Odoo nem Bitrix24 apresentaram discussão explícita sobre consistência transacional entre módulos como problema arquitetural nomeado.

### LL-005 — Customization Debt

O problema mais documentado do mercado — praticamente uma indústria de
consultoria existe em torno dele. A Odoo tem os dados mais concretos:
<cite index="15-1">bancos de dados fortemente customizados custam cerca de 5 vezes mais para migrar e levam 3 vezes mais tempo do que instalações padrão</cite>, e <cite index="16-1">desde março de 2026 a Odoo cobra sobretaxa de assinatura de 25% para clientes rodando versões três ou mais releases atrás da atual</cite>.

ServiceNow trata o mesmo problema via governança formal: consultorias
inteiras auditam <cite index="27-1">"customization sprawl" e restauram instâncias ao padrão out-of-the-box</cite>, e a própria ServiceNow recomenda um Technical Governance Board como <cite index="25-1">autoridade de design que garante que soluções sigam padrões arquiteturais consistentes e mantenham compatibilidade com upgrades</cite>. Bitrix24 não apresentou discussão equivalente.

### LL-006 — Explainability

Salesforce está à frente aqui de forma explícita: o Business Rules Engine
anuncia <cite index="4-1">explicabilidade, simulação e leitura de regras</cite> como funcionalidades formais — validando a tese de que explicabilidade deveria ser recurso de primeira classe. As outras três plataformas não apresentaram funcionalidade equivalente nomeada como tal — apenas trilhas de auditoria técnica, que respondem "o quê" e "quando", mas não necessariamente "por quê" em linguagem de negócio.

### LL-007 — Change Governance

ServiceNow é, disparadamente, a mais madura: <cite index="28-1">boards de governança decidem o quê deve ser decidido e quem tem autoridade, enquanto políticas garantem que essas decisões se traduzam em comportamento operacional repetível</cite>, com recomendação explícita de começar por "governança mínima viável" para não sufocar a velocidade de entrega. Odoo trata governança de mudança principalmente por práticas de engenharia (controle de versão, scripts de migração testáveis). Salesforce concentra sua governança recente em segurança (MFA, verificações de saúde) mais do que em aprovação de regras de negócio. Bitrix24 não apresentou mecanismo formal equivalente.

### LL-008 — Exception-Driven Software (Síntese)

O material de mercado sobre Odoo descreve quase literalmente o sintoma
final desta lição: <cite index="15-1">cada módulo customizado é um empréstimo contra o futuro, com juros que compõem através de scripts de migração, quebra de API e duplicação de funcionalidade</cite>. É confirmação externa de que o problema não é hipotético — é um padrão de mercado reconhecido e monetizado, inclusive pela própria fabricante.

### Síntese Comparativa

| Lesson Learned | Mais madura no mercado | Observação |
|---|---|---|
| LL-001 Parameter Explosion | Salesforce (BRE) | Mas o próprio histórico Salesforce mostra reincidência do problema |
| LL-002 Business Rule Fragmentation | Salesforce (Flow-first) | Odoo tem disciplina equivalente via herança |
| LL-003 Knowledge Concentration | **Nenhuma** | Lacuna real de mercado — maior oportunidade de diferenciação |
| LL-004 Concurrency/Transactions | Salesforce (governor limits) | ServiceNow tem mecanismo assíncrono equivalente |
| LL-005 Customization Debt | ServiceNow (governança formal) | Odoo tem os dados de custo mais concretos e públicos |
| LL-006 Explainability | Salesforce (BRE) | Única com recurso nomeado explicitamente como tal |
| LL-007 Change Governance | ServiceNow | Disparadamente a mais madura das quatro |
| LL-008 Exception-Driven (síntese) | Nenhuma resolve de forma sistêmica | Todas mitigam sintomas; nenhuma trata como métrica de saúde contínua |

**Conclusão da Parte III:** a TRS não está resolvendo problemas
inéditos — está propondo tratar, de forma unificada e desde o primeiro
dia, um conjunto de problemas que o mercado hoje resolve de forma
fragmentada. Essa unificação é a proposta de valor real, e LL-003 é o
único ponto onde nem essa fragmentação existe — é ausência completa.

---

## Parte IV — Requisitos Arquiteturais Formais

Os termos **DEVE**, **NÃO DEVE**, **DEVERIA** e **PODE** são normativos,
no espírito de padrões como RFC 2119: quando um requisito diz "DEVE",
violá-lo é considerado um defeito arquitetural, não uma preferência de
estilo.

### IV.1 — Configuração, Metadados e Políticas

| ID | Requisito |
|---|---|
| AR-CFG-001 | A plataforma DEVE classificar mecanismos variáveis como configuração técnica, preferência, dado mestre, metadado, política ou extensão. |
| AR-CFG-002 | Regras que alterem obrigação, autorização, risco, preço, limite, conformidade ou resultado financeiro DEVEM ser políticas governadas ou invariantes de domínio documentadas. |
| AR-CFG-003 | Políticas NÃO DEVEM depender de precedência implícita. Escopo, composição, herança e exclusão mútua DEVEM ser explícitos. |
| AR-CFG-004 | Publicação de política DEVE exigir validação, testes, simulação de impacto e aprovação proporcional ao risco. |
| AR-CFG-005 | Políticas DEVEM possuir vigência, expiração, revisão e caminho de rollback. |
| AR-CFG-006 | A plataforma DEVE impor limites de complexidade, número de dependências e sobreposição entre políticas. |

### IV.2 — Autoridade das Regras

| ID | Requisito |
|---|---|
| AR-RUL-001 | Cada tipo de regra DEVE possuir um local autoritativo único e declarado. |
| AR-RUL-002 | Frontends, relatórios e conectores NÃO DEVEM redefinir decisões autoritativas já tomadas pelo Policy/Workflow Engine. |
| AR-RUL-003 | Invariantes essenciais de domínio NÃO DEVEM ser desativáveis por política administrativa. |
| AR-RUL-004 | Toda decisão configurável DEVE possuir ID estável, versão e código de razão. |
| AR-RUL-005 | Centralização lógica NÃO DEVE impor um único serviço físico prematuro (microserviço de política obrigatório desde o dia 1). |

### IV.3 — Conhecimento

**Distinção de artefatos (correção introduzida nesta revisão):** antes
dos requisitos abaixo, é preciso separar cinco artefatos que, sem essa
distinção, tendem a se sobrepor e gerar dados parcialmente duplicados:

| Artefato | Responde a | Gerado por |
|---|---|---|
| **Audit Record** | O que mudou? | Qualquer alteração de dado, técnica ou de negócio |
| **Decision Envelope** | Por que a decisão de negócio ocorreu? | Aggregate Root, apenas para decisões relevantes (ver ADR-0006) |
| **Domain Event** | O que aconteceu no domínio? | Aggregate Root, para qualquer mudança de estado relevante ao domínio |
| **Telemetry** | Como o sistema se comportou tecnicamente? | Infraestrutura e instrumentação técnica |
| **Rationale** | Qual foi a intenção humana ou empresarial? | Ator humano ou política, vinculado a um Decision Envelope |

Um Decision Envelope referencia um ou mais Domain Events e pode
referenciar um Rationale; não os substitui, e não é substituído por
Audit Record ou Telemetry.

**Estrutura mínima de Rationale:**

| Campo | Descrição |
|---|---|
| `category` | Classificação do motivo (ex: exceção comercial, ajuste de risco, correção) |
| `reason_code` | Código estruturado, não apenas texto livre (permite agregação e busca) |
| `human_statement` | Texto em linguagem natural, com validação mínima de conteúdo (ver AR-KNW-006) |
| `source_reference` | Referência ao objeto, política ou conversa que originou a decisão |
| `author` | Ator responsável pelo rationale (humano, nunca IA sem revisão — AR-KNW-005) |
| `created_at` | Timestamp de criação |
| `validity` | Vigência (quando aplicável) |
| `confidentiality_level` | Nível de confidencialidade, para autorização de acesso ao próprio rationale |

| ID | Requisito |
|---|---|
| AR-KNW-001 | Mudanças relevantes DEVEM registrar intenção, solicitante, autor, aprovador, impacto e referência. |
| AR-KNW-002 | Conhecimento inferido por IA DEVE ser distinguido visualmente e estruturalmente do conhecimento normativo aprovado por humano. |
| AR-KNW-003 | Busca de conhecimento DEVE respeitar os controles de acesso dos objetos de origem. |
| AR-KNW-004 | Conteúdo normativo DEVE possuir owner, vigência e ciclo de revisão. |
| AR-KNW-005 | IA NÃO DEVE publicar políticas, permissões ou conteúdo normativo sem autorização humana explícita. |
| AR-KNW-006 | O campo `human_statement` de um Rationale NÃO DEVE aceitar texto genérico sem valor informativo (ex: "conforme solicitado") como única justificativa — exige `reason_code` estruturado além do texto livre. |

### IV.4 — Transações e Eventos

| ID | Requisito |
|---|---|
| AR-TXN-001 | Cada agregado crítico DEVE possuir autoridade clara de escrita (um único domínio "dono"). |
| AR-TXN-002 | Integridade financeira, fiscal e estrutural DEVE usar consistência forte no menor escopo possível. |
| AR-TXN-003 | Eventos emitidos após mudança de estado DEVEM usar outbox transacional ou mecanismo equivalente. |
| AR-TXN-004 | Consumidores de evento DEVEM ser idempotentes e capazes de deduplicar. |
| AR-TXN-005 | Processos distribuídos DEVEM expor estado, correlação, retries, compensação e caminho de intervenção manual. |
| AR-TXN-006 | Event sourcing NÃO DEVE ser adotado como padrão global sem um ADR específico que justifique o caso de uso. |
| AR-TXN-007 | O MVP DEVERIA usar arquitetura de monólito modular. |

### IV.5 — Extensibilidade

| ID | Requisito |
|---|---|
| AR-EXT-001 | Extensões NÃO DEVEM alterar código ou schema interno do core. |
| AR-EXT-002 | Extensões DEVEM declarar permissões, eventos, dependências e compatibilidade em um manifesto formal. |
| AR-EXT-003 | Todo upgrade DEVE executar testes automáticos de compatibilidade contra extensões instaladas. |
| AR-EXT-004 | APIs públicas DEVEM possuir versionamento semântico e processo de depreciação. |
| AR-EXT-005 | Extensões privilegiadas DEVEM passar por certificação antes de disponibilização. |
| AR-EXT-006 | Demanda exclusiva de um cliente DEVE ser classificada como capacidade reutilizável, extensão temporária ou requisito rejeitado — nunca como alteração silenciosa do core. |

### IV.6 — Explicabilidade e Auditoria

| ID | Requisito |
|---|---|
| AR-EXP-001 | Toda alteração de estado decorrente de decisão de negócio, autorização, política, workflow ou intervenção humana relevante DEVE produzir um Decision Envelope no instante da decisão. Alterações puramente técnicas (cache, contadores internos, marcação de processamento) DEVEM produzir telemetria ou auditoria técnica conforme sua natureza — não um Decision Envelope completo (ver distinção de artefatos, IV.3). |
| AR-EXP-002 | A evidência DEVE conter entradas, regra aplicada, versão, resultado, razão, ator, tenant, timestamp e ID de correlação. |
| AR-EXP-003 | O texto em linguagem natural da explicação DEVE ser derivado da evidência estruturada — nunca substituí-la nem ser gerado independentemente dela. |
| AR-EXP-004 | O sistema DEVE ser capaz de reproduzir decisões históricas quando autorizado. |
| AR-EXP-005 | Explicações DEVEM respeitar autorização de acesso e mascaramento de dados sensíveis. |

### IV.7 — Governança de Mudanças

| ID | Requisito |
|---|---|
| AR-CHG-001 | Mudanças relevantes DEVEM seguir o ciclo: draft → teste → revisão → aprovação → publicação → observação. |
| AR-CHG-002 | Mudanças críticas DEVEM aplicar segregação de funções (quem propõe não é o único aprovador). |
| AR-CHG-003 | Versões publicadas de política DEVEM ser imutáveis (alteração gera nova versão, não sobrescrita). |
| AR-CHG-004 | Situações de emergência DEVEM usar um caminho de "break-glass" com revisão obrigatória posterior. |
| AR-CHG-005 | Incidentes DEVEM ser correlacionáveis à mudança que os originou. |

### IV.8 — Exceções

| ID | Requisito |
|---|---|
| AR-EXC-001 | Toda variação de comportamento DEVE ser classificada como global, regulatória, regional, setorial, de tenant ou temporária. |
| AR-EXC-002 | Exceções temporárias DEVEM possuir owner, prazo de expiração e plano de eliminação. |
| AR-EXC-003 | O número, as dependências e a sobreposição de exceções ativas DEVEM ser monitorados continuamente. |
| AR-EXC-004 | Contratos comerciais NÃO DEVEM prometer alterações incompatíveis com a governança já estabelecida da plataforma. |

---

## Parte V — Arquitetura de Referência

```text
┌──────────────────────────────────────────────────────────────┐
│ Applications: Sales | CRM | Service | Finance | Industry     │
└──────────────────────────────┬───────────────────────────────┘
                                │
┌──────────────────────────────▼───────────────────────────────┐
│ Application & Extension Framework                            │
│ Metadata | Forms | Packages | SDK | Compatibility             │
└──────────────────────────────┬───────────────────────────────┘
                                │
┌──────────────────────────────▼───────────────────────────────┐
│ Business Runtime                                              │
│ Domain Invariants | Policy | Workflow | Decision Envelope      │
└───────────────┬──────────────────────────────┬────────────────┘
                │                              │
┌───────────────▼──────────────┐ ┌────────────▼────────────────┐
│ Trust & Governance            │ │ Integration & Events         │
│ Tenant | Identity | Permission│ │ Outbox | Idempotency         │
│ Audit | Rationale (Knowledge) │ │ Reconciliation               │
└───────────────┬───────────────┘ └────────────┬────────────────┘
                │                              │
┌───────────────▼──────────────────────────────▼───────────────┐
│ PostgreSQL | Object Storage | Search | Telemetry | Backup     │
└────────────────────────────────────────────────────────────────┘
```

**Leitura da arquitetura:** o Knowledge/Rationale aparece deliberadamente
dentro de "Trust & Governance", ao lado de Audit — não como um módulo
periférico de Fase 6. Isso reflete o achado da Parte III: é a lacuna de
mercado mais significativa, e arquiteturalmente ele depende dos mesmos
dados que Identity e Audit já precisam capturar (autor, motivo,
timestamp) — não é um sistema separado que se conecta depois, é uma
extensão natural do que a Fase 1 já precisa construir.

---

## Parte VI — Decisões Tecnológicas

### VI.1 — Baseline do MVP

| Área | Decisão inicial | Requisito(s) relacionado(s) | Razão |
|---|---|---|---|
| Arquitetura | Monólito modular | AR-TXN-006, AR-TXN-007 | Reduz risco de LL-004 e facilita LL-002/LL-006 |
| Backend | C#/.NET (decidido — ver ADR-0010) | AR-TXN-007, AR-RUL-005, AR-EXP-002 | Equivalência técnica entre candidatos avaliados; decisão por experiência prévia real da equipe, evitando poliglotismo prematuro |
| Frontend | TypeScript + React/Next.js | — | Console administrativo e designers de política/workflow |
| Banco de dados | PostgreSQL e SQL Server (Fase 1); Oracle arquitetado, não implementado — **ver ADR-0011, revisão parcial de ADR-0007** | AR-TXN-001 a 004 | ACID, constraints, JSONB, Row-Level Security (Postgres) / Security Policy (SQL Server), maturidade; suporte a múltiplos motores decidido por razão comercial, não técnica |
| Eventos | Outbox no PostgreSQL — **Fase 3, não baseline do MVP** (ver Parte VII e ADR-0012, que adia `integration_events`/outbox explicitamente); NATS JetStream quando houver consumidores reais, também pós-MVP | AR-TXN-003, AR-TXN-004 | Consistência antes de distribuição — Fase 1/2 usam apenas Audit Record/Rationale (ADR-0012), sem outbox |
| API externa | REST/JSON + OpenAPI | — | Contrato simples, sem redundância de protocolos |
| API interna | Chamadas in-process inicialmente; gRPC apenas após separação física real | AR-RUL-005 | Evita protocolo prematuro para um sistema que ainda é um monólito |
| Identidade | OIDC/OAuth com provedor consolidado | — | Não reinventar autenticação |
| Autorização | RBAC/ABAC contextual próprio | AR-RUL-001, AR-EXP-005 | Decisões de acesso precisam ser explicáveis, não apenas aplicadas |
| Observabilidade | OpenTelemetry + Prometheus/Grafana + logs estruturados | AR-CHG-005, AR-EXP-002 | Correlação de decisões e mudanças |
| Empacotamento | Docker/Compose | — | Simplicidade operacional no início |
| Orquestração | Kubernetes adiado | — | Custo operacional injustificado antes de escala real |
| CI/CD | GitHub Actions | AR-CHG-001 | Código, documentação, testes e ADRs versionados juntos |
| IA | Camada de abstração de provedor (não acoplar a um único fornecedor) | AR-KNW-002, AR-KNW-005 | Evita lock-in e autonomia precoce de modelo sobre decisões normativas |
| Busca/Conhecimento | PostgreSQL full-text/pgvector inicialmente | AR-KNW-003 | Evita motor de busca distribuído antes de volume real justificar |
| Armazenamento de objetos | S3-compatible quando necessário | — | Portabilidade entre provedores de nuvem |

### VI.2 — Decisões Explicitamente Adiadas

- Arquitetura de microserviços;
- Kubernetes antes de necessidade operacional comprovada;
- Kafka (NATS JetStream é suficiente até escala que justifique migração);
- Event sourcing como padrão global (ver AR-TXN-006);
- GraphQL simultâneo a REST;
- múltiplas linguagens no backend;
- banco de dados vetorial dedicado sem evidência de necessidade;
- marketplace público de extensões (ver Parte VII, Fase 9);
- ferramenta de low-code visual completa (o Policy/Workflow Studio nasce mínimo, não como um construtor visual genérico).

### VI.3 — ADRs Obrigatórios (a produzir na Fase 0)

1. TRS como AI-Native Business Operating Platform (registra a Parte I como decisão formal).
2. Monólito modular como arquitetura inicial.
3. Escolha da linguagem principal de backend — **ver ADR-0010, Backend Language, em `docs/adr/ADR-0010-backend-language.md`** (C#/.NET; decidida por experiência prévia real da equipe, diante de equivalência técnica entre os candidatos avaliados).
4. ~~PostgreSQL como system of record único no início~~ — **revisado por ADR-0011**: PostgreSQL e SQL Server são motores de produção suportados desde a Fase 1 (Oracle arquitetado, não implementado), por decisão comercial explícita, não técnica.
5. Taxonomia formal de tipos de regra/configuração — **ver ADR-0008, Rule Placement Standard, em VI.4** (resolve a lacuna de LL-001 e LL-002).
6. Modelo de metadados e extensão (Extension Manifest) — depende do vocabulário definido em **ADR-0006, Core Domain Meta Model, em VI.4**.
7. Estrutura do Decision Envelope (LL-006).
8. Estratégia de outbox e eventos.
9. Modelo de tenancy e isolamento de dados — **ver ADR-0007, Tenant Isolation Strategy, em VI.4**.
10. Ciclo de vida de políticas (draft → publicação → expiração).
11. Core Domain Meta Model — **detalhado como ADR-0006 em VI.4**, e tratado como pré-requisito de qualquer especificação de Domain Model futura.

### VI.4 — ADRs Fundacionais (Extraídos para Arquivos Independentes)

Os três ADRs identificados como decisões mais caras de adiar ou
reverter foram extraídos para arquivos próprios, seguindo o formato
padrão (status, data, responsáveis, contexto, decisão, consequências,
riscos, alternativas rejeitadas, requisitos AR-* relacionados, Lessons
Learned relacionados, critérios de revisão futura). O documento mestre
mantém apenas o resumo executivo de cada um; o conteúdo normativo vive
nos arquivos.

| ADR | Arquivo | Resumo da decisão |
|---|---|---|
| **ADR-0006** — Core Domain Meta Model | `/docs/adr/ADR-0006-core-domain-meta-model.md` | Define o vocabulário obrigatório do Kernel (Entity, Value Object, Aggregate, Capability, Module, Bounded Context, Extension, Policy Scope, Event Scope). Module não pode atravessar Bounded Context. Aggregate Root é o produtor canônico do Decision Envelope, restrito a decisões relevantes (ver IV.6). Inclui a meta-regra de introdução de conceitos, que bloqueia deliberadamente a adição de `Command` nesta versão. |
| **ADR-0007** — Tenant Isolation Strategy | `/docs/adr/ADR-0007-tenant-isolation-strategy.md` | **Row-Level Security (RLS) compartilhado é o único modelo de persistência multi-tenant da Fase 1 à Fase 7** — sem exceção, sem schema dedicado, sem "híbrido" ambíguo. Database dedicado por tenant é decisão explicitamente futura, sujeita a ADR próprio quando houver caso real. |
| **ADR-0008** — Rule Placement Standard | `/docs/adr/ADR-0008-rule-placement-standard.md` | Tabela de 7 tipos de lógica com local autoritativo único. Corrige a versão anterior: cálculo decisório/financeiro (preço, imposto, limite de crédito) pertence ao Aggregate/Domain Service, não à Projection Layer — só derivação sem relevância decisória vai para lá. Define a fronteira entre Authorization Layer ("pode tentar?") e Policy Layer ("é permitido neste contexto?"). |

**Escopo desta tabela:** permanece restrita aos três ADRs que
motivaram originalmente a extração para arquivos próprios (as decisões
técnicas mais caras de reverter, resolvidas ainda na Fase 0). Novos
ADRs (0009 em diante — domínio, infraestrutura, auditoria, governança)
não são adicionados aqui automaticamente só por existirem; a lista
completa e sempre atualizada de todos os ADRs ratificados, com LL/AR/
Fase, vive na matriz de rastreabilidade (Parte IX.4) — esta tabela não
deve duplicá-la.

**Nota de processo:** manter os ADRs como arquivos independentes (em vez
de embutidos no documento mestre, como na v2.0 original) é, em si, uma
aplicação de AR-CHG-003 (versões publicadas são imutáveis, alterações
geram nova versão) — cada ADR agora pode ser revisado e versionado sem
exigir reescrever o documento mestre inteiro.


---

## Parte VII — Roadmap com Critérios de Saída (Gates)

Cada fase termina com **software executável** e um critério de saída
verificável — não apenas uma lista de tarefas concluídas. Esta versão
do roadmap difere da v1.0 anterior em um ponto deliberado: **captura
mínima de conhecimento (rationale) entra já na Fase 1**, não na Fase 6,
por causa do achado da Parte III (LL-003 é a maior lacuna de mercado).

### Fase 0 — Fundação
- Constitution, TRS Definition, os 8 LL revisados, este documento.
- Requisitos AR-* completos.
- ADR-0001 a ADR-0005 (Parte VI.3).
- **ADR-0006 (Core Domain Meta Model), ADR-0007 (Tenant Isolation Strategy) e ADR-0008 (Rule Placement Standard) ratificados — não apenas rascunhados (Parte VI.4).**
- Repositório, CI e padrões de código estabelecidos.

**Gate:** toda decisão de arquitetura registrada a partir daqui referencia explicitamente qual(is) LL e qual(is) requisito(s) AR-* ela atende. Adicionalmente, nenhuma tabela de banco de dados é criada na Fase 1 sem seguir a estratégia de isolamento definida em ADR-0007, e nenhuma nova Capability é registrada sem usar o vocabulário de ADR-0006.

### Fase 1 — Vertical Zero (Núcleo de Governança + Rationale Mínimo)
- Tenant, organização e usuário (Identity mínimo).
- Autorização básica (Permission mínimo).
- Audit log imutável (AR-CHG-005, AR-TXN não aplicável ainda).
- **Campo obrigatório de rationale** em toda entidade crítica desde o primeiro objeto de negócio criado (AR-KNW-001) — esta é a mudança em relação ao roadmap anterior.
- Objetos de negócio: `Tenant`, `Company` (Module `tenancy`, ADR-0013) e
  `User` (Module `identity`), no Bounded Context Trust & Governance;
  `SalesOrder` e `Customer` (Module `sales`, ADR-0009), no Bounded
  Context Sales — cinco Aggregates ao todo, com CRUD simples, cada um
  preservando sua fronteira de contexto (ADR-0006), não um único
  objeto de negócio compartilhado.
- API REST e console administrativo mínimo.

**Gate:** nenhuma operação relevante ocorre sem autorização registrada, autor identificado e motivo capturado.

### Fase 2 — Decisão Governada (Policy Runtime Mínimo)
- Estrutura de política (contexto/condição/ação/governança).
- Política de aprovação por valor (o exemplo `sales_approval_001`).
- Versionamento, vigência e simulação básica (AR-CFG-004, AR-CFG-005).
- Resolução explícita de conflito entre políticas.
- Decision Envelope mínimo (AR-EXP-001, AR-EXP-002).

**Gate:** o usuário consegue simular o efeito de uma política e ver a explicação da decisão antes de publicá-la.

### Fase 3 — Workflow Mínimo
- Máquina de estados simples.
- Tarefa de aprovação humana.
- Cancelamento, timeout e reatribuição.
- Outbox transacional (AR-TXN-003).
- Correlação de eventos e dashboard básico.

**Gate:** o fluxo completo Sales Order → Approval funciona ponta a ponta, com explicação e auditoria em cada etapa.

### Fase 4 — Extensão Controlada
- Extension Manifest (AR-EXT-002).
- Hooks e eventos públicos declarados.
- Pacote de extensão instalável.
- Verificação automática de compatibilidade em upgrade (AR-EXT-003).
- Mecanismo de remoção reversível.

**Gate:** um upgrade de versão do core ocorre sem exigir patch manual em nenhuma extensão instalada.

### Fase 5 — Governança de Mudança Completa
- Ciclo draft/review/approve/publish (AR-CHG-001).
- Segregação de funções (AR-CHG-002).
- Promoção formal entre ambientes dev/test/produção.
- Change sets rastreáveis, análise de impacto, rollback e break-glass (AR-CHG-004).
- Rationale obrigatório vinculado a cada mudança.

**Gate:** uma mudança crítica de política passa, de forma demonstrável em ambiente controlado, por draft, teste, aprovação por um ator distinto de quem propôs, publicação, observação e rollback comprovado — com todo o histórico ligado ao rationale e ao change set correspondente.

### Fase 6 — Knowledge Engine e IA Governada (antecipada em relação ao roadmap v1.0)
- Busca semântica sobre o histórico de rationale acumulado desde a Fase 1.
- Explicação em linguagem natural derivada sempre do Decision Envelope (nunca gerada de forma independente).
- Sugestão de política por IA apenas como rascunho (draft) — nunca publicada automaticamente (AR-KNW-005).
- Registro de proveniência de toda sugestão gerada por IA.
- Testes de isolamento entre tenants para qualquer funcionalidade de IA.

**Gate:** a IA não publica nem executa mudança alguma sem aprovação humana explícita — apenas sugere e explica.

### Fase 7 — Piloto Comercial
- Primeiro produto real: **Sales Order + Approval**, deliberadamente não uma suíte ERP completa.
- Critérios de aceite: nenhuma alteração privada no core para atender o piloto; limites de extensão respeitados; métricas de saúde arquitetural (Parte VIII) coletadas desde o primeiro dia; todo feedback de cliente tratado como candidato a capacidade de produto (AR-EXT-006), nunca como exceção silenciosa.

**Gate:** o piloto opera por um período mínimo definido (ex: um trimestre fiscal completo) sem nenhuma alteração privada no core, sem nenhuma violação de isolamento entre tenants detectada, e com todas as exceções observadas classificadas e mensuradas conforme AR-EXC-001 a 003 — não apenas "sem incidentes reportados".

### Fase 8 — SDK e Ecossistema Privado
- SDK para parceiros selecionados.
- Portal de desenvolvedor, sandbox, certificação (AR-EXT-005).
- Catálogo privado (não público ainda) e modelo de suporte definido.

**Gate:** ao menos um parceiro externo (não a equipe interna) publica e mantém uma extensão certificada, sobrevivendo a pelo menos um upgrade de versão do core sem intervenção manual.

### Fase 9 — Marketplace
- Somente após: contratos de API estáveis, upgrades comprovadamente não-disruptivos em produção real, parceiros piloto validados, e compatibilidade automatizada funcionando de forma confiável — não antes.

**Gate:** o processo de certificação e verificação de compatibilidade (Fase 4/8) roda de forma automatizada, sem revisão manual obrigatória para extensões de baixo risco, antes de qualquer extensão ser aberta a um catálogo público.

---

## Parte VIII — Riscos e Anti-Padrões Consolidados

1. **Centralização vs. extensão:** extensões precisam expressar lógica real; isso só pode ocorrer por meio de tipos e hooks explicitamente autorizados — nunca por acesso irrestrito.
2. **Core imutável vs. evolução do produto:** clientes não modificam o core; a plataforma evolui através de contratos versionados e deliberados (AR-EXT-006).
3. **Política vs. invariante:** integridade estrutural e financeira não pode virar política desativável por engano (AR-RUL-003).
4. **Policy Engine 2.0:** mitigado por DSL limitada e tipada, simulação obrigatória, detecção de conflito, limites de complexidade e expiração de política.
5. **Excesso de engines paralelos:** no MVP, os "engines" do Kernel são módulos internos de um monólito modular — não microserviços desde o dia 1.
6. **Distribuição prematura:** o baseline é monólito modular com persistência relacional (PostgreSQL e SQL Server, ADR-0011), **sem outbox** — outbox entra na Fase 3, quando o roadmap exigir integração assíncrona (ver VI.1, ADR-0012); sagas e event sourcing completo vêm depois, com ADR próprio.
7. **Governança que vira burocracia:** mitigado por proporcionalidade ao risco e por um caminho de break-glass auditado.
8. **IA sem fidelidade:** a IA apresenta e traduz evidência; nunca inventa a causa de uma decisão (AR-EXP-003).
9. **Métricas manipuláveis:** nenhuma métrica isolada (ex: contagem de exceções) deve ser usada sozinha — combinar contagem, complexidade, dependências, incidentes e tempo de compreensão.
10. **Vocabulário arquitetural antecipado sem caso de uso real:** o risco simetricamente oposto ao anti-padrão 5 (excesso de engines). Conceitos como `CommandBus`, `EventStore`, `SagaCoordinator`, `ProcessManager` ou `SnapshotStore` não devem entrar no vocabulário do Kernel antes que um caso de uso concreto do roadmap os exija — ver a meta-regra de introdução de conceitos em ADR-0006 (Parte VI.4). Uma fundação com vocabulário rico demais, cedo demais, é tão frágil quanto uma sem vocabulário nenhum.
11. **Lacunas ainda abertas, fora do escopo detalhado deste documento:** privacidade e retenção de dados pessoais, localização/internacionalização, estratégia de migração de dados legados, disaster recovery, modelo de licenciamento de marketplace, UX do Policy/Workflow Studio, segurança de supply-chain de extensões de terceiros, e estratégia de upgrade sem downtime. Cada um destes merece um ADR ou documento próprio antes da fase do roadmap em que se tornam relevantes (aproximadamente Fases 4 a 9).

    *Nota: tenancy e Meta Model **deixaram de ser lacunas** nesta versão — foram formalizados como ADR-0006, ADR-0007 e ADR-0008 (Parte VI.4), por serem decisões técnicas mais caras de reverter e por isso resolvidas já na Fase 0, antes do início da Fase 1. Modelo econômico/comercial (licenciamento, marketplace, SaaS vs. self-hosted) permanece deliberadamente fora deste documento — é uma decisão de negócio que pode amadurecer em paralelo à arquitetura, sem bloqueá-la, ao contrário das três acima.*

---

## Parte IX — Governança Contínua

Esta parte é nova em relação aos dois documentos originais — nenhum dos
dois definia **quem executa o quê, e com que frequência**, para que os
mecanismos descritos acima não sejam apenas boa intenção documental.

### IX.1 — Execução do Checklist de Architecture Guard Rails

Toda decisão de arquitetura classificada como "relevante" (critério:
afeta um ou mais requisitos AR-* de Parte IV, ou introduz um novo
componente do Kernel) deve, antes de ser implementada, responder por
escrito:

1. Quais LL (001-008) esta decisão toca?
2. Quais requisitos AR-* ela atende, e quais ela poderia violar?
3. Qual seria o modo de falha desta solução, especificamente (não do
   problema que ela resolve — da solução em si; ver Parte VIII)?
4. Esta decisão está na fase correta do roadmap (Parte VII), ou está
   adiantando algo que depende de uma fase anterior ainda incompleta?

Isso deve ser registrado como parte do ADR correspondente — não como
uma reunião informal cujo resultado se perde.

### IX.2 — Cadência de Revisão

- **Métricas de saúde arquitetural** (número de exceções ativas, número
  de políticas por contexto, complexidade média, cobertura de rationale)
  devem ser revisadas a cada fase concluída do roadmap, não apenas
  quando um problema já apareceu.
- **Os 8 Lessons Learned** devem ser revisitados a cada nova fase do
  roadmap iniciada — não porque mudam com frequência, mas porque o
  contexto de implementação muda, e uma lição que parecia resolvida na
  Fase 2 pode reaparecer de forma diferente na Fase 5.
- **Este documento (TRS Foundation v2)** deve ser tratado como vivo:
  toda vez que um ADR novo for aceito, uma referência cruzada deveria
  ser adicionada aqui, para que o documento não fique defasado em
  relação às decisões reais tomadas.

### IX.3 — Papéis Mínimos (mesmo para um time pequeno)

Mesmo que uma única pessoa acumule múltiplos papéis no início, as
funções abaixo precisam existir de forma identificável — não
necessariamente como cargos separados:

- **Guardião do Checklist:** garante que toda decisão relevante passe
  pelo IX.1 antes de ser implementada.
- **Owner do Rationale:** garante que AR-KNW-001 a 004 sejam
  efetivamente preenchidos, não apenas tecnicamente possíveis.
- **Aprovador de Mudança Crítica:** mesmo em um time de uma pessoa, a
  segregação de funções (AR-CHG-002) pode ser simulada por uma pausa
  obrigatória de revisão antes de publicar mudanças de alto impacto —
  não é preciso uma segunda pessoa para ter uma segunda etapa.

### IX.4 — Matrizes de Rastreabilidade

**ADR ↔ Lessons Learned ↔ Requisitos ↔ Fase**

| ADR | LL relacionados | AR relacionados | Fase de bloqueio |
|---|---|---|---|
| ADR-0006 (Meta Model) | LL-002, LL-005 | AR-RUL-001, AR-EXT-002, AR-TXN-001, AR-EXP-001 | 0 |
| ADR-0007 (Tenancy) | LL-004, LL-007 | AR-TXN-001, AR-TXN-002, AR-KNW-003, AR-EXP-005 | 0 (revisado parcialmente por ADR-0011 na Fase 1) |
| ADR-0008 (Rule Placement) | LL-001, LL-002 | AR-CFG-001, AR-RUL-001, AR-RUL-002 | 0 |
| ADR-0009 (Domain Model Fase 1) | LL-001, LL-002, LL-003, LL-004, LL-006, LL-007 | AR-TXN-001, AR-TXN-007, AR-RUL-001 a 003, AR-KNW-001, AR-KNW-006, AR-EXP-001 (parcial) | 1 |
| ADR-0010 (Backend Language) | LL-002, LL-004, LL-006 | AR-TXN-007, AR-RUL-005, AR-EXP-002 | 0→1 |
| ADR-0011 (Multi-Database Support) | LL-004, LL-007, LL-008 | AR-TXN-001, AR-TXN-002, AR-KNW-003, AR-EXP-005, AR-CHG-003, AR-EXC-004 | 1 |
| ADR-0012 (Audit and Logging) | LL-001, LL-007, LL-008 | AR-CHG-005, AR-KNW-001, AR-TXN-001, AR-TXN-002, AR-KNW-003, AR-EXP-005, AR-TXN-006 (limite) | 1 |
| ADR-0013 (Company/Branch) | LL-001, LL-005, LL-007, LL-008 | AR-TXN-001, AR-TXN-002, AR-KNW-003, AR-EXP-005, AR-EXT-006 | 1 (revisão parcial de ADR-0009) |
| ADR-0014 (Architecture Definition Adoption) | LL-002, LL-005, LL-007 | AR-TXN-007, AR-RUL-005, AR-EXT-001, AR-EXT-002, AR-CHG-003 | 0 |
| ADR-0015 (Fase 1 Implementation Reversion) | LL-003, LL-007 | AR-CHG-003, AR-CHG-005 | 1 |
| ADR-0016 (Nested Layout Retroactive Ratification) | LL-007 | AR-CHG-003, AR-RUL-005 | 0/1 (revisa critério do ADR-0014) |

**Requisito ↔ Mecanismo ↔ Evidência (amostra — expandir conforme a implementação avança)**

| Requisito | Mecanismo | Evidência/teste esperado |
|---|---|---|
| AR-TXN-003 | Outbox transacional no PostgreSQL | Teste de atomicidade (evento só existe se a transação local commitou) |
| AR-KNW-003 | Filtros tenant-aware em toda consulta de busca | Teste de isolamento (busca de um tenant nunca retorna dado de outro) |
| AR-EXP-001 | Decision Envelope gerado pelo Aggregate Root | Teste de cobertura (toda decisão relevante tem envelope; toda mudança técnica não gera envelope desnecessário) |
| AR-CFG-004 | Simulação de política antes de publicação | Teste de dry-run (política simulada produz o mesmo resultado que a política publicada, nos mesmos casos) |
| ADR-0007 (RLS) | Política de RLS em toda tabela com `tenant_id` | Teste de CI que falha o build se uma tabela nova não tiver RLS correspondente |

Esta matriz deve crescer junto com a implementação real — cada novo ADR
ou requisito adicionado a partir da Fase 1 deveria vir acompanhado de
uma linha correspondente aqui, não apenas da especificação isolada.

---

## Encerramento

Este documento responde às duas perguntas que motivaram sua fusão:

- **Por que a TRS precisa existir?** Porque os problemas descritos nos
  Lessons Learned são reais, reconhecidos e monetizados pelo mercado
  (Parte III) — e pelo menos um deles (conhecimento organizacional)
  ainda não tem resposta madura em nenhum concorrente relevante.
- **Como impedir que a TRS morra tentando existir?** Através de
  requisitos formais rastreáveis (Parte IV), uma arquitetura de
  referência clara (Parte V), decisões tecnológicas proporcionais ao
  tamanho real do time (Parte VI), um roadmap com critérios de saída
  verificáveis (Parte VII), riscos nomeados com antecedência (Parte
  VIII), e um mecanismo de governança contínua que faz esse rigor
  acontecer na prática, não apenas no papel (Parte IX).
