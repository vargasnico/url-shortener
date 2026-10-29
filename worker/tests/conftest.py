from collections.abc import Iterator
from pathlib import Path

import psycopg
import pytest
from testcontainers.community.postgres import PostgresContainer

# Gerado a partir das migrations da API (ver README), para testar contra o schema real:
# dotnet ef migrations script --project src/UrlShortener.Api -o worker/tests/schema.sql
SCHEMA = (Path(__file__).parent / "schema.sql").read_text(encoding="utf-8-sig")


@pytest.fixture(scope="session")
def database_url() -> Iterator[str]:
    with PostgresContainer("postgres:17", driver=None) as postgres:
        url = postgres.get_connection_url()
        with psycopg.connect(url, autocommit=True) as conn:
            conn.execute(SCHEMA)
        yield url


@pytest.fixture
def conn(database_url: str) -> Iterator[psycopg.Connection]:
    with psycopg.connect(database_url) as conn:
        yield conn
        conn.rollback()
        conn.execute("TRUNCATE links, click_events, link_daily_stats RESTART IDENTITY CASCADE")
        conn.commit()
