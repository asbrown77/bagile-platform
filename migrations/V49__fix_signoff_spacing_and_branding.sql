-- V49: Fix sign-off spacing and ensure b-agile branding in sign-off text
-- Adds margin-top to the "See you soon" paragraph for visual breathing room
-- after the agenda table. Also re-applies the BAgile→b-agile fix in case V48
-- did not run cleanly (e.g. if the sign-off text still reads "BAgile").

UPDATE bagile.pre_course_templates
SET html_body = REPLACE(
    REPLACE(html_body,
        '<p>See you soon,',
        '<p style="margin-top:24px;">See you soon,'
    ),
    'BAgile', 'b-agile'
)
WHERE html_body LIKE '%See you soon%';

UPDATE bagile.pre_course_templates
SET subject_template = REPLACE(subject_template, 'BAgile', 'b-agile');
