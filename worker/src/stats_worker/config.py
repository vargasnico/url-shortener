import os
from dataclasses import dataclass

DEFAULT_DATABASE_URL = "postgresql://urlshortener:urlshortener@localhost:5432/urlshortener"


# Configuração vem de variáveis de ambiente (padrão "12-factor"): o mesmo código roda
# local, no Docker e na AWS, mudando só o ambiente.
@dataclass(frozen=True)
class Settings:
    database_url: str
    interval_seconds: float
    batch_size: int

    @classmethod
    def from_env(cls) -> "Settings":
        return cls(
            database_url=os.environ.get("DATABASE_URL", DEFAULT_DATABASE_URL),
            interval_seconds=float(os.environ.get("INTERVAL_SECONDS", "10")),
            batch_size=int(os.environ.get("BATCH_SIZE", "1000")),
        )
