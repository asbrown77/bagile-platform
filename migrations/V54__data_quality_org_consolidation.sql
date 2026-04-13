-- V54: Organisation data quality — consolidate duplicates and add missing orgs
-- Sprint 26: Fixes billing_company fragmentation found during analytics audit.
-- Revenue fanout bug (V2.6.4) already fixed in SQL queries; this migration
-- ensures the analytics correctly group all billing_company variants under one name.

-- ============================================================
-- 1. NEW ORGANISATIONS — significant customers with no org entry
-- ============================================================

-- The Phoenix Group (books via Hemsley Fraser, students at reassure.co.uk)
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'The Phoenix Group',
    '{"The Phoenix Group c/o Hemsley Fraser","The Phoenix Group"}',
    NULL,
    'thephoenixgroup.com'
)
ON CONFLICT (name) DO NOTHING;

-- 1inch — crypto/DeFi company, one of the top customers by spend
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    '1inch',
    '{"1inch","1inch Limited","1inch Letyagina","1inch Network"}',
    NULL,
    '1inch.io'
)
ON CONFLICT (name) DO NOTHING;

-- Transport for London — books under two different billing names
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'Transport for London',
    '{"Transport for London","TFL Corporate","TfL","tfl.gov.uk"}',
    NULL,
    'tfl.gov.uk'
)
ON CONFLICT (name) DO NOTHING;

-- Volkswagen Digital Solutions — significant one-off bookings
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'Volkswagen Digital Solutions',
    '{"Volkswagen Digital Solutions"}',
    NULL,
    'vwds.de'
)
ON CONFLICT (name) DO NOTHING;

-- BSW Timber — 1 order, 5 delegates (group booking)
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'BSW Timber',
    '{"BSW Timber Ltd","BSW Timber"}',
    NULL,
    'bswgroup.com'
)
ON CONFLICT (name) DO NOTHING;

-- HEINEKEN (books via Hemsley Fraser)
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'HEINEKEN',
    '{"HEINEKEN c/o Hemsley Fraser","HEINEKEN","Heineken"}',
    NULL,
    'heineken.com'
)
ON CONFLICT (name) DO NOTHING;

-- ============================================================
-- 2. DUPLICATE CONSOLIDATION — same company, different billing names
-- ============================================================

-- Curtis Instruments (two billing spellings: "Curtis Instrument" and "Curtis Instruments Ltd")
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'Curtis Instruments',
    '{"Curtis Instruments Ltd","Curtis Instrument","Curtis Instruments","Curtis"}',
    NULL,
    'curtisinstruments.com'
)
ON CONFLICT (name) DO NOTHING;

-- Frazer-Nash Consultancy (also billed as "Fraser-Nash Consultancy" — one-letter typo)
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'Frazer-Nash Consultancy',
    '{"Frazer-Nash Consultancy Ltd","Frazer-Nash Consultancy","Fraser-Nash Consultancy Ltd","Fraser-Nash Consultancy"}',
    NULL,
    'fnc.co.uk'
)
ON CONFLICT (name) DO NOTHING;

-- European Central Bank (also billed as "ECB")
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'European Central Bank',
    '{"European Central Bank","ECB","ecb.europa.eu"}',
    NULL,
    'ecb.europa.eu'
)
ON CONFLICT (name) DO NOTHING;

-- Instanda (billed as "Instanda" and "Instanda Limited")
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'Instanda',
    '{"Instanda","Instanda Limited"}',
    NULL,
    'instanda.com'
)
ON CONFLICT (name) DO NOTHING;

-- Lloyds Bank GmbH (also billed as "Llodys Bank GmbH" — typo)
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'Lloyds Bank GmbH',
    '{"Lloyds Bank GmbH","Llodys Bank GmbH"}',
    NULL,
    'lloydsbank.de'
)
ON CONFLICT (name) DO NOTHING;

-- Vorwerk International (also billed with "AAA" prefix — old test entry)
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'Vorwerk International',
    '{"Vorwerk International & Co. KmG","AAAVorwerk International & Co. KmG","Vorwerk International"}',
    NULL,
    'vorwerk.com'
)
ON CONFLICT (name) DO NOTHING;

-- Edubroker (two billing name formats for same Polish training broker)
INSERT INTO bagile.organisations (name, aliases, partner_type, primary_domain)
VALUES (
    'Edubroker',
    '{"Edubroker sp. z o.o. 5272572670","EduBroker PL5272572670","Edubroker"}',
    NULL,
    'edubroker.pl'
)
ON CONFLICT (name) DO NOTHING;

-- ============================================================
-- 3. ALIAS UPDATES — add missing billing names to existing orgs
-- ============================================================

-- QA Ltd: add "QA Learning" (same qa.com email domain used)
UPDATE bagile.organisations
SET aliases = array(
    SELECT DISTINCT unnest(aliases || ARRAY['QA Learning','QA LTd'])
)
WHERE name = 'QA Ltd';

-- ============================================================
-- 4. DOMAIN FIX — Phoenix Group was showing bhf.org.uk
--    because a BHF employee appeared on one of their orders.
--    bhf.org.uk is already correctly set on the BHF org (V53).
--    This confirms the Phoenix Group gets its own domain.
-- (Already handled by INSERT above with thephoenixgroup.com)
-- ============================================================
