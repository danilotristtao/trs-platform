# TRS — Definição da Arquitetura de Software e Estrutura da Solution

**Status:** Architecture Reference
**Escopo:** Arquitetura-alvo de longo prazo
**Estilo principal:** Modular Monolith
**Princípios complementares:** Domain-Driven Design, Vertical Slice Architecture, Clean Architecture, Microkernel-Inspired Architecture, Event-Driven Architecture e Selective Microservices
**Ratificado por:** ADR-0014

---

# 1. Objetivo

O TRS é concebido como uma plataforma para construção de aplicações empresariais modulares, extensíveis e governadas.

O objetivo arquitetural não é construir apenas um ERP, CRM, HCM ou qualquer aplicação empresarial específica, mas estabelecer uma fundação tecnológica e conceitual capaz de sustentar diferentes soluções empresariais sobre um núcleo comum.

A arquitetura deve preservar:

* baixo acoplamento;
* alta coesão;
* isolamento entre módulos;
* ownership explícito de domínio e dados;
* isolamento entre tenants;
* extensibilidade controlada;
* rastreabilidade;
* testabilidade;
* portabilidade tecnológica quando estrategicamente necessária;
* evolução arquitetural;
* possibilidade de extração seletiva de módulos para serviços independentes.

A arquitetura deve impedir que o TRS evolua para um monólito tradicional no qual módulos compartilham indiscriminadamente:

* tabelas;
* DbContexts;
* entidades;
* regras de negócio;
* implementações internas;
* dependências técnicas.

O princípio central é:

> O TRS começa operacionalmente simples, mas arquiteturalmente disciplinado.

---

# 2. Natureza deste documento

Este documento define a **arquitetura de referência de longo prazo do TRS**.

A presença de um componente na arquitetura de referência não significa que sua implementação física deva existir imediatamente.

Deve existir uma distinção explícita entre:

```text
Architecture Definition
        │
        │ define
        ▼
Onde uma capacidade pertence
caso ela exista
```

e:

```text
ADR + Roadmap
        │
        │ determinam
        ▼
Se, quando e por que
a capacidade será implementada
```

Portanto:

> Arquitetura definida não significa implementação antecipada.

Componentes como:

* Outbox;
* Inbox;
* Process Managers;
* Sagas;
* Event Bus;
* Policy Engine;
* Metadata Engine;
* extensibilidade avançada;

podem possuir um lugar arquitetural previamente definido sem que sua implementação seja automaticamente autorizada.

Quando um ADR existente bloquear explicitamente a criação de determinada abstração até que exista um caso de uso concreto, essa restrição permanece válida.

A Architecture Definition determina **onde** a capacidade deverá viver.

O ADR determina **se e quando** ela deverá existir.

---

# 3. Hierarquia de decisões arquiteturais

A governança arquitetural do TRS segue a seguinte hierarquia:

```text
TRS Constitution / Foundation Principles
                │
                ▼
            Ratified ADRs
                │
                ▼
      Architecture Definition
                │
                ▼
        Architecture Rules
                │
                ▼
         Solution Structure
                │
                ▼
          Implementation
```

Em caso de conflito entre um ADR ratificado e este documento, o conflito deve ser resolvido explicitamente.

Uma nova decisão arquitetural pode:

* complementar um ADR;
* revisar um ADR;
* substituir um ADR;
* marcar um ADR anterior como superseded.

Mudanças estruturais significativas não devem substituir silenciosamente decisões anteriormente ratificadas.

---

# 4. Definição arquitetural do TRS

A arquitetura do TRS combina diferentes estilos e princípios.

Nenhum padrão isoladamente define toda a plataforma.

A arquitetura é composta por:

```text
Modular Monolith
+
Microkernel-Inspired Architecture
+
Domain-Driven Design
+
Vertical Slice Architecture
+
Clean Architecture Principles
+
Explicit Data Ownership
+
Event-Driven Architecture quando aplicável
+
Selective Microservices quando justificável
```

---

# 5. Modular Monolith

O TRS utiliza o **Modular Monolith** como arquitetura operacional inicial e como principal modelo de organização interna.

Os principais componentes podem ser executados inicialmente como uma única unidade de deployment, porém permanecem separados por fronteiras arquiteturais explícitas.

Visão conceitual:

```text
TRS Application
│
├── BuildingBlocks
├── Kernel
├── Modules
├── Processes
├── Infrastructure
└── ApplicationHost
```

O fato de componentes compartilharem:

