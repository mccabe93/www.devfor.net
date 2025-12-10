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
$EnvFile = Join-Path $ScriptDir '.env'

Write-Host "Using repository root: $ScriptDir"
if (Test-Path $EnvFile) {
    Write-Host "Loading env file: $EnvFile"
}
else {
    Write-Host "No .env found at repo root ($EnvFile). Compose will rely on environment for variables." -ForegroundColor Yellow
}

# Ensure shared network exists (use docker network ls to reliably detect)
$networkName = 'devfornet-network'
$exists = $false
try {
    $found = docker network ls --filter "name=^${networkName}$" --format "{{.Name}}" 2>$null
    if ($found -and $found.Trim() -eq $networkName) { $exists = $true }
}
catch {
    # ignore parse errors
}

if ($exists) {
    Write-Host "Docker network '$networkName' already exists."
}
else {
    Write-Host "Creating Docker network '$networkName'..."
    $out = docker network create $networkName 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create network '$networkName': $out"
        exit 1
    }
    Write-Host "Created network '$networkName'."
}

function Run-ComposeFile([string]$composePath) {
    if (-not (Test-Path $composePath)) {
        Write-Host "Compose file not found: $composePath" -ForegroundColor Yellow
        return
    }

    $composeFull = Resolve-Path $composePath
    Write-Host "Bringing up compose file: $composeFull"

    $envArg = @()
    if (Test-Path $EnvFile) { $envArg = @('--env-file', $EnvFile) }

    $cmd = @('compose', '-f', $composeFull)
    if ($envArg.Count -gt 0) { $cmd += $envArg }
    $cmd += @('up','-d','--build')

    & docker @cmd
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose failed for $composeFull (exit $LASTEXITCODE)"
    }

    # show service status for this compose file
    & docker compose -f $composeFull ps
}

# Compose files order: DB first, then API, WEB, REPOS, RSS
$composeFiles = @(
    (Join-Path $ScriptDir 'devfornet.db\docker-compose.yml'),
    (Join-Path $ScriptDir 'devfornet.ApiService\docker-compose.yml'),
    (Join-Path $ScriptDir 'devfornet.Web\docker-compose.yml'),
    (Join-Path $ScriptDir 'devfornet.repos\docker-compose.yml'),
    (Join-Path $ScriptDir 'devfornet.rss\docker-compose.yml')
)

foreach ($file in $composeFiles) {
    try {
        Run-ComposeFile $file
    }
    catch {
        Write-Error "Failed to start compose file $file : $_"
        exit 1
    }
}

Write-Host "All compose projects requested. Use 'docker ps' to inspect running containers." -ForegroundColor Green

