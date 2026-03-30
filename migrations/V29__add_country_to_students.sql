-- V29: Add country code to students table
-- Defaults to billing country from WooCommerce, overridable per student.

ALTER TABLE bagile.students ADD COLUMN IF NOT EXISTS country TEXT;
