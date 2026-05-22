CREATE TABLE IF NOT EXISTS public.notifications
(
  id uuid PRIMARY KEY,
  user_id uuid NOT NULL,
  type text NOT NULL,
  text text NOT NULL,
  scheduled_at timestamptz NOT NULL,
  is_notified boolean NOT NULL DEFAULT false,
  notified_at timestamptz NULL
);