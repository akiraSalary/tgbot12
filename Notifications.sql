
CREATE TABLE IF NOT EXISTS Notifications
(
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL,
    type text NOT NULL,
    text text NOT NULL,
    scheduled_at timestamptz NOT NULL,
    is_notified boolean NOT NULL DEFAULT false,
    notified_at timestamptz NULL
);

CREATE INDEX IF NOT EXISTS ix_notifications_user_id ON notifications (user_id);
CREATE INDEX IF NOT EXISTS ix_notifications_scheduled_at ON notifications (scheduled_at);