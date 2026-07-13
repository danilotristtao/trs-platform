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
        CHECK (reason_code = 'routine_creation' OR human_statement IS NOT NULL), -- ADR-0009, ajuste 1
    -- Necessário como alvo de FK composta (tenant_id, id) — permite que
    -- `author` (aqui e em outras tabelas) seja validado como pertencente
    -- ao MESMO tenant, não só a um User qualquer que exista.
    CONSTRAINT users_tenant_id_id_unique UNIQUE (tenant_id, id),
    -- Auto-referência: o autor de um User é outro User do mesmo tenant
    -- (nulo no primeiro usuário de cada tenant, que não tem autor humano).
    CONSTRAINT users_author_same_tenant
        FOREIGN KEY (tenant_id, author) REFERENCES users (tenant_id, id)
);

ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE users FORCE ROW LEVEL SECURITY; -- dono da tabela também respeita RLS
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
    CONSTRAINT customers_tax_id_unique_per_tenant UNIQUE (tenant_id, tax_id),
    CONSTRAINT customers_tenant_id_id_unique UNIQUE (tenant_id, id),
    CONSTRAINT customers_author_same_tenant
        FOREIGN KEY (tenant_id, author) REFERENCES users (tenant_id, id)
);

ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
ALTER TABLE customers FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON customers
    USING (tenant_id = current_setting('app.tenant_id')::UUID);

-- ============================================================
-- Sales / Module `sales` — Aggregate SalesOrder
-- ============================================================

CREATE TABLE sales_orders (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    customer_id         UUID NOT NULL, -- referência por ID, nunca embutido (AR-TXN-001); FK composta abaixo
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
        CHECK (reason_code = 'routine_creation' OR human_statement IS NOT NULL),
    -- NOTA: "ao menos uma linha" (invariante 1) não é expressável como
    -- CHECK de coluna — DEVE ser garantida pelo Aggregate na camada de
    -- aplicação, com teste de invariante obrigatório (ver docs/adr/ADR-0009).
    CONSTRAINT sales_orders_tenant_id_id_unique UNIQUE (tenant_id, id),
    -- FK composta: garante que o Customer do pedido pertence ao MESMO
    -- tenant do pedido — uma FK simples para customers(id) permitiria
    -- um SalesOrder de um tenant referenciar Customer de outro tenant.
    CONSTRAINT sales_orders_customer_same_tenant
        FOREIGN KEY (tenant_id, customer_id) REFERENCES customers (tenant_id, id),
    CONSTRAINT sales_orders_author_same_tenant
        FOREIGN KEY (tenant_id, author) REFERENCES users (tenant_id, id)
);

ALTER TABLE sales_orders ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales_orders FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON sales_orders
    USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE TABLE sales_order_lines (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID NOT NULL REFERENCES tenants(id),
    sales_order_id      UUID NOT NULL, -- FK composta abaixo garante mesmo tenant
    line_number         INT NOT NULL,
    description         TEXT NOT NULL,
    quantity            NUMERIC(18,4) NOT NULL CHECK (quantity > 0),
    unit_price_amount   NUMERIC(18,2) NOT NULL,
    unit_price_currency TEXT NOT NULL,
    CONSTRAINT sales_order_lines_unique_line UNIQUE (sales_order_id, line_number),
    -- FK composta: garante que a linha pertence ao MESMO tenant do
    -- pedido — uma FK simples para sales_orders(id) permitiria uma
    -- linha de um tenant referenciar um pedido de outro tenant.
    CONSTRAINT sales_order_lines_order_same_tenant
        FOREIGN KEY (tenant_id, sales_order_id) REFERENCES sales_orders (tenant_id, id) ON DELETE CASCADE
);

ALTER TABLE sales_order_lines ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales_order_lines FORCE ROW LEVEL SECURITY;
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
    order_currency TEXT;
BEGIN
    -- Correção (revisão pós-implementação): a versão anterior buscava
    -- a moeda do pedido mas nunca a comparava com NEW.unit_price_currency
    -- — só verificava consistência entre linhas já existentes, nunca
    -- contra a moeda declarada em sales_orders.total_currency. Isso
    -- permitia a primeira linha divergir da moeda do próprio pedido.
    SELECT total_currency INTO order_currency
    FROM sales_orders
    WHERE id = NEW.sales_order_id;

    IF order_currency IS NOT NULL AND NEW.unit_price_currency <> order_currency THEN
        RAISE EXCEPTION 'sales_order_lines: a linha deve usar a mesma moeda do pedido (esperado ''%'', recebido ''%'') (ADR-0009, ajuste 3)',
            order_currency, NEW.unit_price_currency;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_enforce_single_currency_per_order
    BEFORE INSERT OR UPDATE ON sales_order_lines
    FOR EACH ROW EXECUTE FUNCTION enforce_single_currency_per_order();
