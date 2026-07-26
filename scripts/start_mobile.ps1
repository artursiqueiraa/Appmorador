#Requires -Version 5.1
<#
.SYNOPSIS
    Sobe o Mobile (Expo) localmente.
.DESCRIPTION
    Sprint 17.5 (Release 0.9.0). Equivalente a `npx expo start` a partir de
    mobile/, de qualquer diretorio. Use -Web para abrir no navegador (Expo
    Web) — util so como atalho de desenvolvimento, nunca substitui um
    frontend web de verdade (o projeto nao tem um, ver docs/ARCHITECTURE.md).
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
$mobileDir = Join-Path $repoRoot "mobile"

if (-not (Test-Path $mobileDir)) {
    Write-Error "Diretorio nao encontrado: $mobileDir"
    exit 1
}

if (-not (Test-Path (Join-Path $mobileDir ".env"))) {
    Write-Warning "mobile\.env nao existe — copiando de .env.example (ajuste EXPO_PUBLIC_API_URL depois se for testar num celular fisico)."
    Copy-Item (Join-Path $mobileDir ".env.example") (Join-Path $mobileDir ".env")
}

$args = @("expo", "start")
if ($Web) { $args += "--web" }
if ($Clear) { $args += "--clear" }

Push-Location $mobileDir
try {
    Write-Host "Subindo Mobile (Expo)..." -ForegroundColor Cyan
    & npx @args
}
finally {
    Pop-Location
}
