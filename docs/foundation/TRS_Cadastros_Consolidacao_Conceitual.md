# TRS Platform — Consolidação Conceitual de Cadastros, Identidade, Reference Data, Lifecycle, Códigos e Escopo Organizacional

**Status:** Documento conceitual consolidado para reconciliação arquitetural
**Data:** 2026-07-19
**Projeto:** TRS Platform
**Objetivo:** Consolidar as decisões conceituais discutidas e corrigidas antes de sua incorporação à documentação normativa e aos ADRs da plataforma.

---

# 1. Objetivo

Este documento consolida conceitos discutidos durante a evolução do modelo de cadastros da TRS Platform.

A discussão teve origem na necessidade de modelar entidades de negócio de forma flexível, evitando problemas tradicionais de sistemas empresariais relacionados a:

* duplicação de cadastros;
* mistura entre identidade e papel funcional;
* isolamento inadequado entre clientes;
* configurações compartilhadas sem ownership claro;
* códigos técnicos confundidos com códigos de negócio;
* regras inconsistentes de lifecycle;
* geração de códigos implementada separadamente por módulo;
* dificuldade de representar estruturas com múltiplas Companies.

Durante a análise, foram identificados conceitos transversais que ultrapassam o escopo de `BusinessEntity`.

Os principais conceitos consolidados são:

1. Platform, Deployment, Tenant e Company;
2. escopos arquiteturais;
3. EconomicGroup;
4. BusinessEntity;
5. BusinessEntityIdentifier;
6. IdentifierType;
7. Platform Reference Data;
8. Deployment-Scoped Data/Configuration;
9. tipos oficiais e personalizados;
10. versionamento de definições;
11. Data Lifecycle Policy;
12. Technical Identity;
13. Stable Technical Key;
14. Business Code;
15. Business Code Generation Policy;
16. Record Organizational Scope;
17. conceitos identificados para fases futuras de autorização.

Este documento não substitui automaticamente ADRs existentes.

As decisões aqui registradas devem passar pelo processo formal de reconciliação arquitetural da TRS Platform antes de se tornarem normativas.

---

# 2. Princípio Geral

A TRS Platform deve separar explicitamente conceitos que frequentemente são misturados em sistemas empresariais.

```text
Technical Identity
≠
Stable Technical Key
≠
Business Code
```

```text
BusinessEntity
≠
Business Role
```

```text
Operational Status
≠
Logical Delete
≠
Archive
≠
Physical Delete
```

```text
Record Organizational Scope
≠
User Organizational Scope
≠
Functional Permission
≠
Data Scope
```

```text
Platform Scope
≠
Deployment Scope
≠
Tenant Scope
```

A separação explícita desses conceitos é um princípio central desta proposta.

---

# 3. Estrutura Arquitetural: Platform, Deployment, Tenant e Company

## 3.1 Platform

`Platform` representa a própria TRS Platform como produto governado pela TRS.

Conceitos pertencentes ao Platform Scope são definidos e governados pela TRS.

Exemplo:

```text
Platform
└── Platform Reference Data
    └── IdentifierType oficial
        ├── BR_CPF
        ├── BR_CNPJ
        ├── US_SSN
        ├── US_ITIN
        └── US_EIN
```

---

## 3.2 Deployment

`Deployment` representa uma instalação operacional da TRS Platform destinada a um cliente ou ambiente operacional específico.

O cenário comercial inicial mais provável é:

```text
Cliente TRS
└── Deployment próprio
    └── Banco de dados próprio
        └── Tenant
```

Porém, a arquitetura não deve assumir:

```text
1 Deployment
=
1 Banco
=
1 Tenant
```

como restrição permanente.

A arquitetura deve permitir:

```text
1 Deployment
└── 1 Banco
    ├── Tenant A
    ├── Tenant B
    └── Tenant C
```

mesmo que inicialmente a maioria dos deployments possua apenas um Tenant.

---

## 3.3 Tenant

`Tenant` representa a fronteira lógica de isolamento dos dados de negócio.

O cenário inicial mais comum poderá ser:

```text
1 Cliente TRS
=
1 Deployment
=
1 Banco
=
1 Tenant
```

Essa é uma topologia de implantação, não uma restrição arquitetural.

A arquitetura deve permitir:

```text
1 Deployment
=
1 Banco
=
N Tenants
```

sem exigir redesenho futuro do modelo.

---

## 3.4 Company

