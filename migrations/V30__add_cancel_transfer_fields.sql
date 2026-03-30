-- V30: Support cancel + transfer workflow
-- Track cancellation reason and refund/transfer status per enrolment.

ALTER TABLE bagile.enrolments ADD COLUMN IF NOT EXISTS cancellation_reason TEXT;
-- Values: 'provider_cancelled', 'attendee_requested'

-- Update status to support new states
-- Existing: 'active', 'cancelled', 'transferred'
-- New: 'pending_transfer', 'refunded'
-- No schema change needed — status is TEXT, just use new values.

-- Index for finding pending transfers quickly
CREATE INDEX IF NOT EXISTS idx_enrolments_pending_transfer
    ON bagile.enrolments (status) WHERE status = 'pending_transfer';
