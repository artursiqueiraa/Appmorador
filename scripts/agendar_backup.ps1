#Requires -Version 5.1
<#
.SYNOPSIS
    Agenda o backup diario do AppMorador no Agendador de Tarefas do Windows (Sprint 27, ADR 0043).
.DESCRIPTION
    - Guarda a senha do MySQL em %LOCALAPPDATA%\AppMorador\backup.cred, protegida por DPAPI
      (so o mesmo usuario do Windows, nesta maquina, consegue ler). Nunca no repositorio.
    - Cria (ou atualiza) a tarefa "AppMorador - Backup diario", todo dia no horario escolhido,
      rodando scripts\backup_agendado.ps1 com o usuario atual (sem precisar estar logado so se a
      politica do Windows permitir; por padrao roda quando o usuario esta logado).
    - Com -Remover, apaga a tarefa e o arquivo de senha.
.PARAMETER Horario
    Horario diario (padrao 03:00).
.EXAMPLE
    $env:APPMORADOR_DB_PASSWORD = "<senha do usuario appmorador>"
    .\scripts\agendar_backup.ps1
.EXAMPLE
    .\scripts\agendar_backup.ps1 -Remover
#>
param(
    [string]$Horario = "03:00",
    [switch]$Remover
)

$ErrorActionPreference = "Stop"
$nomeTarefa = "AppMorador - Backup diario"
$dirCred = Join-Path $env:LOCALAPPDATA "AppMorador"
$cred = Join-Path $dirCred "backup.cred"

if ($Remover) {
    Unregister-ScheduledTask -TaskName $nomeTarefa -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item $cred -ErrorAction SilentlyContinue
    Write-Host "Tarefa e senha guardada removidas."
    exit 0
}

if (-not $env:APPMORADOR_DB_PASSWORD) {
    Write-Error "Defina APPMORADOR_DB_PASSWORD nesta janela antes de agendar (a senha e guardada protegida por DPAPI)."
    exit 1
}

New-Item -ItemType Directory -Force -Path $dirCred | Out-Null
ConvertTo-SecureString $env:APPMORADOR_DB_PASSWORD -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -Path $cred -Encoding ASCII

$script = Join-Path $PSScriptRoot "backup_agendado.ps1"
$acao = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$script`""
$gatilho = New-ScheduledTaskTrigger -Daily -At $Horario
# StartWhenAvailable: se o computador estava desligado as 3h, roda assim que ligar.
$config = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 1)
Register-ScheduledTask -TaskName $nomeTarefa -Action $acao -Trigger $gatilho -Settings $config `
    -Description "Backup diario do banco do AppMorador com teste de restauracao (Sprint 27, ADR 0043)." -Force | Out-Null

Write-Host "Agendado: '$nomeTarefa' todo dia as $Horario." -ForegroundColor Green
Write-Host "Senha guardada em $cred (DPAPI, so este usuario)."
Write-Host "Para rodar agora: Start-ScheduledTask -TaskName '$nomeTarefa'"