`Company` representa uma unidade empresarial interna dentro de um Tenant.

Exemplo:

```text
Tenant: Grupo Luz Nobre

Companies
├── Luz Nobre São Paulo
└── Luz Nobre Minas Gerais
```

A existência de múltiplas Companies cria necessidades específicas relacionadas a:

* compartilhamento de cadastros;
* aplicabilidade de registros;
* acesso de usuários;
* autorização;
* escopo de dados.

Esses conceitos devem permanecer separados.

---

# 4. Taxonomia de Escopos Arquiteturais

A TRS Platform reconhece três níveis principais de escopo:

```text
Platform Scope
Deployment Scope
Tenant Scope
```

Esses níveis representam ownership e fronteira de governança.

---

## 4.1 Platform Scope

Dados e definições governados pela TRS.

Exemplos:

```text
Platform Root
Platform Reference Data
```

### Platform Root

Representa conceitos que formam a própria raiz de uma fronteira arquitetural.

Exemplo atual:

```text
Tenant
```

### Platform Reference Data

Representa definições oficiais compartilhadas e governadas pela TRS.

Exemplo:

```text
IdentifierType oficial
└── BR_CNPJ
```

---

## 4.2 Deployment Scope

Representa dados ou configurações pertencentes ao cliente/deployment e potencialmente compartilháveis entre múltiplos Tenants daquele deployment.

Exemplo:

```text
Deployment: Cliente ABC

CustomIdentifierType
└── CUSTOM_LEGACY_SUPPLIER_ID

Disponível para:
├── Tenant A
└── Tenant B
```

Esse dado:

* não pertence à TRS;
* não pertence exclusivamente ao Tenant A;
* não pertence exclusivamente ao Tenant B;
* pertence ao deployment do cliente.

Portanto:

> Deployment Scope é uma categoria arquitetural legítima.

Entretanto:

> Nenhum dado deve ser classificado automaticamente como Deployment-Scoped.

O domínio deve justificar explicitamente por que a definição precisa existir no nível do deployment e ser compartilhável entre múltiplos Tenants.

---

## 4.3 Tenant Scope

Representa dados de negócio pertencentes a uma única fronteira de Tenant.

Exemplos:

```text
BusinessEntity
EconomicGroup
Customer
SalesOrder
```

Esses dados permanecem isolados entre Tenants.

---

## 4.4 Regra de decisão de ownership

A pergunta principal é:

> Quem é o dono da definição ou do dado?

```text
TRS
↓
Platform Scope

Cliente / Deployment
↓
Deployment Scope

Tenant
↓
Tenant Scope
```

Essa classificação deve ser explícita.

---

# 5. EconomicGroup

`EconomicGroup` representa um agrupamento econômico de entidades de negócio relacionadas.

Exemplo:

```text
EconomicGroup
└── Google
    ├── BusinessEntity Brasil
    │   └── CNPJ
    │
    └── BusinessEntity Estados Unidos
        └── EIN
```

O objetivo é evitar representar uma multinacional como uma única entidade jurídica contendo identificadores fiscais incompatíveis com sua realidade legal.

Cada entidade jurídica permanece independente.

O grupo econômico estabelece apenas o agrupamento conceitual.

---

## 5.1 Escopo

`EconomicGroup` é Tenant-Scoped.

Não existe EconomicGroup compartilhado automaticamente entre Tenants.

---

## 5.2 Fora do escopo inicial

O conceito inicial não pretende modelar:

* participação societária;
* percentual de controle;
* controle direto;
* controle indireto;
* consolidação contábil;
* ultimate beneficial ownership;
* estruturas societárias complexas.

---

## 5.3 Phase-gating

Não existe, neste momento, um caso comercial concreto que exija implementação completa de grupos econômicos multinacionais.

Portanto:

> O conceito pode ser reconhecido arquiteturalmente sem exigir implementação imediata.

A implementação deve ocorrer mediante necessidade funcional real.

---

# 6. BusinessEntity

`BusinessEntity` representa a identidade central de uma pessoa ou organização com a qual o negócio se relaciona.

Exemplos de papéis:

```text
BusinessEntity
├── Customer
├── Supplier
├── Partner
└── Outros papéis
```

Uma mesma entidade pode desempenhar múltiplos papéis.

Exemplo:

```text
BusinessEntity: Empresa ABC

Roles
├── Customer
└── Supplier
```

A identidade central não deve ser confundida com o papel funcional.

---

## 6.1 Escopo

`BusinessEntity` é Tenant-Scoped.

Portanto:

```text
Tenant A
└── BusinessEntity Microsoft
```

é independente de:

```text
Tenant B
└── BusinessEntity Microsoft
```

Não existe, nesta proposta, um Golden Record global cross-tenant.

---

# 7. BusinessEntityIdentifier

Uma `BusinessEntity` pode possuir identificadores oficiais ou específicos do domínio.

Estrutura conceitual simplificada:

```text
BusinessEntityIdentifier
├── IdentifierTypeId
└── Value
```

Outros metadados somente devem ser adicionados mediante necessidade funcional comprovada.

---

## 7.1 Valor canônico

O identificador deve ser persistido uma única vez em formato canônico.

Exemplo:

```text
Valor apresentado:
12.345.678/0001-90

Valor persistido:
12345678000190
```

A máscara não faz parte do valor persistido.

---

## 7.2 NormalizedValue

Não haverá um `NormalizedValue` separado no modelo conceitual atual.

Princípio:

> O valor persistido já é o valor canônico.

Portanto:

```text
BusinessEntityIdentifier
├── IdentifierTypeId
└── Value
```

A introdução futura de uma segunda representação persistida somente deverá ocorrer mediante caso de uso concreto.

---

# 8. IdentifierType

`IdentifierType` representa a definição semântica e técnica de um tipo de identificador.

Exemplo conceitual:

```text
IdentifierType
├── id
├── technical_key
├── name
├── scope
├── country_code
├── value_type
├── format_mask
├── validator_key
├── definition_version
└── status
```

A estrutura física final deverá ser definida posteriormente.

---

# 9. IdentifierTypes Oficiais

A TRS fornece um catálogo oficial inicial de tipos de identificadores.

Exemplos discutidos:

```text
BR_CPF
BR_CNPJ
US_SSN
US_ITIN
US_EIN
```

Esses tipos são:

* definidos pela TRS;
* governados pela TRS;
* versionados pela TRS;
* distribuídos oficialmente;
* pertencentes ao Platform Scope.

---

# 10. IdentifierTypes Personalizados

Clientes podem criar tipos personalizados.

Exemplos:

```text
CUSTOM_LEGACY_SUPPLIER_ID
CUSTOM_GLOBAL_VENDOR_ID
CUSTOM_INTERNAL_REGISTRATION
```

Esses tipos pertencem ao Deployment Scope.

O administrador do deployment define em quais Tenants o tipo estará disponível.

Exemplo:

```text
CustomIdentifierType:
CUSTOM_LEGACY_SUPPLIER_ID

Owner:
Deployment Cliente ABC

AvailableTo:
├── Tenant A
├── Tenant B
└── Tenant C: não disponível
```

---

## 10.1 Autoridade administrativa

Somente uma função administrativa com autoridade no nível do deployment pode:

* criar tipos personalizados;
* alterar suas definições;
* controlar seu lifecycle;
* definir sua disponibilidade por Tenant.

Um Tenant individual não altera a definição compartilhada.

---

# 11. Platform Reference Data

`Platform Reference Data` representa dados de referência oficiais governados pela TRS e distribuídos aos deployments.

Exemplo:

```text
IdentifierType
└── BR_CNPJ
```

A definição de que CNPJ:

* pertence ao Brasil;
* possui determinado formato;
* possui determinada regra de validação;
* possui determinada semântica;

não é dado de negócio pertencente a um Tenant.

Portanto:

> IdentifierType oficial é candidato formal a Platform Reference Data.

---

## 11.1 Consequência para Tenant Isolation

Platform Reference Data:

* não pertence a Tenant;
* não possui ownership tenant-scoped;
* não deve ser submetido automaticamente à mesma política de RLS de Tenant-Scoped Business Data.

Isso exige reconciliação formal com o ADR-0007.

---

# 12. Technical Identity, Stable Technical Key e Business Code

A TRS Platform distingue três conceitos de identificação.

---

## 12.1 Technical Identity

Representa a identidade interna e imutável do registro.

Convenção:

```text
id
```

Exemplo:

```text
id = UUID
```

Características:

* obrigatória para entidades persistidas;
* normalmente Primary Key;
* imutável;
* sem significado de negócio;
* não deve ser usada como identidade semântica conhecida pela aplicação.

---

## 12.2 Stable Technical Key

