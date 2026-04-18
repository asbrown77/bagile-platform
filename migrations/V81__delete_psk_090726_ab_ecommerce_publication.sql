-- V81: Remove the ecommerce publication for PSK Jul 9 (planned_course_id=4)
-- The WooCommerce product (id=13009) inherited wrong Zoom meeting ID and trainer
-- selector from a CB template before the trainer-specific override fix.
-- The product is being deleted and re-created via the portal with correct fields.
DELETE FROM bagile.course_publications
WHERE planned_course_id = 4 AND gateway = 'ecommerce';
