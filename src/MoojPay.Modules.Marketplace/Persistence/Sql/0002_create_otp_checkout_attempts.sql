-- Additive-only: creates a net-new table. No existing table is altered or dropped.
-- Backs the Identity module's OTP checkout retry-escalation tracking
-- (acr_01a07bf5813f7c73a4edb49f5a339139) on change chg_01a07fe56a137406b460744aa1dbf531.
CREATE TABLE IF NOT EXISTS otp_checkout_attempts
(
    checkout_session_id text PRIMARY KEY,
    failure_count        integer NOT NULL DEFAULT 0,
    escalated_to_support boolean NOT NULL DEFAULT false,
    updated_at            timestamptz NOT NULL
);
