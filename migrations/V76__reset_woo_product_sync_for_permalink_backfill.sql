-- Force a full WooCommerce product re-sync on next ETL cycle.
-- Required so that source_product_url (added in V75) gets populated
-- for all existing products, not just ones modified since last incremental sync.
DELETE FROM bagile.sync_metadata
WHERE source = 'woo' AND entity_type = 'products';
