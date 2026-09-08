-- Additive-only: creates a net-new table. No existing table is altered or dropped.
-- Backs the "Post-purchase order status and confirmation" requirement
-- (req_01a07bf5813f744da27e0e86da5e61b8) on change chg_01a07fe56a137406b460744aa1dbf531.
CREATE TABLE IF NOT EXISTS marketplace_orders
(
    id                     uuid PRIMARY KEY,
    status                 text NOT NULL,
    items                  jsonb NOT NULL,
    total_amount           numeric(18, 2) NOT NULL,
    currency               text NOT NULL,
    created_at             timestamptz NOT NULL,
    updated_at             timestamptz NOT NULL,
    zaps_purchase_reference text NULL,
    idempotency_key        text NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_marketplace_orders_idempotency_key
    ON marketplace_orders (idempotency_key);