Representa uma chave técnica semanticamente estável reconhecida pelo software.

Convenção:

```text
technical_key
```

Exemplos:

```text
BR_CNPJ
US_EIN
USD
BRL
SHIPPING
BILLING
```

Características:

* opcional;
* semanticamente estável;
* legível;
* utilizada quando o software precisa reconhecer uma definição de forma consistente;
* independente do UUID;
* não utiliza Business Code Generation Policy.

Exemplo:

```text
IdentifierType

id:
7f91...

technical_key:
BR_CNPJ
```

Outro deployment pode possuir:

```text
id:
a821...

technical_key:
BR_CNPJ
```

O UUID pode variar.

A semântica permanece estável.

---

## 12.3 Business Code

Representa um código de negócio utilizado por usuários ou processos empresariais.

Convenção:

```text
code
```

Exemplos:

```text
CUST-000123
SO-2026-000001
```

Características:

* opcional;
* governado pelo domínio;
* pode ser manual;
* pode ser automático;
* pode ser alterável quando o domínio permitir;
* utiliza Business Code Generation Policy.

---

## 12.4 Coexistência

Uma entidade pode possuir:

```text
id
```

e, quando necessário:

```text
technical_key
```

e/ou:

```text
code
```

Exemplo conceitual:

```text
SomeEntity
├── id
├── technical_key
└── code
```

Nenhum dos dois campos opcionais é obrigatório universalmente.

---

# 13. Namespace de Stable Technical Keys

A TRS deve impedir colisões futuras entre `technical_key` oficial e personalizado.

Tipos oficiais utilizam o namespace reservado da plataforma.

Exemplos:

```text
BR_CNPJ
BR_CPF
US_EIN
US_SSN
```

Tipos personalizados utilizam namespace reservado:

```text
CUSTOM_*
```

Exemplos:

```text
CUSTOM_LEGACY_SUPPLIER_ID
CUSTOM_GLOBAL_VENDOR_ID
CUSTOM_INTERNAL_REGISTRATION
```

Regra:

> Clientes não podem criar Stable Technical Keys personalizados fora do namespace reservado para customizações.

Isso impede que uma definição criada pelo cliente colida futuramente com uma definição oficial publicada pela TRS.

---

# 14. Versionamento de IdentifierType

O `IdentifierType` mantém a mesma identidade ao longo do tempo.

Mudanças na sua definição não criam automaticamente novos tipos.

Exemplo:

```text
IdentifierType
technical_key: BR_CNPJ
definition_version: 2
status: ACTIVE
```

Não devem ser criados automaticamente:

```text
BR_CNPJ_V1
BR_CNPJ_V2
```

como IdentifierTypes independentes.

---

## 14.1 DefinitionVersion

`DefinitionVersion` representa a evolução da definição.

Exemplo:

```text
BR_CNPJ

Definition Version 1
↓
Definition Version 2
```

A identidade semântica continua sendo:

```text
BR_CNPJ
```

---

## 14.2 Status

`Status` representa o estado operacional do tipo.

Exemplos:

```text
ACTIVE
DEPRECATED
INACTIVE
```

Portanto:

```text
DefinitionVersion
→ evolução da definição
```

```text
Status
→ estado operacional
```

São conceitos diferentes.

---

## 14.3 Histórico de versões

A forma física de preservação do histórico de versões ainda deverá ser definida.

Possibilidades incluem:

* histórico de configuração;
* audit trail;
* tabela específica de versões.

A decisão física não deve alterar o princípio:

> O IdentifierType mantém identidade estável.

---

# 15. Atualização de IdentifierTypes Oficiais

A TRS controla e versiona as definições oficiais.

A versão vigente é distribuída pelo processo oficial de atualização do deployment.

Quando uma mudança tiver impacto relevante, poderá existir período de transição controlado.

Exemplo conceitual:

```text
BR_CNPJ
DefinitionVersion: 2
Status: ACTIVE
```

Uma definição antiga poderá permanecer disponível para histórico e auditoria sem se tornar um novo `IdentifierType`.

---

# 16. Mudanças de Validação

Mudanças de validação seguem o princípio:

> Mudanças de validação valem imediatamente para novas inclusões e alterações.

Dados existentes não são automaticamente modificados ou revalidados em massa.

A revalidação histórica ocorre somente por processo:

* explícito;
* controlado;
* auditável.

Exemplo:

