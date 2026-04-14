-- V57: course_publications table — tracks gateway publication status
-- Applies to both planned_courses AND course_schedules (existing live courses)
-- Part of Sprint 26: Course Calendar v1

CREATE TABLE bagile.course_publications (
    id                  SERIAL PRIMARY KEY,
    planned_course_id   INT          REFERENCES bagile.planned_courses(id),
    course_schedule_id  BIGINT       REFERENCES bagile.course_schedules(id),
    gateway             VARCHAR(20)  NOT NULL
                        CHECK (gateway IN ('ecommerce', 'scrumorg', 'icagile')),
    published_at        TIMESTAMPTZ,
    external_url        VARCHAR(500),
    woocommerce_product_id INT,                  -- for ecommerce gateway
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    -- Exactly one of planned_course_id or course_schedule_id must be non-null
    CONSTRAINT chk_one_course_ref CHECK (
        (planned_course_id IS NOT NULL AND course_schedule_id IS NULL)
        OR
        (planned_course_id IS NULL AND course_schedule_id IS NOT NULL)
    ),

    -- Prevent duplicate gateway entries per course
    CONSTRAINT uq_planned_course_gateway UNIQUE (planned_course_id, gateway),
    CONSTRAINT uq_schedule_gateway       UNIQUE (course_schedule_id, gateway)
);

CREATE INDEX idx_course_publications_planned  ON bagile.course_publications(planned_course_id) WHERE planned_course_id IS NOT NULL;
CREATE INDEX idx_course_publications_schedule ON bagile.course_publications(course_schedule_id) WHERE course_schedule_id IS NOT NULL;
