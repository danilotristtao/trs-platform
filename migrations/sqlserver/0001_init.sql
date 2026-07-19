-- 0001_init.sql (SQL Server / T-SQL)
-- Equivalente T-SQL de migrations/0001_init.sql (Postgres) — ADR-0011
-- (suporte multi-banco), ADR-0007 (isolamento por tenant, revisado
-- parcialmente por ADR-0011), ADR-0009 (Aggregates da Fase 1).
--
-- Convenção obrigatória (ADR-0007/ADR-0011): toda tabela de dado de
-- negócio DEVE ter tenant_id + Security Policy nesta mesma migração em
-- que a tabela é criada. `tenants` é a única exceção documentada.
--
-- Equivalente de current_setting('app.tenant_id') do Postgres aqui é
-- SESSION_CONTEXT(N'tenant_id') — toda conexão de aplicação precisa
-- executar, no início da sessão:
--   EXEC sp_set_session_context @key = N'tenant_id', @value = '<uuid>';

-- ============================================================
-- Trust & Governance / Module `tenancy` — Aggregate Tenant
-- ============================================================

CREATE TABLE dbo.tenants (
    id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    name        NVARCHAR(MAX)    NOT NULL,
    status      NVARCHAR(20)     NOT NULL CHECK (status IN ('active', 'suspended')),
    created_at  DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO
-- Sem tenant_id, sem Security Policy — ver nota acima e ADR-0009, Seção Tenant.

-- ============================================================
-- Trust & Governance / Module `identity` — Aggregate User
-- ============================================================

CREATE TABLE dbo.users (
    id                          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    tenant_id                   UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.tenants(id),
    external_identity_reference NVARCHAR(MAX)    NOT NULL,
    email                       NVARCHAR(320)     NOT NULL,
    name                        NVARCHAR(MAX)    NOT NULL,
    status                      NVARCHAR(20)     NOT NULL CHECK (status IN ('active', 'deactivated')),
    role                        NVARCHAR(20)     NOT NULL CHECK (role IN ('tenant_admin', 'member')), -- ADR-0009, ajuste 2
    created_at                  DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET(),

    -- Rationale mínimo (AR-KNW-001 / AR-KNW-006)
    reason_code                 NVARCHAR(30)     NOT NULL DEFAULT 'routine_creation'
                                    CHECK (reason_code IN ('routine_creation', 'manual_override', 'exception_approval', 'correction')),
    human_statement             NVARCHAR(MAX)    NULL,
    author                      UNIQUEIDENTIFIER NULL, -- referência ao User autor da ação, quando aplicável

    CONSTRAINT users_email_format CHECK (email LIKE '_%@_%._%'),
    CONSTRAINT users_human_statement_required
        CHECK (reason_code = 'routine_creation' OR human_statement IS NOT NULL), -- ADR-0009, ajuste 1
    -- Necessário como alvo de FK composta (tenant_id, id) — mesma razão
    -- do Postgres: valida que `author` pertence ao MESMO tenant.
    CONSTRAINT users_tenant_id_id_unique UNIQUE (tenant_id, id),
    CONSTRAINT users_author_same_tenant
        FOREIGN KEY (tenant_id, author) REFERENCES dbo.users (tenant_id, id)
);
GO

-- ============================================================
-- Sales / Module `sales` — Aggregate Customer
-- ============================================================

CREATE TABLE dbo.customers (
    id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    tenant_id           UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.tenants(id),
    name                NVARCHAR(MAX)    NOT NULL,
    tax_id              NVARCHAR(50)     NOT NULL,
    status              NVARCHAR(20)     NOT NULL CHECK (status IN ('active', 'inactive')),
    created_at          DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET(),

    reason_code         NVARCHAR(30)     NOT NULL DEFAULT 'routine_creation'
                            CHECK (reason_code IN ('routine_creation', 'manual_override', 'exception_approval', 'correction')),
    human_statement     NVARCHAR(MAX)    NULL,
    author              UNIQUEIDENTIFIER NULL,

    CONSTRAINT customers_human_statement_required
        CHECK (reason_code = 'routine_creation' OR human_statement IS NOT NULL),
    CONSTRAINT customers_tax_id_unique_per_tenant UNIQUE (tenant_id, tax_id),
    CONSTRAINT customers_tenant_id_id_unique UNIQUE (tenant_id, id),
    CONSTRAINT customers_author_same_tenant
        FOREIGN KEY (tenant_id, author) REFERENCES dbo.users (tenant_id, id)
);
GO

-- ============================================================
-- Sales / Module `sales` — Aggregate SalesOrder
-- ============================================================

CREATE TABLE dbo.sales_orders (
    id                     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    tenant_id              UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.tenants(id),
    customer_id            UNIQUEIDENTIFIER NOT NULL, -- referência por ID, nunca embutido (AR-TXN-001); FK composta abaixo
    status                 NVARCHAR(20)     NOT NULL CHECK (status IN ('draft', 'active')), -- sem estado de aprovação nesta fase
    total_amount           NUMERIC(18,2)    NOT NULL DEFAULT 0, -- calculado pelo Aggregate, nunca digitado
    total_currency         NVARCHAR(3)      NOT NULL,
    created_at             DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET(),

    reason_code            NVARCHAR(30)     NOT NULL DEFAULT 'routine_creation'
                                CHECK (reason_code IN ('routine_creation', 'manual_override', 'exception_approval', 'correction')),
    human_statement        NVARCHAR(MAX)    NULL,
    source_reference       NVARCHAR(MAX)    NULL,
    author                 UNIQUEIDENTIFIER NULL,
    validity               DATETIMEOFFSET   NULL, -- tipicamente nulo na criação de SalesOrder
    confidentiality_level  NVARCHAR(20)     NOT NULL DEFAULT 'standard',

    CONSTRAINT sales_orders_human_statement_required
        CHECK (reason_code = 'routine_creation' OR human_statement IS NOT NULL),
    -- NOTA: "ao menos uma linha" (invariante 1) não é expressável como
    -- CHECK de coluna — DEVE ser garantida pelo Aggregate na camada de
    -- aplicação, com teste de invariante obrigatório (ver docs/adr/ADR-0009).
    CONSTRAINT sales_orders_tenant_id_id_unique UNIQUE (tenant_id, id),
    CONSTRAINT sales_orders_customer_same_tenant
        FOREIGN KEY (tenant_id, customer_id) REFERENCES dbo.customers (tenant_id, id),
    CONSTRAINT sales_orders_author_same_tenant
        FOREIGN KEY (tenant_id, author) REFERENCES dbo.users (tenant_id, id)
);
GO

CREATE TABLE dbo.sales_order_lines (
    id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    tenant_id           UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.tenants(id),
    sales_order_id      UNIQUEIDENTIFIER NOT NULL, -- FK composta abaixo garante mesmo tenant
    line_number         INT              NOT NULL,
    description         NVARCHAR(MAX)    NOT NULL,
    quantity            NUMERIC(18,4)    NOT NULL CHECK (quantity > 0),
    unit_price_amount   NUMERIC(18,2)    NOT NULL,
    unit_price_currency NVARCHAR(3)      NOT NULL,

    CONSTRAINT sales_order_lines_unique_line UNIQUE (sales_order_id, line_number),
    CONSTRAINT sales_order_lines_order_same_tenant
        FOREIGN KEY (tenant_id, sales_order_id) REFERENCES dbo.sales_orders (tenant_id, id) ON DELETE CASCADE
);
GO

-- ============================================================
-- Isolamento por tenant — Security Policy (equivalente ao RLS)
-- ============================================================
-- Diferença relevante em relação ao Postgres: se SESSION_CONTEXT nunca
-- foi definida, ela retorna NULL, e `tenant_id = NULL` é UNKNOWN (nunca
-- verdadeiro) — falha fechada por construção, sem precisar de
-- `missing_ok`/valor sentinela como no current_setting do Postgres.
-- Também não existe conceito de "dono da tabela ignora Security Policy"
-- equivalente ao bypass de RLS do Postgres — por isso não há aqui um
-- equivalente a FORCE ROW LEVEL SECURITY.

CREATE FUNCTION dbo.fn_tenant_isolation_predicate(@tenant_id UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS fn_result
WHERE @tenant_id = TRY_CAST(SESSION_CONTEXT(N'tenant_id') AS UNIQUEIDENTIFIER);
GO

CREATE SECURITY POLICY dbo.tenant_isolation_policy
    ADD FILTER PREDICATE dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.users,
    ADD BLOCK PREDICATE  dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.users AFTER INSERT,
    ADD BLOCK PREDICATE  dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.users AFTER UPDATE,

    ADD FILTER PREDICATE dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.customers,
    ADD BLOCK PREDICATE  dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.customers AFTER INSERT,
    ADD BLOCK PREDICATE  dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.customers AFTER UPDATE,

    ADD FILTER PREDICATE dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.sales_orders,
    ADD BLOCK PREDICATE  dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.sales_orders AFTER INSERT,
    ADD BLOCK PREDICATE  dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.sales_orders AFTER UPDATE,

    ADD FILTER PREDICATE dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.sales_order_lines,
    ADD BLOCK PREDICATE  dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.sales_order_lines AFTER INSERT,
    ADD BLOCK PREDICATE  dbo.fn_tenant_isolation_predicate(tenant_id) ON dbo.sales_order_lines AFTER UPDATE
    WITH (STATE = ON);
GO

-- ============================================================
-- ADR-0009, ajuste 3 — invariante de moeda única por SalesOrder
-- ============================================================
-- Diferença relevante em relação ao Postgres: triggers em T-SQL operam
-- por instrução (statement-level), sobre a tabela virtual `inserted`,
-- que pode conter várias linhas de uma vez — não existe um `NEW` por
-- linha como no Postgres. A verificação abaixo já é, por construção,
-- segura para INSERT em lote (várias linhas de uma vez).

CREATE TRIGGER dbo.trg_enforce_single_currency_per_order
ON dbo.sales_order_lines
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.sales_orders o ON o.id = i.sales_order_id
        WHERE i.unit_price_currency <> o.total_currency
    )
    BEGIN
        RAISERROR('sales_order_lines: a linha deve usar a mesma moeda do pedido (ADR-0009, ajuste 3)', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO
