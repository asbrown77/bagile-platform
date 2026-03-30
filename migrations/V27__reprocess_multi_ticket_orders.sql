-- V27: Re-process recent WooCommerce orders for multi-ticket enrolment fix
-- etl-v1.11.0 creates a student per ticket instead of one per order.
-- Re-processing is idempotent: order upsert uses ON CONFLICT, student
-- upsert deduplicates by email, enrolment upsert by (student, order, course).

UPDATE bagile.raw_orders
SET status = 'pending',
    processed_at = NULL
WHERE source IN ('woo', 'WooCommerce')
  AND status = 'processed'
  AND created_at >= NOW() - INTERVAL '60 days';
