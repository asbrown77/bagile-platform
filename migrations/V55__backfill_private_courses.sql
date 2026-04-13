-- V55: Backfill historical private/bespoke course records
-- All historical private courses found in Xero invoices, never previously recorded in the platform.
-- SKU = Xero invoice reference for traceability.
-- ~95 course records across ~35 organisations.

-- ============================================================
-- 1. INSERT NEW ORGANISATIONS (not already in V32/V53/V54)
-- ============================================================

INSERT INTO bagile.organisations (name, aliases, primary_domain) VALUES
    ('ANS Group',                    '{"ANS Group Limited","ANS Group"}',                              'ans.co.uk'),
    ('Age UK',                       '{"Age UK","ageuk.org.uk"}',                                      'ageuk.org.uk'),
    ('Apadmi',                       '{"Apadmi","Apadmi Ltd"}',                                        'apadmi.com'),
    ('Aptitude Software',            '{"Aptitude Software Limited","Aptitude Software"}',               'aptitudesoftware.com'),
    ('Babcock International',        '{"Babcock International","Babcock International Group"}',         'babcockinternational.com'),
    ('BearingPoint',                 '{"BearingPoint","BearingPoint GmbH"}',                            'bearingpoint.com'),
    ('C.D.S. Computer Design Systems', '{"C.D.S. Computer Design Systems Limited","CDS"}',             NULL),
    ('C.T.Co',                       '{"C.T.Co LLC","C.T.Co"}',                                        'ct.co'),
    ('Checkatrade',                  '{"Checkatrade","Checkatrade.com"}',                               'checkatrade.com'),
    ('CME Group',                    '{"CME Group","CME Group Inc"}',                                   'cmegroup.com'),
    ('Concept Systems',              '{"Concept Systems Ltd","Concept Systems"}',                       NULL),
    ('Credera',                      '{"Credera Limited","Credera"}',                                   'credera.com'),
    ('Cross Control AB',             '{"Cross Control AB","CrossControl"}',                             'crosscontrol.com'),
    ('Dept Holding BV',              '{"Dept Holding BV","DEPT"}',                                     'deptagency.com'),
    ('DLA Piper',                    '{"DLA Piper UK LLP","DLA Piper"}',                               'dlapiper.com'),
    ('First Rate Exchange Services', '{"First Rate Exchange Services Ltd","First Rate Exchange Services"}', NULL),
    ('Girlguiding',                  '{"Girlguiding","girlguiding.org.uk"}',                           'girlguiding.org.uk'),
    ('Home Instead',                 '{"Home Instead Limited","Home Instead"}',                         'homeinstead.co.uk'),
    ('Insurwave',                    '{"Insurwave Limited","Insurwave"}',                               'insurwave.com'),
    ('ITIL Works',                   '{"ITIL Works Limited","OneWorks","ITIL Works"}',                  NULL),
    ('MASS',                         '{"MASS","MASS Consultants"}',                                     NULL),
    ('Mobysoft',                     '{"Mobysoft LTD","Mobysoft"}',                                     'mobysoft.com'),
    ('Quanta Training',              '{"Quanta Training Ltd","Quanta Training"}',                       NULL),
    ('SAINT-GOBAIN',                 '{"SAINT-GOBAIN UK & IRELAND","SAINT-GOBAIN","Saint-Gobain"}',     'saint-gobain.com'),
    ('Skiddle',                      '{"Skiddle Ltd","Skiddle"}',                                       'skiddle.com'),
    ('Speech Link Multimedia',       '{"Speech Link Multimedia Ltd","Speech Link Multimedia"}',          NULL),
    ('Starr Underwriting',           '{"Starr Underwriting Agents Limited","Starr Underwriting"}',       NULL),
    ('stocker&friends',              '{"stöcker&friends GmbH","stocker&friends","stöcker&friends"}',     NULL),
    ('Wales NHS',                    '{"Wales NHS UK","NHS Wales","Wales NHS"}',                         'wales.nhs.uk')
ON CONFLICT (name) DO NOTHING;


