import logging

import azure.functions as func

from stats_worker.__main__ import run_once
from stats_worker.config import Settings

app = func.FunctionApp()
log = logging.getLogger("stats_worker")


# No Azure o loop vira um agendamento: o timer trigger chama esta função
# (a cada minuto, ver TIMER_SCHEDULE em infra/main.bicep) e ela processa o que estiver pendente.
@app.timer_trigger(schedule="%TIMER_SCHEDULE%", arg_name="timer", run_on_startup=False)
def aggregate_clicks(timer: func.TimerRequest) -> None:
    processed = run_once(Settings.from_env())
    log.info("Aggregated %s click events", processed)
