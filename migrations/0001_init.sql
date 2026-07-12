-- 0001_init.sql
-- Fase 1 (Vertical Zero) — ADR-0007 (RLS único), ADR-0009 (Aggregates da Fase 1)
--
-- Convenção obrigatória (ADR-0007): toda tabela de dado de negócio
-- DEVE ter tenant_id + política de RLS nesta mesma migração em que a
-- tabela é criada. `tenants` é a única exceção documentada — é a raiz
-- da fronteira de isolamento, não dado de negócio dentro dela.

-- ============================================================
-- Trust & Governance / Module `tenancy` — Aggregate Tenant
-- ============================================================

CREATE TABLE tenants (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        TEXT NOT NULL,
    status      TEXT NOT NULL CHECK (status IN ('active', 'suspended')),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
-- Sem tenant_id, sem RLS — ver nota acima e ADR-0009, Seção Tenant.

-- ============================================================
-- Trust & Governance / Module `identity` — Aggregate User
-- ============================================================

CREATE TABLE users (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   UUID NOT NULL REFERENCES tenants(id),
    external_identity_reference TEXT NOT NULL,
    email                       TEXT NOT NULL,
    name                        TEXT NOT NULL,
    status                      TEXT NOT NULL CHECK (status IN ('active', 'deactivated')),
    role                        TEXT NOT NULL CHECK (role IN ('tenant_admin', 'member')), -- ADR-0009, ajuste 2
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT now(),

    -- Rationale mínimo (AR-KNW-001 / AR-KNW-006)
    reason_code                 TEXT NOT NULL DEFAULT 'routine_creation'
                                    CHECK (reason_code IN ('routine_creation', 'manual_override', 'exception_approval', 'correction')),
    human_statement             TEXT,
    author                      UUID, -- referência ao User autor da ação, quando aplicável
    CONSTRAINT users_email_format CHECK (email ~* '^[^@\s]+@[^@\s]+\.[^@\s]+$'),
    CONSTRAINT users_human_statement_required
        CHECK (reason_code = 'routine_creation' OR human_statement IS NOT NULL) -- ADR-0009, ajuste 1
);

ALTER TABLE users ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON users
    USING (tenant_id = current_setting('app.tenant_id')::UUID);

-- ============================================================
-- Sales / Module `sales` — Aggregate Customer
-- ============================================================

CREATE TABLE customers (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    name                TEXT NOT NULL,
    tax_id              TEXT NOT NULL,
    status              TEXT NOT NULL CHECK (status IN ('active', 'inactive')),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),

    reason_code         TEXT NOT NULL DEFAULT 'routine_creation'
                            CHECK (reason_code IN ('routine_creation', 'manual_override', 'exception_approval', 'correction')),
    human_statement     TEXT,
    author              UUID,
    CONSTRAINT customers_human_statement_required
        CHECK (reason_code = 'routine_creation' OR human_statement IS NOT NULL),
    CONSTRAINT customers_tax_id_unique_per_tenant UNIQUE (tenant_id, tax_id)
);

ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON customers
    USING (tenant_id = current_setting('app.tenant_id')::UUID);

-- ============================================================
-- Sales / Module `sales` — Aggregate SalesOrder
-- ============================================================

CREATE TABLE sales_orders (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    customer_id         UUID NOT NULL REFERENCES customers(id), -- referência por ID, nunca embutido (AR-TXN-001)
    status              TEXT NOT NULL CHECK (status IN ('draft', 'active')), -- sem estado de aprovação nesta fase
    total_amount        NUMERIC(18,2) NOT NULL DEFAULT 0, -- calculado pelo Aggregate, nunca digitado
    total_currency      TEXT NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),

    reason_code         TEXT NOT NULL DEFAULT 'routine_creation'
                            CHECK (reason_code IN ('routine_creation', 'manual_override', 'exception_approval', 'correction')),
    human_statement     TEXT,
    source_reference    TEXT,
    author              UUID,
    validity             TIMESTAMPTZ, -- tipicamente nulo na criação de SalesOrder
    confidentiality_level TEXT NOT NULL DEFAULT 'standard',
    CONSTRAINT sales_orders_human_statement_required
        CHECK (reason_code = 'routine_creation' OR human_statement IS NOT NULL)
    -- NOTA: "ao menos uma linha" (invariante 1) não é expressável como
    -- CHECK de coluna — DEVE ser garantida pelo Aggregate na camada de
    -- aplicação, com teste de invariante obrigatório (ver docs/adr/ADR-0009).
);

ALTER TABLE sales_orders ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON sales_orders
    USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE TABLE sales_order_lines (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    sales_order_id      UUID NOT NULL REFERENCES sales_orders(id) ON DELETE CASCADE,
    line_number         INT NOT NULL,
    description         TEXT NOT NULL,
    quantity            NUMERIC(18,4) NOT NULL CHECK (quantity > 0),
    unit_price_amount   NUMERIC(18,2) NOT NULL,
    unit_price_currency TEXT NOT NULL,
    CONSTRAINT sales_order_lines_unique_line UNIQUE (sales_order_id, line_number)
);

ALTER TABLE sales_order_lines ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON sales_order_lines
    USING (tenant_id = current_setting('app.tenant_id')::UUID);

-- ADR-0009, ajuste 3 — invariante de moeda única por SalesOrder.
-- Não é expressável como CHECK de coluna isolada (depende de outras
-- linhas do mesmo pedido). Ver docs/adr/ADR-0009.md — enforced via
-- trigger, não apenas na camada de aplicação, para que a garantia
-- sobreviva a qualquer caminho de escrita (defesa em profundidade,
-- não substitui o teste de invariante do Aggregate).
CREATE OR REPLACE FUNCTION enforce_single_currency_per_order()
RETURNS TRIGGER AS $$
DECLARE
    existing_currency TEXT;
BEGIN
    SELECT total_currency INTO existing_currency
    FROM sales_orders
    WHERE id = NEW.sales_order_id;

    IF existing_currency IS NOT NULL
       AND EXISTS (
           SELECT 1 FROM sales_order_lines
           WHERE sales_order_id = NEW.sales_order_id
             AND unit_price_currency <> NEW.unit_price_currency
       ) THEN
        RAISE EXCEPTION 'sales_order_lines: todas as linhas de um SalesOrder devem compartilhar a mesma moeda (ADR-0009, ajuste 3)';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_enforce_single_currency_per_order
    BEFORE INSERT OR UPDATE ON sales_order_lines
    FOR EACH ROW EXECUTE FUNCTION enforce_single_currency_per_order();