* processo;
* runtime;
* cluster;
* instância de banco;

não significa que compartilham ownership.

Cada módulo empresarial possui:

* domínio próprio;
* linguagem própria;
* regras próprias;
* contratos públicos;
* persistência logicamente própria;
* migrations próprias;
* ownership exclusivo dos seus dados.

Regra:

> Compartilhar infraestrutura não significa compartilhar domínio.

---

# 6. Microkernel-Inspired Architecture

O TRS adota uma arquitetura inspirada no padrão Microkernel.

O `TRS.Kernel` contém capacidades fundamentais necessárias para a plataforma.

Exemplos:

```text
TRS.Kernel
│
├── Tenancy
├── Identity
├── Authorization
├── Audit
├── Metadata
├── Configuration
└── Extensibility
```

O Kernel não contém funcionalidades específicas de:

* Sales;
* CRM;
* Finance;
* HCM;
* Inventory;
* Procurement;
* outros produtos empresariais.

Regra fundamental:

> O Kernel conhece a plataforma, mas não conhece os produtos construídos sobre ela.

Consequentemente:

> Se todos os módulos empresariais forem removidos, o Kernel deve continuar conceitualmente válido.

---

# 7. Domain-Driven Design

Os domínios empresariais do TRS seguem princípios de Domain-Driven Design.

Cada módulo representa um Bounded Context ou uma fronteira de domínio claramente definida.

Cada módulo possui:

* linguagem própria;
* entidades próprias;
* agregados próprios;
* Value Objects próprios;
* invariantes próprias;
* Domain Services quando necessários;
* contratos próprios;
* ownership próprio dos dados.

Exemplo:

```text
Sales
├── SalesOrders
└── SalesLines

CRM
├── Customers
├── Contacts
└── Opportunities

Finance
├── Accounts
├── JournalEntries
└── FiscalPeriods

HCM
├── Employees
├── Positions
└── EmploymentContracts
```

Uma entidade pertence exclusivamente ao contexto responsável pelo seu significado.

Um módulo não acessa diretamente entidades internas de outro módulo.

A comunicação ocorre por contratos públicos.

---

# 8. Vertical Slice Architecture

Dentro dos módulos, os casos de uso são preferencialmente organizados por funcionalidade.

Exemplo:

```text
Modules/
└── Sales/
    └── Features/
        └── SalesOrders/
            ├── CreateSalesOrder/
            ├── UpdateSalesOrder/
            ├── GetSalesOrder/
            └── CancelSalesOrder/
```

Cada Vertical Slice concentra os elementos necessários para executar um caso de uso.

Uma feature pode conter:

```text
CreateSalesOrder/
├── Request
├── Handler
├── Validator
├── Response
└── Endpoint
```

O TRS não exige cerimônia desnecessária.

Operações CRUD simples podem utilizar estruturas simplificadas ou scaffolding padronizado.

A complexidade estrutural deve acompanhar a complexidade real do comportamento.

---

# 9. Clean Architecture Principles

O TRS utiliza princípios de Clean Architecture principalmente para proteger a direção das dependências.

A regra conceitual é:

```text
Domain
   ↑
Application / Features
   ↑
Infrastructure Adapters
   ↑
ApplicationHost
```

O domínio não conhece:

* Entity Framework Core;
* PostgreSQL;
* SQL Server;
* HTTP;
* Redis;
* Kafka;
* RabbitMQ;
* APIs externas;
* OpenTelemetry.

O domínio define necessidades.

A infraestrutura implementa capacidades técnicas.

---

# 10. Estrutura de referência da Solution

```text
TRS.sln

src/
│
├── TRS.BuildingBlocks/
│   ├── Domain/
│   ├── Application/
│   ├── Contracts/
│   │   ├── Messaging/
│   │   └── Versioning/
│   ├── Persistence/
│   └── Observability/
│
├── TRS.Kernel/
│   ├── Tenancy/
│   ├── Identity/
│   ├── Authorization/
│   ├── Audit/
│   ├── Metadata/
│   ├── Configuration/
│   └── Extensibility/
│
├── Modules/
│   ├── Sales/
│   │   ├── Domain/
│   │   ├── Features/
│   │   ├── Contracts/
│   │   ├── Policies/
│   │   ├── Persistence/
│   │   │   ├── Abstractions/
│   │   │   ├── PostgreSQL/
│   │   │   └── SqlServer/
│   │   ├── Migrations/
│   │   │   ├── PostgreSQL/
│   │   │   └── SqlServer/
│   │   └── Module.cs
│   │
│   ├── CRM/
│   ├── Finance/
│   ├── HCM/
│   ├── Inventory/
│   ├── Procurement/
│   └── ...
│
├── Processes/
│   └── OrderFulfillment/
│       ├── Contracts/
│       ├── ProcessManager/
│       ├── State/
│       ├── Persistence/
│       │   ├── Abstractions/
│       │   ├── PostgreSQL/
│       │   └── SqlServer/
│       └── Migrations/
│           ├── PostgreSQL/
│           └── SqlServer/
│
├── TRS.Infrastructure/
│   ├── Database/
│   │   ├── PostgreSQL/
│   │   │   ├── ConnectionManagement/
│   │   │   └── TenantIsolation/
│   │   │       └── RLS/
│   │   └── SqlServer/
│   │       ├── ConnectionManagement/
│   │       └── TenantIsolation/
│   │           ├── SessionContext/
│   │           └── SecurityPolicies/
│   │
│   ├── Messaging/
│   │   ├── Outbox/
│   │   ├── Inbox/
│   │   ├── Idempotency/
│   │   ├── Correlation/
│   │   └── EventBus/
│   │
│   ├── Caching/
│   ├── Authentication/
│   ├── ExternalServices/
│   └── Observability/
│
├── TRS.ApplicationHost/
│   ├── Program.cs
│   ├── DependencyInjection/
│   ├── Middleware/
│   ├── Endpoints/
│   └── Configuration/
│
└── TRS.DatabaseMigrator/
    ├── PostgreSQL/
    ├── SqlServer/
    └── MigrationOrchestrator/

tests/
│
├── TRS.ArchitectureTests/
├── TRS.ModuleBoundaryTests/
├── TRS.MultiTenancyTests/
│   ├── PostgreSQL/
│   └── SqlServer/
├── TRS.ContractTests/
├── TRS.ProcessTests/
├── TRS.PersistenceTests/
│   ├── PostgreSQL/
│   └── SqlServer/
└── TRS.IntegrationTests/
```

---

# 11. Decisão sobre coesão de módulo e persistência

A arquitetura adota uma abordagem híbrida.

O ownership da persistência pertence ao módulo.

A infraestrutura genérica do motor pertence ao `TRS.Infrastructure`.

Isso significa:

```text
Module
│
├── Domain
├── Features
├── Contracts
├── Persistence
│   ├── Abstractions
│   ├── PostgreSQL
│   └── SqlServer
└── Migrations
    ├── PostgreSQL
    └── SqlServer
```

Enquanto:

```text
TRS.Infrastructure
└── Database
    ├── PostgreSQL
    │   ├── ConnectionManagement
    │   └── TenantIsolation/RLS
    │
    └── SqlServer
        ├── ConnectionManagement
        └── TenantIsolation
            ├── SessionContext
            └── SecurityPolicies
```

Essa separação estabelece:

> O módulo é proprietário do modelo de persistência de seu domínio.

> O provider é proprietário dos mecanismos técnicos genéricos do motor de banco.

Exemplo:

```text
Sales
│
├── ISalesOrderRepository
│
├── Persistence/PostgreSQL
│   └── PostgreSqlSalesOrderRepository
│
└── Persistence/SqlServer
    └── SqlServerSalesOrderRepository
```

Os mecanismos genéricos permanecem fora:

```text
TRS.Infrastructure.Database.PostgreSQL
├── ConnectionFactory
├── TenantSession
└── RLS Support

TRS.Infrastructure.Database.SqlServer
├── ConnectionFactory
├── SessionContext
└── SecurityPolicy Support
```

Essa decisão busca equilibrar dois objetivos:

1. Dependency Inversion e suporte a múltiplos providers;
2. coesão física do módulo e facilidade de extração futura.

---

# 12. Ownership de dados

Ownership de dados significa:

> O módulo é semanticamente responsável pelos dados do seu domínio.

Não significa que o módulo seja proprietário do PostgreSQL ou SQL Server.

Exemplo:

```text
Sales
      │
      │ owns
      ▼
SalesOrder
      │
      │ persistence abstraction
      ▼
ISalesOrderRepository
      │
      ├───────────────┐
      ▼               ▼
PostgreSQL          SQL Server
Adapter             Adapter
```

Nenhum outro módulo pode acessar diretamente as tabelas internas de Sales.

Regra:

> Um módulo não executa consultas diretas sobre tabelas pertencentes a outro módulo.

