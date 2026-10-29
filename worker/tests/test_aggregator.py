from datetime import UTC, date, datetime, timedelta, timezone

import psycopg

from stats_worker.aggregator import aggregate_batch, aggregate_pending


def create_link(conn: psycopg.Connection, code: str) -> int:
    row = conn.execute(
        "INSERT INTO links (code, original_url, created_at) VALUES (%s, %s, now()) RETURNING id",
        (code, f"https://example.com/{code}"),
    ).fetchone()
    conn.commit()
    return row[0]


def add_clicks(conn: psycopg.Connection, link_id: int, *clicked_at: datetime) -> None:
    with conn.cursor() as cur:
        cur.executemany(
            "INSERT INTO click_events (link_id, clicked_at) VALUES (%s, %s)",
            [(link_id, ts) for ts in clicked_at],
        )
    conn.commit()


def daily_stats(conn: psycopg.Connection) -> dict[tuple[int, date], int]:
    rows = conn.execute("SELECT link_id, day, clicks FROM link_daily_stats").fetchall()
    return {(link_id, day): clicks for link_id, day, clicks in rows}


def pending_count(conn: psycopg.Connection) -> int:
    return conn.execute("SELECT count(*) FROM click_events WHERE aggregated_at IS NULL").fetchone()[0]


def test_aggregates_clicks_per_link_and_day(conn):
    a, b = create_link(conn, "aaaaaa"), create_link(conn, "bbbbbb")
    day1 = datetime(2026, 10, 27, 10, tzinfo=UTC)
    day2 = datetime(2026, 10, 28, 10, tzinfo=UTC)
    add_clicks(conn, a, day1, day1, day2)
    add_clicks(conn, b, day2)

    processed = aggregate_pending(conn, batch_size=100)

    assert processed == 4
    assert daily_stats(conn) == {(a, day1.date()): 2, (a, day2.date()): 1, (b, day2.date()): 1}
    assert pending_count(conn) == 0


def test_adds_to_existing_totals_on_later_runs(conn):
    link = create_link(conn, "cccccc")
    clicked = datetime(2026, 10, 28, 9, tzinfo=UTC)
    add_clicks(conn, link, clicked, clicked)
    aggregate_pending(conn, batch_size=100)

    add_clicks(conn, link, clicked)
    aggregate_pending(conn, batch_size=100)

    assert daily_stats(conn) == {(link, clicked.date()): 3}


def test_groups_by_utc_day(conn):
    link = create_link(conn, "dddddd")
    # 23:30 em São Paulo (UTC-3) já é o dia seguinte em UTC.
    late_evening_sp = datetime(2026, 10, 28, 23, 30, tzinfo=timezone(timedelta(hours=-3)))
    add_clicks(conn, link, late_evening_sp)

    aggregate_pending(conn, batch_size=100)

    assert daily_stats(conn) == {(link, date(2026, 10, 29)): 1}


def test_batch_processes_at_most_batch_size_events(conn):
    link = create_link(conn, "eeeeee")
    add_clicks(conn, link, *[datetime(2026, 10, 28, tzinfo=UTC)] * 5)

    assert aggregate_batch(conn, batch_size=2) == 2
    assert pending_count(conn) == 3

    assert aggregate_pending(conn, batch_size=2) == 3
    assert daily_stats(conn) == {(link, date(2026, 10, 28)): 5}


def test_nothing_pending_returns_zero(conn):
    assert aggregate_pending(conn, batch_size=100) == 0
    assert daily_stats(conn) == {}
