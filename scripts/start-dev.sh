#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUNTIME_ROOT="$REPO_ROOT/.dev-runtime"
PID_ROOT="$RUNTIME_ROOT/pids"
LOG_ROOT="$RUNTIME_ROOT/logs"
DOTNET_CLI_HOME_DIR="$REPO_ROOT/.dotnet-cli"

API_PROJECT_ROOT="$REPO_ROOT/src/Platform.Api"
BACKOFFICE_PROJECT_ROOT="$REPO_ROOT/src/Platform.Backoffice"
INFRASTRUCTURE_PROJECT="$REPO_ROOT/src/Platform.Infrastructure/Platform.Infrastructure.csproj"
SOLUTION_PATH="$REPO_ROOT/Platform.slnx"
API_DLL="$API_PROJECT_ROOT/bin/Debug/net10.0/Platform.Api.dll"
BACKOFFICE_DLL="$BACKOFFICE_PROJECT_ROOT/bin/Debug/net10.0/Platform.Backoffice.dll"

API_URL="http://localhost:5053/"
BACKOFFICE_URL="http://localhost:5168/"
API_PROBE_URL="$API_URL"
BACKOFFICE_PROBE_URL="${BACKOFFICE_URL}auth/login"
POSTGRES_CONTAINER_NAME="projektpim-postgres"
POSTGRES_IMAGE="postgres:17"

SKIP_DATABASE_START=0
SKIP_MIGRATE=0
OPEN_BROWSER=0

for arg in "$@"; do
  case "$arg" in
    --skip-db)
      SKIP_DATABASE_START=1
      ;;
    --skip-migrate)
      SKIP_MIGRATE=1
      ;;
    --open-backoffice)
      OPEN_BROWSER=1
      ;;
    *)
      echo "Unknown argument: $arg" >&2
      exit 1
      ;;
  esac
done

mkdir -p "$RUNTIME_ROOT" "$PID_ROOT" "$LOG_ROOT" "$DOTNET_CLI_HOME_DIR"

export DOTNET_CLI_HOME="$DOTNET_CLI_HOME_DIR"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

log_step() {
  echo "==> $1"
}

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Required command '$1' was not found in PATH." >&2
    exit 1
  fi
}

run_checked() {
  local description="$1"
  shift
  log_step "$description"
  "$@"
}

is_pid_alive() {
  local process_id="$1"
  [[ -n "$process_id" ]] && kill -0 "$process_id" 2>/dev/null
}

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

start_postgres() {
  if ! docker ps -a --format '{{.Names}}' | grep -Fxq "$POSTGRES_CONTAINER_NAME"; then
    log_step "Creating PostgreSQL container '$POSTGRES_CONTAINER_NAME'"
    docker run \
      --name "$POSTGRES_CONTAINER_NAME" \
      -e POSTGRES_PASSWORD=postgres \
      -e POSTGRES_USER=postgres \
      -e POSTGRES_DB=projektpim \
      -p 5432:5432 \
      -d \
      "$POSTGRES_IMAGE" >/dev/null
  else
    local is_running
    is_running="$(docker inspect -f '{{.State.Running}}' "$POSTGRES_CONTAINER_NAME")"
    if [[ "$is_running" != "true" ]]; then
      log_step "Starting PostgreSQL container '$POSTGRES_CONTAINER_NAME'"
      docker start "$POSTGRES_CONTAINER_NAME" >/dev/null
    else
      log_step "PostgreSQL container '$POSTGRES_CONTAINER_NAME' is already running"
    fi
  fi

  log_step "Waiting for PostgreSQL readiness"
  for _ in $(seq 1 60); do
    if docker exec "$POSTGRES_CONTAINER_NAME" pg_isready -U postgres -d projektpim >/dev/null 2>&1; then
      return 0
    fi

    sleep 1
  done

  echo "PostgreSQL container '$POSTGRES_CONTAINER_NAME' did not become ready within 60 seconds." >&2
  exit 1
}

