import psycopg

# Tudo numa única instrução SQL, dentro de uma transação:
# 1. "batch": pega até N eventos pendentes e já marca como agregados.
#    FOR UPDATE SKIP LOCKED deixa vários workers rodarem juntos sem pegar o mesmo evento.
# 2. "daily": conta os cliques por link e por dia (UTC).
# 3. upsert: soma no total do dia, criando a linha se ainda não existir.
# Se algo falhar, o rollback desfaz as três partes: nenhum clique é perdido nem contado duas vezes.
AGGREGATE_SQL = """
WITH batch AS (
    UPDATE click_events
    SET aggregated_at = now()
    WHERE id IN (
        SELECT id FROM click_events
        WHERE aggregated_at IS NULL
        ORDER BY id
        LIMIT %(batch_size)s
        FOR UPDATE SKIP LOCKED
    )
    RETURNING link_id, clicked_at
),
daily AS (
    SELECT link_id, (clicked_at AT TIME ZONE 'UTC')::date AS day, count(*)::int AS clicks
    FROM batch
    GROUP BY link_id, day
),
upserted AS (
    INSERT INTO link_daily_stats (link_id, day, clicks)
    SELECT link_id, day, clicks FROM daily
    ON CONFLICT (link_id, day) DO UPDATE SET clicks = link_daily_stats.clicks + EXCLUDED.clicks
)
SELECT count(*) FROM batch
"""


def aggregate_batch(conn: psycopg.Connection, batch_size: int) -> int:
    """Aggregates one batch of pending click events and commits. Returns how many were processed."""
    with conn.transaction():
        row = conn.execute(AGGREGATE_SQL, {"batch_size": batch_size}).fetchone()
    return row[0] if row else 0


def aggregate_pending(conn: psycopg.Connection, batch_size: int) -> int:
    """Processes batches until no pending events are left. Returns the total processed."""
    total = 0
    while (processed := aggregate_batch(conn, batch_size)) > 0:
        total += processed
        if processed < batch_size:
            break
    return total
