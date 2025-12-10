#!/usr/bin/env bash
set -euo pipefail

# up.sh - bring up all docker compose projects for the repo
# Run from repo root: ./up.sh

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
ENV_FILE="$SCRIPT_DIR/.env"

echo "Using repo root: $SCRIPT_DIR"
if [ -f "$ENV_FILE" ]; then
  echo "Using env file: $ENV_FILE"
else
  echo "No .env found at repo root ($ENV_FILE). Compose will rely on environment variables." >&2
fi

NETWORK_NAME=devfornet-network

if docker network ls -q -f name="^${NETWORK_NAME}$" | grep -q .; then
  echo "Docker network '$NETWORK_NAME' already exists."
else
  echo "Creating docker network '$NETWORK_NAME'..."
  docker network create "$NETWORK_NAME"
fi

run_compose() {
  local compose_file="$1"
  if [ ! -f "$compose_file" ]; then
    echo "Compose file not found: $compose_file" >&2
    return 0
  fi
  echo "Bringing up compose: $compose_file"
  if [ -f "$ENV_FILE" ]; then
    docker compose --env-file "$ENV_FILE" -f "$compose_file" up -d --build
  else
    docker compose -f "$compose_file" up -d --build
  fi
  docker compose -f "$compose_file" ps
}

# order: db first, then api, web, repos, rss
run_compose "$SCRIPT_DIR/devfornet.db/docker-compose.yml"
run_compose "$SCRIPT_DIR/devfornet.ApiService/docker-compose.yml"
run_compose "$SCRIPT_DIR/devfornet.Web/docker-compose.yml"
run_compose "$SCRIPT_DIR/devfornet.repos/docker-compose.yml"
run_compose "$SCRIPT_DIR/devfornet.rss/docker-compose.yml"

echo "All compose projects requested. Use 'docker ps' to inspect running containers."