Isso inclui JOINs cross-module usados como atalho arquitetural.

---

# 13. Migrations

Migrations pertencem ao componente proprietário dos dados.

Portanto:

```text
Modules/
└── Sales/
    └── Migrations/
        ├── PostgreSQL/
        └── SqlServer/
```

Outro exemplo:

```text
Modules/
└── Finance/
    └── Migrations/
        ├── PostgreSQL/
        └── SqlServer/
```

Isso preserva a regra:

> Quem possui o domínio possui a evolução estrutural dos seus dados.

O `TRS.DatabaseMigrator` não é proprietário das migrations.

Ele apenas:

* descobre;
* ordena;
* valida;
* executa;

as migrations pertencentes aos componentes.

---

# 14. Suporte a múltiplos motores de banco

A arquitetura permite suporte a múltiplos motores de banco.

Quando PostgreSQL e SQL Server forem oficialmente suportados como motores de produção, ambos devem possuir equivalência funcional nos requisitos arquiteturais críticos.

Isso não significa que suas implementações internas sejam idênticas.

Exemplo:

```text
Tenant Isolation
│
├── PostgreSQL
│   └── Row-Level Security
│
└── SQL Server
    ├── SESSION_CONTEXT
    └── Security Policy
```

O contrato arquitetural é comum:

```text
Tenant A
não pode acessar
dados do Tenant B
```

A implementação técnica pode variar por provider.

O suporte a múltiplos motores representa um custo arquitetural real.

Cada novo motor suportado implica potencialmente:

* migrations específicas;
* repositories específicos;
* testes específicos;
* mecanismos específicos de tenant isolation;
* otimizações específicas;
* manutenção contínua.

Portanto:

> A inclusão de um database engine como provider oficialmente suportado deve ser uma decisão estratégica explícita e registrada em ADR.

O dual-engine não deve ser tratado como gratuito.

---

# 15. Multi-tenancy

Multi-tenancy é um requisito estrutural e de segurança.

A arquitetura utiliza defesa em profundidade.

Fluxo conceitual:

```text
Request
   │
   ▼
Tenant Resolution
   │
   ▼
TenantContext
   │
   ▼
Persistence Adapter
   │
   ├── PostgreSQL
   │   └── RLS
   │
   └── SQL Server
       ├── SESSION_CONTEXT
       └── Security Policy
```

Filtros no ORM podem ser utilizados como proteção adicional.

Eles não constituem, isoladamente, a garantia primária de segurança.

Regra:

> O isolamento de tenant deve falhar fechado.

Quando o contexto obrigatório de tenant não existir, operações tenant-scoped não devem silenciosamente acessar dados globais ou dados de múltiplos tenants.

---

# 16. TRS.BuildingBlocks

`TRS.BuildingBlocks` contém abstrações técnicas reutilizáveis.

Não contém conceitos concretos de negócio.

Permitido:

```text
Entity
AggregateRoot
ValueObject
Result
Error
IIntegrationEvent
MessageEnvelope
```

Não permitido:

```text
Customer
Invoice
Employee
SalesOrder
JournalEntry
```

Regra:

> BuildingBlocks fornece linguagem técnica compartilhada, nunca domínio empresarial compartilhado artificialmente.

---

# 17. BuildingBlocks/Contracts

Contém infraestrutura conceitual para contratos.

Exemplos:

```text
IMessage
IIntegrationEvent
MessageEnvelope
MessageMetadata
```

Pode definir metadados como:

```text
MessageId
MessageType
CorrelationId
CausationId
TenantId
Version
Timestamp
TraceId
```

Não contém contratos concretos de negócio.

A distinção é:

```text
BuildingBlocks.Contracts
= infraestrutura de contrato

Sales.Contracts
= contratos concretos de Sales

CRM.Contracts
= contratos concretos de CRM
```

Ownership de contrato pertence ao domínio que possui o conceito.

---

# 18. TRS.Kernel

O Kernel contém capacidades centrais da plataforma.

```text
TRS.Kernel
├── Tenancy
├── Identity
├── Authorization
├── Audit
├── Metadata
├── Configuration
└── Extensibility
```

O Kernel não conhece módulos empresariais.

Regra:

```text
Kernel
    pode → BuildingBlocks

Kernel
    não pode → Modules
```

---

# 19. Kernel/Tenancy

Responsável por:

* definição de tenant;
* resolução do tenant;
* status do tenant;
* TenantContext;
* identificação da fronteira organizacional.

A responsabilidade é:

```text
Kernel.Tenancy
= Quem é o tenant?
```

Enquanto:

```text
Infrastructure.Database
= Como o banco garante o isolamento?
```

---

# 20. Kernel/Identity

Responsável por:

* usuários;
* autenticação;
* credenciais;
* memberships;
* associação usuário-tenant;
* claims fundamentais.

Dados tenant-owned de Identity continuam sujeitos às regras de isolamento.

A existência de Identity dentro do Kernel não torna automaticamente todos os seus dados globais.

Regra:

> O escopo de tenancy é propriedade do dado, não da pasta onde o código está localizado.

O próprio `Tenant`, como raiz da fronteira, pode possuir tratamento global específico.

Um `User` associado obrigatoriamente a um tenant continua tenant-owned.

---

# 21. Kernel/Authorization

O Kernel pode fornecer mecanismos genéricos de autorização.

Exemplo:

```text
PolicyEvaluator
AuthorizationContext
ClaimsEvaluator
```

Regras concretas permanecem nos módulos.

```text
Kernel.Authorization
= mecanismo

Finance.Policies
= regra concreta
```

Exemplo:

```text
ApproveHighValueJournal
```

pertence ao Finance, não ao Kernel.

A existência da posição arquitetural de `Authorization` não autoriza automaticamente a implementação antecipada de um Policy Engine genérico.

---

# 22. Kernel/Audit

Audit fornece mecanismos centrais de rastreabilidade.

Pode registrar:

* quem;
* quando;
* tenant;
* origem;
* operação;
* CorrelationId;
* CausationId;
* alterações;
* justificativa.

Audit possui características próprias de:

* alto volume;
* retenção;
* compliance;
* arquivamento;
* particionamento.

Portanto, sua estratégia de armazenamento pode evoluir independentemente quando necessário.

---

# 23. Kernel/Metadata

Responsável pelo metamodelo controlado da plataforma.

Pode incluir:

* tipos de objeto;
* definições de campo;
* atributos extensíveis;
* metadados configuráveis.

Regra:

> Metadata não substitui indiscriminadamente modelos de domínio fortes.

---

# 24. Kernel/Configuration

Deve distinguir explicitamente:

```text
Configuração técnica
Configuração funcional
Preferência
Dado mestre
Metadado
Política
Invariante
```

Nem toda regra deve ser configurável.

Invariantes fundamentais de domínio não podem ser transformadas em configurações alteráveis.

---

# 25. Kernel/Extensibility

Define mecanismos governados de extensão.

Pode incluir:

* registro de módulos;
* extension points;
* hooks;
* providers;
* plugins;
* customizações controladas.

Regra:

> Extensibilidade não significa permitir alteração arbitrária do núcleo.

---

# 26. Modules

`Modules` contém os domínios empresariais.

Exemplos:

```text
Sales
CRM
Finance
HCM
Inventory
Procurement
```

Cada módulo é uma fronteira independente.

Estrutura conceitual:

```text
Module
│
├── Domain
├── Features
├── Contracts
├── Policies
├── Persistence
├── Migrations
└── Module.cs
```

---

# 27. Module/Contracts

Representa a superfície pública do módulo.

Outros módulos podem consumir contratos públicos.

Não podem acessar diretamente:

```text
Module.Domain
Module.Features
Module.Persistence
```

Contratos podem incluir:

```text
PublicApi/
IntegrationEvents/
```

A evolução deve seguir estratégia additive-first sempre que possível.

Breaking changes exigem versionamento explícito quando necessário.

---

# 28. Comunicação entre módulos

A comunicação pode ser síncrona ou assíncrona.

Síncrona:

```text
Module A
   │
   ▼
Module B.Contracts.PublicApi
```

Assíncrona:

```text
Module A
   │
   ▼
Outbox
   │
   ▼
Event Bus
   │
   ├── Module B
   └── Process Manager
```

Regra:

> Módulos nunca se comunicam acessando diretamente a persistência interna uns dos outros.

---

# 29. Event-Driven Architecture

Quando comunicação assíncrona confiável for necessária, a arquitetura prevê:

```text
Outbox
Inbox
Idempotency
Correlation
Causation
Event Bus
```

Princípio:

> Transações ACID são locais à fronteira proprietária dos dados.

Coordenação distribuída utiliza:

* contratos;
* eventos;
* consistência eventual;
* Process Managers quando justificados.

---

# 30. Outbox

