-- V65: Replace assessment badge URLs with official scrum.org course logos.
-- Old files (PSM-I.png etc.) were certification/assessment badges, not course logos.
-- New files (*-course.png) are the correct course logos from the scrum.org brand pack.

UPDATE bagile.course_definitions SET badge_url = '/badges/PSM-course.png'    WHERE code = 'PSM';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSMA-course.png'   WHERE code = 'PSMA';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSMAI-course.png'  WHERE code = 'PSMAI';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSPO-course.png'   WHERE code = 'PSPO';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSPOA-course.png'  WHERE code IN ('PSPOA', 'PSPO-A');
UPDATE bagile.course_definitions SET badge_url = '/badges/PSPOAI-course.png' WHERE code = 'PSPOAI';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSK-course.png'    WHERE code = 'PSK';
UPDATE bagile.course_definitions SET badge_url = '/badges/PALE-course.png'   WHERE code IN ('PALE', 'PAL-E');
UPDATE bagile.course_definitions SET badge_url = '/badges/PALEBM-course.png' WHERE code IN ('PAL-EBM', 'PALEBM');
UPDATE bagile.course_definitions SET badge_url = '/badges/APS-course.png'    WHERE code = 'APS';
UPDATE bagile.course_definitions SET badge_url = '/badges/APSSD-course.png'  WHERE code IN ('APS-SD', 'APSSD');
UPDATE bagile.course_definitions SET badge_url = '/badges/PSU-course.png'    WHERE code = 'PSU';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSFS-course.png'   WHERE code = 'PSFS';
UPDATE bagile.course_definitions SET badge_url = '/badges/SPS-course.png'    WHERE code = 'SPS';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSPBM-course.png'  WHERE code = 'PSPBM';
