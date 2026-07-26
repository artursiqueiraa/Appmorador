#Requires -Version 5.1
<#
.SYNOPSIS
    Restaura um dump SQL do AppMorador num banco MySQL existente.
.DESCRIPTION
    Sprint 17.5 (Release 0.9.0). Pede confirmacao explicita antes de restaurar
    por cima de um banco que ja existe — nunca sobrescreve dado real
    silenciosamente. NAO cria o banco em si (o usuario de runtime `appmorador`
    nao tem CREATE DATABASE por design, ver ADR 0008/DIVIDA_TECNICA item 8) —
    se o banco de destino ainda nao existir, crie-o antes com um usuario
    privilegiado (ver database/README.md).
.PARAMETER ArquivoSql
    Caminho do arquivo .sql a restaurar (schema, seed ou completo).
.PARAMETER NomeBanco
    Banco de destino (padrao: appmorador).
.PARAMETER Usuario
    Usuario MySQL (padrao: appmorador).
.PARAMETER Forcar
    Pula a confirmacao interativa (uso em automacao/CI).
.EXAMPLE
    $env:APPMORADOR_DB_PASSWORD = "minha-senha-local"
    .\scripts\restore_database.ps1 -ArquivoSql database\backup\appmorador_full_20260725.sql
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ArquivoSql,
    [string]$NomeBanco = "appmorador",
    [string]$Usuario = "appmorador",
    [string]$MysqlPath = "",
    [switch]$Forcar
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ArquivoSql)) {
    Write-Error "Arquivo nao encontrado: $ArquivoSql"
    exit 1
}

if (-not $env:APPMORADOR_DB_PASSWORD) {
    Write-Error "Defina a variavel de ambiente APPMORADOR_DB_PASSWORD antes de rodar este script."
    exit 1
}

function Resolve-MysqlCli {
    param([string]$Explicit)
    if ($Explicit) { return $Explicit }
    $found = Get-Command mysql -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    $default = "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
    if (Test-Path $default) { return $default }
    throw "mysql.exe nao encontrado no PATH nem em '$default'. Informe -MysqlPath."
}

$mysql = Resolve-MysqlCli -Explicit $MysqlPath

if (-not $Forcar) {
    Write-Warning "Isso vai executar '$ArquivoSql' contra o banco '$NomeBanco' como '$Usuario'. Se o banco ja tiver dado, tabelas existentes podem falhar (CREATE TABLE) ou ter dados duplicados (INSERT), dependendo do conteudo do arquivo."
    $resposta = Read-Host "Confirma a restauracao? (digite SIM para continuar)"
    if ($resposta -ne "SIM") {
        Write-Host "Cancelado." -ForegroundColor Yellow
        exit 0
    }
}

$env:MYSQL_PWD = $env:APPMORADOR_DB_PASSWORD
try {
    Write-Host "Restaurando '$ArquivoSql' em '$NomeBanco'..." -ForegroundColor Cyan
    Get-Content $ArquivoSql -Raw | & $mysql -u $Usuario $NomeBanco
    if ($LASTEXITCODE -ne 0) { throw "Restauracao falhou (codigo $LASTEXITCODE). Se o banco '$NomeBanco' ainda nao existe, crie-o primeiro com um usuario privilegiado — ver database/README.md." }
    Write-Host "Restauracao concluida. Valide com:" -ForegroundColor Green
    Write-Host "  mysql -u $Usuario $NomeBanco -e `"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$NomeBanco';`""
}
finally {
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
}
