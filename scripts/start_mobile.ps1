#Requires -Version 5.1
<#
.SYNOPSIS
    Sobe o app (Expo) localmente - base unica web + Android + iOS (ADR 0039).
.DESCRIPTION
    Sprint 17.5 (Release 0.9.0). Equivalente a `npx expo start` a partir de
    webapp/, de qualquer diretorio. Use -Web para abrir no navegador.
    Sprint 23: o antigo mobile/ foi arquivado na tag git mobile-legado.

.PARAMETER Web
    Abre em modo web (Expo Web) em vez do app nativo/Expo Go.
.PARAMETER Clear
    Limpa o cache do Metro bundler antes de subir.
.EXAMPLE
    .\scripts\start_mobile.ps1
.EXAMPLE
    .\scripts\start_mobile.ps1 -Web
#>
param(
    [switch]$Web,
    [switch]$Clear
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$mobileDir = Join-Path $repoRoot "webapp"

if (-not (Test-Path $mobileDir)) {
    Write-Error "Diretorio nao encontrado: $mobileDir"
    exit 1
}

if (-not (Test-Path (Join-Path $mobileDir ".env"))) {
    Write-Warning "webapp\.env nao existe — copiando de .env.example (ajuste EXPO_PUBLIC_API_URL depois se for testar num celular fisico)."
    Copy-Item (Join-Path $mobileDir ".env.example") (Join-Path $mobileDir ".env")
}

$args = @("expo", "start")
if ($Web) { $args += "--web" }
if ($Clear) { $args += "--clear" }

Push-Location $mobileDir
try {
    Write-Host "Subindo app (Expo)..." -ForegroundColor Cyan
    & npx @args
}
finally {
    Pop-Location
}