start_managed_process() {
  local name="$1"
  local working_directory="$2"
  local application_dll="$3"
  local url="$4"
  local pid_file="$PID_ROOT/$name.pid"
  local stdout_log="$LOG_ROOT/$name.out.log"
  local stderr_log="$LOG_ROOT/$name.err.log"

  if process_id="$(get_tracked_pid "$pid_file" 2>/dev/null)"; then
    if is_pid_alive "$process_id"; then
      log_step "$name is already running with pid $process_id"
      return 0
    fi

    rm -f "$pid_file"
  fi

  log_step "Starting $name"
  (
    cd "$working_directory"
    ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$url" nohup dotnet "$application_dll" >"$stdout_log" 2>"$stderr_log" &
    echo $! > "$pid_file"
  )
}

wait_for_http_endpoint() {
  local name="$1"
  local url="$2"
  local pid_file="$PID_ROOT/$name.pid"
  local stdout_log="$LOG_ROOT/$name.out.log"
  local stderr_log="$LOG_ROOT/$name.err.log"

  log_step "Waiting for $name on $url"
  for _ in $(seq 1 60); do
    local http_code
    http_code="$(curl -sS -o /dev/null -w '%{http_code}' "$url" || true)"
    if [[ -n "$http_code" && "$http_code" != "000" ]]; then
      return 0
    fi

    if process_id="$(get_tracked_pid "$pid_file" 2>/dev/null)"; then
      if ! is_pid_alive "$process_id"; then
        echo "$name exited before becoming reachable." >&2
        if [[ -f "$stdout_log" ]]; then
          echo "STDOUT:" >&2
          tail -n 20 "$stdout_log" >&2 || true
        fi
        if [[ -f "$stderr_log" ]]; then
          echo "STDERR:" >&2
          tail -n 20 "$stderr_log" >&2 || true
        fi
        exit 1
      fi
    fi

    sleep 1
  done

  echo "$name did not become reachable on $url within 60 seconds." >&2
  exit 1
}

open_backoffice_browser() {
  local url="$1"

  if [[ "$OPEN_BROWSER" -eq 0 ]]; then
    return 0
  fi

  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "$url" >/dev/null 2>&1 &
    return 0
  fi

  if command -v open >/dev/null 2>&1; then
    open "$url" >/dev/null 2>&1 &
    return 0
  fi

  echo "No supported browser opener found. Open $url manually." >&2
}

require_command docker
require_command dotnet
require_command curl

if [[ "$SKIP_DATABASE_START" -eq 0 ]]; then
  start_postgres
else
  log_step "Skipping PostgreSQL container startup"
fi

if [[ "$SKIP_MIGRATE" -eq 0 ]]; then
  run_checked "Restoring local dotnet tools" dotnet tool restore
fi

run_checked "Building solution" dotnet build "$SOLUTION_PATH" -m:1 -nr:false

if [[ "$SKIP_MIGRATE" -eq 0 ]]; then
  run_checked \
    "Applying PostgreSQL migrations" \
    dotnet tool run dotnet-ef database update \
      --no-build \
      --project "$INFRASTRUCTURE_PROJECT" \
      --startup-project "$INFRASTRUCTURE_PROJECT" \
      --context Platform.Infrastructure.Persistence.PlatformDbContext
fi

if [[ ! -f "$API_DLL" ]]; then
  echo "Built API assembly was not found at $API_DLL." >&2
  exit 1
fi

if [[ ! -f "$BACKOFFICE_DLL" ]]; then
  echo "Built Backoffice assembly was not found at $BACKOFFICE_DLL." >&2
  exit 1
fi

start_managed_process "api" "$API_PROJECT_ROOT" "$API_DLL" "$API_URL"
start_managed_process "backoffice" "$BACKOFFICE_PROJECT_ROOT" "$BACKOFFICE_DLL" "$BACKOFFICE_URL"

wait_for_http_endpoint "api" "$API_PROBE_URL"
wait_for_http_endpoint "backoffice" "$BACKOFFICE_PROBE_URL"

echo
echo "Development stack is running."
echo "API:        $API_URL"
echo "Backoffice: $BACKOFFICE_URL"
echo "Logs:       $LOG_ROOT"
echo "Stop with:  ./scripts/stop-dev.sh"

open_backoffice_browser "$BACKOFFICE_URL"