```text
50.000 CNPJs existentes

Nova ValidationRule publicada

Novos registros
→ nova regra

Registros alterados
→ nova regra

Registros históricos não alterados
→ permanecem como estão
```

Uma operação explícita poderá produzir:

```text
Revalidate Existing Identifiers

50.000 analisados
49.700 válidos
300 inconsistentes
```

Os registros inconsistentes não devem ser automaticamente:

* excluídos;
* alterados;
* bloqueados.

O domínio define o tratamento apropriado.

---

# 17. Data Lifecycle Policy

A TRS deve distinguir:

```text
Operational Status
Logical Delete
Archive
Physical Delete
```

---

## 17.1 Operational Status

Representa se o registro pode participar de novas operações.

Exemplo:

```text
Seller
├── Active
└── Inactive
```

Um vendedor inativo:

* não participa de novas vendas;
* permanece associado ao histórico.

---

## 17.2 Logical Delete

Representa remoção lógica da visão operacional.

O registro permanece fisicamente armazenado.

Pode preservar:

* histórico;
* auditoria;
* Foreign Keys;
* referências existentes.

---

## 17.3 Archive

Representa tratamento de dados históricos que não precisam permanecer nas estruturas operacionais de uso frequente.

Pode atender objetivos como:

* retenção;
* redução de volume operacional;
* performance;
* compliance.

Archive não é sinônimo de Logical Delete.

---

## 17.4 Physical Delete

Representa exclusão definitiva.

Somente deve ocorrer quando:

* o domínio permitir;
* não houver dependências impeditivas;
* não houver obrigação de retenção;
* a integridade referencial for preservada.

Foreign Keys permanecem como proteção de integridade.

---

## 17.5 Aplicação por domínio

A TRS não deve obrigar todas as tabelas a possuir simultaneamente:

```text
IsActive
IsDeleted
Archived
```

Cada Aggregate ou tipo de dado declara explicitamente quais capacidades de lifecycle suporta.

---

## 17.6 Deprecated

`Deprecated` é um estado específico aplicável quando depreciação possuir significado funcional.

Exemplo:

```text
IdentifierType
├── Active
├── Deprecated
└── Inactive
```

Não é um estado universal obrigatório para todos os dados.

---

# 18. Business Code Generation Policy

Quando uma entidade possuir `Business Code`, deve utilizar o mecanismo padrão definido pela plataforma.

Isso não significa que toda tabela deve possuir `code`.

Princípio:

> Business Code é opcional.

> Quando existir e necessitar geração configurável, deve seguir a política transversal da plataforma.

---

## 18.1 Modos

```text
Manual
Automatic
```

---

## 18.2 Manual

O usuário informa o código.

A TRS continua validando:

* unicidade;
* formato;
* tamanho;
* caracteres permitidos;
* códigos reservados, quando aplicável.

---

## 18.3 Automatic

A TRS gera o código.

Configurações poderão incluir:

```text
Prefix
Padding
StartNumber
```

Exemplo:

```text
CUST-000001
CUST-000002
```

---

## 18.4 Tokens

Tokens oficiais poderão ser utilizados.

Exemplos:

```text
SO-{YYYY}-{SEQ}
INV-{YYYY}-{MM}-{SEQ}
PO-{COMPANY_CODE}-{YYYY}-{SEQ}
```

Scripts ou expressões arbitrárias não devem ser permitidos.

---

## 18.5 Reset

A política poderá suportar:

```text
Never
Yearly
Monthly
```

---

## 18.6 Escopo da sequência

Cada domínio declara explicitamente o escopo adequado.

Exemplos:

```text
Customer
→ Tenant

SalesOrder
→ Company
```

Não deve existir default silencioso universal.

---

# 19. Estratégias de Sequência

## 19.1 HighPerformance

Estratégia padrão.

Características:

* unicidade;
* alta concorrência;
* gaps permitidos.

Exemplo:

```text
CUST-000001
CUST-000002
CUST-000004
```

Princípio:

> HighPerformance é o default para geração automática de Business Codes.

---

## 19.2 TransactionalGapless

Utilizada somente quando continuidade for requisito explícito.

Rollback não deve consumir definitivamente o número.

Essa estratégia pode possuir maior custo de concorrência.

---

## 19.3 Regulated Numbering

Numerações fiscais, legais ou regulatórias seguem regras próprias do domínio e da jurisdição.

O domínio define:

* atribuição;
* consumo;
* cancelamento;
* inutilização;
* gaps;
* obrigações regulatórias.

---

# 20. Concorrência na Geração

A geração automática deve impedir duplicidade.

A serialização, quando necessária, deve ocorrer somente sobre a sequência específica.

Exemplo:

```text
Tenant A + Customer
```

deve ser independente de:

```text
Tenant B + Customer
```

O mecanismo concreto deverá ser compatível com PostgreSQL e SQL Server conforme ADR-0011.

A generalização do mecanismo `config_code_sequences` do ADR-0013 deve ser formalmente analisada antes de se tornar política transversal normativa.

---

# 21. Alteração de Business Code

`Technical Identity` é imutável.

`Business Code` poderá ser alterado somente quando o domínio permitir.

Toda alteração deve ser:

* autorizada;
* auditada;
* rastreável.

---

# 22. Record Organizational Scope

Determinados cadastros Tenant-Scoped podem precisar definir em quais Companies são aplicáveis.

Exemplo:

```text
Tenant: Grupo Luz Nobre

Companies
├── Luz Nobre São Paulo
└── Luz Nobre Minas Gerais
```

Um vendedor:

```text
Seller: Danilo

Applicable Companies:
└── Luz Nobre São Paulo
```

Outro:

```text
Seller: Carlos

Applicable Companies:
├── Luz Nobre São Paulo
└── Luz Nobre Minas Gerais
```

---

## 22.1 Modos

Para domínios que suportarem o conceito:

```text
All Companies
```

ou:

```text
Selected Companies
```

Exemplo:

```text
PaymentTerm: À vista

Scope:
All Companies
```

Outro:

```text
CostCenter: Produção SP

Scope:
Selected Companies

Companies:
└── Luz Nobre São Paulo
```

---

## 22.2 Princípio

> Determinados cadastros Tenant-Scoped podem possuir Record Organizational Scope configurável.

O domínio declara se suporta:

```text
All Companies
```

ou:

```text
Selected Companies
```

Esse mecanismo não é obrigatório para todas as tabelas.

---

# 23. Conceitos Identificados para Discussão Futura

Durante a discussão foram identificados conceitos adicionais.

Eles ainda não estão definidos neste documento.

---

## 23.1 User Organizational Scope

Responde:

> Em quais Companies o usuário pode operar?

Exemplo:

```text
User: Danilo

Companies:
└── Luz Nobre São Paulo
```

---

## 23.2 Functional Permission

Responde:

> O que o usuário pode fazer?

Exemplo:

```text
SalesOrder

Permissions:
├── View
├── Create
├── Update
├── Cancel
└── Approve
```

---

## 23.3 Data Scope

Responde:

> Quais registros o usuário pode acessar?

Possíveis conceitos futuros incluem:

```text
Own Records
Seller Records
Team Records
Company Records
All Tenant Records
```

Esses exemplos não representam decisão arquitetural ratificada.

---

# 24. Phase-Gating de Authorization e Data Scope

`User Organizational Scope`, `Functional Permission` contextual e `Data Scope` detalhado podem evoluir para mecanismos de autorização contextual próximos de RBAC avançado ou ABAC.

O ADR-0009 já posiciona capacidades contextuais avançadas de autorização em fase posterior.

Portanto:

> A descrição desses conceitos neste documento não implica implementação na Fase 1.

A orientação atual é:

```text
Record Organizational Scope
→ conceito de dados que pode ser analisado para Fase 1
```

```text
User Organizational Scope
Functional Permission contextual
Data Scope contextual
→ candidatos predominantes à Fase 2
```

Qualquer capacidade mínima necessária na Fase 1 deverá ser explicitamente justificada e reconciliada com os ADRs existentes.

---

# 25. Separação Fundamental de Escopos de Acesso

A arquitetura deve preservar:

```text
Record Organizational Scope
→ onde o dado se aplica
```

```text
User Organizational Scope
→ onde o usuário pode operar
```

```text
Functional Permission
→ o que o usuário pode fazer
```

```text
Data Scope
→ quais registros o usuário pode acessar
```

Esses conceitos não devem ser condensados prematuramente em uma abstração genérica de "permissão".

---

# 26. Impacto Preliminar nos ADRs

## ADR-0006

Analisar:

* Platform Scope;
* Deployment Scope;
* Tenant Scope;
* Platform Reference Data;
* Stable Technical Key;
* ownership dos conceitos.

