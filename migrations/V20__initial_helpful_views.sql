DROP TABLE IF EXISTS bagile.enrolments CASCADE;

CREATE TABLE bagile.enrolments (
    id BIGSERIAL PRIMARY KEY,
    student_id BIGINT NOT NULL,
    order_id BIGINT NOT NULL,
    course_schedule_id BIGINT NULL,   -- NULL for private or unscheduled
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT enrolments_student_fk FOREIGN KEY (student_id)
        REFERENCES bagile.students (id) ON DELETE CASCADE,
    CONSTRAINT enrolments_order_fk FOREIGN KEY (order_id)
        REFERENCES bagile.orders (id) ON DELETE CASCADE,
    CONSTRAINT enrolments_course_schedule_fk FOREIGN KEY (course_schedule_id)
        REFERENCES bagile.course_schedules (id) ON DELETE SET NULL,
    CONSTRAINT enrolments_unique_student_order_course
        UNIQUE (student_id, order_id, course_schedule_id)
);