Outbox resolve o problema de consistência entre:

```text
Alteração persistida
+
Evento a publicar
```

Exemplo:

```text
Transaction
│
├── UPDATE Aggregate
├── INSERT OutboxMessage
└── COMMIT
```

Posteriormente:

```text
Outbox Processor
        │
        ▼
     Event Bus
```

A presença do Outbox na arquitetura de referência não determina sua implementação antes da necessidade ou fase ratificada.

---

# 31. Inbox e Idempotência

Inbox registra mensagens recebidas.

Idempotência protege consumidores contra processamento duplicado.

Esses mecanismos são necessários quando a semântica de entrega da infraestrutura permitir duplicidade.

---

# 32. Correlation e Causation

Mensagens distribuídas devem possuir informações suficientes para reconstrução causal.

Exemplo:

```text
CreateOrder
CorrelationId: ABC
      │
      ▼
OrderCreated
CorrelationId: ABC
CausationId: CreateOrder
      │
      ▼
CreditRequested
CorrelationId: ABC
CausationId: OrderCreated
```

Isso permite tracing ponta a ponta.

---

# 33. Processes

`Processes` representa coordenação de processos empresariais que atravessam múltiplos módulos.

Exemplo conceitual:

```text
OrderFulfillment
│
├── Sales
├── Finance
├── Inventory
└── Logistics
```

Nenhum módulo individual precisa assumir responsabilidade pelo processo completo.

---

# 34. Process Manager / Saga

Um Process Manager coordena.

Os módulos decidem e executam suas próprias regras.

```text
Module Events
      │
      ▼
Process Manager
      │
      ├── Module A.Contracts
      ├── Module B.Contracts
      └── Module C.Contracts
```

Regra:

```text
Processes
    pode → Module.Contracts

Processes
    não pode → Module.Domain

Processes
    não pode → Module.Features

Processes
    não pode → Module.Persistence
```

A implementação de Process Managers continua condicionada à existência de um caso de uso concreto e à decisão arquitetural correspondente.

---

# 35. Estado de Processes

Processos de longa duração podem possuir estado próprio.

Exemplo:

```text
OrderFulfillmentState
├── ProcessId
├── TenantId
├── CorrelationId
├── CurrentStep
├── Status
├── StartedAt
└── UpdatedAt
```

Esse estado pertence ao processo.

Não pertence a Sales, CRM, Finance ou Inventory.

Processos tenant-owned devem respeitar as mesmas garantias estruturais de isolamento multi-tenant.

---

# 36. TRS.Infrastructure

`TRS.Infrastructure` contém mecanismos técnicos compartilhados e adapters que não pertencem semanticamente a um módulo específico.

Exemplos:

```text
Database engine infrastructure
Messaging infrastructure
Caching
Authentication adapters
External services
Observability
```

Não deve se transformar em uma camada genérica onde qualquer código sem dono é colocado.

Regra:

> Todo componente de Infrastructure deve possuir uma responsabilidade técnica claramente identificável.

---

# 37. TRS.ApplicationHost

É o Composition Root e ponto de execução.

Responsável por:

* inicializar ASP.NET Core;
* Dependency Injection;
* middleware;
* endpoints;
* autenticação;
* observabilidade;
* registro de módulos;
* infraestrutura de runtime.

Não contém regras de negócio.

```text
ApplicationHost
= composição

Kernel
= plataforma

Modules
= negócio

Processes
= coordenação

Infrastructure
= tecnologia
```

---

# 38. TRS.DatabaseMigrator

Responsável pela execução coordenada das migrations.

As migrations continuam pertencendo aos seus componentes.

O Migrator apenas coordena.

```text
Deployment
    │
    ▼
DatabaseMigrator
    │
    ├── Kernel Migrations
    ├── Module Migrations
    └── Process Migrations
```

Para cada provider suportado:

```text
PostgreSQL
ou
SQL Server
```

A ordem deve respeitar dependências explícitas.

O ApplicationHost não deve executar automaticamente migrations em produção.

---

# 39. Architecture Tests

A arquitetura deve ser executável sempre que possível.

`TRS.ArchitectureTests` deve validar regras como:

```text
BuildingBlocks não referencia Kernel.

BuildingBlocks não referencia Modules.

Kernel não referencia Modules.

Domain não referencia EF Core.

Domain não referencia ASP.NET Core.

Modules não referenciam providers concretos de outros módulos.
```

---

# 40. Module Boundary Tests

Devem validar:

