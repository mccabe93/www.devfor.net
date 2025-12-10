param(
    [switch]$RemoveVolumes,
    [switch]$RemoveImages
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-Docker {
    try {
        docker version > $null 2>&1
        docker compose version > $null 2>&1
        return $true
    }
    catch {
        Write-Error "Docker or Docker Compose does not appear to be available. Install Docker Desktop or the Docker Engine and ensure `docker` (and `docker compose`) are on PATH."
        return $false
    }
}

if (-not (Test-Docker)) { exit 1 }

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Write-Host "Using repository root: $ScriptDir"

function Run-ComposeDown([string]$composePath) {
    if (-not (Test-Path $composePath)) {
        Write-Host "Compose file not found: $composePath" -ForegroundColor Yellow
        return
    }

    $composeFull = Resolve-Path $composePath
    Write-Host "Tearing down compose file: $composeFull"

    $cmd = @('compose', '-f', $composeFull, 'down')
    if ($RemoveVolumes) { $cmd += '--volumes' }
    if ($RemoveImages) { $cmd += '--rmi'; $cmd += 'all' }

    # Run docker compose down directly and check exit code
    & docker @cmd
    if ($LASTEXITCODE -ne 0) {
        Write-Error "docker compose down failed for $composeFull (exit $LASTEXITCODE)"
    }
}

# Compose files order: reverse of up (stop higher-level apps first)
$composeFiles = @(
    (Join-Path $ScriptDir 'devfornet.ApiService\docker-compose.yml'),
    (Join-Path $ScriptDir 'devfornet.Web\docker-compose.yml'),
    (Join-Path $ScriptDir 'devfornet.repos\docker-compose.yml'),
    (Join-Path $ScriptDir 'devfornet.rss\docker-compose.yml'),
    (Join-Path $ScriptDir 'devfornet.db\docker-compose.yml')
)

foreach ($file in $composeFiles) {
    try {
        Run-ComposeDown $file
    }
    catch {
        Write-Error "Failed to stop compose file $file : $_"
    }
}

Write-Host "Requested teardown for all compose projects. Use 'docker ps -a' and 'docker network ls' to inspect state." -ForegroundColor Green
