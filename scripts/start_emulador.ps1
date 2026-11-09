#Requires -Version 5.1
<#
.SYNOPSIS
    Abre o app no emulador Android com recarga instantânea (sem gerar APK).
.DESCRIPTION
    Sprint 24 — usa o "development build" já instalado no emulador (compilado uma vez com
    `npx expo run:android`). Liga o emulador Pixel_7 se ele não estiver aberto e sobe o Metro na
    porta 8082 (a 8081 fica para o site). Toda mudança de tela aparece no emulador em segundos.

    Recompilar (`-Recompilar`) só é preciso quando mudar algo nativo: app.json (ícone, permissões,
    plugins, som de notificação) ou uma biblioteca nova com código nativo.

    O backend precisa estar rodando em 0.0.0.0:5027 (scripts\start_backend.ps1).

    Se uma tela nova não aparecer (o Metro no Windows às vezes não percebe arquivos recém-criados),
    feche este script (Ctrl+C) e rode de novo: o Metro reinicia e enxerga tudo.
.PARAMETER Recompilar
    Gera o app de desenvolvimento de novo e reinstala no emulador (demora alguns minutos).
.EXAMPLE
    .\scripts\start_emulador.ps1
.EXAMPLE
    .\scripts\start_emulador.ps1 -Recompilar
#>
param(
    [switch]$Recompilar
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$sdk = Join-Path $env:LOCALAPPDATA "Android\Sdk"
$adb = Join-Path $sdk "platform-tools\adb.exe"
$emulador = Join-Path $sdk "emulator\emulator.exe"

$env:ANDROID_HOME = $sdk
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
$env:Path = "$env:JAVA_HOME\bin;$sdk\platform-tools;$env:Path"

# O celular/emulador acessa o backend pelo IP da máquina na rede (localhost seria o próprio aparelho).
$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.*" } | Select-Object -First 1).IPAddress
if (-not $ip) { $ip = "10.0.2.2" }
$env:EXPO_PUBLIC_API_URL = "http://${ip}:5027"
Write-Host "API: $env:EXPO_PUBLIC_API_URL" -ForegroundColor Cyan

$conectado = (& $adb devices) -match "emulator-\d+\s+device"
if (-not $conectado) {
    Write-Host "Ligando o emulador Pixel_7..." -ForegroundColor Cyan
    Start-Process -FilePath $emulador -ArgumentList "-avd", "Pixel_7"
    & $adb wait-for-device
    do { Start-Sleep 2 } until ((& $adb shell getprop sys.boot_completed 2>$null) -match "1")
}

Push-Location (Join-Path $repoRoot "webapp")
try {
    if ($Recompilar) {
        npx expo run:android --port 8082
    }
    else {
        npx expo start --dev-client --android --port 8082
    }
}
finally {
    Pop-Location
}
