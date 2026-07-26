#Requires -Version 5.1
<#
.SYNOPSIS
    Verifica pre-requisitos e prepara o ambiente local do AppMorador.
.DESCRIPTION
    Sprint 17.5 (Release 0.9.0). Nao substitui docs/setup/SETUP_AMBIENTE.md —
    automatiza as partes seguras/idempotentes (checar ferramentas, copiar
    .env.example, restaurar pacotes) e aponta os passos que continuam manuais
    por design (criar o banco, configurar user-secrets — nunca decide uma
    senha por voce).
.EXAMPLE
    .\scripts\setup_project.ps1
#>
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Test-Ferramenta {
    param([string]$Nome, [string]$Comando, [string]$VersaoMinimaDica)
    $cmd = Get-Command $Comando -ErrorAction SilentlyContinue
    if ($cmd) {
        $versao = & $Comando --version 2>$null | Select-Object -First 1
        Write-Host "  [OK] $Nome encontrado: $versao" -ForegroundColor Green
        return $true
    } else {
        Write-Host "  [FALTA] $Nome nao encontrado no PATH. $VersaoMinimaDica" -ForegroundColor Red
        return $false
    }
}

Write-Host "== 0. Configuracao do git (Windows) ==" -ForegroundColor Cyan
$longPaths = git config --global --get core.longpaths
if ($longPaths -ne "true") {
    Write-Host "  Habilitando core.longpaths=true (nomes de arquivo de migration do EF Core passam de 260 caracteres em caminhos aninhados — sem isso, o checkout falha com 'Filename too long', achado real na verificacao de portabilidade da Sprint 17.5)." -ForegroundColor Yellow
    git config --global core.longpaths true
} else {
    Write-Host "  [OK] core.longpaths ja habilitado." -ForegroundColor Green
}

Write-Host "`n== 1. Verificando ferramentas obrigatorias ==" -ForegroundColor Cyan
$ok = $true
$ok = (Test-Ferramenta ".NET SDK" "dotnet" "Instale o .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0") -and $ok
$ok = (Test-Ferramenta "Node.js" "node" "Instale Node.js 20+: https://nodejs.org") -and $ok
$ok = (Test-Ferramenta "npm" "npm" "Vem junto com o Node.js.") -and $ok

$mysqlCli = Get-Command mysql -ErrorAction SilentlyContinue
if (-not $mysqlCli -and (Test-Path "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe")) {
    Write-Host "  [OK] MySQL client encontrado em C:\Program Files\MySQL\MySQL Server 8.0\bin (fora do PATH)." -ForegroundColor Green
} elseif ($mysqlCli) {
    Write-Host "  [OK] MySQL client encontrado no PATH." -ForegroundColor Green
} else {
    Write-Host "  [FALTA] MySQL Server/Client 8.0+ nao encontrado. Instale: https://dev.mysql.com/downloads/mysql/" -ForegroundColor Red
    $ok = $false
}

$dotnetEf = dotnet tool list --global 2>$null | Select-String "dotnet-ef"
if ($dotnetEf) {
    Write-Host "  [OK] dotnet-ef instalado globalmente." -ForegroundColor Green
} else {
    Write-Host "  [FALTA] dotnet-ef nao instalado. Rode: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
}

if (-not $ok) {
    Write-Error "Instale as ferramentas faltando antes de continuar."
    exit 1
}

Write-Host "`n== 2. Preparando .env do Mobile ==" -ForegroundColor Cyan
$mobileEnv = Join-Path $repoRoot "mobile\.env"
$mobileEnvExample = Join-Path $repoRoot "mobile\.env.example"
if (-not (Test-Path $mobileEnv)) {
    Copy-Item $mobileEnvExample $mobileEnv
    Write-Host "  Criado mobile\.env a partir de .env.example — ajuste EXPO_PUBLIC_API_URL para o IP da sua maquina se for testar num celular fisico." -ForegroundColor Yellow
} else {
    Write-Host "  mobile\.env ja existe — mantido sem alteracao." -ForegroundColor Green
}

Write-Host "`n== 3. Restaurando pacotes ==" -ForegroundColor Cyan
Write-Host "  Backend (dotnet restore)..."
Push-Location (Join-Path $repoRoot "backend")
dotnet restore
Pop-Location

Write-Host "  Mobile (npm install)..."
Push-Location (Join-Path $repoRoot "mobile")
npm install
Pop-Location

Write-Host "`n== 4. Passos manuais restantes (por design, nunca automatizados) ==" -ForegroundColor Cyan
Write-Host @"
  a) Criar o banco (uma vez, usuario privilegiado):
       CREATE DATABASE appmorador CHARACTER SET utf8mb4;
       CREATE USER 'appmorador'@'localhost' IDENTIFIED BY '<senha-forte-local>';
       GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES, DROP
         ON appmorador.* TO 'appmorador'@'localhost';
       FLUSH PRIVILEGES;

  b) Configurar segredos do Backend (a partir de backend/src/AppMorador.Api/):
       dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=appmorador;User=appmorador;Password=<senha-forte-local>;"
       dotnet user-secrets set "Jwt:Key" "<chave-aleatoria-longa-64-bytes>"

  Ver docs/setup/SETUP_AMBIENTE.md e docs/ENVIRONMENT.md para o detalhe completo de cada variavel.
"@ -ForegroundColor White

Write-Host "`nSetup concluido. Use scripts\start_backend.ps1 e scripts\start_mobile.ps1 para rodar o projeto." -ForegroundColor Green
