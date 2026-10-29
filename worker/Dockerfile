FROM python:3.13-slim

COPY --from=ghcr.io/astral-sh/uv:0.12.23 /uv /bin/uv

WORKDIR /app
ENV UV_COMPILE_BYTECODE=1 UV_LINK_MODE=copy PYTHONUNBUFFERED=1

# Dependências primeiro: essa camada fica em cache enquanto só o código mudar.
COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-dev --no-install-project

COPY src ./src
RUN uv sync --frozen --no-dev

# Não roda como root dentro do container.
RUN useradd --create-home worker
USER worker

ENV PATH="/app/.venv/bin:$PATH"
CMD ["python", "-m", "stats_worker"]
