-- V32: Organisations table with partner tracking

CREATE TABLE IF NOT EXISTS bagile.organisations (
    id BIGSERIAL PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    aliases TEXT[] DEFAULT '{}',           -- fuzzy match aliases (e.g. {"QA Ltd", "QA Limited", "qa.com"})
    partner_type TEXT,                     -- 'ptn' or NULL for non-partners
    ptn_tier TEXT,                         -- 'ptn10', 'ptn20', 'ptn25', 'ptn30', 'ptn33'
    discount_rate NUMERIC(5,2),            -- 10.00, 20.00, 25.00, 30.00, 33.00
    contact_name TEXT,
    contact_email TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_organisations_name ON bagile.organisations (name);

-- Seed known partners from PTN programme
INSERT INTO bagile.organisations (name, aliases, partner_type, ptn_tier, discount_rate, contact_email) VALUES
    ('QA Ltd', '{"QA Ltd","QA Limited","qa.com","QA LTD"}', 'ptn', 'ptn33', 33.00, NULL),
    ('NobleProg', '{"NobleProg","NOBLEPROG (UK) LTD","nobleprog.co.uk","nobleprog.com"}', 'ptn', 'ptn20', 20.00, 'claire.alcock@nobleprog.co.uk'),
    ('LearnQuest', '{"LearnQuest s.r.o.","LearnQuest"}', 'ptn', 'ptn10', 10.00, 'IBMLearning-EMEA@learnquest.com'),
    ('Knowledge Train', '{"Knowledge Train Limited","Knowledge Train"}', 'ptn', 'ptn10', 10.00, 'vladimir@knowledgetrain.co.uk'),
    ('K21', '{"K21","K21.global"}', 'ptn', 'ptn10', 10.00, 'vania.mendes@k21.global'),
    ('Invensis', '{"Invensis Inc","Invensis"}', 'ptn', 'ptn10', 10.00, NULL),
    ('Scopphu', '{"Scopphu"}', 'ptn', 'ptn10', 10.00, NULL),
    ('The Knowledge Academy', '{"theknowledgeacademy.com","The Knowledge Academy"}', 'ptn', 'ptn10', 10.00, NULL),
    ('Scrum Trainer', '{"scrumtrainer.co.uk","Scrum Trainer"}', 'ptn', 'ptn10', 10.00, NULL),
    ('Optimus Learning', '{"Optimus Learning"}', 'ptn', 'ptn15', 15.00, NULL),
    ('Nellcote', '{"nellcote.co.uk","Nellcote"}', 'ptn', 'ptn15', 15.00, NULL),
    ('Indicia Training', '{"indiciatraining.com","Indicia Training"}', 'ptn', 'ptn15', 15.00, NULL),
    ('Elite Training', '{"Elite Training & Consultancy","Elite Training"}', 'ptn', 'ptn20', 20.00, NULL),
    ('Agility Arabia', '{"agilityarabia.com","Agility Arabia"}', 'ptn', 'ptn20', 20.00, NULL),
    ('Calba', '{"Calba"}', 'ptn', 'ptn25', 25.00, NULL),
    ('Optilearn', '{"optilearn.co.uk","Optilearn"}', 'ptn', 'ptn25', 25.00, NULL),
    ('BHF', '{"BHF","BHF.org.uk","British Heart Foundation","bhf.org.uk"}', NULL, NULL, NULL, NULL),
    ('JISC', '{"JISC","Jisc"}', NULL, NULL, NULL, NULL),
    ('BAgile', '{"BAgile Limited","b-agile","Bagile Limited","BAgile Ltd","bagile.co.uk"}', NULL, NULL, NULL, NULL)
ON CONFLICT (name) DO NOTHING;