```text
Sales não acessa Finance.Domain.

Sales não acessa Finance.Persistence.

Finance não acessa CRM.Domain.

Cross-module communication ocorre via Contracts.
```

Para adapters de persistência:

```text
Sales.Persistence.PostgreSQL
    pode implementar interfaces de Sales

Sales.Persistence.PostgreSQL
    não pode acessar Finance.Domain
```

O mesmo vale para SQL Server.

---

# 41. Multi-Tenancy Tests

Devem existir testes equivalentes para cada provider oficialmente suportado.

```text
TRS.MultiTenancyTests/
├── PostgreSQL/
└── SqlServer/
```

Devem garantir:

* Tenant A não lê Tenant B;
* Tenant A não altera Tenant B;
* ausência de TenantContext falha fechado;
* bypass indevido é impedido;
* Process State tenant-owned é isolado.

---

# 42. Contract Tests

Devem validar:

* serialização;
* compatibilidade;
* versionamento;
* additive-first;
* breaking changes;
* envelopes;
* metadados obrigatórios.

---

# 43. Process Tests

Devem validar:

* transições de estado;
* idempotência;
* retomada após falha;
* correlação;
* causalidade;
* isolamento multi-tenant;
* dependência exclusiva de Contracts públicos.

---

# 44. Persistence Tests

Cada database provider oficialmente suportado deve possuir testes próprios.

```text
PersistenceTests
├── PostgreSQL
└── SqlServer
```

Devem validar:

* repositories;
* transactions;
* migrations;
* constraints;
* concurrency;
* tenant isolation;
* comportamento equivalente esperado.

---

# 45. Regras arquiteturais fundamentais

## Regra 1 — Ownership de domínio

Cada módulo é dono exclusivo do seu domínio.

## Regra 2 — Ownership de dados

Cada módulo é semanticamente proprietário dos dados pertencentes ao seu domínio.

## Regra 3 — Isolamento de dados

Um módulo não acessa diretamente tabelas de outro módulo.

## Regra 4 — Contratos públicos

Comunicação cross-module ocorre por contratos públicos.

## Regra 5 — Fronteira transacional

Transações ACID são locais à fronteira proprietária dos dados.

## Regra 6 — Kernel independente

Kernel não depende de módulos empresariais.

## Regra 7 — BuildingBlocks técnico

BuildingBlocks não contém conceitos concretos de negócio.

## Regra 8 — Dependency Inversion

Domínio e aplicação não dependem diretamente de tecnologia de persistência.

## Regra 9 — Coesão de módulo

Implementações específicas de persistência de um módulo permanecem organizacionalmente associadas ao módulo proprietário.

## Regra 10 — Infraestrutura genérica por provider

Mecanismos genéricos de PostgreSQL e SQL Server pertencem à infraestrutura compartilhada.

## Regra 11 — Multi-tenancy fail-closed

Operações tenant-owned devem falhar fechado quando o tenant obrigatório não puder ser estabelecido.

## Regra 12 — Defesa em profundidade

Filtros de aplicação ou ORM não substituem garantias de isolamento no banco quando estas forem requisito arquitetural.

## Regra 13 — Process isolation

Processes acessam módulos exclusivamente por contratos públicos.

## Regra 14 — Process State ownership

Estado persistente de um processo pertence ao próprio processo.

## Regra 15 — Migrations ownership

Migrations pertencem ao componente proprietário dos dados.

## Regra 16 — DatabaseMigrator

DatabaseMigrator coordena migrations, mas não se torna proprietário delas.

## Regra 17 — Contratos additive-first

Contratos públicos devem evoluir preferencialmente de forma compatível e aditiva.

## Regra 18 — Mensageria confiável

Quando publicação confiável de eventos for necessária em conjunto com persistência, deve ser utilizado mecanismo transacional apropriado, como Outbox.

## Regra 19 — Idempotência

Consumidores devem suportar idempotência quando a infraestrutura permitir entrega duplicada.

## Regra 20 — Correlação e causalidade

Operações distribuídas devem possuir rastreabilidade ponta a ponta.

## Regra 21 — Arquitetura executável

Regras críticas devem ser protegidas automaticamente por testes arquiteturais sempre que tecnicamente possível.

## Regra 22 — Portabilidade tem custo

Cada database provider oficialmente suportado representa compromisso permanente de implementação, migrations, testes e manutenção.

## Regra 23 — Provider como decisão estratégica

A adição ou remoção de um database provider oficialmente suportado deve ser registrada por ADR.

