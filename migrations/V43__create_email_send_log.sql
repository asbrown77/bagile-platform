-- V43: Email send audit log
-- Sprint 21: track who sent what to whom and when.
-- Covers both pre-course and post-course sends (and test sends).

CREATE TABLE IF NOT EXISTS bagile.email_send_log (
    id                  SERIAL PRIMARY KEY,
    course_schedule_id  INTEGER REFERENCES bagile.course_schedules(id),
    template_type       VARCHAR(20)  NOT NULL,   -- 'pre_course' or 'post_course'
    sent_by             VARCHAR(200),            -- email/name of initiating user (from API key or request)
    recipient_count     INTEGER      NOT NULL,
    recipients          TEXT,                    -- comma-separated email addresses
    subject             VARCHAR(500),
    is_test             BOOLEAN      NOT NULL DEFAULT FALSE,
    sent_at             TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE bagile.email_send_log IS
    'Audit log for all pre- and post-course email sends. '
    'Records who triggered each send, the recipients, and whether it was a test send.';

CREATE INDEX IF NOT EXISTS idx_email_send_log_course_schedule_id
    ON bagile.email_send_log (course_schedule_id);

CREATE INDEX IF NOT EXISTS idx_email_send_log_sent_at
    ON bagile.email_send_log (sent_at DESC);