-- ============================================================
-- 2. BACKFILL PRIVATE COURSE SCHEDULES
-- ============================================================
-- Columns: name, sku, start_date, end_date, is_public, status, capacity,
--          trainer_name, format_type, venue_address, course_type,
--          client_organisation_id, source_system, notes, created_by, last_synced
--
-- Trainer names: 'Alex Brown' (AB suffix) or 'Chris Bexon' (CB suffix)
-- BA suffix = both trainers; defaulting to 'Alex Brown' unless context says otherwise.
--
-- End date rules:
--   PSM, PSPO, PAL-E, PSPO-A, PSK, PSM-II = start + 1 (2-day)
--   APS, APS-SD, PSM+PSPO combined = start + 2 (3-day)
--   ICP, EBM, PAL-EBM, SPS, PSU, Custom 1-day = same day
--   ICP-ATF + ICP-ACC combined = start + 1 (2 days)

INSERT INTO bagile.course_schedules
    (name, sku, start_date, end_date, is_public, status, capacity,
     trainer_name, format_type, venue_address, course_type,
     client_organisation_id, source_system, notes, created_by, last_synced)
VALUES

-- ── ANS Group ──────────────────────────────────────────────
('PSPO Private — ANS Group (Virtual, Jun 2022)',
 'ANS-PSPO-220622-AB', '2022-06-22', '2022-06-23', false, 'completed', 6,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'ANS Group'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSPO Private — ANS Group (Manchester, Oct 2022)',
 'ANS-PSPO-101022-BA', '2022-10-10', '2022-10-11', false, 'completed', 9,
 'Alex Brown', 'in_person', 'Manchester', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'ANS Group'),
 'xero', 'Backfilled from Xero invoice. 7+2 on split invoices, same course.', 'V55_migration', NOW()),

