#Requires -Version 5.1
<#
.SYNOPSIS
    Gera backup do banco MySQL do AppMorador (schema, dados e completo).
.DESCRIPTION
    Sprint 17.5 (Release 0.9.0). Le a senha da variavel de ambiente
    APPMORADOR_DB_PASSWORD (nunca pede para digitar em texto plano visivel no
    historico do shell). Gera 3 arquivos com timestamp em database/backup/.
.PARAMETER Usuario
    Usuario MySQL (padrao: appmorador, o mesmo usado pela Api em runtime).
.PARAMETER Banco
    Nome do banco (padrao: appmorador).
.PARAMETER MysqldumpPath
    Caminho completo do mysqldump.exe, caso nao esteja no PATH.
.EXAMPLE
    $env:APPMORADOR_DB_PASSWORD = "minha-senha-local"
    .\scripts\backup_database.ps1
#>
param(
    [string]$Usuario = "appmorador",
    [string]$Banco = "appmorador",
    [string]$MysqldumpPath = ""
)

$ErrorActionPreference = "Stop"

if (-not $env:APPMORADOR_DB_PASSWORD) {
    Write-Error "Defina a variavel de ambiente APPMORADOR_DB_PASSWORD antes de rodar este script. Ex.: `$env:APPMORADOR_DB_PASSWORD = '<sua-senha-local>'"
    exit 1
}

function Resolve-Mysqldump {
    param([string]$Explicit)
    if ($Explicit) { return $Explicit }
    $found = Get-Command mysqldump -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    $default = "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe"
    if (Test-Path $default) { return $default }
    throw "mysqldump.exe nao encontrado no PATH nem em '$default'. Informe -MysqldumpPath."
}

$mysqldump = Resolve-Mysqldump -Explicit $MysqldumpPath
$repoRoot = Split-Path -Parent $PSScriptRoot
$backupDir = Join-Path $repoRoot "database\backup"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

$data = Get-Date -Format "yyyyMMdd_HHmmss"
$env:MYSQL_PWD = $env:APPMORADOR_DB_PASSWORD

try {
    $schemaFile = Join-Path $backupDir "appmorador_schema_$data.sql"
    $seedFile   = Join-Path $backupDir "appmorador_seed_$data.sql"
    $fullFile   = Join-Path $backupDir "appmorador_full_$data.sql"

    Write-Host "Gerando dump de schema..." -ForegroundColor Cyan
    & $mysqldump -u $Usuario --no-data --no-tablespaces --routines --triggers --skip-comments $Banco | Out-File -Encoding utf8 $schemaFile
    if ($LASTEXITCODE -ne 0) { throw "mysqldump falhou ao gerar o schema (codigo $LASTEXITCODE)." }

    Write-Host "Gerando dump de dados (seed)..." -ForegroundColor Cyan
    & $mysqldump -u $Usuario --no-create-info --no-tablespaces --skip-comments --complete-insert $Banco | Out-File -Encoding utf8 $seedFile
    if ($LASTEXITCODE -ne 0) { throw "mysqldump falhou ao gerar os dados (codigo $LASTEXITCODE)." }

    Write-Host "Gerando dump completo..." -ForegroundColor Cyan
    & $mysqldump -u $Usuario --no-tablespaces --routines --triggers --skip-comments --complete-insert $Banco | Out-File -Encoding utf8 $fullFile
    if ($LASTEXITCODE -ne 0) { throw "mysqldump falhou ao gerar o backup completo (codigo $LASTEXITCODE)." }

    Write-Host "`nBackup concluido:" -ForegroundColor Green
    Write-Host "  Schema : $schemaFile"
    Write-Host "  Seed   : $seedFile"
    Write-Host "  Completo: $fullFile"
    Write-Host "`nEsses arquivos ficam em database/backup/ (ignorado pelo git, ver database/README.md)."
}
finally {
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
}
