#Requires -Version 5.1
<#
.SYNOPSIS
    Sobe o Backend (AppMorador.Api) localmente.
.DESCRIPTION
    Sprint 17.5 (Release 0.9.0). Equivalente a `dotnet run` a partir de
    backend/src/AppMorador.Api, mas de qualquer diretorio. Exige banco criado
    e user-secrets configurados (docs/setup/SETUP_AMBIENTE.md) — se faltar,
    a Api falha rapido com uma mensagem clara (por design, ver ADR 0008).
.PARAMETER Https
    Usa o profile "https" em vez de "http" (padrao).
.EXAMPLE
    .\scripts\start_backend.ps1
#>
param(
    [switch]$Https
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$apiDir = Join-Path $repoRoot "backend\src\AppMorador.Api"

if (-not (Test-Path $apiDir)) {
    Write-Error "Diretorio nao encontrado: $apiDir"
    exit 1
}

$profile = if ($Https) { "https" } else { "http" }
Write-Host "Subindo Backend (profile: $profile) — http://localhost:5027" -ForegroundColor Cyan
Write-Host "Swagger (Development): http://localhost:5027/swagger" -ForegroundColor Cyan

Push-Location $apiDir
try {
    dotnet run --launch-profile $profile
}
finally {
    Pop-Location
}