-- ── Age UK ─────────────────────────────────────────────────
('PSPO Private — Age UK (Nov 2023)',
 'AGE-PSPO-161123-BA', '2023-11-16', '2023-11-17', false, 'completed', 5,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Age UK'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Apadmi ─────────────────────────────────────────────────
('PSM Private — Apadmi (Jul 2022)',
 'APA-PSM-260722-BA', '2022-07-26', '2022-07-27', false, 'completed', 6,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Apadmi'),
 'xero', 'Backfilled from Xero invoice. Invoiced Sep but course was Jul.', 'V55_migration', NOW()),

-- ── Aptitude Software ──────────────────────────────────────
('PSPO Private — Aptitude Software (Apr 2023)',
 'AS-PSPO-250423-BA', '2023-04-25', '2023-04-26', false, 'completed', 6,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Aptitude Software'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Babcock International (via QA) ─────────────────────────
('APS Private — Babcock International (Virtual, Jun 2023)',
 'BAB-APS-210623-BA', '2023-06-21', '2023-06-23', false, 'completed', 6,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Babcock International'),
 'xero', 'Backfilled from Xero invoice. Booked via QA.', 'V55_migration', NOW()),

-- ── BearingPoint ───────────────────────────────────────────
('PSPO Private — BearingPoint (Nov 2023)',
 'BEAR-PSPO-281123-BA', '2023-11-28', '2023-11-29', false, 'completed', 7,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'BearingPoint'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSPO Private — BearingPoint (Feb 2025)',
 'PSPO-190225-CB', '2025-02-19', '2025-02-20', false, 'completed', 4,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'BearingPoint'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── BHF ────────────────────────────────────────────────────
('PSPO Private — BHF (Aug 2023)',
 'BHF-0000195566-CB', '2023-08-10', '2023-08-11', false, 'completed', 6,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'BHF'),
 'xero', 'Backfilled from Xero invoice. Private bulk booking.', 'V55_migration', NOW()),

('PAL-E Private — BHF (Oct 2023)',
 'PO-PALE-231023-CB', '2023-10-23', '2023-10-24', false, 'completed', 2,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'BHF'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('APS Private — BHF (Dec 2024)',
 'BHF-APS-161224-CB', '2024-12-16', '2024-12-18', false, 'completed', 2,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'BHF'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('APS Private — BHF (Nov 2025)',
 'APS-101125-AB', '2025-11-10', '2025-11-12', false, 'completed', 3,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'BHF'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── C.D.S. Computer Design Systems ────────────────────────
('PSM Private — CDS (Manchester, Jan 2025)',
 'CDS-PSM-130125-BA', '2025-01-13', '2025-01-14', false, 'completed', 6,
 'Alex Brown', 'in_person', 'Manchester', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'C.D.S. Computer Design Systems'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── C.T.Co ─────────────────────────────────────────────────
('PSPO Private — C.T.Co (Oct 2025)',
 'CTCO-PSPO-301025-BA', '2025-10-30', '2025-10-31', false, 'completed', 14,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'C.T.Co'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP-APM Private — C.T.Co (Dec 2025)',
 'CTCO-APM-121225', '2025-12-12', '2025-12-12', false, 'completed', 15,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'C.T.Co'),
 'xero', 'Backfilled from Xero invoice. Xero invoice is DRAFT but course date was Dec 2025.', 'V55_migration', NOW()),

-- ── Checkatrade ────────────────────────────────────────────
('PSPO Private — Checkatrade (Jul 2021)',
 'PT-PSPO-001-LINE1', '2021-07-29', '2021-07-30', false, 'completed', 8,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Checkatrade'),
 'xero', 'Backfilled from Xero invoice. Line 1 of PT-PSPO-001.', 'V55_migration', NOW()),

('PSPO-A Private — Checkatrade (Nov 2021)',
 'PT-PSPO-001-LINE2', '2021-11-01', '2021-11-02', false, 'completed', 8,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Checkatrade'),
 'xero', 'Backfilled from Xero invoice. Line 2 of PT-PSPO-001. Approximate date (TBC on invoice).', 'V55_migration', NOW()),

('PSPO Private — Checkatrade (Apr 2022)',
 'PSPO-28042020-AB', '2022-04-28', '2022-04-29', false, 'completed', 3,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Checkatrade'),
 'xero', 'Backfilled from Xero invoice. Ref says 2020 but invoice is Apr 2022.', 'V55_migration', NOW()),

-- ── CME Group ──────────────────────────────────────────────
('PSM Private — CME Group (London, Jan 2023)',
 'CME-PSMPO-160123-BA-PSM', '2023-01-16', '2023-01-17', false, 'completed', 15,
 'Alex Brown', 'in_person', 'London', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'CME Group'),
 'xero', 'Backfilled from Xero invoice. PSM component of combined 3-day PSM+PSPO.', 'V55_migration', NOW()),

('PSPO Private — CME Group (London, Jan 2023)',
 'CME-PSMPO-160123-BA-PSPO', '2023-01-18', '2023-01-18', false, 'completed', 15,
 'Alex Brown', 'in_person', 'London', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'CME Group'),
 'xero', 'Backfilled from Xero invoice. PSPO component of combined 3-day PSM+PSPO (day 3).', 'V55_migration', NOW()),

-- ── Concept Systems ────────────────────────────────────────
('PSPO-A Private — Concept Systems (Nov 2024)',
 'CLS-PSPOA-121124-CB', '2024-11-12', '2024-11-13', false, 'completed', 3,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Concept Systems'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Credera ────────────────────────────────────────────────
('PSM Private — Credera (London, Sep 2022)',
 'CRE-PSM-290922-AB', '2022-09-29', '2022-09-30', false, 'completed', 16,
 'Alex Brown', 'in_person', 'London', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Credera'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSM Private — Credera (London, Oct 2022)',
 'CRE-PSM-211022-BA', '2022-10-21', '2022-10-22', false, 'completed', 10,
 'Alex Brown', 'in_person', 'London', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Credera'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSM Private — Credera (Newcastle, May 2024)',
 'CRE-PSM-150524-BA', '2024-05-15', '2024-05-16', false, 'completed', 8,
 'Alex Brown', 'in_person', 'Newcastle', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Credera'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Cross Control AB ───────────────────────────────────────
('PSPO Private — Cross Control AB (Virtual, Jun 2022)',
 'CCL-PSPO-070622', '2022-06-07', '2022-06-08', false, 'completed', 8,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Cross Control AB'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Dept Holding BV ────────────────────────────────────────
('PSPO Private — Dept Holding BV (London, Feb 2025)',
 'DEPT-PSPO-170225-BA', '2025-02-17', '2025-02-18', false, 'completed', 7,
 'Alex Brown', 'in_person', 'London', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Dept Holding BV'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── DLA Piper ──────────────────────────────────────────────
('PSM Private — DLA Piper (Leeds, Jan 2025)',
 'DLA-PSM-150125-BA', '2025-01-15', '2025-01-16', false, 'completed', 9,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (Leeds, Feb 2025)',
 'DLA-PALE-060225-BA', '2025-02-06', '2025-02-07', false, 'completed', 12,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Leeds, Feb 2025)',
 'DLA-ICP-100225-BA', '2025-02-10', '2025-02-10', false, 'completed', 7,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-EBM Private — DLA Piper (Leeds, Feb 2025)',
 'DLA-EBM-250225-BA', '2025-02-25', '2025-02-25', false, 'completed', 11,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (Leeds, Feb 2025)',
 'DLA-PALE-260225-BA', '2025-02-26', '2025-02-27', false, 'completed', 14,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Leeds, Mar 2025)',
 'DLA-ICP-060325-BA', '2025-03-06', '2025-03-06', false, 'completed', 9,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Leeds, Mar 2025)',
 'DLA-ICP-120325-BA', '2025-03-12', '2025-03-12', false, 'completed', 9,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (Leeds, Mar 2025)',
 'DLA-PALE-170325-BA', '2025-03-17', '2025-03-18', false, 'completed', 16,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Leeds, Mar 2025)',
 'DLA-ICP-190325-BA', '2025-03-19', '2025-03-19', false, 'completed', 15,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Leeds, Mar 2025)',
 'DLA-ICP-240325-BA', '2025-03-24', '2025-03-24', false, 'completed', 14,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Virtual, Mar 2025)',
 'DLA-ICP-310325-BA', '2025-03-31', '2025-03-31', false, 'completed', 14,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Leeds, Apr 2025)',
 'DLA-ICP-020425-BA', '2025-04-02', '2025-04-02', false, 'completed', 15,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Leeds, Apr 2025)',
 'DLA-ICP-100425-BA', '2025-04-10', '2025-04-10', false, 'completed', 11,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Virtual, Apr 2025)',
 'DLA-ICP-280425-BA', '2025-04-28', '2025-04-28', false, 'completed', 15,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (Virtual, Apr 2025)',
 'DLA-PALE-300425-BA', '2025-04-30', '2025-05-01', false, 'completed', 10,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (Melbourne, May 2025)',
 'DLA-PALE-050525-BA', '2025-05-05', '2025-05-06', false, 'completed', 6,
 'Alex Brown', 'in_person', 'Melbourne', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Virtual, May 2025)',
 'DLA-ICP-060525-BA', '2025-05-06', '2025-05-06', false, 'completed', 13,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Perth, May 2025)',
 'DLA-ICP-070525-BA', '2025-05-07', '2025-05-07', false, 'completed', 14,
 'Alex Brown', 'in_person', 'Perth', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Perth, May 2025)',
 'DLA-ICP-120525-BA', '2025-05-12', '2025-05-12', false, 'completed', 11,
 'Alex Brown', 'in_person', 'Perth', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Virtual, May 2025)',
 'DLA-ICP-210525-BA', '2025-05-21', '2025-05-21', false, 'completed', 16,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Virtual, May 2025)',
 'DLA-ICP-280525-BA', '2025-05-28', '2025-05-28', false, 'completed', 14,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Warsaw, Jun 2025)',
 'DLA-ICP-020625-BA', '2025-06-02', '2025-06-02', false, 'completed', 11,
 'Alex Brown', 'in_person', 'Warsaw', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Warsaw, Jun 2025)',
 'DLA-ICP-040625-BA', '2025-06-04', '2025-06-04', false, 'completed', 13,
 'Alex Brown', 'in_person', 'Warsaw', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Warsaw, Jun 2025)',
 'DLA-ICP-060625-BA', '2025-06-06', '2025-06-06', false, 'completed', 8,
 'Alex Brown', 'in_person', 'Warsaw', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Warsaw, Jun 2025)',
 'DLA-ICP-100625-BA', '2025-06-10', '2025-06-10', false, 'completed', 9,
 'Alex Brown', 'in_person', 'Warsaw', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Warsaw, Jun 2025)',
 'DLA-ICP-120625-BA', '2025-06-12', '2025-06-12', false, 'completed', 7,
 'Alex Brown', 'in_person', 'Warsaw', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Virtual, Jun 2025)',
 'DLA-ICP-160625-BA', '2025-06-16', '2025-06-16', false, 'completed', 10,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (London, Jun 2025)',
 'DLA-PALE-230625-BA', '2025-06-23', '2025-06-24', false, 'completed', 7,
 'Alex Brown', 'in_person', 'London', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (London, Jul 2025)',
 'DLA-PALE-070725-BA', '2025-07-07', '2025-07-08', false, 'completed', 13,
 'Alex Brown', 'in_person', 'London', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP-ATF + ICP-ACC Private — DLA Piper (Leeds, Jul 2025)',
 'DLA-ICAgile-280725-BA', '2025-07-28', '2025-07-29', false, 'completed', 18,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice. ICP-ATF + ICP-ACC combined 2-day.', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Virtual, Jul 2025)',
 'DLA-ICP-290725-BA', '2025-07-29', '2025-07-29', false, 'completed', 6,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Virtual, Aug 2025)',
 'DLA-ICP-050825-BA', '2025-08-05', '2025-08-05', false, 'completed', 10,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (Leeds, Aug 2025)',
 'DLA-PALE-140825-BA', '2025-08-14', '2025-08-15', false, 'completed', 6,
 'Alex Brown', 'in_person', 'Leeds', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('ICP Private — DLA Piper (Virtual, Aug 2025)',
 'DLA-ICP-190825-BA', '2025-08-19', '2025-08-19', false, 'completed', 15,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (Virtual, Oct 2025)',
 'DLA-PALE-101025-CB', '2025-10-10', '2025-10-11', false, 'completed', 6,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PAL-E Private — DLA Piper (Virtual, Oct 2025)',
 'DLA-PALE-291025-CB', '2025-10-29', '2025-10-30', false, 'completed', 6,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'DLA Piper'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── First Rate Exchange Services ───────────────────────────
('PSPO Private — First Rate Exchange Services (Mar 2022)',
 'FirstRate-PSPO-March-AB', '2022-03-01', '2022-03-02', false, 'completed', 11,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'First Rate Exchange Services'),
 'xero', 'Backfilled from Xero invoice. Approximate date (invoice says March).', 'V55_migration', NOW()),

