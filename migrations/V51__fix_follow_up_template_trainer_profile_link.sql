-- V51: Replace hardcoded Chris Bexon profile link in follow-up templates
-- with {{trainer_profile_url}} variable, populated dynamically by the handler.

UPDATE bagile.post_course_templates
SET html_body = REPLACE(
    REPLACE(html_body,
        'https://www.scrum.org/find-trainers/chris-bexon">Chris''s scrum.org profile',
        '{{trainer_profile_url}}">{{trainer_name}}''s scrum.org profile'
    ),
    'https://www.scrum.org/find-trainers/chris-bexon">scrum.org',
    '{{trainer_profile_url}}">scrum.org'
);