Status:

```text
Revisão conceitual necessária
```

---

## ADR-0007

Impacto direto.

Necessário reconciliar:

```text
Platform Reference Data
Deployment-Scoped Data/Configuration
Tenant-Scoped Business Data
```

Também deve permanecer claro que:

```text
BusinessEntity
EconomicGroup
```

são Tenant-Scoped.

Status:

```text
Revisão parcial necessária
```

---

## ADR-0008

Impacto futuro relacionado a:

* User Organizational Scope;
* Functional Permission;
* Data Scope.

Status:

```text
Análise futura
```

---

## ADR-0009

Impactos:

* relação futura Customer × BusinessEntity;
* phase-gating de autorização contextual.

Status:

```text
Reconciliação necessária
```

---

## ADR-0011

Todos os mecanismos persistidos devem possuir estratégia compatível com:

```text
PostgreSQL
SQL Server
```

Incluindo:

* schema;
* constraints;
* índices;
* code generation;
* reference-data migration;
* deployment-scoped data.

Status:

```text
Preservado com impacto de implementação
```

---

## ADR-0012

Impactos:

* alteração de Business Code;
* revalidação de identificadores;
* mudanças administrativas;
* mudanças de lifecycle;
* mudanças de IdentifierType.

Todas devem ser auditáveis quando aplicável.

Status:

```text
Preservado
```

---

## ADR-0013

Impactos:

* Record Organizational Scope;
* relação futura Company × BusinessEntity;
* possível generalização de config_code_sequences.

Status:

```text
Análise e possível generalização necessárias
```

---

## Estratégia de Exclusão de Dados (ainda sem ADR)

**Correção em relação à versão original deste documento:** a Seção 17
(Operational Status / Logical Delete / Archive / Physical Delete) foi
associada, por engano, ao ADR-0015. Isso está incorreto — o ADR-0015
trata da reversão do código de implementação da Fase 1 (Aggregates,
migrations, testes removidos em 2026-07-19), um assunto totalmente
diferente e sem relação com estratégia de exclusão de dados.

A Seção 17 é, na prática, a primeira proposta concreta para a
pendência "estratégia de exclusão de dados", registrada como tópico
em aberto desde antes deste documento existir — que **ainda não tem
nenhum ADR próprio**.

Status:

```text
Nenhum ADR existe ainda — candidato a ADR próprio (ver Seção 29,
"Próximos Passos")
```

---

# 27. Decisões Conceitualmente Acordadas

Até este ponto:

1. A arquitetura suporta múltiplos Tenants por deployment.

2. O cenário inicial mais comum poderá ser um cliente, um deployment, um banco e um Tenant.

3. Existem três níveis principais de escopo: Platform, Deployment e Tenant.

4. Deployment Scope é reconhecido formalmente.

5. Nenhum dado é automaticamente Deployment-Scoped.

6. BusinessEntity é Tenant-Scoped.

7. EconomicGroup é Tenant-Scoped.

8. IdentifierType oficial é Platform Reference Data.

9. IdentifierType personalizado é Deployment-Scoped.

10. O administrador do deployment controla disponibilidade de tipos personalizados por Tenant.

11. O valor do identificador é persistido uma única vez em formato canônico.

12. Não existe NormalizedValue separado no modelo atual.

13. Technical Identity utiliza `id`.

14. Stable Technical Key utiliza `technical_key`.

15. Business Code utiliza `code`.

16. `id` é obrigatório para entidades persistidas.

17. `technical_key` é opcional.

18. `code` é opcional.

19. `technical_key` e `code` podem coexistir quando necessário.

20. Stable Technical Key não utiliza Business Code Generation Policy.

21. Stable Technical Keys personalizados utilizam namespace reservado `CUSTOM_*`.

22. IdentifierType mantém identidade estável ao longo do tempo.

23. DefinitionVersion representa evolução da definição.

24. Status representa estado operacional.

25. Mudanças de validação afetam novas inclusões e alterações.

26. Dados existentes são revalidados somente por processo explícito e auditável.

27. Data Lifecycle distingue Operational Status, Logical Delete, Archive e Physical Delete.

28. Cada domínio declara quais capacidades de lifecycle suporta.

29. Deprecated é estado específico, não universal.

30. Business Code pode ser Manual ou Automatic.

31. Geração automática pode utilizar prefixo, padding, número inicial e tokens oficiais.

