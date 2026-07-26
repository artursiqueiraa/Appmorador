#Requires -Version 5.1
<#
.SYNOPSIS
    Remove artefatos de build/dependencias locais do AppMorador.
.DESCRIPTION
    Sprint 17.5 (Release 0.9.0). Remove bin/obj (backend), node_modules/.expo
    (mobile) — tudo o que ja e gitignored e pode ser regenerado por
    setup_project.ps1. NUNCA remove banco de dados, .env, user-secrets ou
    qualquer coisa fora do controle de versao que nao seja puramente
    reconstruivel. Pede confirmacao antes de apagar (use -Forcar para pular).
.PARAMETER Forcar
    Pula a confirmacao interativa.
.EXAMPLE
    .\scripts\clean_project.ps1
#>
param(
    [switch]$Forcar
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

$alvos = @(
    (Join-Path $repoRoot "backend"),
    (Join-Path $repoRoot "mobile\node_modules"),
    (Join-Path $repoRoot "mobile\.expo"),
    (Join-Path $repoRoot "mobile\dist")
)

Write-Host "Isto vai remover:" -ForegroundColor Yellow
Write-Host "  - Todas as pastas bin/ e obj/ dentro de backend/"
Write-Host "  - mobile/node_modules, mobile/.expo, mobile/dist"
Write-Host "Nada relacionado a banco de dados, .env ou user-secrets sera tocado."

if (-not $Forcar) {
    $resposta = Read-Host "`nConfirma a limpeza? (digite SIM para continuar)"
    if ($resposta -ne "SIM") {
        Write-Host "Cancelado." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "`nRemovendo bin/ e obj/ do backend..." -ForegroundColor Cyan
Get-ChildItem -Path (Join-Path $repoRoot "backend") -Include bin, obj -Recurse -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
        Write-Host "  Removendo $($_.FullName)"
        Remove-Item -Recurse -Force $_.FullName -ErrorAction SilentlyContinue
    }

foreach ($pasta in @("mobile\node_modules", "mobile\.expo", "mobile\dist")) {
    $caminho = Join-Path $repoRoot $pasta
    if (Test-Path $caminho) {
        Write-Host "Removendo $caminho..." -ForegroundColor Cyan
        Remove-Item -Recurse -Force $caminho
    }
}

Write-Host "`nLimpeza concluida. Rode scripts\setup_project.ps1 para reconstruir." -ForegroundColor Green
