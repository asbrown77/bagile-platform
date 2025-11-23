-- 1. raw_refunds
CREATE TABLE IF NOT EXISTS bagile.raw_refunds (
    id SERIAL PRIMARY KEY,
    woo_order_id BIGINT NOT NULL,
    refund_id BIGINT NOT NULL,
    refund_total NUMERIC(12,2) NOT NULL,
    refund_reason TEXT,
    line_items JSONB NOT NULL DEFAULT '[]',
    raw_json JSONB NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (woo_order_id, refund_id)
);

CREATE INDEX IF NOT EXISTS idx_raw_refunds_order 
    ON bagile.raw_refunds (woo_order_id);


-- 2. raw_transfers
CREATE TABLE IF NOT EXISTS bagile.raw_transfers (
    id SERIAL PRIMARY KEY,
    order_id BIGINT NOT NULL,
    course_schedule_id BIGINT NOT NULL,
    from_student_email TEXT NOT NULL,
    to_student_email TEXT NOT NULL,
    reason TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_raw_transfers_order 
    ON bagile.raw_transfers (order_id);


-- 3. Patch orders
ALTER TABLE bagile.orders
    ADD COLUMN IF NOT EXISTS refund_total NUMERIC(12,2) NOT NULL DEFAULT 0;

ALTER TABLE bagile.orders
    ADD COLUMN IF NOT EXISTS net_total NUMERIC(12,2) NOT NULL DEFAULT 0;

ALTER TABLE bagile.orders
    ADD COLUMN IF NOT EXISTS lifecycle_status TEXT NOT NULL DEFAULT 'pending';

ALTER TABLE bagile.orders
    ADD COLUMN IF NOT EXISTS payment_total NUMERIC(12,2) DEFAULT 0;

ALTER TABLE bagile.orders
    ADD COLUMN IF NOT EXISTS currency TEXT NOT NULL DEFAULT 'GBP';

CREATE INDEX IF NOT EXISTS idx_orders_lifecycle 
    ON bagile.orders (lifecycle_status);


-- 4. Patch enrolments
ALTER TABLE bagile.enrolments
    ADD COLUMN IF NOT EXISTS is_cancelled BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE bagile.enrolments
    ADD COLUMN IF NOT EXISTS cancelled_at TIMESTAMP NULL;

CREATE INDEX IF NOT EXISTS idx_enrolments_cancelled 
    ON bagile.enrolments (is_cancelled);


-- 5. raw_payments
CREATE TABLE IF NOT EXISTS bagile.raw_payments (
    id SERIAL PRIMARY KEY,
    order_id BIGINT NOT NULL,
    source TEXT NOT NULL,
    amount NUMERIC(12,2) NOT NULL,
    currency TEXT NOT NULL DEFAULT 'GBP',
    raw_json JSONB NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_raw_payments_order 
    ON bagile.raw_payments (order_id);


-- 6. Ensure correct uniqueness
ALTER TABLE bagile.orders
    ADD CONSTRAINT orders_source_externalid_unique
        UNIQUE (source, external_id);


