from datetime import UTC, datetime

from stats_worker.lambda_handler import handler


def test_handler_aggregates_pending_events(conn, database_url, monkeypatch):
    link_id = conn.execute(
        "INSERT INTO links (code, original_url, created_at) VALUES ('lambda', 'https://example.com', now())"
        " RETURNING id"
    ).fetchone()[0]
    conn.execute(
        "INSERT INTO click_events (link_id, clicked_at) VALUES (%s, %s), (%s, %s)",
        (link_id, datetime(2026, 10, 30, tzinfo=UTC), link_id, datetime(2026, 10, 30, tzinfo=UTC)),
    )
    conn.commit()
    monkeypatch.setenv("DATABASE_URL", database_url)

    assert handler({}, None) == {"processed": 2}
