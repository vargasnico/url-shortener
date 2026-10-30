import logging

from .__main__ import run_once
from .config import Settings

logging.getLogger().setLevel(logging.INFO)
log = logging.getLogger("stats_worker")


# Na AWS o loop vira um agendamento: o EventBridge Scheduler chama esta função a cada minuto
# e ela processa o que estiver pendente, como o `stats-worker --once`.
def handler(event: dict, context: object) -> dict:
    processed = run_once(Settings.from_env())
    log.info("Aggregated %s click events", processed)
    return {"processed": processed}
