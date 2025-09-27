-- Drop all functions/procedures
DO $$
DECLARE r RECORD;
BEGIN
  FOR r IN (
    SELECT routine_name, routine_schema, routine_type
    FROM information_schema.routines
    WHERE specific_schema = 'bagile'
  )
  LOOP
    EXECUTE format('DROP %s IF EXISTS bagile.%I CASCADE',
                   CASE WHEN r.routine_type = 'FUNCTION' THEN 'FUNCTION' ELSE 'PROCEDURE' END,
                   r.routine_name);
  END LOOP;
END $$;

-- Drop all tables
DO $$
DECLARE r RECORD;
BEGIN
  FOR r IN (
    SELECT tablename
    FROM pg_tables
    WHERE schemaname = 'bagile'
  )
  LOOP
    EXECUTE format('DROP TABLE IF EXISTS bagile.%I CASCADE', r.tablename);
  END LOOP;
END $$;

-- Drop schema
DROP SCHEMA IF EXISTS bagile CASCADE;
