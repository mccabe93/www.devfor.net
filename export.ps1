param(
    [string]$OutputDir = "./exports",
    [switch]$IncludeVolumes
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-Docker {
    try {
        docker version > $null 2>&1
        return $true
    }
    catch {
        Write-Error "Docker does not appear to be available. Install Docker and ensure 'docker' is on PATH."
        return $false
    }
}

if (-not (Test-Docker)) { exit 1 }

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not [System.IO.Path]::IsPathRooted($OutputDir)) { $OutputDir = Join-Path $ScriptDir $OutputDir }
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }
$OutputDir = (Get-Item $OutputDir).FullName
Write-Host "Export directory: $OutputDir"

# Get running containers
$psFormat = "{{.ID}}||{{.Names}}||{{.Image}}"
$lines = & docker ps --format $psFormat
if ($LASTEXITCODE -ne 0) { Write-Error 'Failed to list running containers.'; exit 1 }

$containers = @()
$imagesSet = @{}
foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line -split '\|\|'
    if ($parts.Count -lt 3) { continue }
    $id = $parts[0]; $name = $parts[1]; $image = $parts[2]
    $containers += [PSCustomObject]@{ Id = $id; Name = $name; Image = $image }
    $imagesSet[$image] = $true
}

if ($containers.Count -eq 0) {
    Write-Host "No running containers found. Nothing to export." -ForegroundColor Yellow
}

# Export each unique image
$images = $imagesSet.Keys
$exported = @()
foreach ($image in $images) {
    # sanitize filename
    $safe = $image -replace '[/:@]', '_' -replace '[^a-zA-Z0-9_\-\.]',''
    $outFile = Join-Path $OutputDir ("image_{0}.tar" -f $safe)
    Write-Host "Saving image $image -> $outFile"
    & docker save -o "$outFile" $image
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to save image $image"; continue }
    $exported += [PSCustomObject]@{ Image = $image; File = $outFile }
}

# Optionally export volumes named devfornet_*
$volumesExported = @()
if ($IncludeVolumes) {
    $volNames = & docker volume ls --format '{{.Name}}' | Where-Object { $_ -like 'devfornet_*' }
    foreach ($vol in $volNames) {
        $volOut = Join-Path $OutputDir ("volume_{0}.tar.gz" -f ($vol -replace '[/:@]','_'))
        Write-Host "Exporting volume $vol -> $volOut"
        & docker run --rm -v "$vol":/volume -v "$OutputDir":/backup alpine sh -c "cd /volume && tar -czf /backup/$(Split-Path -Leaf $volOut) ."
        if ($LASTEXITCODE -ne 0) { Write-Error "Failed to export volume $vol"; continue }
        $volumesExported += [PSCustomObject]@{ Volume = $vol; File = $volOut }
    }
}

# Write manifest
$manifest = [PSCustomObject]@{
    GeneratedAt = (Get-Date).ToString('o')
    Containers = $containers
    Images = $exported
    Volumes = $volumesExported
}
$manifestFile = Join-Path $OutputDir 'export-manifest.json'
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $manifestFile -Encoding UTF8
Write-Host "Export complete. Manifest: $manifestFile" -ForegroundColor Green