## Regra 24 — Phase-gating preservado

A existência de uma posição na arquitetura de referência não autoriza automaticamente a implementação da capacidade.

## Regra 25 — ADR prevalece sobre antecipação

Quando um ADR exigir caso de uso concreto antes da introdução de uma abstração, essa abstração não deve ser materializada apenas porque aparece na arquitetura-alvo.

---

# 46. Extração seletiva para microsserviços

O TRS não inicia como arquitetura de microsserviços.

A extração ocorre somente quando houver necessidade concreta.

Exemplos:

* escala independente;
* disponibilidade diferente;
* isolamento operacional;
* deploy independente;
* ownership organizacional independente.

A coesão por módulo busca facilitar:

```text
Modular Monolith
        │
        ▼
Identificação de necessidade
        │
        ▼
Extração do módulo
        │
        ▼
Serviço independente
```

A extração pode exigir mover:

* Domain;
* Features;
* Contracts;
* Persistence adapters;
* Migrations.

Por isso, implementações específicas do domínio permanecem associadas organizacionalmente ao módulo sempre que possível.

Infraestrutura genérica continua compartilhada.

---

# 47. Visão consolidada

```text
                         TRS PLATFORM
                              │
                    Modular Monolith
                              │
              Microkernel-Inspired Kernel
                              │
       ┌──────────────────────┼──────────────────────┐
       │                      │                      │
     Sales                   CRM                  Finance
       │                      │                      │
       └──────────── Public Contracts ──────────────┘
                              │
                    Event-Driven quando necessário
                              │
                    Processes quando necessários
                              │
                 Outbox / Inbox / Idempotency
                              │
                Correlation / Causation / Tracing
                              │
                Explicit Data Ownership
                              │
                 Database Provider Abstraction
                              │
                  ┌───────────┴───────────┐
                  │                       │
              PostgreSQL              SQL Server
                  │                       │
                 RLS          SESSION_CONTEXT +
                              Security Policies
```

Internamente:

```text
Module
│
├── Domain
├── Features
├── Contracts
├── Policies
├── Persistence
│   ├── Abstractions
│   ├── PostgreSQL
│   └── SqlServer
├── Migrations
│   ├── PostgreSQL
│   └── SqlServer
└── Module.cs
```

Infraestrutura compartilhada:

```text
TRS.Infrastructure
│
├── Database
│   ├── PostgreSQL
│   └── SqlServer
├── Messaging
├── Caching
├── Authentication
├── ExternalServices
└── Observability
```

A distinção fundamental é:

```text
Module
= proprietário do domínio e dos dados

Module Persistence Adapter
= implementação específica de acesso aos dados do módulo

TRS.Infrastructure.Database
= capacidades genéricas do database provider

DatabaseMigrator
= coordenação de migrations

ApplicationHost
= composição e execução
```

---

# 48. Princípio final

O TRS deve evitar dois extremos:

```text
Over-engineering prematuro
```

e:

```text
Acoplamento estrutural irreversível
```

A arquitetura deve permitir que o sistema comece simples operacionalmente sem comprometer sua capacidade de evolução.

O resultado esperado é uma plataforma na qual:

* o Kernel permanece pequeno e governado;
* BuildingBlocks permanece estritamente técnico;
* módulos mantêm autonomia sobre domínio e dados;
* implementações de persistência permanecem associadas aos seus módulos;
* mecanismos genéricos de banco permanecem isolados por provider;
* PostgreSQL e SQL Server, quando oficialmente suportados, possuem garantias equivalentes;
* contratos representam fronteiras públicas explícitas;
* processos cross-module possuem ownership próprio;
* multi-tenancy é uma garantia estrutural de segurança;
* mensagens são rastreáveis por correlação e causalidade;
* migrations pertencem aos componentes proprietários;
* DatabaseMigrator apenas coordena sua execução;
* extração futura de módulos permanece viável;
* abstrações futuras não são implementadas antes de justificativa concreta;
* regras arquiteturais são protegidas automaticamente.

Esta definição constitui a **arquitetura de referência de longo prazo do TRS**.

Ela define onde cada responsabilidade pertence caso seja necessária.

Os ADRs continuam responsáveis por registrar decisões específicas, trade-offs, tecnologias, mecanismos e critérios de ativação.

O roadmap determina quando cada capacidade será implementada.

A implementação deve permanecer consistente com essas três fontes:

```text
Architecture Definition
        +
       ADRs
        +
     Roadmap
```
