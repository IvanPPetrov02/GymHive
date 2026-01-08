param(
  [Parameter(Mandatory = $false)]
  [string]$Namespace = "gymhive",

  [Parameter(Mandatory = $false)]
  [string]$IngressName = "gymhive-ingress",

  [Parameter(Mandatory = $false)]
  [string]$TargetUrl,

  [Parameter(Mandatory = $false)]
  [switch]$UseHttp,

  [Parameter(Mandatory = $false)]
  [string]$OutDir = "security/zap/results"
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
  $here = Resolve-Path $PSScriptRoot
  return Resolve-Path (Join-Path $here "..\..")
}

$repoRoot = Resolve-RepoRoot
Set-Location $repoRoot

if (-not $TargetUrl) {
  $ingressHost = kubectl -n $Namespace get ingress $IngressName -o jsonpath="{.spec.rules[0].host}" 2>$null
  if ([string]::IsNullOrWhiteSpace($ingressHost)) {
    throw "Could not determine ingress host from $Namespace/$IngressName. Pass -TargetUrl explicitly."
  }

  $scheme = $(if ($UseHttp) { "http" } else { "https" })
  $TargetUrl = "${scheme}://$ingressHost/"
}

$fullOutDir = Join-Path $repoRoot $OutDir
New-Item -ItemType Directory -Force -Path $fullOutDir | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportHtml = Join-Path $fullOutDir "zap-baseline-$timestamp.html"
$reportJson = Join-Path $fullOutDir "zap-baseline-$timestamp.json"
$reportMd = Join-Path $fullOutDir "zap-baseline-$timestamp.md"

Write-Host "Running OWASP ZAP baseline scan..."
Write-Host "Target: $TargetUrl"
Write-Host "Reports: $reportHtml"

# -I: do not fail build on warnings
# -j: include the JSON report
# -m: max minutes
# -a: include alpha passive rules
# -r/-w/-J: report outputs
$dockerArgs = @(
  "run", "--rm",
  "-v", "${fullOutDir}:/zap/wrk",
  "zaproxy/zap-stable",
  "zap-baseline.py",
  "-t", $TargetUrl,
  "-m", "10",
  "-I",
  "-a",
  "-r", (Split-Path -Leaf $reportHtml),
  "-w", (Split-Path -Leaf $reportMd),
  "-J", (Split-Path -Leaf $reportJson)
)

& docker @dockerArgs
if ($LASTEXITCODE -ne 0) {
  throw "ZAP baseline scan failed (docker exit code $LASTEXITCODE)."
}

Write-Host "Done. Open the HTML report at: $reportHtml"
