CREATE TABLE bagile.course_schedules (
                                  id BIGSERIAL PRIMARY KEY,             -- internal system ID
                                  name TEXT NOT NULL,
                                  status TEXT,                          -- published / draft / sold_out / cancelled
                                  start_date DATE,
                                  end_date DATE,
                                  capacity INT,
                                  price NUMERIC(10,2),
                                  sku TEXT,
                                  trainer_name TEXT,                    -- e.g. 'Alex Brown'
                                  format_type TEXT,                     -- 'virtual' / 'in_person'
                                  is_public BOOLEAN DEFAULT TRUE,
                                  source_system TEXT,                   -- e.g. 'WooCommerce'
                                  source_product_id BIGINT,             -- store's product ID if applicable
                                  last_synced TIMESTAMP DEFAULT now()
);

CREATE UNIQUE INDEX idx_course_schedules_source
    ON bagile.course_schedules(source_system, source_product_id);

CREATE INDEX idx_course_schedules_status
    ON bagile.course_schedules(status);

CREATE INDEX idx_course_schedules_start_date
    ON bagile.course_schedules(start_date);

CREATE INDEX idx_course_schedules_format_type
    ON bagile.course_schedules(format_type);