-- ── Girlguiding ────────────────────────────────────────────
('PSPO Private — Girlguiding (Virtual, Jul 2022)',
 'GG-PSPO-280722-BA', '2022-07-28', '2022-07-29', false, 'completed', 5,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Girlguiding'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Home Instead ───────────────────────────────────────────
('PSM Private — Home Instead (Nov 2024)',
 'HIL-PSMPO-13112024-BA', '2024-11-13', '2024-11-14', false, 'completed', 3,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Home Instead'),
 'xero', 'Backfilled from Xero invoice. PSM component of PSM+PSPO engagement.', 'V55_migration', NOW()),

('PSPO Private — Home Instead (Nov 2024)',
 'HIL-PSMPO-13112024-BA-PSPO', '2024-11-15', '2024-11-16', false, 'completed', 3,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Home Instead'),
 'xero', 'Backfilled from Xero invoice. PSPO component of PSM+PSPO engagement.', 'V55_migration', NOW()),

-- ── Insurwave ──────────────────────────────────────────────
('PSK Private — Insurwave (London, Nov 2024)',
 'INL-PSPBM-271124-BA', '2024-11-27', '2024-11-27', false, 'completed', 4,
 'Alex Brown', 'in_person', 'London', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Insurwave'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── ITIL Works / OneWorks ──────────────────────────────────
('PSM Private — OneWorks (Mar 2026)',
 'ONEWORKS-PSM-250326-CB', '2026-03-25', '2026-03-26', false, 'completed', 4,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'ITIL Works'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Jisc / JISC ────────────────────────────────────────────
('PSPO Private — Jisc (Jun 2022)',
 'JISC-PSPO-220622-CB', '2022-06-22', '2022-06-23', false, 'completed', 14,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'JISC'),
 'xero', 'Backfilled from Xero invoice. Ref says PSM but was PSPO per invoice description.', 'V55_migration', NOW()),

('APS Private — Jisc (Bristol, Feb 2023)',
 'JISC-APS-210223-BA', '2023-02-21', '2023-02-23', false, 'completed', 25,
 'Alex Brown', 'in_person', 'Bristol', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'JISC'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('APS Private — Jisc (Nov 2023)',
 'JISC-APS-071123-BA', '2023-11-07', '2023-11-09', false, 'completed', 29,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'JISC'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('APS Private — Jisc (Jul 2024)',
 'JISC-APS-160724-BA', '2024-07-16', '2024-07-18', false, 'completed', 19,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'JISC'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('APS Private — Jisc (Nov 2024)',
 'JISC-APS-051124-BA', '2024-11-05', '2024-11-07', false, 'completed', 30,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'JISC'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('APS Private — Jisc (Apr 2025)',
 'JISC-APS-160425-BA', '2025-04-16', '2025-04-18', false, 'completed', 21,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'JISC'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── MASS ───────────────────────────────────────────────────
('PSM Private — MASS (Jan 2023)',
 'MASS-PSM-090123-BA', '2023-01-09', '2023-01-10', false, 'completed', 5,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'MASS'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Mobysoft ───────────────────────────────────────────────
('PAL-E Private — Mobysoft (Virtual, Jan 2025)',
 'MBY-PAL-070125-BA', '2025-01-07', '2025-01-08', false, 'completed', 7,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Mobysoft'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Nellcote / Quanta Digital ──────────────────────────────
('Custom Agile Private — Nellcote (Nov 2023)',
 'NELLCOAT-QUANTA-131123', '2023-11-13', '2023-11-13', false, 'completed', 5,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Nellcote'),
 'xero', 'Backfilled from Xero invoice. Custom 1-day agile session. Capacity estimated.', 'V55_migration', NOW()),

('PSPO Private — Nellcote (Apr 2024)',
 'NELLCOTE-PSPO-240424-AB', '2024-04-24', '2024-04-25', false, 'completed', 5,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Nellcote'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── QA Ltd (private PSM-II cohorts) ────────────────────────
('PSM-II Private — QA Ltd (Oct 2022)',
 'QA-PSMII-101022-BA', '2022-10-10', '2022-10-11', false, 'completed', 10,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'QA Ltd'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSM-II Private — QA Ltd (Nov 2022)',
 'QA-PSMII-101122-BA', '2022-11-10', '2022-11-11', false, 'completed', 10,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'QA Ltd'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSM-II Private — QA Ltd (Windsor, Apr 2023)',
 'QA-PSMII-240423-BA', '2023-04-24', '2023-04-25', false, 'completed', 10,
 'Alex Brown', 'in_person', 'Windsor', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'QA Ltd'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Quanta Training ────────────────────────────────────────
('PSPO-A Private — Quanta Training (Feb 2022)',
 'QT-PSPOA-160222-BA', '2022-02-16', '2022-02-17', false, 'completed', 12,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Quanta Training'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSPO-A Private — Quanta Training (Mar 2022)',
 'QT-PSPOA-160322-BA', '2022-03-16', '2022-03-17', false, 'completed', 12,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Quanta Training'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('SPS Private — Quanta Training (Edinburgh, Apr 2024)',
 'QT-SPS-230424-CB', '2024-04-23', '2024-04-23', false, 'completed', 10,
 'Chris Bexon', 'in_person', 'Edinburgh', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Quanta Training'),
 'xero', 'Backfilled from Xero invoice. SPS/Nexus course.', 'V55_migration', NOW()),

('SPS Private — Quanta Training (Virtual, Nov 2024)',
 'QT-SPS-041124-CB', '2024-11-04', '2024-11-04', false, 'completed', 12,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Quanta Training'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSU Private — Quanta Training (London, Oct 2024)',
 'QT-PSU-101024-BA', '2024-10-10', '2024-10-11', false, 'completed', 14,
 'Alex Brown', 'in_person', 'London', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Quanta Training'),
 'xero', 'Backfilled from Xero invoice. Professional Scrum with UX.', 'V55_migration', NOW()),

-- ── SAINT-GOBAIN ───────────────────────────────────────────
('PSM Private — Saint-Gobain (Jul 2022)',
 'SAINT-PSM-170722-CB', '2022-07-17', '2022-07-18', false, 'completed', 3,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'SAINT-GOBAIN'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSM Private — Saint-Gobain (Jun 2024)',
 'SAINT-PSM-260624-CB', '2024-06-26', '2024-06-27', false, 'completed', 3,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'SAINT-GOBAIN'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Skiddle ────────────────────────────────────────────────
('APS Private — Skiddle (Manchester, Jan 2024)',
 'SKI-APS-220124-BA', '2024-01-22', '2024-01-24', false, 'completed', 17,
 'Alex Brown', 'in_person', 'Manchester', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Skiddle'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Speech Link Multimedia ─────────────────────────────────
('PSPO Private — Speech Link Multimedia (Feb 2022)',
 'SpeechLink-PSPO-001', '2022-02-15', '2022-02-16', false, 'completed', 1,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Speech Link Multimedia'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('PSM Private — Speech Link Multimedia (Feb 2022)',
 'SpeechLink-PSM-001', '2022-02-17', '2022-02-18', false, 'completed', 1,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Speech Link Multimedia'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

('APS-SD Private — Speech Link Multimedia (Feb 2022)',
 'SpeechLink-APSSD-001', '2022-02-14', '2022-02-16', false, 'completed', 1,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Speech Link Multimedia'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Starr Underwriting ─────────────────────────────────────
('PSM Private — Starr Underwriting (Jul 2025)',
 'STAR-010725-PSM-BA', '2025-07-01', '2025-07-02', false, 'completed', 6,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Starr Underwriting'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── stocker&friends (for Philip Morris) ────────────────────
('PSM Private — stocker&friends / Philip Morris (Dec 2022)',
 'SF-PSM-131222-CB', '2022-12-13', '2022-12-14', false, 'completed', 7,
 'Chris Bexon', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'stocker&friends'),
 'xero', 'Backfilled from Xero invoice. Delivered to Philip Morris GmbH.', 'V55_migration', NOW()),

-- ── Volkswagen Digital Solutions ───────────────────────────
('PSPO-A Private — Volkswagen Digital Solutions (Portugal, Oct 2024)',
 'VWDS-PSPOA-021024-CB', '2024-10-02', '2024-10-03', false, 'completed', 2,
 'Chris Bexon', 'in_person', 'Portugal', 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Volkswagen Digital Solutions'),
 'xero', 'Backfilled from Xero invoice', 'V55_migration', NOW()),

-- ── Wales NHS ──────────────────────────────────────────────
('PSM Private — Wales NHS (Nov 2025)',
 'WalesNHSUK-PSM-071125', '2025-11-07', '2025-11-08', false, 'completed', 13,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Wales NHS'),
 'xero', 'Backfilled from Xero invoice. Split from WalesNHSUK-0126 covering PSM+PSPO.', 'V55_migration', NOW()),

('PSPO Private — Wales NHS (Nov 2025)',
 'WalesNHSUK-PSPO-071125', '2025-11-07', '2025-11-08', false, 'completed', 5,
 'Alex Brown', 'virtual', NULL, 'private',
 (SELECT id FROM bagile.organisations WHERE name = 'Wales NHS'),
 'xero', 'Backfilled from Xero invoice. Split from WalesNHSUK-0126 covering PSM+PSPO.', 'V55_migration', NOW())
;
