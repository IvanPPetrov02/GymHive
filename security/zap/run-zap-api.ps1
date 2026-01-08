param(
  [Parameter(Mandatory = $false)]
  [string]$Namespace = "gymhive",

  [Parameter(Mandatory = $false)]
  [string]$IngressName = "gymhive-ingress",

  [Parameter(Mandatory = $false)]
  [string]$TargetUrl,

  # Optional: if provided (or auto-detected), use ZAP's api scan against an OpenAPI spec.
  [Parameter(Mandatory = $false)]
  [string]$OpenApiUrl,

  [Parameter(Mandatory = $false)]
  [string]$OutDir = "security/zap/results",

  [Parameter(Mandatory = $false)]
  [int]$MaxMinutes = 10
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
  $here = Resolve-Path $PSScriptRoot
  return Resolve-Path (Join-Path $here "..\..")
}

$repoRoot = Resolve-RepoRoot
Set-Location $repoRoot

function Get-StatusCode([string]$Url) {
  try {
    $code = (curl.exe -s -o NUL -L -w "%{http_code}" $Url)
    return [int]$code
  } catch {
    return 0
  }
}

if (-not $TargetUrl) {
  $ingressHost = kubectl -n $Namespace get ingress $IngressName -o jsonpath="{.spec.rules[0].host}" 2>$null
  if ([string]::IsNullOrWhiteSpace($ingressHost)) {
    throw "Could not determine ingress host from $Namespace/$IngressName. Pass -TargetUrl explicitly."
  }
  $TargetUrl = "https://$ingressHost/api/"
}

# Auto-detect OpenAPI if not provided.
if (-not $OpenApiUrl) {
  $base = ($TargetUrl -replace "/api/?$", "")
  $candidates = @(
    "$base/swagger/v1/swagger.json",
    "$base/openapi.json"
  )

  foreach ($cand in $candidates) {
    $code = Get-StatusCode $cand
    if ($code -ge 200 -and $code -lt 300) {
      $OpenApiUrl = $cand
      break
    }
  }
}

$fullOutDir = Join-Path $repoRoot $OutDir
New-Item -ItemType Directory -Force -Path $fullOutDir | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportHtml = Join-Path $fullOutDir "zap-api-$timestamp.html"
$reportJson = Join-Path $fullOutDir "zap-api-$timestamp.json"
$reportMd = Join-Path $fullOutDir "zap-api-$timestamp.md"

Write-Host "Running OWASP ZAP API scan..."
Write-Host "Target API base: $TargetUrl"
if ($OpenApiUrl) { Write-Host "OpenAPI: $OpenApiUrl" } else { Write-Host "OpenAPI: (not found) -> falling back to baseline scan of /api/" }

if ($OpenApiUrl) {
  # ZAP API scan (OpenAPI) via Docker.
  $dockerArgs = @(
    "run", "--rm",
    "-v", "${fullOutDir}:/zap/wrk",
    "zaproxy/zap-stable",
    "zap-api-scan.py",
    "-t", $OpenApiUrl,
    "-f", "openapi",
    "-I",
    "-T", "${MaxMinutes}",
    "-r", (Split-Path -Leaf $reportHtml),
    "-w", (Split-Path -Leaf $reportMd),
    "-J", (Split-Path -Leaf $reportJson)
  )

  & docker @dockerArgs
  if ($LASTEXITCODE -ne 0) {
    throw "ZAP API scan failed (docker exit code $LASTEXITCODE)."
  }
} else {
  # Fallback: baseline scan on /api/ (still useful even if unauthenticated endpoints are limited).
  $dockerArgs = @(
    "run", "--rm",
    "-v", "${fullOutDir}:/zap/wrk",
    "zaproxy/zap-stable",
    "zap-baseline.py",
    "-t", $TargetUrl,
    "-m", "$MaxMinutes",
    "-I",
    "-a",
    "-r", (Split-Path -Leaf $reportHtml),
    "-w", (Split-Path -Leaf $reportMd),
    "-J", (Split-Path -Leaf $reportJson)
  )

  & docker @dockerArgs
  if ($LASTEXITCODE -ne 0) {
    throw "ZAP baseline (/api) scan failed (docker exit code $LASTEXITCODE)."
  }
}

Write-Host "Done. Reports:"
Write-Host "- $reportHtml"
Write-Host "- $reportMd"
Write-Host "- $reportJson"
