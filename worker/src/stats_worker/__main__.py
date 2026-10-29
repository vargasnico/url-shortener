import argparse
import logging
import signal
import threading

import psycopg

from .aggregator import aggregate_pending
from .config import Settings

log = logging.getLogger("stats_worker")


def run_once(settings: Settings) -> int:
    with psycopg.connect(settings.database_url) as conn:
        return aggregate_pending(conn, settings.batch_size)


def run_forever(settings: Settings, stop: threading.Event) -> None:
    log.info("Starting: interval=%ss batch_size=%s", settings.interval_seconds, settings.batch_size)
    while not stop.is_set():
        try:
            processed = run_once(settings)
            if processed:
                log.info("Aggregated %s click events", processed)
        except psycopg.Error as error:
            # Banco fora do ar ou tabelas ainda não criadas (a API aplica as migrations):
            # registra e tenta de novo no próximo ciclo, em vez de derrubar o processo.
            log.warning("Aggregation failed, retrying in %ss: %s", settings.interval_seconds, error)
        # wait() em vez de sleep(): acorda na hora se chegar um sinal de parada.
        stop.wait(settings.interval_seconds)
    log.info("Stopped")


def main() -> None:
    parser = argparse.ArgumentParser(description="Aggregate link click events into daily statistics.")
    parser.add_argument("--once", action="store_true", help="process pending events once and exit")
    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
    settings = Settings.from_env()

    if args.once:
        log.info("Aggregated %s click events", run_once(settings))
        return

    # SIGTERM é o que o Docker/ECS manda ao parar o container: termina o ciclo atual e sai.
    stop = threading.Event()
    signal.signal(signal.SIGTERM, lambda *_: stop.set())
    signal.signal(signal.SIGINT, lambda *_: stop.set())
    run_forever(settings, stop)


if __name__ == "__main__":
    main()
