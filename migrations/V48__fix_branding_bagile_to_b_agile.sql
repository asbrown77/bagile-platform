-- V48: Fix brand name in all pre-course templates
-- V47 ran before the branding audit, so templates in the DB still say 'BAgile'.
-- Flyway won't re-run V47, so we fix with direct UPDATEs here.

UPDATE bagile.pre_course_templates
SET html_body        = REPLACE(html_body,        'BAgile', 'b-agile'),
    subject_template = REPLACE(subject_template, 'BAgile', 'b-agile');
