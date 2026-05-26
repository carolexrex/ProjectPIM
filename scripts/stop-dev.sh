#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PID_ROOT="$REPO_ROOT/.dev-runtime/pids"
POSTGRES_CONTAINER_NAME="projektpim-postgres"
KEEP_DATABASE=0

for arg in "$@"; do
  case "$arg" in
    --keep-db)
      KEEP_DATABASE=1
      ;;
    *)
      echo "Unknown argument: $arg" >&2
      exit 1
      ;;
  esac
done

get_tracked_pid() {
  local pid_file="$1"
  if [[ ! -f "$pid_file" ]]; then
    return 1
  fi

  local process_id
  process_id="$(tr -d '[:space:]' < "$pid_file")"
  if [[ ! "$process_id" =~ ^[0-9]+$ ]]; then
    rm -f "$pid_file"
    return 1
  fi

  printf '%s' "$process_id"
}

stop_tracked_process() {
  local name="$1"
  local pid_file="$PID_ROOT/$name.pid"

  if ! process_id="$(get_tracked_pid "$pid_file" 2>/dev/null)"; then
    echo "$name is not tracked."
    return 0
  fi

  if kill -0 "$process_id" 2>/dev/null; then
    echo "Stopping $name (pid $process_id)"
    kill "$process_id" 2>/dev/null || true
  else
    echo "$name pid $process_id is not running."
  fi

  rm -f "$pid_file"
}

stop_tracked_process "api"
stop_tracked_process "storefront-api"
stop_tracked_process "backoffice"
stop_tracked_process "worker"

if [[ "$KEEP_DATABASE" -eq 0 ]]; then
  if docker ps -a --format '{{.Names}}' | grep -Fxq "$POSTGRES_CONTAINER_NAME"; then
    if [[ "$(docker inspect -f '{{.State.Running}}' "$POSTGRES_CONTAINER_NAME")" == "true" ]]; then
      echo "Stopping PostgreSQL container '$POSTGRES_CONTAINER_NAME'"
      docker stop "$POSTGRES_CONTAINER_NAME" >/dev/null
    fi
  fi
fi
