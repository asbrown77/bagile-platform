-- ==========================================================
-- Recreate course_definitions, course_pricing, and course_code_aliases
-- Includes seed data and canonical mapping
-- ==========================================================

-- 1️⃣ Drop existing tables if present
DROP TABLE IF EXISTS bagile.course_pricing CASCADE;
DROP TABLE IF EXISTS bagile.course_code_aliases CASCADE;
DROP TABLE IF EXISTS bagile.course_definitions CASCADE;

-- 2️⃣ Recreate course_definitions
CREATE TABLE bagile.course_definitions (
    id SERIAL PRIMARY KEY,
    code TEXT UNIQUE NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    duration_days INT DEFAULT 2,
    active BOOLEAN DEFAULT TRUE
);

-- 3️⃣ Create course_pricing (normalized)
CREATE TABLE bagile.course_pricing (
    id SERIAL PRIMARY KEY,
    course_definition_id INT NOT NULL REFERENCES bagile.course_definitions(id) ON DELETE CASCADE,
    format_type TEXT NOT NULL CHECK (format_type IN ('virtual', 'in_person')),
    market TEXT NOT NULL CHECK (market IN ('primary', 'secondary')),
    price NUMERIC(10,2) NOT NULL,
    currency TEXT DEFAULT 'GBP',
    UNIQUE(course_definition_id, format_type, market)
);

-- 4️⃣ Create course_code_aliases
CREATE TABLE bagile.course_code_aliases (
    id SERIAL PRIMARY KEY,
    alias TEXT UNIQUE NOT NULL,
    canonical_code TEXT NOT NULL REFERENCES bagile.course_definitions(code)
);

CREATE INDEX IF NOT EXISTS idx_course_code_alias_canonical
    ON bagile.course_code_aliases (canonical_code);

-- ==========================================================
-- SEED COURSE DEFINITIONS
-- ==========================================================
INSERT INTO bagile.course_definitions (code, name, description, duration_days, active)
VALUES
('APS',     'Applying Professional Scrum', 'Introduces core Scrum principles and practices for teams and individuals.', 2, TRUE),
('PSM',     'Professional Scrum Master', 'Scrum framework, facilitation, and servant leadership for team effectiveness.', 2, TRUE),
('PSPO',    'Professional Scrum Product Owner', 'Value-driven product ownership and backlog management.', 2, TRUE),
('APS-SD',  'Applying Professional Scrum for Software Development', 'Combines Scrum with XP and technical practices such as TDD and CI/CD.', 3, TRUE),
('PSMA',    'Professional Scrum Master Advanced', 'Advanced Scrum Master practices and servant leadership.', 2, TRUE),
('PAL-E',   'Professional Agile Leadership Essentials', 'Helps leaders support, guide, and coach agile teams effectively.', 2, TRUE),
('PSK',     'Professional Scrum with Kanban', 'Blends Scrum and Kanban to improve flow and predictability.', 2, TRUE),
('PSPOA',   'Professional Scrum Product Owner Advanced', 'Advanced product ownership and stakeholder management.', 2, TRUE),
('PSU',     'Professional Scrum with User Experience', 'Integrates UX and Scrum to deliver user-centered products.', 2, TRUE),
('SPS',     'Scaled Professional Scrum', 'Scaling Scrum with Nexus for multiple teams working on one product.', 2, TRUE),
('PAL-EBM', 'Professional Agile Leadership – Evidence-Based Management', 'Using EBM metrics to lead with data and improve outcomes.', 1, TRUE),
('PSFS',    'Professional Scrum Facilitation Skills', 'Facilitation mindset and skills to improve team collaboration.', 1, TRUE),
('PSPBM',   'Professional Scrum Product Backlog Management Skills', 'Backlog refinement and value prioritization for Product Owners.', 1, TRUE);

-- ==========================================================
-- SEED PRIMARY MARKET PRICING (GBP)
-- ==========================================================
INSERT INTO bagile.course_pricing (course_definition_id, format_type, market, price, currency)
SELECT id, 'virtual', 'primary',
       CASE code
           WHEN 'APS' THEN 900
           WHEN 'PSM' THEN 950
           WHEN 'PSPO' THEN 950
           WHEN 'APS-SD' THEN 1495
           WHEN 'PSMA' THEN 1095
           WHEN 'PAL-E' THEN 995
           WHEN 'PSK' THEN 950
           WHEN 'PSPOA' THEN 1095
           WHEN 'PSU' THEN 1050
           WHEN 'SPS' THEN 1095
           WHEN 'PAL-EBM' THEN 595
           WHEN 'PSFS' THEN 595
           WHEN 'PSPBM' THEN 595
       END AS price,
       'GBP'
FROM bagile.course_definitions;

INSERT INTO bagile.course_pricing (course_definition_id, format_type, market, price, currency)
SELECT id, 'in_person', 'primary',
       CASE code
           WHEN 'APS' THEN 900
           WHEN 'PSM' THEN 950
           WHEN 'PSPO' THEN 950
           WHEN 'APS-SD' THEN 1495
           WHEN 'PSMA' THEN 1095
           WHEN 'PAL-E' THEN 995
           WHEN 'PSK' THEN 950
           WHEN 'PSPOA' THEN 1095
           WHEN 'PSU' THEN 1050
           WHEN 'SPS' THEN 1095
           WHEN 'PAL-EBM' THEN 595
           WHEN 'PSFS' THEN 595
           WHEN 'PSPBM' THEN 595
       END AS price,
       'GBP'
FROM bagile.course_definitions;

-- ==========================================================
-- SEED KNOWN COURSE CODE ALIASES
-- ==========================================================
INSERT INTO bagile.course_code_aliases (alias, canonical_code)
VALUES
    ('PSMII', 'PSMA'),
    ('PSPOII', 'PSPOA')
ON CONFLICT (alias) DO NOTHING;

