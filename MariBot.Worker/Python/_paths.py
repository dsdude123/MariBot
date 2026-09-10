"""Shared path handling for the worker's Python scripts.

Every script here used to build its own paths as ".{chr(92)}Python{chr(92)}..."
against the current directory: a literal Windows separator, so on Linux the whole
thing collapsed into one filename with backslashes in it. Paths now arrive as
arguments from the worker, which is the only party that knows where its scratch
directory is, and the model cache is resolved once here.
"""

import os


def model_cache() -> str:
    """Where downloaded weights live.

    MARIBOT_MODEL_CACHE is what the container image sets, pointing at a mounted
    volume so multi-gigabyte downloads survive a container recreate. Unset — the
    Windows workers — keeps the "cache" directory next to these scripts, which is
    where their existing downloads already are.
    """
    configured = os.environ.get("MARIBOT_MODEL_CACHE")
    if configured:
        os.makedirs(configured, exist_ok=True)
        return configured

    fallback = os.path.join(os.path.dirname(os.path.abspath(__file__)), "cache")
    os.makedirs(fallback, exist_ok=True)
    return fallback


def read_prompt(path: str) -> str:
    with open(path, "r", encoding="utf-8") as handle:
        return handle.read()
