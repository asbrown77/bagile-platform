REATE TABLE IF NOT EXISTS course_code_aliases (
    id SERIAL PRIMARY KEY,
    alias TEXT UNIQUE NOT NULL,
    canonical_code TEXT NOT NULL REFERENCES course_definitions(code)
);

-- Seed only current known aliases
INSERT INTO course_code_aliases (alias, canonical_code)
VALUES
    ('PSMII', 'PSMA'),
    ('PSPOII', 'PSPOA')
ON CONFLICT (alias) DO NOTHING;

CREATE INDEX IF NOT EXISTS idx_course_code_alias_canonical
    ON course_code_aliases (canonical_code);
