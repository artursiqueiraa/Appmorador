#Requires -Version 5.1
<#
.SYNOPSIS
    Nao ha Frontend Web separado neste projeto — este script documenta isso.
.DESCRIPTION
    Sprint 17.5 (Release 0.9.0). O AppMorador tem só duas superficies de
    cliente: Backend (.NET, API REST + SignalR) e Mobile (React Native/Expo).
    Nao existe um projeto de Frontend Web (SPA) separado. Este script existe
    para satisfazer o inventario padrao de scripts de um projeto full-stack
    (backup/restore/setup/clean/start_backend/start_frontend/start_mobile) sem
    fingir uma superficie que nao existe de verdade — ver docs/ARCHITECTURE.md.
    O mais proximo de "rodar no navegador" e o modo Web do Expo
    (`scripts\start_mobile.ps1 -Web`), com as limitacoes normais do Expo Web
    (alguns modulos nativos - camera, secure-store - se comportam diferente
    ou ficam indisponiveis).
.EXAMPLE
    .\scripts\start_frontend.ps1
#>
Write-Host "Este projeto NAO possui um Frontend Web separado — apenas Backend (.NET) e Mobile (Expo/React Native)." -ForegroundColor Yellow
Write-Host "Ver docs/ARCHITECTURE.md para o mapa completo dos componentes." -ForegroundColor Yellow
Write-Host "`nO mais proximo de 'rodar no navegador' e o modo Web do Expo (com limitacoes de modulos nativos):" -ForegroundColor Cyan
Write-Host "  .\scripts\start_mobile.ps1 -Web"
