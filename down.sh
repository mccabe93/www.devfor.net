#!/usr/bin/env bash
set -euo pipefail

# down.sh - tear down all docker compose projects for the repo
# Usage: ./down.sh [--remove-volumes] [--remove-images]

REMOVE_VOLUMES=0
REMOVE_IMAGES=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --remove-volumes)
      REMOVE_VOLUMES=1; shift ;;
    --remove-images)
      REMOVE_IMAGES=1; shift ;;
    *) echo "Unknown arg: $1" >&2; exit 2 ;;
  esac
done

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
ENV_FILE="$SCRIPT_DIR/.env"

echo "Using repo root: $SCRIPT_DIR"

run_compose_down() {
  local compose_file="$1"
  if [ ! -f "$compose_file" ]; then
    echo "Compose file not found: $compose_file" >&2
    return 0
  fi
  echo "Tearing down compose: $compose_file"
  local args=(down)
  if [ -f "$ENV_FILE" ]; then
    if [ "$REMOVE_VOLUMES" -eq 1 ]; then args+=(--volumes); fi
    if [ "$REMOVE_IMAGES" -eq 1 ]; then args+=(--rmi all); fi
    docker compose --env-file "$ENV_FILE" -f "$compose_file" "${args[@]}"
  else
    if [ "$REMOVE_VOLUMES" -eq 1 ]; then args+=(--volumes); fi
    if [ "$REMOVE_IMAGES" -eq 1 ]; then args+=(--rmi all); fi
    docker compose -f "$compose_file" "${args[@]}"
  fi
}

# reverse order: stop apps then db
run_compose_down "$SCRIPT_DIR/devfornet.ApiService/docker-compose.yml"
run_compose_down "$SCRIPT_DIR/devfornet.Web/docker-compose.yml"
run_compose_down "$SCRIPT_DIR/devfornet.repos/docker-compose.yml"
run_compose_down "$SCRIPT_DIR/devfornet.rss/docker-compose.yml"
run_compose_down "$SCRIPT_DIR/devfornet.db/docker-compose.yml"

echo "Requested teardown for all compose projects. Use 'docker ps -a' to inspect state."