32. Reset pode ser configurável.

33. Escopo da sequência é definido pelo domínio.

34. HighPerformance é a estratégia padrão.

35. TransactionalGapless exige requisito explícito.

36. Regulated Numbering pertence ao domínio/jurisdição.

37. Technical Identity é imutável.

38. Business Code pode ser alterado somente quando o domínio permitir e com auditoria.

39. Determinados cadastros podem possuir Record Organizational Scope.

40. O domínio pode suportar All Companies ou Selected Companies.

---

# 28. Questões Ainda Pendentes

Ainda precisam ser discutidas ou reconciliadas:

1. localização física/ownership arquitetural definitivo de Platform Reference Data;

2. revisão formal do ADR-0007;

3. tratamento formal de Deployment Scope nos ADRs;

4. schema migration versus reference-data migration;

5. implementação física do histórico de DefinitionVersion;

6. lifecycle completo de Platform Reference Data;

7. natureza final de Customer diante de BusinessEntity;

8. relação Company × BusinessEntity;

9. implementação futura de EconomicGroup;

10. estratégia completa de Data Lifecycle Policy (exclusão de dados) — ainda sem ADR;

11. generalização formal de config_code_sequences;

12. catálogo oficial de tokens de Business Code;

13. definição futura de User Organizational Scope;

14. definição futura de Functional Permission;

15. definição futura de Data Scope;

16. reconciliação do modelo futuro de autorização com ADR-0008 e ADR-0009.

---

# 29. Próximos Passos

A sequência acordada (reconciliação de 2026-07-19) — **status atualizado**:

```text
1. Corrigir este documento consolidado — FEITO (ver Seção 26)
        ↓
2. ADR — Platform / Deployment / Tenant Scope — FEITO (ADR-0017,
   inclui a revisão do ADR-0007 no mesmo documento)
        ↓
3. ADR — BusinessEntity / relação com Customer e Company — FEITO
   (ADR-0018, cria Bounded Context Party Management, revisa ADR-0009)
        ↓
4. ADR — Data Lifecycle Policy — FEITO (ADR-0019)
        ↓
5. ADR — Business Code Generation Policy — FEITO (ADR-0020, revisa
   ADR-0013)
        ↓
6. Auditoria documental completa — FEITO (IX.4 e CLAUDE.md
   sincronizados com ADR-0017 a ADR-0020)
        ↓
7. Retomar User Organizational Scope — PENDENTE (Fase 2, phase-gated)
        ↓
8. Discutir Functional Permission — PENDENTE (Fase 2, phase-gated)
        ↓
9. Discutir Data Scope — PENDENTE (Fase 2, phase-gated)
        ↓
10. Reconciliar Authorization com ADR-0008/0009 — PENDENTE (Fase 2)
```

Itens 7-10 permanecem deliberadamente fora de escopo — ADR-0009 já
posiciona autorização contextual avançada como Fase 2 (ver Seção 24
deste documento), e nenhum ADR desta rodada antecipou isso.

---

# 30. Conclusão

A discussão iniciada no modelo de `BusinessEntity` revelou a necessidade de fundamentos transversais mais amplos.

A arquitetura passa a distinguir:

```text
Platform Scope
Deployment Scope
Tenant Scope
```

e:

```text
Technical Identity
Stable Technical Key
Business Code
```

e:

```text
Operational Status
Logical Delete
Archive
Physical Delete
```

e:

```text
Record Organizational Scope
User Organizational Scope
Functional Permission
Data Scope
```

A direção consolidada é:

> A TRS Platform deve fornecer mecanismos transversais reutilizáveis, mas cada domínio deve declarar explicitamente quando e como utiliza esses mecanismos.

Isso evita:

```text
Cada módulo inventa sua própria solução
```

e também evita:

```text
Toda entidade é obrigada a utilizar todas as capacidades da plataforma
```

O objetivo é manter:

* consistência;
* governança;
* extensibilidade;
* isolamento;
* clareza semântica;
* evolução controlada;
* ausência de generalização prematura.

Antes de avançar para novos conceitos de autorização, esta consolidação deve ser reconciliada com a documentação existente no repositório.

Somente depois dessa reconciliação, o próximo tópico conceitual recomendado é:

```text
User Organizational Scope
```

seguido futuramente por:

```text
Functional Permission
```

e:

```text
Data Scope
```

respeitando o phase-gating estabelecido pelos ADRs existentes.
